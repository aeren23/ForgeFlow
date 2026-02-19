<p align="center">
  <h1 align="center">ForgeFlow 🚀</h1>
  <p align="center">
    <strong>AI-Assisted Software Delivery Platform</strong>
  </p>
  <p align="center">
    <em>Conversations don't scale. Processes do.</em>
  </p>
  <p align="center">
    <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8" />
    <img src="https://img.shields.io/badge/React-19-61DAFB?logo=react" alt="React 19" />
    <img src="https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript" alt="TypeScript" />
    <img src="https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker" alt="Docker" />
    <img src="https://img.shields.io/badge/RabbitMQ-Event--Driven-FF6600?logo=rabbitmq" alt="RabbitMQ" />
    <img src="https://img.shields.io/badge/License-MIT-green" alt="MIT License" />
  </p>
</p>

---

## 📌 What is ForgeFlow?

**ForgeFlow** is an AI-assisted, event-driven software delivery platform that transforms ideas and issues into **structured, versioned, and auditable SDLC artifacts**.

Instead of using AI as a chat or code-generation toy, ForgeFlow treats AI as a **planning and quality engine** and embeds its outputs directly into the software development lifecycle.

> **IDE agents generate answers. ForgeFlow generates processes.**

<!-- 📸 SCREENSHOT: Ana dashboard ekran görüntüsü buraya eklenecek -->
<!-- ![ForgeFlow Dashboard](docs/screenshots/dashboard.png) -->

---

## 🎯 Why ForgeFlow?

Modern teams already use AI tools and IDE agents. However, they still face the same problems:

| Problem | Current State | ForgeFlow Solution |
|---------|--------------|-------------------|
| AI outputs are unstructured | Chat-based, ephemeral | Structured, versioned artifacts |
| No versioning or approval flow | Copy-paste from AI | Revision-based artifact management |
| No audit trail | No record of AI decisions | Full audit logging with correlation |
| No CI/CD integration | Manual handoff | GitHub webhook automation |
| No governance or quality enforcement | Ad-hoc reviews | AI-powered code review + quality gates |

> **ChatGPT gives you answers. ForgeFlow gives you processes.**

---

## 🧠 Core Philosophy

```
"Generate quality before you write code."
```

ForgeFlow places AI at the center of the software development process but **keeps control and governance with humans**. The platform converts:

```
Issues → Plans → Task Breakdowns → Test Strategies → Security Checklists → AI Code Reviews → Release Artifacts
```

into **validated, versioned, and traceable delivery assets**.

---

## 🏗️ Architecture Overview

ForgeFlow is built using modern backend and cloud-native principles:

- **Microservice Architecture** — 7 independent, deployable services
- **Event-Driven Communication** — RabbitMQ with MassTransit
- **Onion Architecture** — Clean separation per service (Api → Application → Domain → Infrastructure)
- **CQRS** — Command/Query Responsibility Segregation via MediatR
- **Database-per-Service** — Each service owns its data store
- **Real-time Communication** — SignalR with Redis backplane
- **API Gateway** — YARP reverse proxy with JWT authentication
- **Infrastructure as Code** — Docker Compose orchestration

### High-Level Architecture Diagram

```
                         ┌─────────────────────┐
                         │   React Frontend     │
                         │  (Vite + TypeScript)  │
                         └────────┬─────────────┘
                                  │ HTTP / WebSocket
                                  ▼
                    ┌──────────────────────────────┐
                    │    API Gateway (YARP)         │
                    │  JWT Auth + Route Forwarding  │
                    └──┬───┬───┬───┬───┬───────────┘
                       │   │   │   │   │
          ┌────────────┘   │   │   │   └──────────────┐
          ▼                ▼   │   ▼                  ▼
   ┌──────────┐    ┌──────────┐│ ┌──────────┐   ┌──────────┐
   │ Identity │    │   Work   ││ │ Artifact │   │  GitHub  │
   │ Service  │    │ Service  ││ │ Service  │   │ Service  │
   └──────────┘    └──────────┘│ └──────────┘   └──────────┘
                               │
                     ┌─────────┘
                     ▼
          ┌───────────────────┐      ┌───────────────────┐
          │  AI Orchestrator  │      │   Notification    │
          │     (Worker)      │      │     Service       │
          └───────────────────┘      └───────────────────┘
                     │                         │
                     └────────┐  ┌─────────────┘
                              ▼  ▼
                    ┌──────────────────┐
                    │    RabbitMQ      │
                    │  (Event Bus)     │
                    └──────────────────┘
```

