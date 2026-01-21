# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BestelAppBoeken.Receiver/BestelAppBoeken.Receiver.csproj", "BestelAppBoeken.Receiver/"]
COPY ["BestelAppBoeken.Core/BestelAppBoeken.Core.csproj", "BestelAppBoeken.Core/"]
COPY ["BestelAppBoeken.Infrastructure/BestelAppBoeken.Infrastructure.csproj", "BestelAppBoeken.Infrastructure/"]

RUN dotnet restore "BestelAppBoeken.Receiver/BestelAppBoeken.Receiver.csproj"

COPY . .
WORKDIR "/src/BestelAppBoeken.Receiver"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BestelAppBoeken.Receiver.dll"]