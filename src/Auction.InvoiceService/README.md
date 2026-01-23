# Auction.InvoiceService

Purpose
- Consumes `AuctionEnded` messages.
- Creates invoices and publishes `InvoiceCreated`.

Key files
- Consumer: `src/Auction.InvoiceService/Consumers/AuctionEndedConsumer.cs`
- Repo: `src/Auction.InvoiceService/Repo/*`
- Models: `src/Auction.InvoiceService/Models/*`

Endpoints
- `GET /invoices` — list invoices
- `GET /invoices/{invoiceId}` — get invoice

Configuration
- `RABBITMQ_HOST` (default: `rabbitmq`)
- `INVOICE_DB` (default `Data Source=/data/invoices.db`)

Run
- Local: `cd src/Auction.InvoiceService && dotnet run`
- Docker Compose: `docker-compose up --build auction.invoiceservice`

Notes
- Ensure consumer is idempotent to avoid duplicate invoices on re-delivery.
- QuestPDF is available for PDF invoice generation if needed.