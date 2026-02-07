namespace BotEngine.Services;

public interface IConversationFlowService
{
    Task<string> ProcessMessageAsync(string conversationId, string message);
}
