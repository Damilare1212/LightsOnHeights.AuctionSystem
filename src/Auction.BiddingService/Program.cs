using MassTransit;
using Auction.Shared;
using Auction.BiddingService.Services;
using Auction.BiddingService.Models;
using Auction.BiddingService.Repo;
using Microsoft.EntityFrameworkCore;
using Auction.BiddingService.IRepo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HealthChecks.RabbitMQ;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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

if (!disablePersistence)
{
    // EF Core for bidding persistence with retry logic
    builder.Services.AddDbContext<BiddingDbContext>(options => 
        options.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.MigrationsAssembly(typeof(BiddingDbContext).Assembly.FullName);
        })
        .EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: new[] { 1 } // SQLITE_BUSY
        ));
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

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BiddingDbContext>("database")
    .AddRabbitMQ(rabbitHost, name: "rabbitmq", timeout: TimeSpan.FromSeconds(3));

// Apply EF migrations if persistence enabled and enable WAL mode
if (!disablePersistence)
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<BiddingDbContext>();
            db.Database.Migrate();
            // Enable WAL mode for better concurrency
            db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            
            // Initialize auction state from history
            var manager = scope.ServiceProvider.GetRequiredService<AuctionManager>();
            var repo = scope.ServiceProvider.GetRequiredService<IBidRepository>();
            await manager.InitializeFromHistoryAsync(repo);
        }
        catch (Exception ex)
        {
            // Log and continue so Swagger/UI is available for testing
            Console.WriteLine($"Warning: failed to apply migrations or initialize state: {ex.Message}");
        }
    }
}

// Configure the HTTP request pipeline.
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

    public Task<IEnumerable<BidEntity>> GetAllAsync()
    {
        return Task.FromResult(_bids.AsEnumerable());
    }
}
