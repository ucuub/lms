namespace LmsApp.DTOs;

// ── Request ───────────────────────────────────────────────────────────────────

public record GenerateQuestionsRequest(
    string Topic,
    int Count,
    List<string> Types,    // "MultipleChoice", "TrueFalse", "Essay"
    string Difficulty,     // "easy", "medium", "hard"
    int? CourseId = null   // opsional — jika diisi, AI akan menggunakan materi kursus sebagai konteks
);

// ── Response ──────────────────────────────────────────────────────────────────

public record GeneratedQuestionDto(
    string Text,
    string Type,
    int Points,
    string? Explanation,
    List<GeneratedOptionDto> Options
);

public record GeneratedOptionDto(string Text, bool IsCorrect);
