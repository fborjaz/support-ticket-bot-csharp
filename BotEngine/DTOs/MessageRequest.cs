using System.ComponentModel.DataAnnotations;

namespace BotEngine.DTOs;

/// <summary>
/// Request para el endpoint POST /messages
/// </summary>
public class MessageRequest
{
    /// <summary>
    /// Identificador único de la conversación
    /// </summary>
    [Required(ErrorMessage = "El conversationId es requerido")]
    public string ConversationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Mensaje del usuario
    /// </summary>
    [Required(ErrorMessage = "El mensaje es requerido")]
    public string Message { get; set; } = string.Empty;
}
