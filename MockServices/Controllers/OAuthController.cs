using Microsoft.AspNetCore.Mvc;
using MockServices.Models;

namespace MockServices.Controllers;

[ApiController]
[Route("oauth")]
public class OAuthController : ControllerBase
{
    // Credenciales válidas para el mock
    private const string ValidClientId = "bot-client";
    private const string ValidClientSecret = "bot-secret";
    
    // Token válido que generamos (en producción sería JWT real)
    private static readonly string CurrentToken = Guid.NewGuid().ToString("N");
    private static DateTime _tokenExpiry = DateTime.UtcNow.AddMinutes(30);

    [HttpPost("token")]
    public IActionResult GetToken([FromForm] TokenRequest request)
    {
        // Validar grant_type
        if (request.GrantType != "client_credentials")
        {
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        // Validar credenciales
        if (request.ClientId != ValidClientId || request.ClientSecret != ValidClientSecret)
        {
            return Unauthorized(new { error = "invalid_client" });
        }

        // Generar token con expiración
        _tokenExpiry = DateTime.UtcNow.AddMinutes(30);
        
        return Ok(new TokenResponse
        {
            AccessToken = CurrentToken,
            TokenType = "Bearer",
            ExpiresIn = 1800 // 30 minutos en segundos
        });
    }

    // Método helper para validar tokens (usado internamente)
    public static bool ValidateToken(string? authHeader)
    {
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return false;
        }

        var token = authHeader["Bearer ".Length..];
        return token == CurrentToken && DateTime.UtcNow < _tokenExpiry;
    }
}
