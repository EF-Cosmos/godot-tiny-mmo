using Game.AuthService.Data;
using Game.AuthService.Services;
using Game.Shared.Consul;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add Database Context
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication Service
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Add Consul service discovery
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddJaegerExporter()
            .AddConsoleExporter())
    .WithMetrics(metricProviderBuilder =>
        metricProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter());

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    try 
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Log error or handle it (e.g. if DB is not ready yet)
        Console.WriteLine($"Could not migrate database: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection(); // Often disabled in internal microservices behind gateway

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "Game.AuthService",
    version = "1.0.0"
}));

// Register with Consul
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var consulClient = app.Services.GetRequiredService<ConsulClient>();
var consulOptions = app.Services.GetRequiredService<ConsulServiceOptions>();

lifetime.ApplicationStarted.Register(async () =>
{
    // Update port if running on a random port in development
    if (app.Environment.IsDevelopment())
    {
        var addresses = app.Urls;
        if (addresses.Count > 0)
        {
            var address = addresses.First();
            var uri = new Uri(address);
            consulOptions.Port = uri.Port;
        }
    }
    await consulClient.RegisterServiceAsync(consulOptions);
});

lifetime.ApplicationStopping.Register(async () =>
{
    await consulClient.DeregisterServiceAsync(consulOptions.ServiceId);
});

app.Run();
