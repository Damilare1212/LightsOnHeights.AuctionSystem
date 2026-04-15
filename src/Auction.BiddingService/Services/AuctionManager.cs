using System.Collections.Concurrent;
using Auction.Shared;
using Auction.BiddingService.Models;
using Auction.BiddingService.IRepo;

namespace Auction.BiddingService.Services;

public class AuctionManager
{
    private readonly ConcurrentDictionary<Guid, AuctionState> _auctions = new();
    private readonly decimal _minimumBidIncrement = 1.00m; // Configurable in production
    private readonly ILogger<AuctionManager> _logger;

    public AuctionManager(ILogger<AuctionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize auction state from historical bids on service startup.
    /// This ensures state is not lost on service restart.
    /// </summary>
    public async Task InitializeFromHistoryAsync(IBidRepository repo)
    {
        try
        {
            var allBids = await repo.GetAllAsync();
            var bidList = allBids.ToList();
            
            foreach (var bid in bidList)
            {
                if (!_auctions.ContainsKey(bid.AuctionId))
                {
                    _auctions[bid.AuctionId] = new AuctionState 
                    { 
                        AuctionId = bid.AuctionId,
                        Active = true,
                        HighestAmount = 0,
                        HighestBidderId = Guid.Empty,
                        UpdatedAt = bid.Timestamp
                    };
                }

                var state = _auctions[bid.AuctionId];
                
                // Only update if this bid is higher than current highest
                if (bid.Amount > state.HighestAmount)
                {
                    state.HighestAmount = bid.Amount;
                    state.HighestBidderId = bid.BidderId;
                    state.HighestBidId = bid.Id;
                    state.UpdatedAt = bid.Timestamp;
                }
            }
            
            _logger.LogInformation("AuctionManager initialized with {Count} auctions from history", _auctions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AuctionManager from history");
        }
    }

    public void StartAuction(AuctionStarted msg)
    {
        var state = new AuctionState
        {
            AuctionId = msg.AuctionId,
            Active = true,
            HighestAmount = 0m,
            UpdatedAt = msg.StartTime,
            EndTime = msg.EndTime
        };

        _auctions.AddOrUpdate(msg.AuctionId, state, (k, v) => state);
    }

    public bool IsActive(Guid auctionId)
    {
        if (!_auctions.TryGetValue(auctionId, out var state))
            return false;
        
        // Check if auction has expired
        if (state.Active && state.EndTime.HasValue && DateTimeOffset.UtcNow > state.EndTime.Value)
        {
            state.Active = false;
            return false;
        }
        
        return state.Active;
    }

    public AuctionState? GetState(Guid auctionId)
    {
        _auctions.TryGetValue(auctionId, out var s);
        return s;
    }

    /// <summary>
    /// Atomically attempts to place a bid with proper validation and race condition prevention.
    /// </summary>
    /// <returns>
    /// Tuple containing:
    /// - Success: whether the bid was accepted
    /// - ErrorMessage: reason for failure if not successful
    /// - IsNewHighest: whether this bid became the new highest bid
    /// - UpdateEvent: The HighestBidUpdated event to publish if it's a new highest bid
    /// </returns>
    public (bool Success, string? ErrorMessage, bool IsNewHighest, HighestBidUpdated? UpdateEvent) TryPlaceBid(
        Guid auctionId, 
        Guid bidId, 
        Guid bidderId, 
        decimal amount, 
        DateTimeOffset timestamp)
    {
        if (!_auctions.TryGetValue(auctionId, out var state))
        {
            return (false, "Auction not found", false, null);
        }

        lock (state)
        {
            // Re-check active status inside lock (double-check pattern)
            if (!state.Active)
            {
                return (false, "Auction is not active", false, null);
            }

            // Check if auction has ended based on time
            if (state.EndTime.HasValue && timestamp > state.EndTime.Value)
            {
                state.Active = false;
                return (false, "Auction has ended", false, null);
            }

            // Validate minimum bid increment
            if (state.HighestAmount > 0)
            {
                var requiredMinimum = state.HighestAmount + _minimumBidIncrement;
                if (amount < requiredMinimum)
                {
                    return (false, $"Bid must be at least {requiredMinimum:C}", false, null);
                }
            }
            else if (amount <= 0)
            {
                return (false, "First bid must be greater than 0", false, null);
            }

            // Check if this is a new highest bid
            bool isNewHighest = amount > state.HighestAmount;

            if (isNewHighest)
            {
                state.HighestAmount = amount;
                state.HighestBidId = bidId;
                state.HighestBidderId = bidderId;
                state.UpdatedAt = timestamp;

                var updateEvent = new HighestBidUpdated(
                    auctionId, 
                    bidId, 
                    bidderId, 
                    amount, 
                    timestamp
                );

                return (true, null, true, updateEvent);
            }

            // Bid is valid but not highest (still record it in DB, just don't update high water mark)
            return (true, null, false, null);
        }
    }

    // Returns HighestBidUpdated if new highest, otherwise null
    [Obsolete("Use TryPlaceBid instead for atomic bid placement")]
    public HighestBidUpdated? TryUpdateHighestBid(Guid auctionId, Guid bidId, Guid bidderId, decimal amount, DateTimeOffset timestamp)
    {
        if (!_auctions.TryGetValue(auctionId, out var state) || !state.Active)
            return null;

        lock (state)
        {
            if (amount > state.HighestAmount)
            {
                state.HighestAmount = amount;
                state.HighestBidId = bidId;
                state.HighestBidderId = bidderId;
                state.UpdatedAt = timestamp;

                return new HighestBidUpdated(auctionId, bidId, bidderId, amount, timestamp);
            }
        }

        return null;
    }

    // Attempt to end auction: returns null if no auction or not active; otherwise returns winner info
    public (AuctionEnded ended, AuctionState state)? EndAuction(Guid auctionId, DateTimeOffset endedAt)
    {
        if (!_auctions.TryGetValue(auctionId, out var state) || !state.Active)
            return null;

        lock (state)
        {
            state.Active = false;
            state.UpdatedAt = endedAt;

            var winnerId = state.HighestBidderId ?? Guid.Empty;
            var bidId = state.HighestBidId ?? Guid.Empty;
            var amount = state.HighestAmount;

            var ended = new AuctionEnded(state.AuctionId, winnerId, bidId, amount, endedAt);
            return (ended, state);
        }
    }

    public IEnumerable<(Guid AuctionId, DateTimeOffset? EndTime)> GetAuctionsWithEndTime()
    {
        return _auctions.Values.Select(a => (a.AuctionId, a.EndTime));
    }
}
