using MassTransit;
using Auction.Shared;
using Auction.PaymentService.Gateways;
using Auction.PaymentService.IRepo;

namespace Auction.PaymentService.Consumers;

public class InvoiceCreatedConsumer : IConsumer<InvoiceCreated>
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentGateway _gateway;
    private readonly IPublishEndpoint _publisher;

    public InvoiceCreatedConsumer(IPaymentRepository repo, IPaymentGateway gateway, IPublishEndpoint publisher)
    {
        _repo = repo;
        _gateway = gateway;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<InvoiceCreated> context)
    {
        var msg = context.Message;

        // Validate invoice data - don't process if buyer or amount is invalid
        if (msg.BuyerId == Guid.Empty)
        {
            throw new InvalidOperationException($"Invalid BuyerId in InvoiceCreated message for invoice {msg.InvoiceId}");
        }

        if (msg.Amount <= 0)
        {
            throw new InvalidOperationException($"Invalid amount {msg.Amount} in InvoiceCreated message for invoice {msg.InvoiceId}");
        }

        // Check for idempotency - skip if already processed
        var existingPayment = await _repo.GetByInvoiceIdAsync(msg.InvoiceId);
        if (existingPayment != null)
        {
            // Already processed, log and skip to prevent duplicate payments
            return;
        }

        // Persist payment record
        var payment = await _repo.CreateFromInvoiceAsync(msg.InvoiceId, msg.AuctionId, msg.BuyerId, msg.Amount);

        try
        {
            // Calls external payment gateway
            var success = await _gateway.ProcessPaymentAsync(payment.PaymentId, payment.Amount);

            payment.Success = success;
            payment.ProcessedAt = DateTimeOffset.UtcNow;
            await _repo.UpdateAsync(payment);

            var processed = new PaymentProcessed(
                payment.PaymentId, 
                payment.InvoiceId, 
                payment.AuctionId, 
                payment.BuyerId, 
                payment.Amount, 
                payment.Success, 
                payment.ProcessedAt
            );
            await _publisher.Publish(processed);
        }
        catch (Exception ex)
        {
            // Log error but don't rethrow - let MassTransit handle retry
            payment.Success = false;
            payment.ProcessedAt = DateTimeOffset.UtcNow;
            payment.ErrorMessage = ex.Message;
            await _repo.UpdateAsync(payment);
            
            throw; // Re-throw to trigger MassTransit retry mechanism
        }
    }
}
