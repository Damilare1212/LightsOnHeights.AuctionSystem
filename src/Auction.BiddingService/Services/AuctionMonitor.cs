using MassTransit;

namespace Auction.BiddingService.Services;

public class AuctionMonitor : BackgroundService
{
    private readonly AuctionManager _manager;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<AuctionMonitor> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);

    public AuctionMonitor(AuctionManager manager, IPublishEndpoint publisher, ILogger<AuctionMonitor> logger)
    {
        _manager = manager;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuctionMonitor started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var auctions = _manager.GetAuctionsWithEndTime();

                foreach (var (auctionId, endTime) in auctions)
                {
                    if (endTime.HasValue && endTime.Value <= now)
                    {
                        var result = _manager.EndAuction(auctionId, now);
                        if (result != null)
                        {
                            var (ended, state) = result.Value;
                            _logger.LogInformation("Publishing AuctionEnded for {AuctionId}", auctionId);
                            await _publisher.Publish(ended, stoppingToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) {   }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuctionMonitor loop");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
        _logger.LogInformation("AuctionMonitor stopping.");
    }
}