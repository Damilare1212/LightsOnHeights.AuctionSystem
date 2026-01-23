using Microsoft.EntityFrameworkCore;
using Auction.PaymentService.Models;
using Auction.PaymentService.IRepo;

namespace Auction.PaymentService.Repo;

public class EfPaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;

    public EfPaymentRepository(PaymentDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentRecord> CreateFromInvoiceAsync(Guid invoiceId, Guid auctionId, Guid buyerId, decimal amount)
    {
        var payment = new PaymentRecord
        {
            PaymentId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            AuctionId = auctionId,
            BuyerId = buyerId,
            Amount = amount,
            Success = false,
            ProcessedAt = DateTimeOffset.MinValue
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task UpdateAsync(PaymentRecord payment)
    {
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync();
    }

    public async Task<PaymentRecord?> GetAsync(Guid id)
    {
        return await _db.Payments.FindAsync(id);
    }

    public async Task<IEnumerable<PaymentRecord>> GetAllAsync()
    {
        return await _db.Payments.ToListAsync();
    }
}
