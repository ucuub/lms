using LmsApp.DTOs;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LmsApp.Services;

public class AiQuestionService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AiQuestionService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<List<GeneratedQuestionDto>> GenerateAsync(GenerateQuestionsRequest req, string materialContext = "")
    {
        var apiKey      = config["LLM:ApiKey"] ?? "";
        var model       = config["LLM:Model"] ?? "meta/llama-4-maverick-instruct";
        var baseUrl     = (config["LLM:BaseUrl"] ?? "https://dekallm.cloudeka.ai/v1").TrimEnd('/');

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("LLM API key belum dikonfigurasi.");

        var typeList  = string.Join(", ", req.Types);
        var diffLabel = req.Difficulty switch { "easy" => "mudah", "hard" => "sulit", _ => "sedang" };

        var systemPrompt = """
            Kamu adalah generator soal ujian profesional untuk sistem LMS pendidikan.
            Buat soal dalam Bahasa Indonesia yang jelas dan relevan dengan topik yang diberikan.
            PENTING: Kembalikan HANYA JSON murni tanpa markdown, tanpa ```json, tanpa teks penjelasan apapun.
            Format JSON yang harus diikuti persis:
            {"questions":[{"text":"teks soal","type":"MultipleChoice|TrueFalse|Essay","points":10,"explanation":"penjelasan singkat (kosong untuk Essay)","options":[{"text":"pilihan","isCorrect":true},{"text":"pilihan","isCorrect":false}]}]}
            Aturan:
            - MultipleChoice: 4 pilihan, tepat 1 benar, points=10
            - TrueFalse: 2 pilihan persis ("Benar" dan "Salah"), tepat 1 benar, points=5
            - Essay: options=[], explanation="", points=20
            - Semua teks dalam Bahasa Indonesia
            - Output harus dimulai dengan { dan diakhiri dengan }
            """;

        var materialSection = string.IsNullOrWhiteSpace(materialContext)
            ? ""
            : $"\n\nGunakan materi kursus berikut sebagai referensi utama dalam membuat soal:\n{materialContext}\n";

        var userPrompt = $"""
            Buat {req.Count} soal tentang topik: "{req.Topic}"
            Tingkat kesulitan: {diffLabel}
            Tipe soal yang diizinkan: {typeList}
            Distribusikan tipe soal secara merata jika lebih dari satu tipe dipilih.{materialSection}
            """;

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt   },
            },
            temperature = 0.7,
        };

        var client = httpClientFactory.CreateClient("dekallm");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var bodyJson = JsonSerializer.Serialize(requestBody);
        logger.LogInformation("[AI] Sending to {Url} | model={Model} | body_len={Len}",
            $"{baseUrl}/chat/completions", model, bodyJson.Length);

        using var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{baseUrl}/chat/completions", content);
        logger.LogInformation("[AI] Response status: {Status}", response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            logger.LogError("[AI] LLM error {Status}: {Body}", response.StatusCode, err);
            throw new Exception($"LLM error {(int)response.StatusCode}: {err}");
        }

        var raw        = await response.Content.ReadAsStringAsync();
        var completion = JsonSerializer.Deserialize<OpenAiCompletion>(raw, JsonOpts)
                         ?? throw new Exception("Respons LLM tidak valid.");

        var jsonContent = completion.Choices?.FirstOrDefault()?.Message?.Content
                         ?? throw new Exception("Tidak ada konten dalam respons LLM.");

        // Strip markdown code fences if model wraps output in ```json ... ```
        var cleaned = jsonContent.Trim();
        if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline >= 0) cleaned = cleaned[(firstNewline + 1)..];
            var lastFence = cleaned.LastIndexOf("```");
            if (lastFence >= 0) cleaned = cleaned[..lastFence];
            cleaned = cleaned.Trim();
        }

        var result = JsonSerializer.Deserialize<GeneratedQuestionsWrapper>(cleaned, JsonOpts)
                     ?? throw new Exception("Format JSON dari LLM tidak sesuai.");

        return result.Questions ?? [];
    }

    // ── Internal OpenAI response models ──────────────────────────────────────

    private record OpenAiCompletion(
        [property: JsonPropertyName("choices")] List<OpenAiChoice>? Choices
    );

    private record OpenAiChoice(
        [property: JsonPropertyName("message")] OpenAiMessage? Message
    );

    private record OpenAiMessage(
        [property: JsonPropertyName("content")] string? Content
    );

    private record GeneratedQuestionsWrapper(
        [property: JsonPropertyName("questions")] List<GeneratedQuestionDto>? Questions
    );
}
