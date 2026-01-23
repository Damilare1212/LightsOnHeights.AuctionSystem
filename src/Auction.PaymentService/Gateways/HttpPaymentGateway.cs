using System.Net.Http.Json;
using Auction.PaymentService.Gateways;

namespace Auction.PaymentService.Gateways
{
    public class HttpPaymentGateway : IPaymentGateway
    {
        private readonly HttpClient _client;

        public HttpPaymentGateway(HttpClient client)
        {
            _client = client;
        }

        public async Task<bool> ProcessPaymentAsync(System.Guid paymentId, decimal amount)
        {
            try
            {
                var req = new { PaymentId = paymentId, Amount = amount };
                var resp = await _client.PostAsJsonAsync("/process", req);
                if (!resp.IsSuccessStatusCode) return false;

                var result = await resp.Content.ReadFromJsonAsync<PaymentGatewayResponse>();
                return result?.Success ?? false;
            }
            catch
            {
                return false;
            }
        }

        private record PaymentGatewayResponse(bool Success);
    }
}