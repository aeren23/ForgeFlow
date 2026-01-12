# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["contracts/ForgeFlow.Contracts/ForgeFlow.Contracts.csproj", "contracts/ForgeFlow.Contracts/"]
COPY ["services/gateway/ForgeFlow.Gateway/ForgeFlow.Gateway.csproj", "services/gateway/ForgeFlow.Gateway/"]

# Restore
RUN dotnet restore "services/gateway/ForgeFlow.Gateway/ForgeFlow.Gateway.csproj"

# Copy the rest of the source
COPY . .

# Publish
WORKDIR "/src/services/gateway/ForgeFlow.Gateway"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ForgeFlow.Gateway.dll"]
