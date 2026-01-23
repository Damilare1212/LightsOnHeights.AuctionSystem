using MassTransit;
using Auction.Shared;
using Auction.InvoiceService.Services;
using Auction.InvoiceService.Models;
using Auction.InvoiceService.Consumers;
using Auction.InvoiceService.Repo;
using Microsoft.EntityFrameworkCore;
using Auction.InvoiceService.IRepo;

var builder = WebApplication.CreateBuilder(args);

var rabbitHost = builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq";
var connectionString = builder.Configuration.GetConnectionString("InvoiceDb") ?? builder.Configuration["INVOICE_DB"] ?? "Data Source=/data/invoices.db";

builder.Services.AddOpenApi();
builder.Services.AddSingleton<InvoiceManager>();

// EF Core for invoices
builder.Services.AddDbContext<InvoiceDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AuctionEndedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("invoice-auction-ended", e =>
        {
            e.ConfigureConsumer<AuctionEndedConsumer>(context);
        });
    });
});

var app = builder.Build();

// Ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/invoices", async (IInvoiceRepository repo) => Results.Ok(await repo.GetAllAsync()));
app.MapGet("/invoices/{invoiceId:guid}", async (Guid invoiceId, IInvoiceRepository repo) =>
{
    var inv = await repo.GetAsync(invoiceId);
    return inv is null ? Results.NotFound() : Results.Ok(inv);
});

app.Run();
