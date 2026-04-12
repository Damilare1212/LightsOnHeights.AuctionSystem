using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace Auction.NotificationService.Services;

public class NotificationManager
{
    private readonly ConcurrentDictionary<Guid, List<object>> _events = new();
    private readonly ConcurrentBag<string> _subscribers = new();
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationManager(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public void AddSubscriber(string callbackUrl)
    {
        _subscribers.Add(callbackUrl);
    }

    public IEnumerable<string> GetSubscribers() => _subscribers.ToArray();

    public void AddEvent(Guid auctionId, object evt)
    {
        var list = _events.GetOrAdd(auctionId, _ => new List<object>());
        lock (list)
        {
            list.Add(evt);
        }

        // notify subscribers asynchronously 
        _ = NotifySubscribersAsync(auctionId, evt);
    }

    public List<object>? GetEvents(Guid auctionId)
    {
        _events.TryGetValue(auctionId, out var list);
        return list;
    }

    private async Task NotifySubscribersAsync(Guid auctionId, object evt)
    {
        var clients = GetSubscribers().ToList();
        if (!clients.Any()) return;

        foreach (var url in clients)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Add timeout to prevent hanging
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                // Send a simple POST with JSON body { auctionId, event }
                var payload = new { AuctionId = auctionId, Event = evt };
                await client.PostAsJsonAsync(url, payload, cts.Token);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cts.Token)
            {
                // Timeout - log but continue with other subscribers
                Console.WriteLine($"Notification to {url} timed out");
            }
            catch (HttpRequestException ex)
            {
                // HTTP error - log but continue with other subscribers
                Console.WriteLine($"HTTP error notifying {url}: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Generic error - log but continue with other subscribers
                Console.WriteLine($"Failed to notify {url}: {ex.Message}");
            }
        }
    }
}
