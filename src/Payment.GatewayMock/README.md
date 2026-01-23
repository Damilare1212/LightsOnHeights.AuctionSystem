# Payment.GatewayMock

Purpose
- Simple mock HTTP payment gateway used for local end-to-end testing.
- Exposes endpoints for `POST /process` etc. (see project code).

Run
- Local:
  - `cd src/Payment.GatewayMock`
  - `dotnet run`
- Docker Compose:
  - included in compose file; starts automatically when you run `docker-compose up --build`

Notes
- The gateway mock allows quick testing of PaymentService without a third-party provider.
				- Replace with a real gateway integration in production and secure network access.