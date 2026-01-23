using System.ComponentModel.DataAnnotations;
using System;
namespace Auction.PaymentService.Models
{
    public class PaymentRecord
    {
        [Key]
        public Guid PaymentId { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid AuctionId { get; set; }
        public Guid BuyerId { get; set; }
        public decimal Amount { get; set; }
        public bool Success { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }
}