<!-- 📸 SCREENSHOT: Mimari diyagram veya Mermaid görseli buraya eklenecek -->
<!-- ![Architecture Diagram](docs/screenshots/architecture.png) -->

---

## 🧩 Services & Modules

### 1. 🔐 Identity Service
> User authentication, registration, and role-based access control.

| Layer | Project |
|-------|---------|
| API | `ForgeFlow.Identity.Api` |
| Application | `ForgeFlow.Identity.Application` |
| Domain | `ForgeFlow.Identity.Domain` |
| Infrastructure | `ForgeFlow.Identity.Infrastructure` |

**Key Features:**
- JWT token generation with refresh token support
- User registration, login, and profile management
- Role-based access control (Admin / Developer / Viewer)
- System admin management
- Password hashing with BCrypt
- Database: `ForgeFlow_Identity` (MSSQL)

---

### 2. 📋 Work Service
> Project management, issue tracking, and Kanban board operations.

| Layer | Project |
|-------|---------|
| API | `ForgeFlow.Work.Api` |
| Application | `ForgeFlow.Work.Application` |
| Domain | `ForgeFlow.Work.Domain` |
| Infrastructure | `ForgeFlow.Work.Infrastructure` |

**Key Features:**
- Project CRUD with member management and role-based permissions
- Issue lifecycle: `To Do → In Progress → In Review → Done`
- Issue types: Epic, Feature, Story, Bug, Task
- Issue priorities: Low, Medium, High, Critical
- Drag-and-drop Kanban board with status transitions
- AI Plan generation trigger (`AiPlanRequested` event)
- GitHub repository linking per project
- Database: `ForgeFlow_Work` (MSSQL)

<!-- 📸 SCREENSHOT: Kanban board ekran görüntüsü buraya eklenecek -->
<!-- ![Kanban Board](docs/screenshots/kanban-board.png) -->

---

### 3. 🤖 AI Orchestrator Service
> AI-powered artifact generation engine. Produces plans, task breakdowns, and code reviews.

| Layer | Project |
|-------|---------|
| Worker | `ForgeFlow.AiOrchestrator.Worker` |
| Application | `ForgeFlow.AiOrchestrator.Application` |
| Domain | `ForgeFlow.AiOrchestrator.Domain` |
| Infrastructure | `ForgeFlow.AiOrchestrator.Infrastructure` |

**Key Features:**
- Multi-provider AI support (Gemini, Groq)
- AI Plan generation: Plans + Task Breakdowns + Test Strategies + Security Checklists
- AI Code Review: PR diff analysis with code quality scoring
- Plan-vs-code compliance checking
- Idempotent request handling with correlation IDs
- Real-time progress reporting via events (`AiProcessingProgress`)
- Token usage tracking (prompt + completion tokens, duration)
- Database: `ForgeFlow_AiOrchestrator` (MSSQL)

---

### 4. 📦 Artifact Service
> Versioned storage for all AI-generated artifacts with immutable revision history.

| Layer | Project |
|-------|---------|
| API | `ForgeFlow.Artifact.Api` |
| Application | `ForgeFlow.Artifact.Application` |
| Domain | `ForgeFlow.Artifact.Domain` |
| Infrastructure | `ForgeFlow.Artifact.Infrastructure` |

