using System;

namespace Auction.BiddingService.Models
{
    public class    AuctionState
    {
        public Guid AuctionId { get; set; }
        public bool Active { get; set; }
        public Guid? HighestBidId { get; set; }
        public Guid? HighestBidderId { get; set; }
        public decimal HighestAmount { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? EndTime { get; set; }  
    }
}