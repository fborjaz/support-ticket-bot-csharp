using BotEngine.Models;

namespace BotEngine.Services;

public interface IConversationStateService
{
    ConversationContext GetOrCreateContext(string conversationId);
    void UpdateContext(ConversationContext context);
    void ClearContext(string conversationId);
}
