using System.ComponentModel.DataAnnotations;

namespace Auction.InvoiceService.Repo;

public class InvoiceEntity
{
    [Key]
    public System.Guid InvoiceId { get; set; }
    public System.Guid AuctionId { get; set; }
    public System.Guid BuyerId { get; set; }
    public decimal Amount { get; set; }
    public System.DateTimeOffset CreatedAt { get; set; }
    public bool Paid { get; set; }
}
