using System.Collections.Concurrent;
using MassTransit;
using Auction.Shared;
using Microsoft.EntityFrameworkCore;
using Auction.PaymentService.Repo;
using Auction.PaymentService.Models;
using Auction.PaymentService.Gateways;
using Auction.PaymentService.Consumers;
using Auction.PaymentService.IRepo;

var builder = WebApplication.CreateBuilder(args);

var rabbitHost = builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq";
var connectionString = builder.Configuration.GetConnectionString("PaymentDb") ?? builder.Configuration["PAYMENT_DB"] ?? "Data Source=/data/payments.db";
var gatewayUrl = builder.Configuration["PAYMENT_GATEWAY_URL"] ?? "http://payment-gateway";

builder.Services.AddOpenApi();

// EF Core DbContext
builder.Services.AddDbContext<PaymentDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IPaymentRepository, EfPaymentRepository>();
// Register HttpPaymentGateway using Typed HttpClient
builder.Services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>(client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
});

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InvoiceCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("payment-invoice-created", e =>
        {
            e.ConfigureConsumer<InvoiceCreatedConsumer>(context);
        });
    });
});

var app = builder.Build();

// Ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/payments", async (IPaymentRepository repo) => Results.Ok(await repo.GetAllAsync()));
app.MapGet("/payments/{paymentId:guid}", async (Guid paymentId, IPaymentRepository repo) =>
{
    var p = await repo.GetAsync(paymentId);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

//  manually process an invoice by id (useful for testing)
app.MapPost("/invoices/{invoiceId:guid}/process", async (Guid invoiceId, IPaymentRepository repo, IPaymentGateway gateway, IPublishEndpoint publisher) =>
{
    var payment = await repo.CreateFromInvoiceAsync(invoiceId, Guid.Empty, Guid.Empty, 0m);
    var result = await gateway.ProcessPaymentAsync(payment.PaymentId, payment.Amount);
    payment.Success = result;
    payment.ProcessedAt = DateTimeOffset.UtcNow;
    await repo.UpdateAsync(payment);

    var processed = new PaymentProcessed(payment.PaymentId, payment.InvoiceId, payment.AuctionId, payment.BuyerId, payment.Amount, payment.Success, payment.ProcessedAt);
    await publisher.Publish(processed);
    return Results.Accepted($"/payments/{payment.PaymentId}", payment);
});

app.Run();
