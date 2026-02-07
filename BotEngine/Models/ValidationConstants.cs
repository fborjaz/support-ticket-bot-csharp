namespace BotEngine.Models;

public static class ValidationConstants
{
    // limites de longitud
    public const int MinNameLength = 2;
    public const int MaxNameLength = 100;
    
    public const int MaxEmailLength = 254; // rfc dice 254 max
    
    public const int MinDescriptionLength = 10;
    public const int MaxDescriptionLength = 1000;
    
    public const int MaxMessageLength = 2000;
    public const int MaxConversationIdLength = 100;
    
    // inactividad
    public const int SessionTimeoutMinutes = 30;
    
    // intentos antes de cancelar
    public const int MaxFailedAttempts = 5;
}
