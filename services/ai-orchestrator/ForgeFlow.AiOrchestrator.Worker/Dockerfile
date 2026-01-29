# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore (all dependencies)
COPY ["contracts/ForgeFlow.Contracts/ForgeFlow.Contracts.csproj", "contracts/ForgeFlow.Contracts/"]
COPY ["services/ai-orchestrator/ForgeFlow.AiOrchestrator.Domain/ForgeFlow.AiOrchestrator.Domain.csproj", "services/ai-orchestrator/ForgeFlow.AiOrchestrator.Domain/"]
COPY ["services/ai-orchestrator/ForgeFlow.AiOrchestrator.Application/ForgeFlow.AiOrchestrator.Application.csproj", "services/ai-orchestrator/ForgeFlow.AiOrchestrator.Application/"]
COPY ["services/ai-orchestrator/ForgeFlow.AiOrchestrator.Infrastructure/ForgeFlow.AiOrchestrator.Infrastructure.csproj", "services/ai-orchestrator/ForgeFlow.AiOrchestrator.Infrastructure/"]
COPY ["services/ai-orchestrator/ForgeFlow.AiOrchestrator.Worker/ForgeFlow.AiOrchestrator.Worker.csproj", "services/ai-orchestrator/ForgeFlow.AiOrchestrator.Worker/"]

# Restore
RUN dotnet restore "services/ai-orchestrator/ForgeFlow.AiOrchestrator.Worker/ForgeFlow.AiOrchestrator.Worker.csproj"

# Copy the rest of the source
COPY . .

# Publish
WORKDIR "/src/services/ai-orchestrator/ForgeFlow.AiOrchestrator.Worker"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ForgeFlow.AiOrchestrator.Worker.dll"]
