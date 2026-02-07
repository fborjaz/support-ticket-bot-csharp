using System.Collections.Concurrent;
using BotEngine.Models;

namespace BotEngine.Services;

// Estado de las conversaciones (en memoria)
public class ConversationStateService : IConversationStateService
{
    private readonly ConcurrentDictionary<string, ConversationContext> _contexts = new();
    private readonly ILogger<ConversationStateService> _logger;

    public ConversationStateService(ILogger<ConversationStateService> logger)
    {
        _logger = logger;
    }

    public ConversationContext GetOrCreateContext(string conversationId)
    {
        return _contexts.GetOrAdd(conversationId, id =>
        {
            _logger.LogInformation("Creando nuevo contexto para conversación: {ConversationId}", id);
            return new ConversationContext { ConversationId = id };
        });
    }

    public void UpdateContext(ConversationContext context)
    {
        context.LastActivity = DateTime.UtcNow;
        _contexts[context.ConversationId] = context;
        _logger.LogDebug("Contexto actualizado para conversación: {ConversationId}", context.ConversationId);
    }

    public void ClearContext(string conversationId)
    {
        if (_contexts.TryGetValue(conversationId, out var context))
        {
            context.Reset();
            _logger.LogInformation("Contexto limpiado para conversación: {ConversationId}", conversationId);
        }
    }
}
