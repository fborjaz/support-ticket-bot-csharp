namespace BotEngine.Models;

// estado de la conversacion del usuario
public class ConversationContext
{
    public string ConversationId { get; set; } = string.Empty;
    
    public ConversationFlow ActiveFlow { get; set; } = ConversationFlow.None;
    
    public CreateTicketStep? CurrentStep { get; set; }
    
    // datos del ticket que se va llenando
    public TicketData TicketData { get; set; } = new();
    
    // cuantas errores tuvo el usuario en este paso
    public int FailedAttempts { get; set; } = 0;
    
    public const int MaxFailedAttempts = ValidationConstants.MaxFailedAttempts;
    
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    // timeout de 30 min
    public bool IsSessionExpired => 
        DateTime.UtcNow - LastActivity > TimeSpan.FromMinutes(ValidationConstants.SessionTimeoutMinutes);
    
    public void Reset()
    {
        ActiveFlow = ConversationFlow.None;
        CurrentStep = null;
        TicketData = new TicketData();
        FailedAttempts = 0;
        LastActivity = DateTime.UtcNow;
    }
    
    public void ResetAttempts()
    {
        FailedAttempts = 0;
    }
}
