# Auction.RoomService

Purpose
- Exposes APIs to manage rooms and start auctions.
- Publishes `AuctionStarted` messages to trigger the bidding pipeline.

Key files
- Controller: `src/Auction.RoomService/Controllers/RoomsController.cs`
- Room state manager: `src/Auction.RoomService/Services/RoomManager.cs`

Important endpoints
- `POST /api/rooms/{roomId}/auctions`
  - Body: `{ "itemId": "<guid>", "endTime": "<ISO-8601|optional>" }`
  - Publishes `AuctionStarted` (see `Auction.Shared`).
  - Returns `202 Accepted` with `auctionId`.
- `GET /api/rooms/{roomId}`
  - Returns room state.it

Environment variables
- `RABBITMQ_HOST` — RabbitMQ host (default `rabbitmq` when using compose).

Run
- Local:
  - `cd src/Auction.RoomService`
  - `dotnet run`
- Docker Compose:
  - `docker-compose up --build auction.roomservice`

Notes
- Uses MVC controllers (AddControllers).
- Validate `EndTime` client-side or add server validation if required.