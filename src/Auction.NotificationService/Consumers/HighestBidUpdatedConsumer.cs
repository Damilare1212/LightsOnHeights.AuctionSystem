using MassTransit;
using Auction.Shared;
using Auction.NotificationService.Services;
using System.Text.Json;
using Auction.NotificationService.IRepo;

namespace Auction.NotificationService.Consumers;

public class HighestBidUpdatedConsumer : IConsumer<HighestBidUpdated>
{
    private readonly NotificationManager _manager;
    private readonly IEventRepository _repo;

    public HighestBidUpdatedConsumer(NotificationManager manager, IEventRepository repo)
    {
        _manager = manager;
        _repo = repo;
    }

    public async Task Consume(ConsumeContext<HighestBidUpdated> context)
    {
        var msg = context.Message;
        _manager.AddEvent(msg.AuctionId, msg);

        var entity = new Auction.NotificationService.Repo.EventEntity
        {
            Id = Guid.NewGuid(),
            AuctionId = msg.AuctionId,
            EventType = nameof(HighestBidUpdated),
            Payload = JsonSerializer.Serialize(msg),
            Timestamp = msg.Timestamp
        };

        await _repo.AddAsync(entity);
    }
}
