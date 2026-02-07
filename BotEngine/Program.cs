using System.Diagnostics;
using BotEngine.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS para el chat HTML
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// servicios del bot
builder.Services.AddSingleton<IConversationStateService, ConversationStateService>();
builder.Services.AddSingleton<IInputValidationService, InputValidationService>();
builder.Services.AddScoped<IConversationFlowService, ConversationFlowService>();

// HttpClient para el servicio de tickets
builder.Services.AddHttpClient<IExternalTicketService, ExternalTicketService>(client =>
{
    var baseUrl = builder.Configuration["ExternalServices:BaseUrl"] ?? "http://localhost:5121";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/index.html"));

// abrir navegador automaticamente
app.Lifetime.ApplicationStarted.Register(() =>
{
    var url = "http://localhost:5020";
    try
    {
        // linux
        if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", url);
        // mac
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", url);
        // windows
        else if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch { /* si falla no pasa nada */ }
});

app.Run();
