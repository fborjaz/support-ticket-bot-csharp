using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BotEngine.DTOs;
using BotEngine.Models;

namespace BotEngine.Services;

// cliente para el API de tickets (con OAuth)
public class ExternalTicketService : IExternalTicketService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalTicketService> _logger;
    private readonly IConfiguration _configuration;
    
    // cache del token oauth
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExternalTicketService(
        HttpClient httpClient,
        ILogger<ExternalTicketService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<CreateTicketResponse?> CreateTicketAsync(TicketData ticketData)
    {
        return await ExecuteWithTokenAsync(async () =>
        {
            var payload = new
            {
                name = ticketData.Name,
                email = ticketData.Email,
                description = ticketData.Description
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/tickets", content);
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Token expirado");
            }

            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CreateTicketResponse>(json, JsonOptions);
        });
    }

    public async Task<TicketStatusResponse?> GetTicketStatusAsync(string ticketId)
    {
        return await ExecuteWithTokenAsync(async () =>
        {
            var response = await _httpClient.GetAsync($"/tickets/{ticketId}");
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Token expirado");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TicketStatusResponse>(json, JsonOptions);
        });
    }

    // ejecuta con token, si da 401 renueva y reintenta
    private async Task<T?> ExecuteWithTokenAsync<T>(Func<Task<T?>> operation)
    {
        await EnsureValidTokenAsync();
        
        try
        {
            return await operation();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Token inválido, renovando...");
            await InvalidateAndRefreshTokenAsync();
            return await operation();
        }
    }

    private async Task EnsureValidTokenAsync()
    {
        // si ya tenemos token valido, usarlo
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _cachedToken);
            return;
        }

        await InvalidateAndRefreshTokenAsync();
    }

    private async Task InvalidateAndRefreshTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            // double check despues del lock
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _cachedToken);
                return;
            }

            _logger.LogInformation("Obteniendo nuevo token OAuth...");

            var clientId = _configuration["ExternalServices:ClientId"] ?? "bot-client";
            var clientSecret = _configuration["ExternalServices:ClientSecret"] ?? "bot-secret";

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            var response = await _httpClient.PostAsync("/oauth/token", tokenRequest);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(json, JsonOptions);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("No se pudo obtener el token OAuth");
            }

            // guardar token (le resto 5 min por seguridad)
            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300);

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _cachedToken);

            _logger.LogInformation("Token OAuth obtenido exitosamente");
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
