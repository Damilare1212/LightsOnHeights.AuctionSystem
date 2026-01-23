using Auction.InvoiceService.Repo;

namespace Auction.InvoiceService.IRepo;

public interface IInvoiceRepository
{
    Task AddAsync(InvoiceEntity invoice);
    Task<InvoiceEntity?> GetAsync(Guid invoiceId);
    Task<IEnumerable<InvoiceEntity>> GetAllAsync();
    Task MarkPaidAsync(Guid invoiceId);
}