**Key Features:**
- Artifact storage with type classification (`AI_PLAN`, `CODE_REVIEW`)
- Immutable revision history — every update creates a new revision
- Idempotent upsert with content hash deduplication
- Correlation ID-based retrieval (e.g., `pr-123` for linking reviews to PRs)
- Metadata management (PR status tracking in review artifacts)
- Audit trail with `AuditBehavior` pipeline
- Database: `ForgeFlow_Artifact` (MSSQL)

---

### 5. 🐙 GitHub Service
> GitHub App integration for repository management, branch creation, webhooks, and PR automation.

| Layer | Project |
|-------|---------|
| API | `ForgeFlow.GitHub.Api` |
| Application | `ForgeFlow.GitHub.Application` |
| Domain | `ForgeFlow.GitHub.Domain` |
| Infrastructure | `ForgeFlow.GitHub.Infrastructure` |

**Key Features:**
- GitHub App authentication (JWT + Installation tokens)
- Automatic feature branch creation on issue assignment (`feature/{issueKey}-{slug}`)
- Smart base branch detection (`develop` → `master` → `main` → default)
- GitHub Webhook processing:
  - `pull_request.opened` → Issue moves to **In Review** + triggers AI Code Review
  - `pull_request.closed` (merged) → Issue moves to **Done**
  - `pull_request.closed` (not merged) → Issue moves back to **In Progress**
  - `push` → Commit activity tracking
- AI Code Review results posted as PR comments (`forgeflow[bot]`)
- Repository connection management per project
- Database: `ForgeFlow_GitHub` (MSSQL)

---

### 6. 🔔 Notification Service
> Real-time notification delivery via SignalR with Redis backplane for horizontal scaling.

| Project |
|---------|
| `ForgeFlow.Notification.Service` |

**Key Features:**
- SignalR hub (`/hubs/forge`) with WebSocket transport
- Redis backplane for multi-instance SignalR scaling
- User-specific groups (`user:{userId}`) and project groups (`project:{projectId}`)
- Real-time event types:
  - `AiProgress` — AI processing progress updates with percentage
  - `BoardUpdate` — Kanban board state changes (issue status transitions)
  - `Notification` — Branch creation alerts, general user notifications
  - `ReviewUpdate` — PR status changes (Open → Merged → Closed)
  - `InstallationListUpdated` — GitHub App installation changes

---

### 7. 🌐 API Gateway (YARP)
> Centralized entry point with JWT authentication, authorization policies, and reverse proxying.

| Project |
|---------|
| `ForgeFlow.Gateway` |

**Key Features:**
- YARP (Yet Another Reverse Proxy) for request routing
- JWT authentication and validation
- Authorization policies: `Authenticated`, `AdminOnly`, `DeveloperOrAbove`
- Custom middleware: JWT claims → HTTP headers (`X-User-Id`, `X-User-Email`, `X-User-Roles`)
- CORS configuration for frontend access
- WebSocket passthrough for SignalR
- 14 route definitions across 5 backend clusters

---

### 8. 📜 Shared Contracts
> Event contracts shared across all services for type-safe, decoupled communication.

| Project |
|---------|
| `ForgeFlow.Contracts` |

**Event Categories:**

| Category | Events | Flow |
|----------|--------|------|
| AI Planning | `AiPlanRequested`, `AiPlanGenerated`, `AiPlanFailed` | Work → AI Orchestrator → Artifact |
| Code Review | `CodeReviewRequested`, `CodeReviewWithDiffReady`, `CodeReviewCompleted`, `CodeReviewFailed` | GitHub → AI Orchestrator → GitHub + Artifact |
| GitHub | `GitHubPullRequestOpened`, `GitHubPullRequestMerged`, `GitHubPullRequestClosed`, `GitHubPushReceived` | GitHub Webhook → Work |
| Notifications | `IssueStatusChanged`, `IssueAssigned`, `BranchCreated`, `UserNotification` | Work/GitHub → Notification |
| PR Status | `PullRequestStatusChanged`, `CodeReviewUpdated` | GitHub → Artifact → Notification |

