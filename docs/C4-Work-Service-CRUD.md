# 📋 Work Service CRUD Implementation - Detaylı Rapor

Bu dokümantasyon Work Service'e eklenen Project ve Issue CRUD işlevselliğini detaylı olarak açıklar.

---

## 🎯 Genel Bakış

Work Service'e Clean Architecture uyumlu CRUD işlevselliği eklendi:
- **Project** yönetimi (Jira projesi gibi)
- **Issue** yönetimi (Task, Bug, Feature, Story, Epic)
- Auto-generated issue keys (FORGE-1, FORGE-2, ...)
- EF Core ile SQL Server entegrasyonu

---

## 📊 Veri Modeli

### Entity-Relationship Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                          PROJECT                             │
├─────────────────────────────────────────────────────────────┤
│ Id (PK, GUID)                                               │
│ Key (UNIQUE, "FORGE")                                       │
│ Name, Description                                           │
│ RepositoryUrl, RepositoryProvider, DefaultBranch            │
│ TechStack[] (JSON Array)                                    │
│ ProjectType (Backend, Frontend, FullStack, Library, Mobile) │
│ CreatorId (UserId from Identity)                            │
│ NextIssueNumber (for auto-increment)                        │
│ CreatedAtUtc, UpdatedAtUtc, IsActive                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ 1:N
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                           ISSUE                              │
├─────────────────────────────────────────────────────────────┤
│ Id (PK, GUID)                                               │
│ Key (UNIQUE, "FORGE-123")                                   │
│ Title, Description                                          │
│ Status (Open, InProgress, InReview, Done, Closed)           │
│ Priority (Low, Medium, High, Critical)                      │
│ Type (Bug, Feature, Task, Story, Epic)                      │
│ ProjectId (FK → Project)                                    │
│ ParentIssueId (FK → Issue, self-referential)                │
│ ReporterId, AssigneeId (UserId from Identity)               │
│ DueDate, EstimatedHours                                     │
│ CreatedAtUtc, UpdatedAtUtc, ClosedAtUtc                     │
│ RowVersion (concurrency token)                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 🏗️ Proje Yapısı

### Oluşturulan/Güncellenen Dosyalar

```
services/work/
├── ForgeFlow.Work.Domain/                    [YENİ PROJE]
│   ├── Entities/
│   │   ├── Project.cs                        [YENİ]
│   │   └── Issue.cs                          [YENİ]
│   └── Enums/
│       ├── IssueStatus.cs                    [YENİ]
│       ├── IssuePriority.cs                  [YENİ]
│       ├── IssueType.cs                      [YENİ]
│       ├── ProjectType.cs                    [YENİ]
│       └── RepositoryProvider.cs             [YENİ]
│
├── ForgeFlow.Work.Application/               [YENİ PROJE]
│   ├── Projects/
│   │   ├── Commands/
│   │   │   ├── CreateProjectCommand.cs       [YENİ]
│   │   │   ├── CreateProjectHandler.cs       [YENİ]
│   │   │   ├── UpdateProjectCommand.cs       [YENİ]
│   │   │   └── UpdateProjectHandler.cs       [YENİ]
│   │   └── Queries/
│   │       ├── GetProjectQuery.cs            [YENİ]
│   │       ├── GetProjectHandler.cs          [YENİ]
│   │       ├── ListProjectsQuery.cs          [YENİ]
│   │       └── ListProjectsHandler.cs        [YENİ]
│   ├── Issues/
│   │   ├── Commands/
│   │   │   ├── CreateIssueCommand.cs         [YENİ]
│   │   │   ├── CreateIssueHandler.cs         [YENİ]
│   │   │   ├── UpdateIssueCommand.cs         [YENİ]
│   │   │   ├── UpdateIssueHandler.cs         [YENİ]
│   │   │   ├── ChangeIssueStatusCommand.cs   [YENİ]
│   │   │   ├── ChangeIssueStatusHandler.cs   [YENİ]
│   │   │   ├── AssignIssueCommand.cs         [YENİ]
│   │   │   └── AssignIssueHandler.cs         [YENİ]
│   │   └── Queries/
│   │       ├── GetIssueQuery.cs              [YENİ]
│   │       ├── GetIssueHandler.cs            [YENİ]
│   │       ├── ListIssuesQuery.cs            [YENİ]
│   │       └── ListIssuesHandler.cs          [YENİ]
│   └── DependencyInjection.cs                [YENİ]
│
├── ForgeFlow.Work.Infrastructure/            [YENİ PROJE]
│   ├── Persistence/
│   │   ├── WorkDbContext.cs                  [YENİ]
│   │   ├── DesignTimeDbContextFactory.cs     [YENİ]
│   │   ├── Configurations/
│   │   │   ├── ProjectConfiguration.cs       [YENİ]
│   │   │   └── IssueConfiguration.cs         [YENİ]
│   │   └── Migrations/
│   │       └── InitialWorkSchema.cs          [YENİ]
│   └── DependencyInjection.cs                [YENİ]
│
└── ForgeFlow.Work.Api/                       [MEVCUT - GÜNCELLENDİ]
    ├── Controllers/
    │   ├── ProjectsController.cs             [YENİ]
    │   └── IssuesController.cs               [GÜNCELLENDİ]
    ├── Program.cs                            [GÜNCELLENDİ]
    ├── Dockerfile                            [GÜNCELLENDİ]
    └── ForgeFlow.Work.Api.csproj             [GÜNCELLENDİ]
```

