using Auction.PaymentService.Models;

namespace Auction.PaymentService.IRepo;

public interface IPaymentRepository
{
    Task<PaymentRecord> CreateFromInvoiceAsync(Guid invoiceId, Guid auctionId, Guid buyerId, decimal amount);
    Task UpdateAsync(PaymentRecord payment);
    Task<PaymentRecord?> GetAsync(Guid id);
    Task<PaymentRecord?> GetByInvoiceIdAsync(Guid invoiceId);
    Task<IEnumerable<PaymentRecord>> GetAllAsync();
}
