using BotEngine.DTOs;
using BotEngine.Models;

namespace BotEngine.Services;

public interface IExternalTicketService
{
    Task<CreateTicketResponse?> CreateTicketAsync(TicketData ticketData);
    Task<TicketStatusResponse?> GetTicketStatusAsync(string ticketId);
}
