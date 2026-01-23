namespace Auction.NotificationService.Repo;

public class EventEntity
{
    public System.Guid Id { get; set; }
    public System.Guid AuctionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public System.DateTimeOffset Timestamp { get; set; }
}
