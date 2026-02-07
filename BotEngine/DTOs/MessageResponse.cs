namespace BotEngine.DTOs;

/// <summary>
/// Response del endpoint POST /messages
/// </summary>
public class MessageResponse
{
    /// <summary>
    /// Identificador de la conversación
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Respuesta del bot
    /// </summary>
    public string Reply { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si hay un flujo activo
    /// </summary>
    public bool HasActiveFlow { get; set; }
    
    /// <summary>
    /// Nombre del flujo activo (si existe)
    /// </summary>
    public string? ActiveFlow { get; set; }
}
