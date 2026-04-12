using MassTransit;
using Auction.Shared;
using Auction.InvoiceService.IRepo;

namespace Auction.InvoiceService.Consumers;

public class AuctionEndedConsumer : IConsumer<AuctionEnded>
{
    private readonly IInvoiceRepository _repo;
    private readonly IPublishEndpoint _publisher;

    public AuctionEndedConsumer(IInvoiceRepository repo, IPublishEndpoint publisher)
    {
        _repo = repo;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<AuctionEnded> context)
    {
        var msg = context.Message;

        // Don't create invoice if there's no winner (no bids were placed)
        if (msg.WinnerId == Guid.Empty || msg.WinningAmount <= 0)
        {
            return; // No winner, no invoice needed
        }

        // Check for idempotency - skip if invoice already exists for this auction
        var existingInvoice = await _repo.GetByAuctionIdAsync(msg.AuctionId);
        if (existingInvoice != null)
        {
            // Already processed, skip to prevent duplicate invoices
            return;
        }

        var entity = new Auction.InvoiceService.Repo.InvoiceEntity
        {
            InvoiceId = Guid.NewGuid(),
            AuctionId = msg.AuctionId,
            BuyerId = msg.WinnerId,
            Amount = msg.WinningAmount,
            CreatedAt = msg.EndedAt,
            Paid = false
        };

        await _repo.AddAsync(entity);

        var invoiceCreated = new InvoiceCreated(entity.InvoiceId, entity.AuctionId, entity.BuyerId, entity.Amount, entity.CreatedAt);
        await _publisher.Publish(invoiceCreated);
    }
}
