namespace BotEngine.DTOs;

/// <summary>
/// Respuesta OAuth del servicio externo
/// </summary>
public class OAuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
