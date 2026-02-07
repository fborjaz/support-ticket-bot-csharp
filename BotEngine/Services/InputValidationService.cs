using System.Text.RegularExpressions;

namespace BotEngine.Services;

// Validaciones de entrada para el bot
public interface IInputValidationService
{
    (bool IsValid, string? Error, string SanitizedValue) ValidateConversationId(string? conversationId);
    (bool IsValid, string? Error, string SanitizedValue) ValidateMessage(string? message);
    (bool IsValid, string? Error) ValidateName(string name);
    (bool IsValid, string? Error) ValidateEmail(string email);
    (bool IsValid, string? Error) ValidateDescription(string description);
}

public partial class InputValidationService : IInputValidationService
{
    // regex email
    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex StrictEmailRegex();
    
    // solo letras, espacios y eso
    [GeneratedRegex(@"^[\p{L}\s\-'\.]+$", RegexOptions.IgnoreCase)]
    private static partial Regex NameRegex();
    
    // para evitar XSS basico
    [GeneratedRegex(@"<script|javascript:|on\w+\s*=|<\s*iframe|<\s*object", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousPatternRegex();
    
    [GeneratedRegex(@"^[a-zA-Z0-9\-_]+$")]
    private static partial Regex ConversationIdRegex();

    public (bool IsValid, string? Error, string SanitizedValue) ValidateConversationId(string? conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return (false, "El conversationId es requerido.", string.Empty);
        }
        
        var trimmed = conversationId.Trim();
        
        if (trimmed.Length > BotEngine.Models.ValidationConstants.MaxConversationIdLength)
        {
            return (false, $"El conversationId no puede exceder {BotEngine.Models.ValidationConstants.MaxConversationIdLength} caracteres.", string.Empty);
        }
        
        if (!ConversationIdRegex().IsMatch(trimmed))
        {
            return (false, "El conversationId contiene caracteres inválidos. Solo se permiten letras, números, guiones y guiones bajos.", string.Empty);
        }
        
        return (true, null, trimmed);
    }

    public (bool IsValid, string? Error, string SanitizedValue) ValidateMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, "El mensaje no puede estar vacío.", string.Empty);
        }
        
        var trimmed = message.Trim();
        
        if (trimmed.Length > BotEngine.Models.ValidationConstants.MaxMessageLength)
        {
            return (false, $"El mensaje no puede exceder {BotEngine.Models.ValidationConstants.MaxMessageLength} caracteres.", string.Empty);
        }
        
        // filtro basico anti xss
        if (DangerousPatternRegex().IsMatch(trimmed))
        {
            return (false, "El mensaje contiene contenido no permitido.", string.Empty);
        }
        
        // quitar chars raros pero dejar enters
        var sanitized = new string(trimmed.Where(c => !char.IsControl(c) || c == '\n' || c == '\r').ToArray());
        
        return (true, null, sanitized);
    }

    public (bool IsValid, string? Error) ValidateName(string name)
    {
        var trimmed = name.Trim();
        
        if (trimmed.Length < BotEngine.Models.ValidationConstants.MinNameLength)
        {
            return (false, $"El nombre debe tener al menos {BotEngine.Models.ValidationConstants.MinNameLength} caracteres.");
        }
        
        if (trimmed.Length > BotEngine.Models.ValidationConstants.MaxNameLength)
        {
            return (false, $"El nombre no puede exceder {BotEngine.Models.ValidationConstants.MaxNameLength} caracteres.");
        }
        
        if (!NameRegex().IsMatch(trimmed))
        {
            return (false, "El nombre solo puede contener letras, espacios, guiones y apóstrofes.");
        }
        
        // que no pongan "aaaaaaa" o algo asi
        if (trimmed.Distinct().Count() < 2)
        {
            return (false, "Por favor, ingresa un nombre válido.");
        }
        
        // detectar si parece un comando en vez de nombre
        var lower = trimmed.ToLowerInvariant();
        var comandos = new[] { "ticket", "crear", "estado", "cancelar", "consultar", "ver", "nuevo", "abrir" };
        if (comandos.Any(c => lower.Contains(c)))
        {
            return (false, "Eso parece un comando, no un nombre. Por favor, ingresa tu nombre real.");
        }
        
        return (true, null);
    }

    public (bool IsValid, string? Error) ValidateEmail(string email)
    {
        var trimmed = email.Trim().ToLowerInvariant();
        
        if (trimmed.Length > BotEngine.Models.ValidationConstants.MaxEmailLength)
        {
            return (false, $"El email no puede exceder {BotEngine.Models.ValidationConstants.MaxEmailLength} caracteres.");
        }
        
        if (!StrictEmailRegex().IsMatch(trimmed))
        {
            return (false, "El formato del correo electrónico no es válido.");
        }
        
        // typos comunes que la gente pone
        var commonTypos = new Dictionary<string, string>
        {
            { "@gmial.com", "@gmail.com" },
            { "@gmai.com", "@gmail.com" },
            { "@gamil.com", "@gmail.com" },
            { "@hotmal.com", "@hotmail.com" },
            { "@outloo.com", "@outlook.com" },
            { "@yahooo.com", "@yahoo.com" }
            // TODO: agregar mas si aparecen
        };
        
        foreach (var typo in commonTypos)
        {
            if (trimmed.EndsWith(typo.Key))
            {
                return (false, $"¿Quisiste decir {trimmed.Replace(typo.Key, typo.Value)}? Verifica tu correo.");
            }
        }
        
        return (true, null);
    }

    public (bool IsValid, string? Error) ValidateDescription(string description)
    {
        var trimmed = description.Trim();
        
        if (trimmed.Length < BotEngine.Models.ValidationConstants.MinDescriptionLength)
        {
            return (false, $"La descripción debe tener al menos {BotEngine.Models.ValidationConstants.MinDescriptionLength} caracteres.");
        }
        
        if (trimmed.Length > BotEngine.Models.ValidationConstants.MaxDescriptionLength)
        {
            return (false, $"La descripción no puede exceder {BotEngine.Models.ValidationConstants.MaxDescriptionLength} caracteres.");
        }
        
        // minimo algo de texto real
        var letterCount = trimmed.Count(char.IsLetter);
        if (letterCount < 5)
        {
            return (false, "Por favor, proporciona una descripción con contenido más detallado.");
        }
        
        if (DangerousPatternRegex().IsMatch(trimmed))
        {
            return (false, "La descripción contiene contenido no permitido.");
        }
        
        return (true, null);
    }
}