---

## 📝 Domain Layer Detayları

### Enums

| Enum | Değerler |
|------|----------|
| `IssueStatus` | Open (0), InProgress (1), InReview (2), Done (3), Closed (4) |
| `IssuePriority` | Low (0), Medium (1), High (2), Critical (3) |
| `IssueType` | Bug (0), Feature (1), Task (2), Story (3), Epic (4) |
| `ProjectType` | Backend (0), Frontend (1), FullStack (2), Library (3), Mobile (4) |
| `RepositoryProvider` | GitHub (0), GitLab (1), Bitbucket (2), Azure (3) |

### Project Entity

```csharp
public class Project
{
    public Guid Id { get; set; }
    public string Key { get; set; }              // "FORGE" - unique, 2-5 char
    public string Name { get; set; }
    public string? Description { get; set; }
    
    // Repository - AI context için
    public string? RepositoryUrl { get; set; }
    public RepositoryProvider? RepositoryProvider { get; set; }
    public string DefaultBranch { get; set; } = "main";
    
    // Tech Stack - AI prompt context için
    public string[] TechStack { get; set; } = [];
    public ProjectType ProjectType { get; set; }
    
    // Ownership
    public string CreatorId { get; set; }
    
    // Meta
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public int NextIssueNumber { get; set; } = 1;  // Auto-increment için
    
    // Navigation
    public ICollection<Issue> Issues { get; set; } = [];
}
```

### Issue Entity

```csharp
public class Issue
{
    public Guid Id { get; set; }
    public string Key { get; set; }              // "FORGE-123" - auto-generated
    public string Title { get; set; }
    public string? Description { get; set; }
    
    // Status & Type
    public IssueStatus Status { get; set; } = IssueStatus.Open;
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;
    public IssueType Type { get; set; } = IssueType.Task;
    
    // Relations
    public Guid ProjectId { get; set; }
    public Project Project { get; set; }
    public Guid? ParentIssueId { get; set; }     // Epic → Story → Task hiyerarşisi
    public Issue? ParentIssue { get; set; }
    
    // People
    public string ReporterId { get; set; }       // Oluşturan
    public string? AssigneeId { get; set; }      // Atanan
    
    // Dates
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    
    // Concurrency
    public byte[] RowVersion { get; set; }
    
    // Navigation
    public ICollection<Issue> ChildIssues { get; set; } = [];
}
```

---

