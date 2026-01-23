using System.Collections.Concurrent;
using Auction.Shared;
using Auction.BiddingService.Models;

namespace Auction.BiddingService.Services;

public class AuctionManager
{
    private readonly ConcurrentDictionary<Guid, AuctionState> _auctions = new();

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
        return _auctions.TryGetValue(auctionId, out var s) && s.Active;
    }

    public AuctionState? GetState(Guid auctionId)
    {
        _auctions.TryGetValue(auctionId, out var s);
        return s;
    }

    // Returns HighestBidUpdated if new highest, otherwise null
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
