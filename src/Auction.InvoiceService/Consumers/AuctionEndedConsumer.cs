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
