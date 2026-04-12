using Auction.InvoiceService.IRepo;
using Microsoft.EntityFrameworkCore;

namespace Auction.InvoiceService.Repo;

public class EfInvoiceRepository : IInvoiceRepository
{
    private readonly InvoiceDbContext _db;

    public EfInvoiceRepository(InvoiceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(InvoiceEntity invoice)
    {
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
    }

    public async Task<InvoiceEntity?> GetAsync(Guid invoiceId)
    {
        return await _db.Invoices.FindAsync(invoiceId);
    }

    public async Task<InvoiceEntity?> GetByAuctionIdAsync(Guid auctionId)
    {
        return await _db.Invoices.FirstOrDefaultAsync(i => i.AuctionId == auctionId);
    }

    public async Task<IEnumerable<InvoiceEntity>> GetAllAsync()
    {
        return await _db.Invoices.ToListAsync();
    }

    public async Task MarkPaidAsync(Guid invoiceId)
    {
        var inv = await _db.Invoices.FindAsync(invoiceId);
        if (inv != null)
        {
            inv.Paid = true;
            await _db.SaveChangesAsync();
        }
    }
}
