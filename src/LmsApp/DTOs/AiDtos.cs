using System.Text.Json;
using System.Text.Json.Serialization;

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
    [property: JsonConverter(typeof(FlexibleIntConverter))] int Points,
    string? Explanation,
    List<GeneratedOptionDto> Options
);

public record GeneratedOptionDto(string Text, bool IsCorrect);

// Handles LLM returning points as either number (10) or string ("10")
public class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (int.TryParse(s, out var parsed)) return parsed;
            throw new JsonException($"Cannot convert \"{s}\" to int.");
        }
        return reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
