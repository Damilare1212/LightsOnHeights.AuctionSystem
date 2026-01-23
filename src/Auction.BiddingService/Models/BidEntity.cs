namespace Auction.BiddingService.Models;
using System;

public class BidEntity
{
    public Guid BidId { get; set; }
    public Guid AuctionId { get; set; }
    public Guid BidderId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
