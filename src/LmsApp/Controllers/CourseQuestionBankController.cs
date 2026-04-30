using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/question-bank")]
[Authorize]
public class CourseQuestionBankController(LmsDbContext db) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value
        ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
        ?? string.Empty;

    private string UserName => User.FindFirst("preferred_username")?.Value
        ?? User.FindFirst("name")?.Value
        ?? string.Empty;

    private string UserRole => User.FindFirst("role")?.Value
        ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value
        ?? string.Empty;

    // ── Authorization helper ──────────────────────────────────────────────────

    /// <summary>
    /// Teacher yang merupakan instructor kursus ini, atau admin.
    /// </summary>
    private async Task<(bool ok, IActionResult? err)> AuthorizeManageAsync(int courseId)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return (false, NotFound(new { message = "Kursus tidak ditemukan." }));

        var isAdmin  = UserRole == "admin";
        var isOwner  = UserRole == "teacher" && course.InstructorId == UserId;
        if (!isAdmin && !isOwner)
            return (false, Forbid());

        return (true, null);
    }

    // ── GET /api/courses/{courseId}/question-bank ─────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(int courseId, [FromQuery] int? moduleId)
    {
        var (ok, err) = await AuthorizeManageAsync(courseId);
        if (!ok) return err!;

        var query = db.CourseQuestionBanks
            .Include(q => q.Options)
            .Include(q => q.Module)
            .Where(q => q.CourseId == courseId);

        if (moduleId.HasValue)
            query = query.Where(q => q.ModuleId == moduleId.Value);

        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(ToResponse));
    }

    // ── GET /api/courses/{courseId}/question-bank/{id} ────────────────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int courseId, int id)
    {
        var (ok, err) = await AuthorizeManageAsync(courseId);
        if (!ok) return err!;

        var item = await db.CourseQuestionBanks
            .Include(q => q.Options)
            .Include(q => q.Module)
            .FirstOrDefaultAsync(q => q.Id == id && q.CourseId == courseId);

        if (item == null) return NotFound(new { message = "Soal tidak ditemukan." });
        return Ok(ToResponse(item));
    }

    // ── POST /api/courses/{courseId}/question-bank ────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create(int courseId, SaveCourseQuestionRequest req)
    {
        var (ok, err) = await AuthorizeManageAsync(courseId);
        if (!ok) return err!;

        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(new { message = "Teks soal tidak boleh kosong." });

        if (req.Points < 1)
            return BadRequest(new { message = "Poin harus minimal 1." });

        if (req.Type == QuestionType.MultipleChoice && req.Options.Count < 2)
            return BadRequest(new { message = "Soal pilihan ganda harus memiliki minimal 2 pilihan." });

        if (req.Type == QuestionType.MultipleChoice && !req.Options.Any(o => o.IsCorrect))
            return BadRequest(new { message = "Harus ada minimal satu pilihan yang benar." });

        // Validate moduleId belongs to this course
        if (req.ModuleId.HasValue)
        {
            var moduleExists = await db.CourseModules
                .AnyAsync(m => m.Id == req.ModuleId.Value && m.CourseId == courseId);
            if (!moduleExists)
                return BadRequest(new { message = "Modul tidak ditemukan dalam kursus ini." });
        }

        var question = new CourseQuestionBank
        {
            CourseId    = courseId,
            ModuleId    = req.ModuleId,
            Text        = req.Text.Trim(),
            Type        = req.Type,
            Points      = req.Points,
            Explanation = req.Explanation?.Trim(),
            CreatedBy   = UserId,
            CreatedByName = UserName,
            CreatedAt   = DateTime.UtcNow,
            Options     = req.Options.Select(o => new CourseQuestionBankOption
            {
                Text      = o.Text.Trim(),
                IsCorrect = o.IsCorrect
            }).ToList()
        };

        db.CourseQuestionBanks.Add(question);
        await db.SaveChangesAsync();

        var result = await db.CourseQuestionBanks
            .Include(q => q.Options)
            .Include(q => q.Module)
            .FirstAsync(q => q.Id == question.Id);

        return CreatedAtAction(nameof(GetById),
            new { courseId, id = question.Id },
            ToResponse(result));
    }

    // ── PUT /api/courses/{courseId}/question-bank/{id} ────────────────────────

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int courseId, int id, SaveCourseQuestionRequest req)
    {
        var (ok, err) = await AuthorizeManageAsync(courseId);
        if (!ok) return err!;

        var question = await db.CourseQuestionBanks
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id && q.CourseId == courseId);

        if (question == null) return NotFound(new { message = "Soal tidak ditemukan." });

        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(new { message = "Teks soal tidak boleh kosong." });

        if (req.Points < 1)
            return BadRequest(new { message = "Poin harus minimal 1." });

        if (req.Type == QuestionType.MultipleChoice && req.Options.Count < 2)
            return BadRequest(new { message = "Soal pilihan ganda harus memiliki minimal 2 pilihan." });

        if (req.Type == QuestionType.MultipleChoice && !req.Options.Any(o => o.IsCorrect))
            return BadRequest(new { message = "Harus ada minimal satu pilihan yang benar." });

        if (req.ModuleId.HasValue)
        {
            var moduleExists = await db.CourseModules
                .AnyAsync(m => m.Id == req.ModuleId.Value && m.CourseId == courseId);
            if (!moduleExists)
                return BadRequest(new { message = "Modul tidak ditemukan dalam kursus ini." });
        }

        question.ModuleId    = req.ModuleId;
        question.Text        = req.Text.Trim();
        question.Type        = req.Type;
        question.Points      = req.Points;
        question.Explanation = req.Explanation?.Trim();

        // Replace options
        db.CourseQuestionBankOptions.RemoveRange(question.Options);
        question.Options = req.Options.Select(o => new CourseQuestionBankOption
        {
            Text      = o.Text.Trim(),
            IsCorrect = o.IsCorrect
        }).ToList();

        await db.SaveChangesAsync();

        var result = await db.CourseQuestionBanks
            .Include(q => q.Options)
            .Include(q => q.Module)
            .FirstAsync(q => q.Id == id);

        return Ok(ToResponse(result));
    }

    // ── POST /api/courses/{courseId}/question-bank/import-csv ────────────────────
    // Format CSV:
    //   Teks Soal,Tipe,Poin,Opsi A,Opsi B,Opsi C,Opsi D,Jawaban Benar,Penjelasan
    //   Tipe: PG | BS | Uraian
    //   Jawaban Benar: A/B/C/D (untuk PG), A (Benar) / B (Salah) untuk BS

    [HttpPost("import-csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportCsv(int courseId, IFormFile file, [FromQuery] int? moduleId)
    {
        var (ok, err) = await AuthorizeManageAsync(courseId);
        if (!ok) return err!;

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File CSV diperlukan." });
        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Hanya file .csv yang didukung." });

        if (moduleId.HasValue)
        {
            var moduleExists = await db.CourseModules
                .AnyAsync(m => m.Id == moduleId.Value && m.CourseId == courseId);
            if (!moduleExists)
                return BadRequest(new { message = "Modul tidak ditemukan." });
        }

        var imported = new List<CourseQuestionBank>();
        var errors   = new List<string>();
        int row = 1;

        using var reader = new System.IO.StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8);
        string? headerLine = await reader.ReadLineAsync(); // skip header
        if (headerLine == null) return BadRequest(new { message = "File CSV kosong." });

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            row++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = ParseCsvLine(line);
            if (cols.Length < 3)
            {
                errors.Add($"Baris {row}: kolom tidak cukup.");
                continue;
            }

            var text        = cols[0].Trim();
            var tipeRaw     = cols[1].Trim().ToUpperInvariant();
            var pointsRaw   = cols[2].Trim();
            var opsiA       = cols.Length > 3 ? cols[3].Trim() : "";
            var opsiB       = cols.Length > 4 ? cols[4].Trim() : "";
            var opsiC       = cols.Length > 5 ? cols[5].Trim() : "";
            var opsiD       = cols.Length > 6 ? cols[6].Trim() : "";
            var jawabanRaw  = cols.Length > 7 ? cols[7].Trim().ToUpperInvariant() : "";
            var penjelasan  = cols.Length > 8 ? cols[8].Trim() : "";

            if (string.IsNullOrEmpty(text)) { errors.Add($"Baris {row}: teks soal kosong."); continue; }
            if (!int.TryParse(pointsRaw, out var points) || points < 1) points = 10;

            QuestionType qType;
            if      (tipeRaw == "PG")     qType = QuestionType.MultipleChoice;
            else if (tipeRaw == "BS")     qType = QuestionType.TrueFalse;
            else if (tipeRaw == "URAIAN") qType = QuestionType.Essay;
            else { errors.Add($"Baris {row}: tipe '{cols[1]}' tidak valid (PG/BS/Uraian)."); continue; }

            var options = new List<CourseQuestionBankOption>();

            if (qType == QuestionType.MultipleChoice)
            {
                var rawOptions = new[] { ("A", opsiA), ("B", opsiB), ("C", opsiC), ("D", opsiD) }
                    .Where(o => !string.IsNullOrEmpty(o.Item2)).ToList();

                if (rawOptions.Count < 2) { errors.Add($"Baris {row}: minimal 2 opsi untuk PG."); continue; }

                var correctLetters = jawabanRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(x => x.Trim()).ToHashSet();
                if (correctLetters.Count == 0) { errors.Add($"Baris {row}: jawaban benar kosong."); continue; }

                options = rawOptions.Select(o => new CourseQuestionBankOption
                {
                    Text      = o.Item2,
                    IsCorrect = correctLetters.Contains(o.Item1)
                }).ToList();

                if (!options.Any(o => o.IsCorrect)) { errors.Add($"Baris {row}: tidak ada jawaban yang benar."); continue; }
            }
            else if (qType == QuestionType.TrueFalse)
            {
                var benarIsCorrect = jawabanRaw == "A";
                options =
                [
                    new CourseQuestionBankOption { Text = "Benar", IsCorrect = benarIsCorrect },
                    new CourseQuestionBankOption { Text = "Salah", IsCorrect = !benarIsCorrect },
                ];
            }

            imported.Add(new CourseQuestionBank
            {
                CourseId      = courseId,
                ModuleId      = moduleId,
                Text          = text,
                Type          = qType,
                Points        = points,
                Explanation   = string.IsNullOrEmpty(penjelasan) ? null : penjelasan,
                CreatedBy     = UserId,
                CreatedByName = UserName,
                CreatedAt     = DateTime.UtcNow,
                Options       = options
            });
        }

        if (imported.Count == 0)
            return BadRequest(new { message = "Tidak ada soal valid yang bisa diimpor.", errors });

        db.CourseQuestionBanks.AddRange(imported);
        await db.SaveChangesAsync();

        return Ok(new
        {
            imported = imported.Count,
            errors,
            message = $"{imported.Count} soal berhasil diimpor." + (errors.Count > 0 ? $" {errors.Count} baris dilewati." : "")
        });
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    // ── DELETE /api/courses/{courseId}/question-bank/{id} ─────────────────────

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int courseId, int id)
    {
        var (ok, err) = await AuthorizeManageAsync(courseId);
        if (!ok) return err!;

        var question = await db.CourseQuestionBanks
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id && q.CourseId == courseId);

        if (question == null) return NotFound(new { message = "Soal tidak ditemukan." });

        db.CourseQuestionBankOptions.RemoveRange(question.Options);
        db.CourseQuestionBanks.Remove(question);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static CourseQuestionBankResponse ToResponse(CourseQuestionBank q) => new(
        q.Id,
        q.CourseId,
        q.ModuleId,
        q.Module?.Title,
        q.Text,
        q.Type.ToString(),
        q.Points,
        q.Explanation,
        q.CreatedBy,
        q.CreatedByName,
        q.CreatedAt,
        q.Options.Select(o => new CourseOptionResponse(o.Id, o.Text, o.IsCorrect)).ToList()
    );
}
