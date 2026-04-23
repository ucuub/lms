using DocumentFormat.OpenXml.Packaging;
using LmsApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace LmsApp.Services;

public class MaterialContextService(LmsDbContext db, IWebHostEnvironment env, ILogger<MaterialContextService> logger)
{
    private const int MaxTotalChars = 6000; // batas konteks agar tidak melebihi token limit LLM

    /// <summary>
    /// Mengambil teks materi dari kursus: judul, deskripsi, konten modul, dan isi file (.txt/.docx).
    /// Mengembalikan string kosong jika courseId null atau tidak ditemukan.
    /// </summary>
    public async Task<string> ExtractAsync(int? courseId)
    {
        if (courseId is null) return string.Empty;

        var course = await db.Courses
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Title, c.Description })
            .FirstOrDefaultAsync();

        if (course is null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"=== MATERI KURSUS: {course.Title} ===");
        if (!string.IsNullOrWhiteSpace(course.Description))
            sb.AppendLine(StripHtml(course.Description));
        sb.AppendLine();

        // ── Module content ────────────────────────────────────────────────────
        var modules = await db.CourseModules
            .Where(m => m.CourseId == courseId && m.IsPublished)
            .OrderBy(m => m.Order)
            .Include(m => m.Attachments)
            .ToListAsync();

        foreach (var mod in modules)
        {
            if (sb.Length >= MaxTotalChars) break;

            sb.AppendLine($"--- Modul: {mod.Title} ---");

            if (!string.IsNullOrWhiteSpace(mod.Content))
                sb.AppendLine(Truncate(StripHtml(mod.Content), 800));

            // File attachment pada modul
            foreach (var att in mod.Attachments)
            {
                if (sb.Length >= MaxTotalChars) break;
                var fileText = await ReadFileTextAsync(att.FileUrl, att.FileName);
                if (!string.IsNullOrWhiteSpace(fileText))
                {
                    sb.AppendLine($"[File: {att.FileName}]");
                    sb.AppendLine(Truncate(fileText, 600));
                }
            }
        }

        // ── Course-level resources ────────────────────────────────────────────
        var resources = await db.CourseResources
            .Where(r => r.CourseId == courseId && r.IsVisible)
            .OrderBy(r => r.Order)
            .ToListAsync();

        foreach (var res in resources)
        {
            if (sb.Length >= MaxTotalChars) break;

            // Selalu sertakan judul & deskripsi
            sb.AppendLine($"[Sumber Daya: {res.Title}]");
            if (!string.IsNullOrWhiteSpace(res.Description))
                sb.AppendLine(res.Description);

            // Coba baca isi file jika tipe yang didukung
            var fileText = await ReadFileTextAsync(res.FileUrl, res.FileName);
            if (!string.IsNullOrWhiteSpace(fileText))
                sb.AppendLine(Truncate(fileText, 600));
        }

        var result = sb.ToString();
        if (result.Length > MaxTotalChars)
            result = result[..MaxTotalChars] + "\n[... materi dipotong ...]";

        logger.LogInformation("[AI] Material context extracted for courseId={CourseId}: {Chars} chars", courseId, result.Length);
        return result;
    }

    // ── File reading ──────────────────────────────────────────────────────────

    private async Task<string> ReadFileTextAsync(string fileUrl, string fileName)
    {
        try
        {
            // fileUrl disimpan sebagai "/uploads/resources/xxx.pdf"
            var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(env.WebRootPath, relativePath);

            if (!File.Exists(fullPath)) return string.Empty;

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".txt"  => await ReadTxtAsync(fullPath),
                ".docx" => ReadDocx(fullPath),
                _       => string.Empty  // pdf, ppt, video, image → skip isi file, pakai judul saja
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning("[AI] Gagal baca file {File}: {Msg}", fileName, ex.Message);
            return string.Empty;
        }
    }

    private static async Task<string> ReadTxtAsync(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        return text;
    }

    private static string ReadDocx(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            var text = para.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine(text);
        }
        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string StripHtml(string html) =>
        Regex.Replace(html, "<[^>]+>", " ").Trim();

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";
}
