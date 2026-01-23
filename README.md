# Auction Chat — Backend Services (mono-repo)

Summary
- A .NET 9 microservice demo implementing an asynchronous auction workflow using MassTransit + RabbitMQ.
- Purpose: allow users to open rooms, start auctions, place bids, receive real-time updates, generate invoices, and process payments — all via event-driven services.

Repository layout (short)
- `src/Auction.Shared` — shared message contracts and DTOs (`src/Auction.Shared/Contract.cs`) — single source of truth for messages.
- `src/Auction.RoomService` — room management and start-auction API (MVC controller).
- `src/Auction.BiddingService` — accepts bids, persists them, maintains in-memory auction state, auto-ends auctions (background monitor).
- `src/Auction.NotificationService` — persists events and notifies subscriber callbacks (webhooks).
- `src/Auction.InvoiceService` — creates invoices when auctions end and publishes `InvoiceCreated`.
- `src/Auction.PaymentService` — consumes invoices, calls payment gateway, publishes `PaymentProcessed`.
- `src/Payment.GatewayMock` — simple HTTP mock gateway for local testing.
- `docker-compose.yml` — local integration stack (RabbitMQ + services).

Key concepts
- Message-driven integration: services publish/consume records declared in `src/Auction.Shared/Contract.cs`.
- Minimal APIs are used for small services; MVC controllers are used where there is a richer HTTP surface.
- `Auction.BiddingService` runs `AuctionMonitor` to automatically publish `AuctionEnded` when an auction reaches `EndTime`.

Prerequisites
- .NET 9 SDK
- Docker & Docker Compose (for running full stack)
- Visual Studio 2022 (optional)

Environment variables (per service)
- `RABBITMQ_HOST` — RabbitMQ host (default: `rabbitmq` in compose)
- `BIDDING_DB`, `NOTIFICATION_DB`, `INVOICE_DB`, `PAYMENT_DB` — SQLite connection strings (defaults to `Data Source=/data/<file>.db`)
- `PAYMENT_GATEWAY_URL` — payment gateway base URL (default `http://payment-gateway`)
- `DISABLE_MESSAGING=true` — run a service without MassTransit (useful for quick local testing)
- `DISABLE_PERSISTENCE=true` — use in-memory repo (useful for quick local testing)

Quick start (Docker Compose)
1. Build and run full stack:
   - `docker-compose up --build`
2. Watch logs (each service logs to STDOUT). Use `docker-compose logs -f <service>` to tail a service.

Quick start (single service)
1. Build solution:
   - `dotnet build`
   - In Visual Studio: use __Build > Build Solution__.
2. Run single service:
   - `cd src/Auction.BiddingService && dotnet run` (or open project in VS and run).
3. Use Postman / HTTP client to exercise endpoints (see service READMEs for details).

Essential HTTP endpoints (examples)
- RoomService
  - `POST /api/rooms/{roomId}/auctions` — start auction (body: `{ "itemId": "<guid>", "endTime": "<ISO-8601|optional>" }`)
  - `GET /api/rooms/{roomId}`
- BiddingService
  - `POST /api/bids/{auctionId}` — place bid (body: `{ "bidderId": "<guid>", "amount": <decimal> }`)
  - `GET /api/bids/{auctionId}/highest`
- NotificationService
  - `POST /subscribe` — register webhook callback `{ "callbackUrl": "https://..." }`
  - `GET /auctions/{auctionId}/events`
- InvoiceService
  - `GET /invoices`
  - `GET /invoices/{invoiceId}`
- PaymentService
  - `GET /payments`
  - `POST /invoices/{invoiceId}/process` (demo)

If you change contracts
- Edit `src/Auction.Shared/Contract.cs` and rebuild all projects that reference it. Consumers must run the same compiled contracts to avoid deserialization errors.

Troubleshooting & tips
- Common build/runtime issue: duplicate or mismatched message types. Ensure every project references the single `Auction.Shared` project and you rebuilt the entire solution after changes.
- If MassTransit consumers fail to deserialize, confirm all services are using the same `Auction.Shared` assembly version and restart consumers.
- If RabbitMQ connectivity fails, verify `RABBITMQ_HOST` and check Rabbit container health.

Recommended production improvements (short checklist)
- Add MassTransit retry policies, durable endpoints, and dead-letter exchanges.
- Implement an outbox or transactional pattern to guarantee publish-after-commit semantics.
- Ensure consumers are idempotent.
- Add structured logging (ILogger), correlation IDs, and metrics (Prometheus).
- Add health/readiness probes and liveness checks for Kubernetes.
- Replace mock gateway with a secure, robust payment provider; add circuit breakers and retries (Polly).
- Secure endpoints (TLS, authentication, authorization) and protect messaging credentials.

Where to find more
- Service-specific READMEs: `src/*/README.md` — contain endpoints, env vars, and run instructions per service.
- Shared contracts: `src/Auction.Shared/Contract.cs`.

  