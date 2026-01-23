namespace Auction.RoomService.Models
{
    public record StartAuctionRequest(System.Guid ItemId, System.DateTimeOffset? EndTime);
}