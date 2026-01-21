# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["BestelAppBoeken.Web/BestelAppBoeken.Web.csproj", "BestelAppBoeken.Web/"]
COPY ["BestelAppBoeken.Core/BestelAppBoeken.Core.csproj", "BestelAppBoeken.Core/"]
COPY ["BestelAppBoeken.Infrastructure/BestelAppBoeken.Infrastructure.csproj", "BestelAppBoeken.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "BestelAppBoeken.Web/BestelAppBoeken.Web.csproj"

# Copy everything else INCLUDING certificate
COPY . .

# Build and publish
WORKDIR "/src/BestelAppBoeken.Web"
RUN dotnet build "BestelAppBoeken.Web.csproj" -c Release -o /app/build
RUN dotnet publish "BestelAppBoeken.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Create directory for certificate
RUN mkdir -p /https

# Copy certificate from build stage
COPY --from=build /src/aspnetapp.pfx /https/aspnetapp.pfx

# Copy published app
COPY --from=build /app/publish .

# Expose ports
EXPOSE 443

# Start the app
ENTRYPOINT ["dotnet", "BestelAppBoeken.Web.dll"]
