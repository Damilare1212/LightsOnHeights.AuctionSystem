FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Auction.InvoiceService.csproj", "./"]
COPY ["../Auction.Shared/Auction.Shared.csproj", "../Auction.Shared/"]
RUN dotnet restore "Auction.InvoiceService.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet publish "Auction.InvoiceService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Auction.InvoiceService.dll"]
