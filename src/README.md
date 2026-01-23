# Auction Chat — Backend Services

Mono-repo of small .NET 9 services demonstrating an asynchronous auction workflow using MassTransit + RabbitMQ.

Services
- `Auction.RoomService` — room management and start-auction API.
- `Auction.BiddingService` — accept bids, manage auction state, persist bids, auto-end auctions.
- `Auction.NotificationService` — store auction events and notify subscribers.
- `Auction.InvoiceService` — create invoices when auctions end.
- `Auction.PaymentService` — process invoices and call a payment gateway.
- `Payment.GatewayMock` — local mock HTTP payment gateway for testing.

Quick commands
- Build solution: `dotnet build`
- Run a single service: `cd src/<service> && dotnet run`
- Full stack (Docker Compose): `docker-compose up --build`

Notes
- Shared message contracts: `src/Auction.Shared/Contract.cs` — update and rebuild all services after any change.
- Default databases are SQLite files under `/dat	a` in containers. Configure connection strings via env vars per service README.