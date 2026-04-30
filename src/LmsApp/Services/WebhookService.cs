using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LmsApp.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IWebhookService
{
    /// <summary>
    /// Fire-and-forget: kirim event ke DWI Mobile webhook URL.
    /// Tidak melempar exception — error dicatat ke log saja.
    /// </summary>
    void Fire(string eventType, object data);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// Moodle-style outgoing webhook.
/// - Satu URL untuk semua event (discriminated by "event" field)
/// - Payload di-sign dengan HMAC-SHA256 → header X-Webhook-Signature: sha256=<hex>
/// - DWI Mobile validasi signature menggunakan shared secret
/// - Fire-and-forget: tidak block request, error hanya di-log
/// </summary>
public class WebhookService(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<WebhookService> logger) : IWebhookService
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        WriteIndented               = false,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public void Fire(string eventType, object data)
    {
        var url    = config["Webhooks:DwiMobile:Url"];
        var secret = config["Webhooks:DwiMobile:Secret"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(url))
            return; // Webhook tidak dikonfigurasi — skip tanpa error

        // Bangun payload — format Moodle-style
        var envelope = new
        {
            @event     = eventType,
            timestamp  = DateTime.UtcNow.ToString("o"), // ISO 8601
            lmsVersion = "1.0",
            data,
        };

        var json    = JsonSerializer.Serialize(envelope, _jsonOpts);
        var sig     = ComputeSignature(json, secret);

        // Fire-and-forget (Task.Run agar tidak block caller)
        _ = Task.Run(async () =>
        {
            try
            {
                using var client  = httpFactory.CreateClient();
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                client.Timeout    = TimeSpan.FromSeconds(10);

                content.Headers.Add("X-Webhook-Signature", $"sha256={sig}");
                content.Headers.Add("X-Webhook-Event",     eventType);

                var res = await client.PostAsync(url, content);
                if (!res.IsSuccessStatusCode)
                    logger.LogWarning("Webhook {Event} ke {Url} gagal: HTTP {Status}",
                        eventType, url, (int)res.StatusCode);
                else
                    logger.LogInformation("Webhook {Event} berhasil dikirim ke {Url}", eventType, url);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Webhook {Event} exception saat kirim ke {Url}", eventType, url);
            }
        });
    }

    // ── HMAC-SHA256 signature (sama seperti GitHub/Moodle webhook) ────────────
    // Receiver compute: HMAC-SHA256(secret, body) → harus sama dengan header

    private static string ComputeSignature(string body, string secret)
    {
        if (string.IsNullOrEmpty(secret)) return string.Empty;
        var key   = Encoding.UTF8.GetBytes(secret);
        var bytes = Encoding.UTF8.GetBytes(body);
        return Convert.ToHexString(HMACSHA256.HashData(key, bytes)).ToLowerInvariant();
    }
}
