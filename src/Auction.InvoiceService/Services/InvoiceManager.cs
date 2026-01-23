using System.Collections.Concurrent;
using Auction.InvoiceService.Models;

namespace Auction.InvoiceService.Services;

public class InvoiceManager
{
    private readonly ConcurrentDictionary<Guid, Invoice> _invoices = new();

    public Invoice Create(Guid auctionId, Guid buyerId, decimal amount)
    {
        var invoice = new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            AuctionId = auctionId,
            BuyerId = buyerId,
            Amount = amount,
            CreatedAt = DateTimeOffset.UtcNow,
            Paid = false
        };

        _invoices.TryAdd(invoice.InvoiceId, invoice);
        return invoice;
    }

    public Invoice? Get(Guid invoiceId)
    {
        _invoices.TryGetValue(invoiceId, out var inv);
        return inv;
    }

    public IEnumerable<Invoice> GetAll() => _invoices.Values;

    public void MarkPaid(Guid invoiceId)
    {
        if (_invoices.TryGetValue(invoiceId, out var inv))
        {
            inv.Paid = true;
        }
    }
}
