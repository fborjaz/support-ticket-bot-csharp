namespace BotEngine.Models;

/// <summary>
/// Datos recopilados durante el flujo de creación de ticket
/// </summary>
public class TicketData
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    
    public bool IsComplete => 
        !string.IsNullOrWhiteSpace(Name) && 
        !string.IsNullOrWhiteSpace(Email) && 
        !string.IsNullOrWhiteSpace(Description);
}
