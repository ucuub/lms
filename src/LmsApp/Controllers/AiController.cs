using LmsApp.DTOs;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = "admin")]
public class AiController(AiQuestionService aiService, MaterialContextService materialCtx, IConfiguration config, ILogger<AiController> logger) : ControllerBase
{
    // GET /api/ai/status — cek apakah API key sudah dikonfigurasi
    [HttpGet("status")]
    public IActionResult Status()
    {
        var key = config["LLM:ApiKey"] ?? "";
        return Ok(new
        {
            configured   = !string.IsNullOrWhiteSpace(key),
            model        = config["LLM:Model"] ?? "meta/llama-3.3-70b-instruct",
            providerName = config["LLM:ProviderName"] ?? "DekaLLM",
        });
    }

    // POST /api/ai/generate-questions
    [HttpPost("generate-questions")]
    public async Task<ActionResult<List<GeneratedQuestionDto>>> Generate(GenerateQuestionsRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Topic))
            return BadRequest(new { message = "Topik tidak boleh kosong." });

        if (req.Count < 1 || req.Count > 20)
            return BadRequest(new { message = "Jumlah soal harus antara 1 dan 20." });

        var validTypes = new[] { "MultipleChoice", "TrueFalse", "Essay" };
        if (req.Types == null || req.Types.Count == 0 || req.Types.Any(t => !validTypes.Contains(t)))
            return BadRequest(new { message = "Tipe soal tidak valid." });

        if (!new[] { "easy", "medium", "hard" }.Contains(req.Difficulty))
            return BadRequest(new { message = "Tingkat kesulitan tidak valid." });

        try
        {
            var context   = await materialCtx.ExtractAsync(req.CourseId);
            var questions = await aiService.GenerateAsync(req, context);
            return Ok(questions);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? "-";
            var inner2 = ex.InnerException?.InnerException?.Message ?? "-";
            logger.LogError("[AI] Error: {Msg} | Inner: {Inner} | Inner2: {Inner2}", ex.Message, inner, inner2);
            return StatusCode(500, new { message = $"Gagal generate soal: {ex.Message} | {inner} | {inner2}" });
        }
    }
}
