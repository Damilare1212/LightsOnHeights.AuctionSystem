FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Auction.Shared.csproj", "./"]
RUN dotnet restore "Auction.Shared.csproj"
COPY . .
RUN dotnet build "Auction.Shared.csproj" -c Release -o /app/build

FROM scratch AS final
