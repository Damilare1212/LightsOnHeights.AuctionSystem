using MassTransit;
using Auction.Shared;
using Auction.RoomService.Services;
using Auction.RoomService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HealthChecks.RabbitMQ;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var rabbitHost = builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq";

// Add services
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<RoomManager>();

// JWT Authentication
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "SuperSecretKeyForDevelopmentOnly_12345!";
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Bidder", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Auctioneer", policy => 
        policy.RequireAuthenticatedUser()
              .RequireClaim("role", "Auctioneer"));
    options.AddPolicy("Admin", policy => 
        policy.RequireAuthenticatedUser()
              .RequireClaim("role", "Admin"));
});

// Health checks
builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitHost, name: "rabbitmq", timeout: TimeSpan.FromSeconds(3));

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Name != "memory" });

app.MapControllers();

app.Run();
