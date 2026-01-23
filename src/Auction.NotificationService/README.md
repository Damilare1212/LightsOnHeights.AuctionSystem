# Auction.NotificationService

Purpose
	- Consumes `HighestBidUpdated` and `AuctionEnded`.
- Persists events and forwards them to subscriber callback URLs.

Key files
- Consumers: `src/Auction.NotificationService/Consumers/*`
- Manager: `src/Auction.NotificationService/Services/NotificationManager.cs`
- Repo: `src/Auction.NotificationService/Repo/*`

Endpoints (minimal API)
- `POST /subscribe` — Body: `{ "callbackUrl":"https://..." }` — register webhook
- `GET /subscribers` — list registered callbacks
- `GET /auctions/{auctionId}/events` — list persisted events
- `POST /auctions/{auctionId}/end` — demo: publish `AuctionEnded`

Configuration
- `RABBITMQ_HOST` (default: `rabbitmq`)
- `NOTIFICATION_DB` (default `Data Source=/data/notifications.db`)

Run
- Local: `cd src/Auction.NotificationService && dotnet run`
- Docker Compose: `docker-compose up --build auction.notificationservice`

Notes
- Current webhook delivery is fire-and-forget; add retry/backoff and pruning for production.