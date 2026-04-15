using Microsoft.AspNetCore.Mvc;
using Auction.RoomService.Models;
using Auction.RoomService.Services;
using Auction.Shared;
using MassTransit;
using Microsoft.AspNetCore.Authorization;

namespace Auction.RoomService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly RoomManager _manager;
    private readonly IPublishEndpoint _publisher;

    public RoomsController(RoomManager manager, IPublishEndpoint publisher)
    {
        _manager = manager;
        _publisher = publisher;
    }

    [HttpPost("{roomId:guid}/auctions")]
    [Authorize(Policy = "Auctioneer")]
    public async Task<IActionResult> StartAuction(Guid roomId, [FromBody] StartAuctionRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        
        // Validate end time is in the future
        if (request.EndTime <= now)
        {
            return BadRequest(new { error = "End time must be in the future" });
        }

        // Validate minimum auction duration (e.g., at least 10 seconds)
        var minDuration = TimeSpan.FromSeconds(10);
        if (request.EndTime - now < minDuration)
        {
            return BadRequest(new { error = $"Auction must run for at least {minDuration.TotalSeconds} seconds" });
        }

        var auctionId = Guid.NewGuid();
        var startTime = now;

        var msg = new AuctionStarted(auctionId, roomId, request.ItemId, startTime, request.EndTime);
        _manager.AddAuction(roomId, auctionId);

        await _publisher.Publish(msg);

        return Accepted($"/api/rooms/{roomId}/auctions/{auctionId}", new { auctionId });
    }

    [HttpGet("{roomId:guid}")]
    public IActionResult GetRoom(Guid roomId)
    {
        var state = _manager.GetRoom(roomId);
        if (state == null) return NotFound();
        return Ok(state);
    }
}
