namespace BotEngine.DTOs;

/// <summary>
/// Respuesta del servicio externo al crear un ticket
/// </summary>
public class CreateTicketResponse
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
