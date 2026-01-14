# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["contracts/ForgeFlow.Contracts/ForgeFlow.Contracts.csproj", "contracts/ForgeFlow.Contracts/"]
COPY ["services/artifact/ForgeFlow.Artifact.Api/ForgeFlow.Artifact.Api.csproj", "services/artifact/ForgeFlow.Artifact.Api/"]
COPY ["services/artifact/ForgeFlow.Artifact.Application/ForgeFlow.Artifact.Application.csproj", "services/artifact/ForgeFlow.Artifact.Application/"]
COPY ["services/artifact/ForgeFlow.Artifact.Infrastructure/ForgeFlow.Artifact.Infrastructure.csproj", "services/artifact/ForgeFlow.Artifact.Infrastructure/"]
COPY ["services/artifact/ForgeFlow.Artifact.Domain/ForgeFlow.Artifact.Domain.csproj", "services/artifact/ForgeFlow.Artifact.Domain/"]

# Restore
RUN dotnet restore "services/artifact/ForgeFlow.Artifact.Api/ForgeFlow.Artifact.Api.csproj"

# Copy the rest of the source
COPY . .

# Publish
WORKDIR "/src/services/artifact/ForgeFlow.Artifact.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ForgeFlow.Artifact.Api.dll"]
