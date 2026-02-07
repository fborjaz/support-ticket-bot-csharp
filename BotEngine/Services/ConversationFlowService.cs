using System.Text.RegularExpressions;
using BotEngine.Models;

namespace BotEngine.Services;

// Logica del bot conversacional
public partial class ConversationFlowService : IConversationFlowService
{
    private readonly IConversationStateService _stateService;
    private readonly IExternalTicketService _ticketService;
    private readonly IInputValidationService _validationService;
    private readonly ILogger<ConversationFlowService> _logger;

    // para sacar el id del ticket del mensaje
    [GeneratedRegex(@"TCK-\d+", RegexOptions.IgnoreCase)]
    private static partial Regex TicketIdRegex();

    public ConversationFlowService(
        IConversationStateService stateService,
        IExternalTicketService ticketService,
        IInputValidationService validationService,
        ILogger<ConversationFlowService> logger)
    {
        _stateService = stateService;
        _ticketService = ticketService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<string> ProcessMessageAsync(string conversationId, string message)
    {
        var context = _stateService.GetOrCreateContext(conversationId);
        
        // sesion timeout?
        if (context.IsSessionExpired && context.ActiveFlow != ConversationFlow.None)
        {
            _logger.LogInformation("Sesión expirada para {ConversationId}", conversationId);
            _stateService.ClearContext(conversationId);
            context = _stateService.GetOrCreateContext(conversationId);
            
            return "⏰ Tu sesión anterior ha expirado por inactividad.\n\n" + GetHelpMessage();
        }
        var normalizedMessage = message.Trim().ToLowerInvariant();

        _logger.LogInformation(
            "Procesando mensaje para {ConversationId}. Flujo actual: {Flow}, Paso: {Step}",
            conversationId, context.ActiveFlow, context.CurrentStep);

        // cancelar?
        if (normalizedMessage == "cancelar")
        {
            return HandleCancel(context);
        }

        // si ya hay un flujo activo seguimos ahi
        if (context.ActiveFlow != ConversationFlow.None)
        {
            return await HandleActiveFlowAsync(context, message, normalizedMessage);
        }

        // Detectar intención del usuario
        return await DetectIntentAndRespond(context, message, normalizedMessage);
    }

    private string HandleCancel(ConversationContext context)
    {
        if (context.ActiveFlow == ConversationFlow.None)
        {
            return "No hay ningún proceso activo que cancelar. ¿En qué puedo ayudarte?";
        }

        var flowName = context.ActiveFlow == ConversationFlow.CreateTicket 
            ? "creación de ticket" 
            : "consulta";
            
        _stateService.ClearContext(context.ConversationId);
        
        return $"He cancelado el proceso de {flowName}. Todos los datos han sido descartados. ¿En qué más puedo ayudarte?";
    }

    // detectar que quiere hacer el usuario
    private async Task<string> DetectIntentAndRespond(
        ConversationContext context, 
        string originalMessage,
        string normalizedMessage)
    {
        // quiere crear ticket?
        if (IsCreateTicketIntent(normalizedMessage))
        {
            return StartCreateTicketFlow(context);
        }

        // quiere ver estado?
        if (IsCheckStatusIntent(normalizedMessage))
        {
            var ticketId = ExtractTicketId(originalMessage);
            if (ticketId != null)
            {
                return await GetTicketStatusAsync(ticketId);
            }
            
            return "Por favor, indica el ID del ticket que deseas consultar. Ejemplo: 'ver estado del ticket TCK-001'";
        }

        // no entendimos, mostrar ayuda
        return GetHelpMessage();
    }

    private async Task<string> HandleActiveFlowAsync(
        ConversationContext context,
        string originalMessage,
        string normalizedMessage)
    {
        return context.ActiveFlow switch
        {
            ConversationFlow.CreateTicket => await HandleCreateTicketFlowAsync(context, originalMessage, normalizedMessage),
            _ => GetHelpMessage()
        };
    }

    #region Flujo de Creación de Ticket

    private string StartCreateTicketFlow(ConversationContext context)
    {
        context.ActiveFlow = ConversationFlow.CreateTicket;
        context.CurrentStep = CreateTicketStep.AskingName;
        context.TicketData = new TicketData();
        _stateService.UpdateContext(context);

        return "🎫 **CREAR NUEVO TICKET**\n" +
               "══════════════════════════\n\n" +
               "📊 Progreso: [▓░░] 1/3\n\n" +
               "👤 ¿Cuál es tu **nombre**?";
    }

    private async Task<string> HandleCreateTicketFlowAsync(
        ConversationContext context,
        string originalMessage,
        string normalizedMessage)
    {
        return context.CurrentStep switch
        {
            CreateTicketStep.AskingName => HandleNameInput(context, originalMessage),
            CreateTicketStep.AskingEmail => HandleEmailInput(context, originalMessage),
            CreateTicketStep.AskingDescription => HandleDescriptionInput(context, originalMessage),
            CreateTicketStep.AwaitingConfirmation => await HandleConfirmationAsync(context, normalizedMessage),
            _ => GetHelpMessage()
        };
    }

    private string HandleNameInput(ConversationContext context, string name)
    {
        var (isValid, error) = _validationService.ValidateName(name);
        
        if (!isValid)
        {
            context.FailedAttempts++;
            _stateService.UpdateContext(context);
            
            if (context.FailedAttempts >= ConversationContext.MaxFailedAttempts)
            {
                _stateService.ClearContext(context.ConversationId);
                return "❌ **Demasiados intentos fallidos**\n\n" +
                       "Has ingresado un nombre inválido demasiadas veces.\n" +
                       "Por favor, intenta crear el ticket nuevamente.";
            }
            
            var remainingAttempts = ConversationContext.MaxFailedAttempts - context.FailedAttempts;
            return $"❌ {error}\n\n⚠️ Intentos restantes: {remainingAttempts}";
        }

        context.ResetAttempts();
        context.TicketData.Name = name.Trim();
        context.CurrentStep = CreateTicketStep.AskingEmail;
        _stateService.UpdateContext(context);

        return $"✅ ¡Hola, **{context.TicketData.Name}**!\n\n" +
               "══════════════════════════\n" +
               "📊 Progreso: [▓▓░] 2/3\n\n" +
               "📧 ¿Cuál es tu **correo electrónico**?";
    }

    private string HandleEmailInput(ConversationContext context, string email)
    {
        var (isValid, error) = _validationService.ValidateEmail(email);
        
        if (!isValid)
        {
            context.FailedAttempts++;
            _stateService.UpdateContext(context);
            
            // Verificar si excedió el máximo de intentos
            if (context.FailedAttempts >= ConversationContext.MaxFailedAttempts)
            {
                _stateService.ClearContext(context.ConversationId);
                return "❌ **Demasiados intentos fallidos**\n\n" +
                       "Has ingresado un formato de correo incorrecto demasiadas veces.\n" +
                       "Por favor, verifica bien tu correo electrónico e intenta crear el ticket nuevamente.\n\n" +
                       "💡 **Tip:** El formato correcto es: usuario@dominio.com";
            }
            
            var remainingAttempts = ConversationContext.MaxFailedAttempts - context.FailedAttempts;
            return $"❌ {error}\n\n" +
                   $"Por favor, ingresa un correo válido (ejemplo: usuario@dominio.com)\n" +
                   $"⚠️ Intentos restantes: {remainingAttempts}";
        }

        context.ResetAttempts();
        context.TicketData.Email = email.Trim().ToLowerInvariant();
        context.CurrentStep = CreateTicketStep.AskingDescription;
        _stateService.UpdateContext(context);

        return "✅ Email registrado\n\n" +
               "══════════════════════════\n" +
               "📊 Progreso: [▓▓▓] 3/3\n\n" +
               "📝 **Describe tu problema** o consulta:\n" +
               "_(mínimo 10 caracteres)_";
    }

    private string HandleDescriptionInput(ConversationContext context, string description)
    {
        var (isValid, error) = _validationService.ValidateDescription(description);
        
        if (!isValid)
        {
            context.FailedAttempts++;
            _stateService.UpdateContext(context);
            
            if (context.FailedAttempts >= ConversationContext.MaxFailedAttempts)
            {
                _stateService.ClearContext(context.ConversationId);
                return "❌ **Demasiados intentos fallidos**\n\n" +
                       "Has ingresado una descripción inválida demasiadas veces.\n" +
                       "Por favor, intenta crear el ticket nuevamente con una descripción más detallada.";
            }
            
            var remainingAttempts = ConversationContext.MaxFailedAttempts - context.FailedAttempts;
            return $"❌ {error}\n\n⚠️ Intentos restantes: {remainingAttempts}";
        }

        context.ResetAttempts();
        context.TicketData.Description = description.Trim();
        context.CurrentStep = CreateTicketStep.AwaitingConfirmation;
        _stateService.UpdateContext(context);

        return GetTicketSummary(context) + 
               "\n\n¿Confirmas la creación del ticket? (responde **sí** o **no**)";
    }

    private string GetTicketSummary(ConversationContext context)
    {
        return "\n📋 **RESUMEN DEL TICKET**\n" +
               "╔══════════════════════════════╗\n" +
               $"║ 👤 {context.TicketData.Name}\n" +
               $"║ 📧 {context.TicketData.Email}\n" +
               "╠══════════════════════════════╣\n" +
               $"║ 📝 {context.TicketData.Description}\n" +
               "╚══════════════════════════════╝";
    }

    private async Task<string> HandleConfirmationAsync(ConversationContext context, string response)
    {
        if (IsAffirmative(response))
        {
            return await CreateTicketAsync(context);
        }
        
        if (IsNegative(response))
        {
            _stateService.ClearContext(context.ConversationId);
            return "🚫 **Ticket cancelado**\n\n" +
                   "Los datos han sido descartados.\n\n" +
                   "💬 ¿En qué más puedo ayudarte?";
        }

        // no entendio si o no
        context.FailedAttempts++;
        _stateService.UpdateContext(context);
        
        if (context.FailedAttempts >= ConversationContext.MaxFailedAttempts)
        {
            _stateService.ClearContext(context.ConversationId);
            return "❌ **Demasiados intentos fallidos**\n\n" +
                   "No pudimos entender tu respuesta. El proceso ha sido cancelado.\n" +
                   "Por favor, intenta crear el ticket nuevamente.";
        }
        
        var remainingAttempts = ConversationContext.MaxFailedAttempts - context.FailedAttempts;
        return $"No entendí tu respuesta.\n\n" +
               $"Por favor, responde **sí** para confirmar o **no** para cancelar.\n" +
               $"⚠️ Intentos restantes: {remainingAttempts}";
    }

    private async Task<string> CreateTicketAsync(ConversationContext context)
    {
        try
        {
            var result = await _ticketService.CreateTicketAsync(context.TicketData);

            if (result == null)
            {
                context.FailedAttempts++;
                _stateService.UpdateContext(context);
                
                if (context.FailedAttempts >= ConversationContext.MaxFailedAttempts)
                {
                    _stateService.ClearContext(context.ConversationId);
                    return "❌ **Error persistente**\n\n" +
                           "No pudimos crear el ticket después de varios intentos.\n" +
                           "Por favor, intenta más tarde.";
                }
                
                return "❌ Hubo un error al crear el ticket.\n\n" +
                       "¿Deseas intentar nuevamente? (responde **sí** o **no**)";
            }

            _stateService.ClearContext(context.ConversationId);

            return "🎉 **¡TICKET CREADO EXITOSAMENTE!**\n" +
                   "══════════════════════════════\n\n" +
                   $"🎫 **Tu ID de Ticket:**\n\n" +
                   $"    🔹 `{result.Id}`\n\n" +
                   "💾 Guarda este ID para consultar el estado.\n\n" +
                   "══════════════════════════════\n" +
                   "💬 ¿Hay algo más en lo que pueda ayudarte?";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear ticket");
            
            context.FailedAttempts++;
            _stateService.UpdateContext(context);
            
            if (context.FailedAttempts >= ConversationContext.MaxFailedAttempts)
            {
                _stateService.ClearContext(context.ConversationId);
                return "❌ **Error de comunicación persistente**\n\n" +
                       "No pudimos conectar con el servicio de tickets.\n" +
                       "Por favor, intenta más tarde o contacta soporte técnico.";
            }
            
            var remainingAttempts = ConversationContext.MaxFailedAttempts - context.FailedAttempts;
            return $"❌ Ocurrió un error al comunicarse con el servicio.\n\n" +
                   $"¿Deseas intentar nuevamente? (responde **sí** o **no**)\n" +
                   $"⚠️ Intentos restantes: {remainingAttempts}";
        }
    }

    #endregion

    // -- Consulta de tickets --

    private async Task<string> GetTicketStatusAsync(string ticketId)
    {
        try
        {
            var ticket = await _ticketService.GetTicketStatusAsync(ticketId);

            if (ticket == null)
            {
                return $"❌ No se encontró ningún ticket con el ID **{ticketId}**.\n\n" +
                       "💡 Verifica que el ID sea correcto (formato: `TCK-XXX`).";
            }

            var statusEmoji = ticket.Status?.ToLower() switch
            {
                "open" or "abierto" => "🟢",
                "in progress" or "en progreso" => "🟡",
                "closed" or "cerrado" => "🔴",
                "pending" or "pendiente" => "🟠",
                _ => "⚪"
            };

            return $"🔍 **ESTADO DEL TICKET**\n" +
                   "╔══════════════════════════════╗\n" +
                   $"║ 🎫 ID: **{ticket.Id}**\n" +
                   $"║ {statusEmoji} Estado: **{ticket.Status}**\n" +
                   "╠══════════════════════════════╣\n" +
                   $"║ 👤 {ticket.Name}\n" +
                   $"║ 📧 {ticket.Email}\n" +
                   $"║ 📝 {ticket.Description}\n" +
                   "╠══════════════════════════════╣\n" +
                   $"║ 📅 Creado: {ticket.CreatedAt:dd/MM/yyyy HH:mm}\n" +
                   "╚══════════════════════════════╝\n\n" +
                   "💬 ¿Hay algo más en lo que pueda ayudarte?";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar ticket {TicketId}", ticketId);
            return "❌ Ocurrió un error al consultar el ticket. Por favor, intenta nuevamente.";
        }
    }

    // -- Helpers --

    private static bool IsCreateTicketIntent(string message)
    {
        // palabras clave por separado para ser mas flexible
        var hasTicket = message.Contains("ticket");
        var hasAction = message.Contains("crear") || message.Contains("nuevo") || 
                        message.Contains("abrir") || message.Contains("quiero") ||
                        message.Contains("necesito") || message.Contains("generar");
        
        // si tiene "ticket" + alguna accion, es crear ticket
        if (hasTicket && hasAction) return true;
        
        // frases exactas por si acaso
        var keywords = new[] { "crear ticket", "nuevo ticket", "abrir ticket" };
        return keywords.Any(k => message.Contains(k));
    }

    private static bool IsCheckStatusIntent(string message)
    {
        var keywords = new[] { "estado", "consultar", "ver ticket", "buscar ticket", "status" };
        return keywords.Any(k => message.Contains(k));
    }

    private static string? ExtractTicketId(string message)
    {
        var match = TicketIdRegex().Match(message);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static bool IsAffirmative(string response)
    {
        var affirmatives = new[] { "sí", "si", "yes", "confirmar", "confirmo", "correcto", "afirmativo", "dale", "claro", "por supuesto" };
        return affirmatives.Any(a => response.Equals(a, StringComparison.OrdinalIgnoreCase) || response.Contains(a));
    }

    private static bool IsNegative(string response)
    {
        var negatives = new[] { "no", "cancelar", "rechazar", "negar" };
        return negatives.Any(n => response.Contains(n));
    }

    private static string GetHelpMessage()
    {
        return "🤖 **BOT DE SOPORTE**\n" +
               "══════════════════════════\n\n" +
               "¿En qué puedo ayudarte hoy?\n\n" +
               "🎫 **Crear ticket**\n" +
               "   → _\"Quiero crear un ticket\"_\n\n" +
               "🔍 **Ver estado de ticket**\n" +
               "   → _\"Ver estado del ticket TCK-001\"_\n\n" +
               "══════════════════════════\n" +
               "💡 Escribe **cancelar** en cualquier momento\n" +
               "   para interrumpir el proceso.";
    }
}
