using MassTransit;
using Auction.Shared;
using Auction.NotificationService.Services;
using Auction.NotificationService.Models;
using Auction.NotificationService.Consumers;
using Auction.NotificationService.Repo;
using Microsoft.EntityFrameworkCore;
using Auction.NotificationService.IRepo;

var builder = WebApplication.CreateBuilder(args);

var rabbitHost = builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq";
var connectionString = builder.Configuration.GetConnectionString("NotificationDb") ?? builder.Configuration["NOTIFICATION_DB"] ?? "Data Source=/data/notifications.db";

// Services
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<NotificationManager>();

// EF Core for notifications
builder.Services.AddDbContext<NotificationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IEventRepository, EfEventRepository>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<HighestBidUpdatedConsumer>();
    x.AddConsumer<AuctionEndedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("notification-highestbid-updated", e =>
        {
            e.ConfigureConsumer<HighestBidUpdatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("notification-auction-ended", e =>
        {
            e.ConfigureConsumer<AuctionEndedConsumer>(context);
        });
    });
});

var app = builder.Build();

// Ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Subscribe for callbacks
app.MapPost("/subscribe", async (SubscribeRequest req, NotificationManager manager, IEventRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.CallbackUrl)) return Results.BadRequest();
    manager.AddSubscriber(req.CallbackUrl);
    return Results.Accepted();
});

app.MapGet("/subscribers", (NotificationManager manager) => Results.Ok(manager.GetSubscribers()));

// Query events for auction
app.MapGet("/auctions/{auctionId:guid}/events", async (Guid auctionId, IEventRepository repo) =>
{
    var events = await repo.GetByAuctionAsync(auctionId);
    if (events == null) return Results.NotFound();
    return Results.Ok(events);
});

// For demo: endpoint to end auction and publish AuctionEnded
app.MapPost("/auctions/{auctionId:guid}/end", async (Guid auctionId, EndAuctionRequest req, IPublishEndpoint publisher) =>
{
    var ended = new AuctionEnded(auctionId, req.WinnerId, req.BidId, req.Amount, DateTimeOffset.UtcNow);
    await publisher.Publish(ended);
    return Results.Accepted();
});

app.Run();
