using Microsoft.AspNetCore.Mvc;
using MockServices.Models;

namespace MockServices.Controllers;

[ApiController]
[Route("tickets")]
public class TicketsController : ControllerBase
{
    // Almacén en memoria de tickets
    private static readonly Dictionary<string, Ticket> _tickets = new();
    private static int _ticketCounter = 0;

    [HttpPost]
    public IActionResult CreateTicket([FromBody] CreateTicketRequest request)
    {
        // Validar autorización
        if (!OAuthController.ValidateToken(Request.Headers.Authorization))
        {
            return Unauthorized(new { error = "invalid_token", message = "Token inválido o expirado" });
        }

        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "validation_error", message = "El nombre es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "validation_error", message = "El email es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "validation_error", message = "La descripción es requerida" });
        }

        // Crear ticket
        var ticketId = $"TCK-{++_ticketCounter:D3}";
        var ticket = new Ticket
        {
            Id = ticketId,
            Name = request.Name,
            Email = request.Email,
            Description = request.Description,
            Status = "Abierto",
            CreatedAt = DateTime.UtcNow
        };

        _tickets[ticketId] = ticket;

        return Created($"/tickets/{ticketId}", new
        {
            id = ticket.Id,
            message = "Ticket creado exitosamente"
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetTicket(string id)
    {
        // Validar autorización
        if (!OAuthController.ValidateToken(Request.Headers.Authorization))
        {
            return Unauthorized(new { error = "invalid_token", message = "Token inválido o expirado" });
        }

        // Buscar ticket
        if (!_tickets.TryGetValue(id.ToUpper(), out var ticket))
        {
            return NotFound(new { error = "not_found", message = $"Ticket {id} no encontrado" });
        }

        return Ok(new
        {
            id = ticket.Id,
            name = ticket.Name,
            email = ticket.Email,
            description = ticket.Description,
            status = ticket.Status,
            createdAt = ticket.CreatedAt
        });
    }
}
