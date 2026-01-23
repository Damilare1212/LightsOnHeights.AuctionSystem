using Microsoft.AspNetCore.Mvc;
using Auction.BiddingService.Models;
using Auction.BiddingService.Services;
using Auction.Shared;
using MassTransit;
using Auction.BiddingService.IRepo;

namespace Auction.BiddingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidsController : ControllerBase
{
    private readonly AuctionManager _manager;
    private readonly IPublishEndpoint _publisher;
    private readonly IBidRepository _repo;

    public BidsController(AuctionManager manager, IPublishEndpoint publisher, IBidRepository repo)
    {
        _manager = manager;
        _publisher = publisher;
        _repo = repo;
    }

    [HttpPost("{auctionId:guid}")]
    public async Task<IActionResult> PlaceBid (Guid auctionId, [FromBody] BidRequest request)
    {
        if (!_manager.IsActive(auctionId))
            return BadRequest(new { error = "Auction is not active or does not exist." });

        var bidId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var bidPlaced = new BidPlaced(auctionId, bidId, request.BidderId, request.Amount, timestamp);
        await _publisher.Publish(bidPlaced);

        // persist
        var entity = new BidEntity { BidId = bidId, AuctionId = auctionId, BidderId = request.BidderId, Amount = request.Amount, Timestamp = timestamp };
        await _repo.AddAsync(entity);

        var highestUpdated = _manager.TryUpdateHighestBid(auctionId, bidId, request.BidderId, request.Amount, timestamp);
        if (highestUpdated != null)
        {
            await _publisher.Publish(highestUpdated);
        }

        return Accepted($"/api/bids/{auctionId}/{bidId}", new { bidId });
    }

    [HttpGet("{auctionId:guid}/highest")]
    public async Task<IActionResult> GetHighest (Guid auctionId)
    {
        var state = _manager.GetState(auctionId);
        if (state == null) return NotFound();

        var highest = await _repo.GetHighestByAuctionAsync(auctionId);

        return Ok(new
        {
            state.AuctionId,
            state.Active,
            state.HighestBidId,
            state.HighestBidderId,
            state.HighestAmount,
            state.UpdatedAt,
            HighestFromRepo = highest
        });
    }
}
