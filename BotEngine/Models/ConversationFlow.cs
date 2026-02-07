namespace BotEngine.Models;

/// <summary>
/// Representa los posibles flujos de conversación del bot
/// </summary>
public enum ConversationFlow
{
    None,
    CreateTicket,
    CheckTicketStatus
}

/// <summary>
/// Representa los pasos dentro del flujo de creación de ticket
/// </summary>
public enum CreateTicketStep
{
    AskingName,
    AskingEmail,
    AskingDescription,
    ShowingSummary,
    AwaitingConfirmation
}
