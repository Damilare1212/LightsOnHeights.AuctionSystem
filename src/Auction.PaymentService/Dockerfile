FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Auction.PaymentService.csproj", "./"]
COPY ["../Auction.Shared/Auction.Shared.csproj", "../Auction.Shared/"]
RUN dotnet restore "Auction.PaymentService.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet publish "Auction.PaymentService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Auction.PaymentService.dll"]
