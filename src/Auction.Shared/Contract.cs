using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auction.Shared
{
     
    // Message contracts for inter-service communication
    public record AuctionStarted(Guid AuctionId, Guid RoomId, Guid ItemId, DateTimeOffset StartTime, DateTimeOffset? EndTime = null);

    public record BidPlaced(Guid AuctionId, Guid BidId, Guid BidderId, decimal Amount, DateTimeOffset Timestamp);

    public record HighestBidUpdated(Guid AuctionId, Guid BidId, Guid BidderId, decimal Amount, DateTimeOffset Timestamp);

    public record AuctionEnded(Guid AuctionId, Guid WinnerId, Guid BidId, decimal WinningAmount, DateTimeOffset EndedAt);

    public record InvoiceCreated(Guid InvoiceId, Guid AuctionId, Guid BuyerId, decimal Amount, DateTimeOffset CreatedAt);

    public record PaymentProcessed(Guid PaymentId, Guid InvoiceId, Guid AuctionId, Guid BuyerId, decimal Amount, bool Success, DateTimeOffset ProcessedAt);
}
