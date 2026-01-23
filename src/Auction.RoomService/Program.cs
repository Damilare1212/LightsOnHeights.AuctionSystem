using MassTransit;
using Auction.Shared;
using Auction.RoomService.Services;
using Auction.RoomService.Models;

var builder = WebApplication.CreateBuilder(args);

var rabbitHost = builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq";

// Add services
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<RoomManager>();

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
app.UseAuthorization();
app.MapControllers();

app.Run();
