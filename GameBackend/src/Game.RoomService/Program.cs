using Game.RoomService.Services;
using Game.Shared.Consul;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Consul service discovery
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

// Register GameServerManager as Singleton (to keep state in memory)
// In production, you might want to use Redis instead of memory
builder.Services.AddSingleton<IGameServerManager, GameServerManager>();

// Background service to clean up inactive servers
builder.Services.AddHostedService<ServerCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection(); // Disable HTTPS for internal microservices usually

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "Game.RoomService",
    version = "1.0.0"
}));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
