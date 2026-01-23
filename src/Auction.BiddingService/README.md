# Auction.BiddingService

Purpose
- Consumes `AuctionStarte		d` to initialize auctions.
- Accepts bids, persists them, publishes `BidPlaced` and `HighestBidUpdated`.
- Runs `AuctionMonitor` background service to publish `AuctionEnded` at `EndTime`.

Key files
- Controller: `src/Auction.BiddingService/Controllers/BidsController.cs`
- State: `src/Auction.BiddingService/Services/AuctionManager.cs`
- Monitor: `src/Auction.BiddingService/Services/AuctionMonitor.cs`
- Repos/EF: `src/Auction.BiddingService/Repo/*`

Endpoints
- `POST /api/bids/{auctionId}` — Body: `{ "bidderId": "<guid>", "amount": <decimal> }` ? `202 Accepted`
- `GET /api/bids/{auctionId}/highest` — current highest and persisted top bid

Configuration
- `RABBITMQ_HOST` (default: `rabbitmq`)
- `BIDDING_DB` (SQLite connection string; default `Data Source=/data/bidding.db`)
- `DISABLE_MESSAGING=true` — run without MassTransit
- `DISABLE_PERSISTENCE=true` — use in-memory repo
- `RUN_MODE=swagger` — quick in-memory swagger mode

Run
- Local: `cd src/Auction.BiddingService && dotnet run`
- Docker Compose: `docker-compose up --build auction.biddingservice`

Notes
- Add retries, idempotency, and transactional outbox for production reliability.