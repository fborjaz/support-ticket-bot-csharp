namespace BotEngine.DTOs;

/// <summary>
/// Respuesta del servicio externo al consultar un ticket
/// </summary>
public class TicketStatusResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