---

## 🖥️ Frontend

> Modern, responsive single-page application built with React 19 and TypeScript.

**Tech Stack:**

| Technology | Purpose |
|-----------|---------|
| React 19 | UI framework |
| TypeScript 5.9 | Type safety |
| Vite 7 | Build tool & dev server |
| TailwindCSS 4 | Utility-first styling |
| Zustand | Global state management |
| @microsoft/signalr | Real-time communication |
| @dnd-kit | Drag-and-drop Kanban board |
| Axios | HTTP client |
| Lucide React | Icon library |
| SweetAlert2 | Confirmation dialogs |
| React Router DOM 7 | Client-side routing |

**Frontend Features:**
- 🔐 **Authentication** — Login, registration, JWT token management
- 📊 **Dashboard** — Project overview and quick navigation
- 📁 **Project Management** — Create, configure, invite members, link GitHub repos
- 📋 **Kanban Board** — Drag-and-drop issue management with real-time updates
- 🗂️ **Issue Detail** — Full issue view with subtasks, metadata, and AI reviews tab
- 🤖 **AI Plan Modal** — View and manage AI-generated plans and task breakdowns
- 📝 **AI Code Review Cards** — Expandable review cards with:
  - Code quality scores and progress bars
  - Rating badges (Approved ✅, Changes Requested ❌)
  - Severity-colored findings (🔴 Critical, 🟡 Warning, 🔵 Info, 🟢 Suggestion)
  - File/line references with code styling
  - PR status badges (🟢 Open, 🟣 Merged, 🔴 Closed) with real-time updates
- ⚙️ **Project Settings** — Repository linking, member management, configurations
- 👤 **Profile** — User profile management
- 🛡️ **Admin Panel** — System administration (Admin-only)

<!-- 📸 SCREENSHOT: Issue detay modal ekran görüntüsü buraya eklenecek -->
<!-- ![Issue Detail](docs/screenshots/issue-detail.png) -->

<!-- 📸 SCREENSHOT: AI Code Review sonuç kartı ekran görüntüsü buraya eklenecek -->
<!-- ![AI Code Review](docs/screenshots/ai-code-review.png) -->

<!-- 📸 SCREENSHOT: AI Plan oluşturma modalı ekran görüntüsü buraya eklenecek -->
<!-- ![AI Plan Modal](docs/screenshots/ai-plan-modal.png) -->

---

## 🔄 Event-Driven Flow

ForgeFlow uses RabbitMQ with MassTransit for fully decoupled, asynchronous inter-service communication.

### End-to-End Workflow

```
1. User creates an Issue
     │
     ▼
2. User clicks "Generate AI Plan"
     │
     ▼
3. Work Service publishes ─── AiPlanRequested ──► AI Orchestrator
     │                                                    │
     │                              AI generates plan, tasks,
     │                              test plan, security checklist
     │                                                    │
     │◄─────── AiPlanGenerated ───────────────────────────┘
     │
     ▼
4. Artifact Service stores the plan as a versioned artifact
     │
     ▼
5. User assigns the Issue to a developer
     │
     ▼
6. Work Service publishes ─── IssueAssigned ──► GitHub Service
     │                                                │
     │                          Creates feature branch │
     │                          feature/{key}-{slug}   │
     │◄──────── BranchCreated ────────────────────────┘
     │
     ▼
7. Developer opens a Pull Request
     │
     ▼
8. GitHub Webhook ─── PR Opened ──► GitHub Service
     │                                     │
     │          ┌──────────────────────────┤
     │          ▼                          ▼
     │   Issue → "In Review"      CodeReviewRequested
     │                                     │
     │                              AI reviews the diff
     │                              against the plan
     │                                     │
     │◄── CodeReviewCompleted ─────────────┘
     │          │
     │          ├──► PR Comment posted (forgeflow[bot])
     │          └──► Review artifact stored
     │
     ▼
9. PR Merged ──► Issue automatically moves to "Done" ✅
```

