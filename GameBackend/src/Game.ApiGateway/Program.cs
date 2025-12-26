using Game.ApiGateway.Middleware;
using Game.ApiGateway.Services;
using Game.Shared.Consul;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using Consul;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot
builder.Configuration.AddJsonFile("OcelotConfiguration/ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddConsul();

// Add services to the container.
builder.Services.AddControllers();

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

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("GamePolicy",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:4100")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
});

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Add Swagger/OpenAPI (temporarily disabled due to compatibility issues)
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// Add HTTP Client for service communication
builder.Services.AddHttpClient();

// Add services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Add Consul service discovery
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger temporarily disabled due to compatibility issues
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("GamePolicy");

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Add global error handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Add rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// Simple health endpoint (bypasses Ocelot, used by Consul)
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        await context.Response.WriteAsJsonAsync(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Game.ApiGateway",
            version = "1.0.0"
        });
        return;
    }
    await next();
});

await app.UseOcelot();

app.Run();