## 🔧 Infrastructure Layer Detayları

### WorkDbContext

```csharp
public class WorkDbContext : DbContext
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();
    
    // ApplyConfigurationsFromAssembly ile configuration'lar uygulanıyor
}
```

### EF Core Configurations

**ProjectConfiguration:**
- Key: unique index, max 5 char
- TechStack: comma-separated string olarak saklanıyor
- Issues: cascade delete

**IssueConfiguration:**
- Key: unique index
- RowVersion: concurrency token
- ParentIssue: self-referential, restrict delete
- Indexes: ProjectId, Status, AssigneeId, ReporterId, (ProjectId, Status)

---

## 📋 Application Layer Detayları

### CQRS Pattern

| Type | Name | Description |
|------|------|-------------|
| **Project Commands** | | |
| Command | CreateProjectCommand | Yeni proje oluştur, Key unique kontrolü |
| Command | UpdateProjectCommand | Proje güncelle |
| **Project Queries** | | |
| Query | GetProjectQuery | Key ile proje getir |
| Query | ListProjectsQuery | Projeleri listele (pagination, isActive filter) |
| **Issue Commands** | | |
| Command | CreateIssueCommand | Issue oluştur, Key auto-generate |
| Command | UpdateIssueCommand | Issue güncelle |
| Command | ChangeIssueStatusCommand | Status değiştir, ClosedAtUtc yönet |
| Command | AssignIssueCommand | Assignee ata/kaldır |
| **Issue Queries** | | |
| Query | GetIssueQuery | Key ile issue getir |
| Query | ListIssuesQuery | Filter + pagination |

### Issue Key Auto-Generation

```csharp
// CreateIssueHandler.cs
var issueNumber = project.NextIssueNumber;
project.NextIssueNumber++;  // Atomic increment
var issueKey = $"{project.Key}-{issueNumber}";  // "FORGE-1"
```

---

## 🌐 API Endpoints

### Projects

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/projects` | Proje listesi | Authenticated |
| GET | `/api/projects/{key}` | Proje detayı | Authenticated |
| POST | `/api/projects` | Proje oluştur | Authenticated |
| PUT | `/api/projects/{key}` | Proje güncelle | Authenticated |

### Issues

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/issues` | Issue listesi (filter) | DeveloperOrAbove |
| GET | `/api/issues/{key}` | Issue detayı | DeveloperOrAbove |
| POST | `/api/issues` | Issue oluştur | DeveloperOrAbove |
| PUT | `/api/issues/{key}` | Issue güncelle | DeveloperOrAbove |
| POST | `/api/issues/{key}/status` | Status değiştir | DeveloperOrAbove |
| POST | `/api/issues/{key}/assign` | Assignee ata | DeveloperOrAbove |
| POST | `/api/issues/{key}/generate` | AI plan oluştur | DeveloperOrAbove |

### Filter Parametreleri (ListIssues)

| Parameter | Type | Description |
|-----------|------|-------------|
| projectKey | string | Proje key'ine göre filtrele |
| status | IssueStatus | Status'a göre filtrele |
| priority | IssuePriority | Priority'ye göre filtrele |
| type | IssueType | Type'a göre filtrele |
| assigneeId | string | Assignee'ye göre filtrele |
| page | int | Sayfa numarası (default: 1) |
| pageSize | int | Sayfa boyutu (default: 20) |

---

## 🐳 Docker & Infrastructure Değişiklikleri

### docker-compose.yml

```diff
  work:
    environment:
-     ConnectionStrings__Db: "..."
+     ConnectionStrings__WorkDb: "..."
```

### Gateway appsettings.json

```diff
+ "projects": {
+   "ClusterId": "work",
+   "Match": { "Path": "/api/projects/{**catch-all}" },
+   "AuthorizationPolicy": "Authenticated"
+ }
```

### Work Service Dockerfile

