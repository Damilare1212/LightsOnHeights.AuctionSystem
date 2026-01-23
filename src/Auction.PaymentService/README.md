# Auction.PaymentService

Purpose
- Consumes `InvoiceCreated`.
- Creates payment record, calls payment gateway, publishes `PaymentProcessed`.

Key filesKey files
- Consumer: `src/Auction.PaymentService/Consumers/InvoiceCreatedConsumer.cs`
		- Repo & models: `src/Auction.PaymentService/Repo/*`, `src/Auction.PaymentService/Models/*`
- Gateway abstraction: `src/Auction.PaymentService/Gateways/*`

Important endpoints
- `GET /payments` — list payments.
- `GET /payments/{paymentId}` — get payment by id.
- `POST /invoices/{invoiceId}/process` — demo endpoint to trigger processing manually.

Configuration & environment
- `RABBITMQ_HOST` — RabbitMQ host.
- `PAYMENT_DB` — SQLite connection string (default `Data Source=/data/payments.db`).
- `PAYMENT_GATEWAY_URL` — base address for the payment gateway (default `http://payment-gateway`).

Run
- Local:
  - `cd src/Auction.PaymentService`
  - `dotnet run`
- Docker Compose:
  - `docker-compose up --build auction.paymentservice`

Notes & improvements
- Implement robust retry/circuit-breaker semantics for external gateway calls (Polly).
	- Make payment processing idempotent and observable.