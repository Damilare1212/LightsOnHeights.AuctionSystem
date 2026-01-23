using MassTransit;
using Auction.Shared;
using Auction.BiddingService.Services;

namespace Auction.BiddingService.Consumers;

public class AuctionStartedConsumer : IConsumer<AuctionStarted>
{
    private readonly AuctionManager _manager;

    public AuctionStartedConsumer(AuctionManager manager)
    {
        _manager = manager;
    }

    public Task Consume(ConsumeContext<AuctionStarted> context)
    {
        var msg = context.Message;
        _manager.StartAuction(msg);
        return Task.CompletedTask;
    }
}
