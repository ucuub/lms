using System.Security.Claims;
using System.Text.Json;

namespace LmsApp.Services;

/// <summary>
/// Mengambil role LMS dari Keycloak JWT claims.
///
/// Keycloak meletakkan roles di dua tempat:
///   1. resource_access.{clientId}.roles  — role spesifik untuk client ini (prioritas utama)
///   2. realm_access.roles                — role realm-wide (fallback)
///
/// Priority role: admin > teacher > student
/// Jika user punya "teacher" dan "admin" sekaligus → diambil "admin"
/// </summary>
public static class KeycloakRoleExtractor
{
    // Role LMS yang dikenali, diurutkan dari prioritas tertinggi ke terendah
    private static readonly string[] LmsRoles = ["admin", "teacher", "student"];

    /// <summary>
    /// Ekstrak role LMS dari ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">ClaimsPrincipal dari JWT yang sudah divalidasi</param>
    /// <param name="clientId">Keycloak client ID untuk resource_access (mis. "lms-app")</param>
    /// <returns>Role LMS lowercase, atau null jika tidak ditemukan</returns>
    public static string? Extract(ClaimsPrincipal principal, string clientId)
    {
        // 1. Cek resource_access.{clientId}.roles — paling spesifik untuk LMS
        var resourceAccess = principal.FindFirst("resource_access")?.Value;
        if (!string.IsNullOrEmpty(resourceAccess))
        {
            var role = ExtractFromResourceAccess(resourceAccess, clientId);
            if (role != null) return role;
        }

        // 2. Fallback ke realm_access.roles — role realm-wide
        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrEmpty(realmAccess))
        {
            var role = ExtractFromRealmAccess(realmAccess);
            if (role != null) return role;
        }

        return null;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string? ExtractFromResourceAccess(string json, string clientId)
    {
        // Struktur: { "lms-app": { "roles": ["admin", "offline_access"] } }
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(clientId, out var clientEl)) return null;
            if (!clientEl.TryGetProperty("roles", out var rolesEl)) return null;
            return HighestLmsRole(rolesEl);
        }
        catch (JsonException) { return null; }
    }

    private static string? ExtractFromRealmAccess(string json)
    {
        // Struktur: { "roles": ["teacher", "offline_access", "default-roles-lms"] }
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("roles", out var rolesEl)) return null;
            return HighestLmsRole(rolesEl);
        }
        catch (JsonException) { return null; }
    }

    /// <summary>
    /// Dari array role Keycloak, ambil role LMS dengan prioritas tertinggi.
    /// Role non-LMS (offline_access, uma_authorization, dll.) diabaikan.
    /// </summary>
    private static string? HighestLmsRole(JsonElement rolesArray)
    {
        if (rolesArray.ValueKind != JsonValueKind.Array) return null;

        var userRoles = rolesArray
            .EnumerateArray()
            .Select(r => r.GetString()?.ToLowerInvariant())
            .Where(r => r != null)
            .ToHashSet();

        // LmsRoles sudah diurutkan priority: admin > teacher > student
        return LmsRoles.FirstOrDefault(r => userRoles.Contains(r));
    }
}