### Real-time Updates via SignalR

All state changes are broadcast in real-time to connected clients:

```
Event Bus (RabbitMQ)  ──►  Notification Service  ──►  SignalR Hub  ──►  Frontend
                                                          │
                                                    Redis Backplane
                                                   (Multi-instance)
```

---

## 🛠️ Tech Stack

### Backend
| Technology | Purpose |
|-----------|---------|
| .NET 8 | Runtime & framework |
| ASP.NET Core | Web API framework |
| MassTransit | Message bus abstraction over RabbitMQ |
| MediatR | In-process CQRS mediator |
| Entity Framework Core | ORM with code-first migrations |
| SignalR | Real-time WebSocket communication |
| YARP | Reverse proxy & API gateway |
| Serilog + Seq | Structured logging & log aggregation |
| Octokit | GitHub API client |
| BCrypt.Net | Password hashing |

### Infrastructure
| Technology | Purpose |
|-----------|---------|
| Docker & Docker Compose | Container orchestration |
| RabbitMQ 3 | Message broker (Topic Exchange) |
| Microsoft SQL Server 2022 | Relational database |
| Redis 7 | SignalR backplane & caching |
| Seq | Centralized log viewer |
| Smee.io | GitHub webhook proxy (development) |

### Frontend
| Technology | Purpose |
|-----------|---------|
| React 19 | UI framework |
| TypeScript 5.9 | Type safety |
| Vite 7 | Build tool |
| TailwindCSS 4 | Styling |
| Zustand | State management |
| SignalR Client | Real-time updates |

---

