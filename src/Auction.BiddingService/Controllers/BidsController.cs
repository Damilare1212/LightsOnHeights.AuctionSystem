using Microsoft.AspNetCore.Mvc;
using Auction.BiddingService.Models;
using Auction.BiddingService.Services;
using Auction.Shared;
using MassTransit;
using Auction.BiddingService.IRepo;
using Microsoft.AspNetCore.Authorization;

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
    [Authorize(Policy = "Bidder")]
    public async Task<IActionResult> PlaceBid(Guid auctionId, [FromBody] BidRequest request)
    {
        // Validate input
        if (request.Amount <= 0)
            return BadRequest(new { error = "Bid amount must be greater than 0" });

        var bidId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Atomically check and update - prevents race conditions
        var result = _manager.TryPlaceBid(auctionId, bidId, request.BidderId, request.Amount, timestamp);
        
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        // Publish BidPlaced event for all bids
        var bidPlaced = new BidPlaced(auctionId, bidId, request.BidderId, request.Amount, timestamp);
        await _publisher.Publish(bidPlaced);

        // Persist bid to database
        var entity = new BidEntity 
        { 
            BidId = bidId, 
            AuctionId = auctionId, 
            BidderId = request.BidderId, 
            Amount = request.Amount, 
            Timestamp = timestamp 
        };
        await _repo.AddAsync(entity);

        // Publish HighestBidUpdated only if this is a new highest bid
        if (result.IsNewHighest && result.UpdateEvent != null)
        {
            await _publisher.Publish(result.UpdateEvent);
        }

        return Accepted($"/api/bids/{auctionId}/{bidId}", new { bidId, result.IsNewHighest });
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
