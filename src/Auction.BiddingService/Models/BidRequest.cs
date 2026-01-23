using System;
namespace Auction.BiddingService.Models
{
    public record BidRequest(Guid BidderId, decimal Amount);
}