using Microsoft.AspNetCore.Mvc;
using Auction.RoomService.Models;
using Auction.RoomService.Services;
using Auction.Shared;
using MassTransit;

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
    public async Task<IActionResult> StartAuction(Guid roomId, [FromBody] StartAuctionRequest request)
    {
        var auctionId = Guid.NewGuid();
        var startTime = DateTimeOffset.UtcNow;

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
