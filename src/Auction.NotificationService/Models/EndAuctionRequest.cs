namespace Auction.NotificationService.Models
{
    public record EndAuctionRequest(System.Guid WinnerId, System.Guid BidId, decimal Amount);
}