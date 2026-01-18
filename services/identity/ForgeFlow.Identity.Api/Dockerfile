# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["contracts/ForgeFlow.Contracts/ForgeFlow.Contracts.csproj", "contracts/ForgeFlow.Contracts/"]
COPY ["services/identity/ForgeFlow.Identity.Domain/ForgeFlow.Identity.Domain.csproj", "services/identity/ForgeFlow.Identity.Domain/"]
COPY ["services/identity/ForgeFlow.Identity.Application/ForgeFlow.Identity.Application.csproj", "services/identity/ForgeFlow.Identity.Application/"]
COPY ["services/identity/ForgeFlow.Identity.Infrastructure/ForgeFlow.Identity.Infrastructure.csproj", "services/identity/ForgeFlow.Identity.Infrastructure/"]
COPY ["services/identity/ForgeFlow.Identity.Api/ForgeFlow.Identity.Api.csproj", "services/identity/ForgeFlow.Identity.Api/"]

# Restore
RUN dotnet restore "services/identity/ForgeFlow.Identity.Api/ForgeFlow.Identity.Api.csproj"

# Copy the rest of the source
COPY . .

# Publish
WORKDIR "/src/services/identity/ForgeFlow.Identity.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ForgeFlow.Identity.Api.dll"]

