using BotEngine.DTOs;
using BotEngine.Models;
using BotEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace BotEngine.Controllers;

[ApiController]
[Route("[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IConversationFlowService _flowService;
    private readonly IConversationStateService _stateService;
    private readonly IInputValidationService _validationService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IConversationFlowService flowService,
        IConversationStateService stateService,
        IInputValidationService validationService,
        ILogger<MessagesController> logger)
    {
        _flowService = flowService;
        _stateService = stateService;
        _validationService = validationService;
        _logger = logger;
    }


    [HttpPost]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponse>> ProcessMessage([FromBody] MessageRequest request)
    {
        // validar input
        var (isValidConvId, convIdError, sanitizedConvId) = _validationService.ValidateConversationId(request.ConversationId);
        if (!isValidConvId)
        {
            _logger.LogWarning("ConversationId inválido: {Error}", convIdError);
            return BadRequest(new { error = convIdError });
        }
        
        var (isValidMessage, messageError, sanitizedMessage) = _validationService.ValidateMessage(request.Message);
        if (!isValidMessage)
        {
            return Ok(new MessageResponse
            {
                ConversationId = sanitizedConvId,
                Reply = $"⚠️ {messageError}",
                HasActiveFlow = false
            });
        }

        _logger.LogInformation(
            "Mensaje recibido - ConversationId: {ConversationId}, Longitud: {Length}",
            sanitizedConvId,
            sanitizedMessage.Length);

        try
        {
            var reply = await _flowService.ProcessMessageAsync(sanitizedConvId, sanitizedMessage);

            var context = _stateService.GetOrCreateContext(sanitizedConvId);

            var response = new MessageResponse
            {
                ConversationId = sanitizedConvId,
                Reply = reply,
                HasActiveFlow = context.ActiveFlow != ConversationFlow.None,
                ActiveFlow = context.ActiveFlow != ConversationFlow.None 
                    ? context.ActiveFlow.ToString() 
                    : null
            };

            _logger.LogInformation(
                "Respuesta enviada - ConversationId: {ConversationId}, HasActiveFlow: {HasActiveFlow}",
                response.ConversationId,
                response.HasActiveFlow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando mensaje para {ConversationId}", sanitizedConvId);
            
            return Ok(new MessageResponse
            {
                ConversationId = sanitizedConvId,
                Reply = "Lo siento, ha ocurrido un error interno. Por favor, intenta nuevamente.",
                HasActiveFlow = false
            });
        }
    }
}