## 🚀 Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (v4.0+)
- [Git](https://git-scm.com/)
- A [GitHub App](https://docs.github.com/en/apps/creating-github-apps) (for GitHub integration)
- A [Gemini API Key](https://ai.google.dev/) or [Groq API Key](https://console.groq.com/) (for AI features)
- A [Smee.io](https://smee.io/) channel (for local webhook proxying)

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/ForgeFlow.git
cd ForgeFlow
```

### 2. Configure Environment Variables

Create the `.env` file inside the `infra/` directory:

```bash
cp infra/.env.example infra/.env
```

Then edit `infra/.env` with your own values.

> ⚠️ **Important:** The `.env` file is gitignored and contains sensitive secrets. Never commit it to version control.

### 3. Start All Services

```bash
cd infra
docker compose up -d --build
```

This will start **15 containers**:

| Container | Port | Description |
|-----------|------|-------------|
| `forgeflow-frontend` | `3000` | React frontend |
| `forgeflow-gateway` | `8090` | API Gateway (YARP) |
| `forgeflow-identity` | `8081` | Identity Service |
| `forgeflow-work` | `8082` | Work Service |
| `forgeflow-artifact` | `8084` | Artifact Service |
| `forgeflow-notification` | `8085` | Notification Service |
| `forgeflow-github` | `8086` | GitHub Service |
| `forgeflow-ai-orchestrator` | — | AI Orchestrator (Worker) |
| `forgeflow-rabbitmq` | `5672` / `15672` | Message Broker / Management UI |
| `forgeflow-mssql` | `1433` | SQL Server |
| `forgeflow-redis` | `6379` | Redis |
| `forgeflow-seq` | `5380` | Log Viewer |
| `forgeflow-smee` | — | Webhook Proxy |
| `forgeflow-db-init` | — | Database Initializer (runs once) |

### 4. Access the Application

| Service | URL |
|---------|-----|
| 🖥️ **Frontend** | http://localhost:3000 |
| 🌐 **API Gateway** | http://localhost:8090 |
| 📊 **Seq Logs** | http://localhost:5380 |
| 🐰 **RabbitMQ Management** | http://localhost:15672 (forgeflow / forgeflow) |

---

## 🔑 Environment Variables (`.env.example`)

The `infra/.env` file is required for the GitHub Service and AI Orchestrator. Create it from the example below:

```env
# ──────────────────────────────────────────────────────────────
# AI Provider Keys
# ──────────────────────────────────────────────────────────────
# Google Gemini API Key (required if using Gemini as AI provider)
# Get yours at: https://ai.google.dev/
GEMINI_API_KEY=your_gemini_api_key_here

# Groq API Key (optional, alternative AI provider)
# Get yours at: https://console.groq.com/
GROQ_API_KEY=your_groq_api_key_here

# ──────────────────────────────────────────────────────────────
# GitHub App Configuration
# ──────────────────────────────────────────────────────────────
# GitHub App ID (Settings → General → App ID)
GITHUB_APP_ID=your_github_app_id

# GitHub Webhook Secret (used to verify incoming webhooks)
GITHUB_WEBHOOK_SECRET=your_webhook_secret

# GitHub App Private Key (Base64 encoded)
# 1. Download the .pem file from GitHub App settings
# 2. Encode it: base64 -w 0 your-app.private-key.pem
# 3. Paste the output here
GITHUB_PRIVATE_KEY_BASE64=your_base64_encoded_private_key

# ──────────────────────────────────────────────────────────────
# GitHub Webhook Proxy (Development Only)
# ──────────────────────────────────────────────────────────────
# Create a channel at https://smee.io/new
# This proxies GitHub webhooks to your local machine
GITHUB_WEBHOOK_PROXY_URL=https://smee.io/your_channel_id
```

### Creating a GitHub App

1. Go to **GitHub Settings → Developer Settings → GitHub Apps → New GitHub App**
2. Set the following:
   - **Homepage URL:** `http://localhost:3000`
   - **Webhook URL:** Your Smee.io proxy URL
   - **Webhook Secret:** A strong secret string (same as `GITHUB_WEBHOOK_SECRET`)
3. **Permissions required:**
   - Repository: `Contents` (Read & Write), `Pull requests` (Read & Write), `Metadata` (Read)
   - Subscribe to events: `Pull request`, `Push`
4. After creation, note the **App ID** and generate a **Private Key** (.pem file)
5. Base64 encode the private key:
   ```bash
   base64 -w 0 your-app.private-key.pem
   ```

---

## 🗄️ Database Architecture

ForgeFlow follows the **Database-per-Service** pattern. Each microservice owns its data:

| Database | Service | Purpose |
|----------|---------|---------|
| `ForgeFlow_Identity` | Identity Service | Users, roles, refresh tokens |
| `ForgeFlow_Work` | Work Service | Projects, issues, members, permissions |
| `ForgeFlow_Artifact` | Artifact Service | AI artifacts, revisions, audit logs |
| `ForgeFlow_AiOrchestrator` | AI Orchestrator | AI request tracking, idempotency |
| `ForgeFlow_GitHub` | GitHub Service | Installations, repository connections |

Databases are automatically created on first startup by the `db-init` container. Each service applies its own EF Core migrations on startup.

---

## 📸 Screenshots

> Add your screenshots to the `docs/screenshots/` directory and uncomment the relevant sections below.

### Recommended Screenshots

| # | Screenshot | Description | Suggested Filename |
|---|-----------|-------------|-------------------|
| 1 | **Login Page** | User authentication screen | `login.png` |
| 2 | **Dashboard** | Main dashboard with project list | `dashboard.png` |
| 3 | **Kanban Board** | Drag-and-drop issue board with status columns | `kanban-board.png` |
| 4 | **Issue Detail Modal** | Issue details with subtasks tab | `issue-detail.png` |
| 5 | **AI Plan Generation** | AI plan generation in progress (with progress bar) | `ai-plan-generation.png` |
| 6 | **AI Plan Result** | Generated plan with task breakdown | `ai-plan-result.png` |
| 7 | **AI Code Review** | Code review card with scores and findings | `ai-code-review.png` |
| 8 | **PR Status Badges** | Review cards showing Open/Merged/Closed badges | `pr-status-badges.png` |
| 9 | **Project Settings** | Project configuration with GitHub repo linking | `project-settings.png` |
| 10 | **GitHub Integration** | Branch creation notification + PR comment | `github-integration.png` |
| 11 | **RabbitMQ Dashboard** | Message broker queues and exchanges | `rabbitmq-dashboard.png` |
| 12 | **Seq Logs** | Centralized logging view | `seq-logs.png` |

Once you have your screenshots, uncomment the image tags in this README. Example:

```markdown
![Kanban Board](docs/screenshots/kanban-board.png)
```

---

## 🔄 CI/CD & Development

### Local Development

For local development without Docker, you can run individual services:

```bash
# Backend service (example: Work Service)
cd services/work/ForgeFlow.Work.Api
dotnet run

# Frontend
cd frontend
npm install
npm run dev    # → http://localhost:5173
```

### Useful Commands

```bash
# Build all services
docker compose build

# Start all services
docker compose up -d

# View logs for a specific service
docker logs forgeflow-github -f

# Restart a single service after code changes
docker compose up -d --build github

# Stop all services
docker compose down

# Stop and remove volumes (⚠️ deletes all data)
docker compose down -v
```

### Swagger Documentation

Each backend service exposes Swagger UI in Development mode:

| Service | Swagger URL |
|---------|-------------|
| Identity | http://localhost:8090/identity/swagger |
| Work | http://localhost:8090/work/swagger |
| Artifact | http://localhost:8090/artifact/swagger |

---

## 🧪 Testing

### Manual Testing Flow

1. **Register & Login** at `http://localhost:3000`
2. **Create a Project** and invite team members
3. **Link a GitHub Repository** in project settings
4. **Create an Issue** (Feature, Bug, Story, etc.)
5. **Generate AI Plan** — watch real-time progress via SignalR
6. **Assign the Issue** — feature branch is automatically created on GitHub
7. **Open a Pull Request** — AI code review is triggered automatically
8. **Merge the PR** — issue automatically moves to Done

---

## 📈 Roadmap

| Phase | Status | Features |
|-------|--------|----------|
| **Phase 1: Foundation** | ✅ Done | Microservices, Auth, Issue CRUD, Kanban, AI Plan, Artifact Versioning |
| **Phase 2: Workflow** | ✅ Done | GitHub Webhooks, In Review status, Branch automation, PR lifecycle |
| **Phase 3: AI Code Review** | ✅ Done | PR diff analysis, AI scoring, PR comments, real-time status updates |
| **Phase 4: CI/CD** | 🔜 Planned | GitHub Actions webhooks, build status tracking, quality gates |
| **Phase 5: Advanced** | 🔜 Planned | Release notes automation, sprint/milestone support, analytics dashboard |

---

## 🤝 Contributing

1. Fork the repository
2. Create the `infra/.env` file (see [Environment Variables](#-environment-variables-envexample))
3. Run `docker compose up -d --build` in the `infra/` directory
4. Create a feature branch: `git checkout -b feature/amazing-feature`
5. Commit your changes: `git commit -m 'Add amazing feature'`
6. Push to the branch: `git push origin feature/amazing-feature`
7. Open a Pull Request

### What You'll Need to Fork & Run

| Requirement | Required? | Notes |
|-------------|-----------|-------|
| Docker Desktop | ✅ Yes | All services run in containers |
| GitHub App | ⚠️ Optional | Required for GitHub integration features |
| Gemini API Key | ⚠️ Optional | Required for AI features (free tier available) |
| Smee.io Channel | ⚠️ Optional | Required for receiving GitHub webhooks locally |
| .NET 8 SDK | ❌ No | Only needed if running services outside Docker |
| Node.js 18+ | ❌ No | Only needed if running frontend outside Docker |

---

## 📜 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  <strong>ForgeFlow</strong> — Generate quality before you write code. 🚀
</p>
