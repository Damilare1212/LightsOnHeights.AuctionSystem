using MassTransit;
using Auction.Shared;
using Auction.BiddingService.Services;
using Auction.BiddingService.Models;
using Auction.BiddingService.Repo;
using Microsoft.EntityFrameworkCore;
using Auction.BiddingService.IRepo;

var builder = WebApplication.CreateBuilder(args);

// RabbitMQ host configurable via env var
var rabbitHost = builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq";
var connectionString = builder.Configuration.GetConnectionString("BiddingDb") ?? builder.Configuration["BIDDING_DB"] ?? "Data Source=/data/bidding.db";

var runMode = (builder.Configuration["RUN_MODE"] ?? "").ToLowerInvariant();
var disableMessaging = (builder.Configuration["DISABLE_MESSAGING"] ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
var disablePersistence = (builder.Configuration["DISABLE_PERSISTENCE"] ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);

// If explicit swagger mode requested, build minimal host quickly
if (runMode == "swagger")
{
    builder.Services.AddOpenApi();
    builder.Services.AddControllers();
    builder.Services.AddSingleton<AuctionManager>();
    builder.Services.AddSingleton<IBidRepository, InMemoryBidRepository>();

    var appMinimal = builder.Build();
    if (appMinimal.Environment.IsDevelopment()) appMinimal.MapOpenApi();
    appMinimal.UseHttpsRedirection();
    appMinimal.UseAuthorization();
    appMinimal.MapControllers();
    appMinimal.Run();
    return;
}

// Regular startup
builder.Services.AddOpenApi();
builder.Services.AddControllers();

if (!disablePersistence)
{
    // EF Core for bidding persistence
    builder.Services.AddDbContext<BiddingDbContext>(options => options.UseSqlite(connectionString));
    builder.Services.AddScoped<IBidRepository, EfBidRepository>();
}
else
{
    builder.Services.AddSingleton<IBidRepository, InMemoryBidRepository>();
}

// In-memory auction state manager (will use persisted bids from repo as needed)
builder.Services.AddSingleton<AuctionManager>();

if (!disableMessaging)
{
    // MassTransit with RabbitMQ
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<Auction.BiddingService.Consumers.AuctionStartedConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ReceiveEndpoint("bidding-service-auction-started", e =>
            {
                e.ConfigureConsumer<Auction.BiddingService.Consumers.AuctionStartedConsumer>(context);
            });
        });
    });
}

var app = builder.Build();

// Apply EF migrations if persistence enabled
if (!disablePersistence)
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<BiddingDbContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            // Log and continue so Swagger/UI is available for testing
            Console.WriteLine($"Warning: failed to apply migrations: {ex.Message}");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// In-memory repo for local testing
class InMemoryBidRepository : IBidRepository
{
    private readonly List<BidEntity> _bids = new();

    public Task AddAsync(BidEntity bid)
    {
        _bids.Add(bid);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<BidEntity>> GetByAuctionAsync(Guid auctionId)
    {
        return Task.FromResult(_bids.Where(b => b.AuctionId == auctionId).AsEnumerable());
    }

    public Task<BidEntity?> GetHighestByAuctionAsync(Guid auctionId)
    {
        var highest = _bids.Where(b => b.AuctionId == auctionId).OrderByDescending(b => b.Amount).FirstOrDefault();
        return Task.FromResult(highest);
    }
}
