namespace Auction.PaymentService.Gateways
{
    public interface IPaymentGateway
    {
        Task<bool> ProcessPaymentAsync(System.Guid paymentId, decimal amount);
    }
}