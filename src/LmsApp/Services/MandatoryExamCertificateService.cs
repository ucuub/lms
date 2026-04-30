using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LmsApp.Data;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IMandatoryExamCertificateService
{
    Task<MandatoryExamCertificateTemplate> UploadTemplateAsync(int examId, IFormFile file, string uploadedBy);
    Task<MandatoryExamCertificateTemplate?> GetTemplateInfoAsync(int examId);
    Task DeleteTemplateAsync(int examId);
    Task<MandatoryExamCertificate?> GenerateAsync(MandatoryExam exam, MandatoryExamAttempt attempt, int scorePercentage);
    Task<(byte[] Data, string FileName)?> DownloadAsync(string certNumber, string userId);
    Task<List<MandatoryExamCertificate>> GetIssuedAsync(int examId);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class MandatoryExamCertificateService(
    LmsDbContext db,
    IWebHostEnvironment env,
    ILogger<MandatoryExamCertificateService> logger) : IMandatoryExamCertificateService
{
    // ── Paths ─────────────────────────────────────────────────────────────────

    private string TemplateDir  => Path.Combine(env.WebRootPath, "uploads", "mandatory-exam-templates");
    private string CertDir(int examId) => Path.Combine(env.WebRootPath, "uploads", "mandatory-exam-certificates", examId.ToString());

    // ── Upload Template ───────────────────────────────────────────────────────

    public async Task<MandatoryExamCertificateTemplate> UploadTemplateAsync(
        int examId, IFormFile file, string uploadedBy)
    {
        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Hanya file .docx yang diizinkan.");

        Directory.CreateDirectory(TemplateDir);

        var storedPath = Path.Combine(TemplateDir, $"exam-{examId}.docx");

        await using (var fs = new FileStream(storedPath, FileMode.Create))
            await file.CopyToAsync(fs);

        // Upsert — satu template per exam
        var existing = await db.MandatoryExamCertificateTemplates
            .FirstOrDefaultAsync(t => t.ExamId == examId);

        if (existing != null)
        {
            existing.FileName   = file.FileName;
            existing.StoredPath = storedPath;
            existing.UploadedBy = uploadedBy;
            existing.UploadedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new MandatoryExamCertificateTemplate
            {
                ExamId     = examId,
                FileName   = file.FileName,
                StoredPath = storedPath,
                UploadedBy = uploadedBy,
            };
            db.MandatoryExamCertificateTemplates.Add(existing);
        }

        await db.SaveChangesAsync();
        return existing;
    }

    // ── Template Info ─────────────────────────────────────────────────────────

    public Task<MandatoryExamCertificateTemplate?> GetTemplateInfoAsync(int examId) =>
        db.MandatoryExamCertificateTemplates.FirstOrDefaultAsync(t => t.ExamId == examId);

    // ── Delete Template ───────────────────────────────────────────────────────

    public async Task DeleteTemplateAsync(int examId)
    {
        var template = await db.MandatoryExamCertificateTemplates
            .FirstOrDefaultAsync(t => t.ExamId == examId);
        if (template == null) return;

        if (File.Exists(template.StoredPath))
            File.Delete(template.StoredPath);

        db.MandatoryExamCertificateTemplates.Remove(template);
        await db.SaveChangesAsync();
    }

    // ── Generate Certificate ──────────────────────────────────────────────────

    public async Task<MandatoryExamCertificate?> GenerateAsync(
        MandatoryExam exam, MandatoryExamAttempt attempt, int scorePercentage)
    {
        // Idempotency: jangan generate ulang jika sudah ada
        var existing = await db.MandatoryExamCertificates
            .FirstOrDefaultAsync(c => c.AttemptId == attempt.Id);
        if (existing != null) return existing;

        var template = await db.MandatoryExamCertificateTemplates
            .FirstOrDefaultAsync(t => t.ExamId == exam.Id);

        // Tidak ada template — skip tanpa error (opsional per ujian)
        if (template == null || !File.Exists(template.StoredPath))
            return null;

        var certNumber = GenerateCertNumber(exam.Id);
        var certDir    = CertDir(exam.Id);
        Directory.CreateDirectory(certDir);

        var outputPath = Path.Combine(certDir, $"{certNumber}.docx");

        var replacements = new Dictionary<string, string>
        {
            ["{{NAMA_PESERTA}}"]     = attempt.Assignment?.UserName ?? attempt.UserId,
            ["{{JUDUL_UJIAN}}"]      = exam.Title,
            ["{{TANGGAL_LULUS}}"]    = DateTime.UtcNow.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID")),
            ["{{NOMOR_SERTIFIKAT}}"] = certNumber,
            ["{{NILAI}}"]            = $"{scorePercentage}%",
        };

        try
        {
            File.Copy(template.StoredPath, outputPath, overwrite: true);
            FillTemplate(outputPath, replacements);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gagal generate sertifikat untuk attempt {AttemptId}", attempt.Id);
            if (File.Exists(outputPath)) File.Delete(outputPath);
            return null;
        }

        var cert = new MandatoryExamCertificate
        {
            ExamId            = exam.Id,
            AttemptId         = attempt.Id,
            UserId            = attempt.UserId,
            UserName          = attempt.Assignment?.UserName ?? attempt.UserId,
            CertificateNumber = certNumber,
            StoredPath        = outputPath,
            ScorePercentage   = scorePercentage,
        };
        db.MandatoryExamCertificates.Add(cert);
        await db.SaveChangesAsync();

        return cert;
    }

    // ── Download ──────────────────────────────────────────────────────────────

    public async Task<(byte[] Data, string FileName)?> DownloadAsync(string certNumber, string userId)
    {
        var cert = await db.MandatoryExamCertificates
            .Include(c => c.Exam)
            .FirstOrDefaultAsync(c => c.CertificateNumber == certNumber);

        if (cert == null) return null;

        // Validasi kepemilikan
        if (cert.UserId != userId) return null;

        if (!File.Exists(cert.StoredPath)) return null;

        var data     = await File.ReadAllBytesAsync(cert.StoredPath);
        var fileName = $"Sertifikat_{cert.Exam?.Title?.Replace(" ", "_") ?? "Ujian"}_{certNumber}.docx";
        return (data, fileName);
    }

    // ── Issued Certificates ───────────────────────────────────────────────────

    public Task<List<MandatoryExamCertificate>> GetIssuedAsync(int examId) =>
        db.MandatoryExamCertificates
            .Where(c => c.ExamId == examId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

    // ── DOCX Placeholder Replacement ─────────────────────────────────────────

    private static void FillTemplate(string path, Dictionary<string, string> replacements)
    {
        using var doc = WordprocessingDocument.Open(path, isEditable: true);
        var mainPart  = doc.MainDocumentPart!;

        ReplaceInElement(mainPart.Document.Body!, replacements);

        foreach (var hp in mainPart.HeaderParts)
            ReplaceInElement(hp.Header, replacements);
        foreach (var fp in mainPart.FooterParts)
            ReplaceInElement(fp.Footer, replacements);

        mainPart.Document.Save();
    }

    /// <summary>
    /// Menggabungkan semua run dalam satu paragraf menjadi satu teks,
    /// lalu mengganti placeholder. Menangani kasus di mana Word memecah
    /// placeholder menjadi beberapa run yang terpisah.
    /// </summary>
    private static void ReplaceInElement(
        DocumentFormat.OpenXml.OpenXmlElement root,
        Dictionary<string, string> replacements)
    {
        foreach (var para in root.Descendants<Paragraph>())
        {
            var runs = para.Descendants<Run>().ToList();
            if (runs.Count == 0) continue;

            var fullText = string.Concat(
                runs.SelectMany(r => r.Elements<Text>()).Select(t => t.Text));

            if (!fullText.Contains("{{")) continue;

            var newText = replacements.Aggregate(fullText,
                (current, kv) => current.Replace(kv.Key, kv.Value));

            if (newText == fullText) continue;

            // Simpan formatting run pertama, hapus semua run, rebuild satu run
            var firstRun = runs[0];
            var rProps   = firstRun.GetFirstChild<RunProperties>()?.CloneNode(true);

            foreach (var run in runs) run.Remove();

            var newRun = new Run();
            if (rProps != null) newRun.AppendChild(rProps);
            newRun.AppendChild(new Text(newText)
            {
                Space = SpaceProcessingModeValues.Preserve
            });
            para.AppendChild(newRun);
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static string GenerateCertNumber(int examId)
    {
        var date   = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Random.Shared.Next(100000, 999999);
        return $"ME-{examId:D4}-{date}-{random}";
    }
}
