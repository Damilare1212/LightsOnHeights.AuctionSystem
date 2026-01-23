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

        // Persist payment record
        var payment = await _repo.CreateFromInvoiceAsync(msg.InvoiceId, msg.AuctionId, msg.BuyerId, msg.Amount);

        // Calls external payment gateway
        var success = await _gateway.ProcessPaymentAsync(payment.PaymentId, payment.Amount);

        payment.Success = success;
        payment.ProcessedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(payment);

        var processed = new PaymentProcessed(payment.PaymentId, payment.InvoiceId, payment.AuctionId, payment.BuyerId, payment.Amount, payment.Success, payment.ProcessedAt);
        await _publisher.Publish(processed);
    }
}
