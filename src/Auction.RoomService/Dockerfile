FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Auction.RoomService.csproj", "./"]
COPY ["../Auction.Shared/Auction.Shared.csproj", "../Auction.Shared/"]
RUN dotnet restore "Auction.RoomService.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet publish "Auction.RoomService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Auction.RoomService.dll"]