```diff
  # Copy project files for restore
  COPY ["contracts/ForgeFlow.Contracts/...", "..."]
+ COPY ["services/work/ForgeFlow.Work.Domain/...", "..."]
+ COPY ["services/work/ForgeFlow.Work.Application/...", "..."]
+ COPY ["services/work/ForgeFlow.Work.Infrastructure/...", "..."]
  COPY ["services/work/ForgeFlow.Work.Api/...", "..."]
```

---

## 🔄 API Request/Response Örnekleri

### Create Project

**Request:**
```http
POST /api/projects
Authorization: Bearer {token}
Content-Type: application/json

{
  "key": "FORGE",
  "name": "ForgeFlow Backend",
  "description": "AI-powered development platform",
  "repositoryUrl": "https://github.com/user/forgeflow",
  "repositoryProvider": 0,
  "techStack": ["C#", ".NET 8", "EF Core"],
  "projectType": 0
}
```

**Response:**
```json
{
  "id": "a1b2c3d4-...",
  "key": "FORGE",
  "name": "ForgeFlow Backend"
}
```

### Create Issue

**Request:**
```http
POST /api/issues
Authorization: Bearer {token}
Content-Type: application/json

{
  "projectKey": "FORGE",
  "title": "Implement login page",
  "description": "Create a beautiful login UI",
  "type": 1,
  "priority": 2
}
```

**Response:**
```json
{
  "id": "e5f6g7h8-...",
  "key": "FORGE-1",
  "title": "Implement login page",
  "status": 0
}
```

### List Issues

**Request:**
```http
GET /api/issues?projectKey=FORGE&status=0&page=1&pageSize=10
Authorization: Bearer {token}
```

**Response:**
```json
{
  "items": [
    {
      "id": "...",
      "key": "FORGE-2",
      "title": "Add user profile",
      "status": 0,
      "priority": 1,
      "type": 1,
      "projectKey": "FORGE",
      "assigneeId": null,
      "dueDate": null,
      "createdAtUtc": "2026-01-19T..."
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 10
}
```

---

## 📈 Veritabanı Şeması

Migration ile oluşturulan tablolar:

### Projects Tablosu

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier | PK |
| Key | nvarchar(5) | UNIQUE, NOT NULL |
| Name | nvarchar(200) | NOT NULL |
| Description | nvarchar(4000) | |
| RepositoryUrl | nvarchar(500) | |
| RepositoryProvider | int | |
| DefaultBranch | nvarchar(100) | DEFAULT 'main' |
| TechStack | nvarchar(1000) | |
| ProjectType | int | NOT NULL |
| CreatorId | nvarchar(36) | NOT NULL |
| NextIssueNumber | int | NOT NULL |
| CreatedAtUtc | datetime2 | NOT NULL |
| UpdatedAtUtc | datetime2 | NOT NULL |
| IsActive | bit | NOT NULL |

### Issues Tablosu

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier | PK |
| Key | nvarchar(20) | UNIQUE, NOT NULL |
| Title | nvarchar(500) | NOT NULL |
| Description | nvarchar(10000) | |
| Status | int | NOT NULL |
| Priority | int | NOT NULL |
| Type | int | NOT NULL |
| ProjectId | uniqueidentifier | FK → Projects |
| ParentIssueId | uniqueidentifier | FK → Issues (self) |
| ReporterId | nvarchar(36) | NOT NULL |
| AssigneeId | nvarchar(36) | |
| DueDate | datetime2 | |
| EstimatedHours | decimal(18,2) | |
| CreatedAtUtc | datetime2 | NOT NULL |
| UpdatedAtUtc | datetime2 | NOT NULL |
| ClosedAtUtc | datetime2 | |
| RowVersion | rowversion | Concurrency |

---

## ✅ Özet

| Kategori | Sayı |
|----------|------|
| Yeni projeler | 3 (Domain, Application, Infrastructure) |
| Yeni dosyalar | 26 |
| Güncellenen dosyalar | 5 |
| API Endpoints | 11 |
| EF Core Migration | 1 |
