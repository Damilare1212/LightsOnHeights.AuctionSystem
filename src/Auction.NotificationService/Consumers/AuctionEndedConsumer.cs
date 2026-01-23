using MassTransit;
using Auction.Shared;
using Auction.NotificationService.Services;
using System.Text.Json;
using Auction.NotificationService.IRepo;

namespace Auction.NotificationService.Consumers;

public class AuctionEndedConsumer : IConsumer<AuctionEnded>
{
    private readonly NotificationManager _manager;
    private readonly IEventRepository _repo;

    public AuctionEndedConsumer(NotificationManager manager, IEventRepository repo)
    {
        _manager = manager;
        _repo = repo;
    }

    public async Task Consume(ConsumeContext<AuctionEnded> context)
    {
        var msg = context.Message;
        _manager.AddEvent(msg.AuctionId, msg);

        var entity = new Auction.NotificationService.Repo.EventEntity
        {
            Id = Guid.NewGuid(),
            AuctionId = msg.AuctionId,
            EventType = nameof(AuctionEnded),
            Payload = JsonSerializer.Serialize(msg),
            Timestamp = msg.EndedAt
        };

        await _repo.AddAsync(entity);
    }
}
