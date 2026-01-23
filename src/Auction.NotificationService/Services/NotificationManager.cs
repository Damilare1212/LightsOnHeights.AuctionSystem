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

        var client = _httpClientFactory.CreateClient();
        foreach (var url in clients)
        {
            try
            {
                // Send a simple POST with JSON body { auctionId, event }
                var payload = new { AuctionId = auctionId, Event = evt };
                await client.PostAsJsonAsync(url, payload);
            }
            catch
            {
                
            }
        }
    }
}
