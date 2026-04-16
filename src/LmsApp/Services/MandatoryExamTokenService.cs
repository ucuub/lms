using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LmsApp.Services;

/// <summary>
/// Generates and validates short-lived HMAC-SHA256 signed tokens for mandatory exam deep links.
/// These tokens are separate from Keycloak JWT — they carry { sub, examId, exp } only.
/// </summary>
public class MandatoryExamTokenService(IConfiguration config)
{
    private const string Issuer = "lms-mandatory-exam";

    private string SigningKey => config["MandatoryExam:SigningKey"]
        ?? throw new InvalidOperationException("MandatoryExam:SigningKey is not configured.");

    /// <summary>Returns (Token, ExpiresAt, Jti)</summary>
    public (string Token, DateTime ExpiresAt, string Jti) GenerateToken(string userId, int examId, int expiryMinutes = 60)
    {
        var key   = BuildKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var exp   = DateTime.UtcNow.AddMinutes(Math.Clamp(expiryMinutes, 10, 1440));
        var jti   = Guid.NewGuid().ToString();

        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("examId", examId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
            ],
            expires: exp,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(jwt), exp, jti);
    }

    /// <summary>
    /// Validates the token and returns (UserId, ExamId, Jti).
    /// Throws SecurityTokenException / SecurityTokenExpiredException on failure.
    /// </summary>
    public (string UserId, int ExamId, string Jti) ValidateToken(string token)
    {
        var handler   = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer   = true,
            ValidIssuer      = Issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.FromSeconds(30),
            IssuerSigningKey = BuildKey(),
        }, out _);

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? throw new SecurityTokenException("Token tidak mengandung sub claim.");

        var examIdStr = principal.FindFirst("examId")?.Value
                     ?? throw new SecurityTokenException("Token tidak mengandung examId claim.");

        if (!int.TryParse(examIdStr, out var examId))
            throw new SecurityTokenException("examId tidak valid.");

        var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
               ?? throw new SecurityTokenException("Token tidak mengandung jti claim.");

        return (userId, examId, jti);
    }

    private SymmetricSecurityKey BuildKey()
        => new(Encoding.UTF8.GetBytes(SigningKey));
}
