# GitHub Integration - Kapsamlı Dokümantasyon

Bu doküman ForgeFlow'a GitHub entegrasyonunun nasıl ekleneceğini, diğer servislerle ilişkilerini, iş akışlarını ve gelecek planlarını detaylıca açıklar.

---

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [GitHub OAuth App vs GitHub App](#github-oauth-app-vs-github-app)
3. [Mimari Tasarım](#mimari-tasarım)
4. [Servisler Arası İlişkiler](#servisler-arası-ilişkiler)
5. [İş Akışları (Senaryolar)](#iş-akışları-senaryolar)
6. [GitHub App Kurulumu](#github-app-kurulumu)
7. [Teknik Implementasyon](#teknik-implementasyon)
8. [Gelecek Planları](#gelecek-planları)

---

## Genel Bakış

### Amaç

GitHub entegrasyonu ForgeFlow'un **code-aware** olmasını sağlayacak:

| Özellik | Açıklama |
|---------|----------|
| **Repository Bağlama** | Projelere GitHub repo'su bağlama |
| **Codebase Analizi** | AI'ın mevcut kodu anlayarak plan üretmesi |
| **PR Oluşturma** | Otomatik branch, commit ve PR açma |
| **Webhook Alma** | Push, PR events ile senkronizasyon |

### Hibrit Yaklaşım

```
┌──────────────────────────────────────────────────────────────┐
│                    ForgeFlow + GitHub                         │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌────────────────────┐      ┌────────────────────┐          │
│  │   GitHub OAuth      │      │    GitHub App      │          │
│  │   (Opsiyonel)       │      │    (Zorunlu)       │          │
│  ├────────────────────┤      ├────────────────────┤          │
│  │ ✓ Login with       │      │ ✓ Repo erişimi     │          │
│  │   GitHub           │      │ ✓ PR açma (bot)    │          │
│  │ ✓ Kullanıcı        │      │ ✓ Webhooks         │          │
│  │   bilgisi          │      │ ✓ Code okuma       │          │
│  └────────────────────┘      └────────────────────┘          │
│           │                           │                       │
│           ▼                           ▼                       │
│  "Ali GitHub ile                "forgeflow[bot]              │
│   login oldu"                   PR açtı"                     │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

## GitHub OAuth App vs GitHub App

### GitHub OAuth App

**Nasıl Çalışır?**
```
Kullanıcı "GitHub ile Giriş Yap" tıklar
        │
        ▼
GitHub login sayfasına yönlendirilir
        │
        ▼
İzin verir: "ForgeFlow şu bilgilere erişsin mi?"
        │
        ▼
ForgeFlow bir ACCESS TOKEN alır
        │
        ▼
Bu token ile KULLANICININ adına işlem yapar
```

**Örnek Senaryo:**
- Ali, ForgeFlow'a GitHub OAuth ile login oldu
- Token Ali'nin hesabına bağlı
- ForgeFlow, Ali'nin görebildiği tüm repo'lara erişebilir
- PR açarsa "Ali tarafından açıldı" görünür

**Özellikler:**
| Özellik | Değer |
|---------|-------|
| Kim olarak çalışır? | **Kullanıcı** adına |
| Scope | Kullanıcının tüm repo'ları (izin verdiği) |
| Rate Limit | Kullanıcının limiti paylaşılır |
| Webhook | ❌ Yok |
| Fine-grained permissions | ❌ Sınırlı |

---

### GitHub App

**Nasıl Çalışır?**
```
Admin, GitHub App'i Organization'a/Repo'ya install eder
        │
        ▼
Hangi repo'lara erişeceğini seçer
        │
        ▼
GitHub App kendi adına (bot olarak) çalışır
        │
        ▼
Her repo için ayrı installation token alır
```

**Örnek Senaryo:**
- Ali, ForgeFlow GitHub App'i "forgeflow-inc" org'una kurdu
- Sadece "backend" ve "frontend" repo'larını seçti
- ForgeFlow, BOT olarak bu 2 repo'ya erişir
- PR açarsa "forgeflow[bot] tarafından açıldı" görünür

**Özellikler:**
| Özellik | Değer |
|---------|-------|
| Kim olarak çalışır? | **Bot** olarak (kendi kimliği) |
| Scope | Sadece install edilen repo'lar |
| Rate Limit | App'e özel, çok yüksek (15,000+/saat) |
| Webhook | ✅ Var |
| Fine-grained permissions | ✅ Çok detaylı |

---

### Karşılaştırma Tablosu

| Özellik | OAuth App | GitHub App |
|---------|-----------|------------|
| **Identity** | Kullanıcı | Bot |
| **Scope** | Tüm repo'lar | Seçilen repo'lar |
| **Permission** | Genel (read, write) | Detaylı (issues:read, contents:write) |
| **Rate Limit** | 5,000/saat (kullanıcı) | 15,000+/saat |
| **Webhooks** | ❌ | ✅ |
| **Setup** | Kolay | Orta |
| **Security** | Daha riskli | Daha güvenli |
| **Use Case** | "Login with GitHub" | CI/CD, Bots, Integrations |

---

### ForgeFlow için Karar

| Yaklaşım | Kullanım |
|----------|----------|
| **GitHub App** | Repo erişimi, PR açma, webhook (ZORUNLU) |
| **GitHub OAuth** | "GitHub ile Giriş Yap" (OPSİYONEL, Phase 2) |

---

## Mimari Tasarım

### Sistem Mimarisi

```
┌────────────────────────────────────────────────────────────────┐
│                         ForgeFlow                               │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────┐    ┌─────────────┐    ┌──────────────────────┐    │
│  │ Gateway │───▶│   GitHub    │───▶│   GitHub API         │    │
│  │         │    │   Service   │    │   (api.github.com)   │    │
│  └────┬────┘    └──────┬──────┘    └──────────────────────┘    │
│       │                │                                        │
│       │                ▼                                        │
│       │        ┌─────────────┐                                  │
│       │        │  RabbitMQ   │                                  │
│       │        └──────┬──────┘                                  │
│       │               │                                         │
│       ▼               ▼                                         │
│  ┌─────────┐   ┌─────────────┐   ┌──────────────┐              │
│  │  Work   │◀─▶│     AI      │──▶│   Artifact   │              │
│  │ Service │   │ Orchestrator│   │   Service    │              │
│  └─────────┘   └─────────────┘   └──────────────┘              │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
                           ▲
                           │ Webhook
                           │
                    ┌──────┴──────┐
                    │   GitHub    │
                    │  (Events)   │
                    └─────────────┘
```

### GitHub App Sahipliği

**Kişisel Hesapla Oluşturma:**
```
Developer → github.com/settings/apps/new
          → "ForgeFlow" adında App oluşturdun

Sonuç:
├── App SENIN hesabına bağlı (owner: @username)
├── Sen App'i yönetebilirsin (credentials, permissions)
├── AMA kullanıcılar kendi repo'larına install edebilir
└── App çalışırken "forgeflow[bot]" olarak görünür
```

**Organization ile Oluşturma (Production):**
```
Organization → github.com/organizations/forgeflow-inc/settings/apps/new
             → "ForgeFlow" adında App oluşturdun

Sonuç:
├── App ORGANIZATION'a bağlı (owner: @forgeflow-inc)
├── Org admin'leri App'i yönetebilir
└── Daha profesyonel görünüm
```

---

## Servisler Arası İlişkiler

### Ecosystem Görünümü

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ForgeFlow Ecosystem                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐                                                        │
│  │   Gateway    │◄─────────── Kullanıcı Request'leri                    │
│  └──────┬───────┘                                                        │
│         │                                                                │
│         ├────────────────┬────────────────┬────────────────┐            │
│         ▼                ▼                ▼                ▼            │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐   │
│  │   Identity   │ │    Work      │ │   GitHub     │ │   Artifact   │   │
│  │   Service    │ │   Service    │ │   Service    │ │   Service    │   │
│  ├──────────────┤ ├──────────────┤ ├──────────────┤ ├──────────────┤   │
│  │ • Auth       │ │ • Projects   │ │ • Repos      │ │ • AI Plans   │   │
│  │ • Users      │ │ • Issues     │ │ • PRs        │ │ • Code       │   │
│  │ • Roles      │ │              │ │ • Webhooks   │ │   Revisions  │   │
│  └──────────────┘ └──────┬───────┘ └──────┬───────┘ └──────────────┘   │
│                          │                │                             │
│                          └────────┬───────┘                             │
│                                   ▼                                      │
│                          ┌─────────────────┐                            │
│                          │   RabbitMQ      │                            │
│                          └────────┬────────┘                            │
│                                   │                                      │
│                                   ▼                                      │
│                          ┌─────────────────┐                            │
│                          │      AI         │                            │
│                          │  Orchestrator   │                            │
│                          └─────────────────┘                            │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Servis Sorumlulukları

| Servis | GitHub Entegrasyonundaki Rol |
|--------|------------------------------|
| **GitHub Service** | Ana entegrasyon noktası, GitHub API ile iletişim |
| **Work Service** | Project'e repo bağlama, issue-PR ilişkisi |
| **AI Orchestrator** | Kod context'i alarak AI'a gönderme |
| **Artifact Service** | AI ürettiği kodu saklama |
| **Gateway** | Webhook endpoint, routing |

---

## İş Akışları (Senaryolar)

### Senaryo 1: GitHub App Kurulumu

**Kullanıcı ilk kez repo bağlamak istiyor**

```
┌──────────┐    ┌─────────┐    ┌────────────┐    ┌──────────┐
│ Kullanıcı │    │ Gateway │    │  GitHub    │    │  GitHub  │
│  (Ali)   │    │         │    │  Service   │    │   API    │
└─────┬────┘    └────┬────┘    └─────┬──────┘    └────┬─────┘
      │              │               │                │
      │ 1. "GitHub Bağla" tıklar     │                │
      │──────────────┼───────────────▶│                │
      │              │               │                │
      │◄─────────────┼───────────────│ 2. GitHub OAuth URL döner
      │ (Redirect)   │               │                │
      │              │               │                │
      │ 3. GitHub'a gider, App'i install eder         │
      │───────────────────────────────────────────────▶│
      │              │               │                │
      │              │               │ 4. Webhook: installation.created
      │              │               │◄───────────────│
      │              │               │                │
      │              │               │ 5. Installation kaydedilir
      │              │               │                │
      │◄─────────────┼───────────────│ 6. "Bağlantı başarılı!"
```

**Sonuç:** Kullanıcının GitHub installation'ı veritabanına kaydedildi.

---

### Senaryo 2: Proje'ye Repo Bağlama

**Kullanıcı mevcut projeye GitHub repo'su bağlıyor**

```
┌──────────┐    ┌─────────┐    ┌────────────┐    ┌────────────┐
│ Kullanıcı │    │ Gateway │    │  GitHub    │    │   Work     │
│  (Ali)   │    │         │    │  Service   │    │  Service   │
└─────┬────┘    └────┬────┘    └─────┬──────┘    └─────┬──────┘
      │              │               │                 │
      │ 1. GET /api/installations/{id}/repos           │
      │──────────────┼───────────────▶│                │
      │              │               │                │
      │◄─────────────┼───────────────│ 2. Repo listesi döner
      │ [backend, frontend, docs]    │                │
      │              │               │                │
      │ 3. POST /api/repositories/connect              │
      │    { projectId: "FORGE", repoId: 12345 }       │
      │──────────────┼───────────────▶│                │
      │              │               │                │
      │              │               │ 4. Work Service'e bildir
      │              │               │───────────────▶│
      │              │               │                │ Project güncelle
      │              │               │◄───────────────│
      │              │               │                │
      │◄─────────────┼───────────────│ 5. "Repo bağlandı!"
```

**Sonuç:** FORGE projesi artık `backend` repo'suyla bağlantılı.

---

### Senaryo 3: Issue → AI Plan (Code Context ile)

**Kullanıcı issue oluşturuyor, AI mevcut kodu analiz ederek plan yapıyor**

```
┌──────────┐   ┌────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐
│ Kullanıcı │   │  Work  │   │  RabbitMQ  │   │    AI      │   │  GitHub    │
│  (Ali)   │   │ Service│   │            │   │Orchestrator│   │  Service   │
└────┬─────┘   └───┬────┘   └─────┬──────┘   └─────┬──────┘   └─────┬──────┘
     │             │              │                │                │
     │ 1. POST /api/issues        │                │                │
     │ { title: "Add login" }     │                │                │
     │─────────────▶│              │                │                │
     │             │              │                │                │
     │             │ 2. AiPlanRequested event      │                │
     │             │─────────────▶│                │                │
     │             │              │                │                │
     │             │              │ 3. Event alındı │                │
     │             │              │───────────────▶│                │
     │             │              │                │                │
     │             │              │                │ 4. GET /repos/{id}/contents
     │             │              │                │───────────────▶│
     │             │              │                │                │
     │             │              │                │◄───────────────│
     │             │              │                │ 5. Dosya içerikleri
     │             │              │                │                │
     │             │              │                │ 6. AI'a gönder:
     │             │              │                │ - Issue description
     │             │              │                │ - Mevcut kod
     │             │              │                │ - Tech stack
     │             │              │                │                │
     │             │              │                │ 7. AI plan üretir
     │             │              │                │                │
     │             │              │ 8. AiPlanGenerated event         │
     │             │              │◄───────────────│                │
```

**Sonuç:** AI, mevcut kodu bilerek daha akıllı ve tutarlı plan üretiyor!

---

### Senaryo 4: Plan Onayı → PR Açma

**Kullanıcı AI planını onaylıyor, otomatik PR açılıyor**

```
┌──────────┐   ┌────────┐   ┌────────────┐   ┌────────────┐   ┌──────────┐
│ Kullanıcı │   │Artifact│   │  RabbitMQ  │   │  GitHub    │   │  GitHub  │
│  (Ali)   │   │Service │   │            │   │  Service   │   │   API    │
└────┬─────┘   └───┬────┘   └─────┬──────┘   └─────┬──────┘   └────┬─────┘
     │             │              │                │                │
     │ 1. POST /api/artifacts/{id}/approve         │                │
     │─────────────▶│              │                │                │
     │             │              │                │                │
     │             │ 2. AiPlanApproved event       │                │
     │             │─────────────▶│                │                │
     │             │              │                │                │
     │             │              │ 3. Event alındı │                │
     │             │              │───────────────▶│                │
     │             │              │                │                │
     │             │              │                │ 4. Branch oluştur
     │             │              │                │ (feature/FORGE-1)
     │             │              │                │───────────────▶│
     │             │              │                │                │
     │             │              │                │ 5. Dosyaları commit
     │             │              │                │───────────────▶│
     │             │              │                │                │
     │             │              │                │ 6. PR aç
     │             │              │                │───────────────▶│
     │             │              │                │                │
     │             │              │                │◄───────────────│
     │             │              │                │ PR #42 oluşturuldu
     │             │              │                │                │
     │             │              │ 7. PullRequestCreated event     │
     │             │              │◄───────────────│                │
     │             │              │                │                │
     │◄────────────┼──────────────┼────────────────│                │
     │ 8. "PR açıldı: github.com/repo/pull/42"    │                │
```

**Sonuç:** `forgeflow[bot]` otomatik PR açtı!

---

### Senaryo 5: Webhook - PR Merge Edildi

**Birisi PR'ı merge etti, ForgeFlow issue'yu kapatıyor**

```
┌──────────┐   ┌────────────┐   ┌────────────┐   ┌────────┐
│  GitHub  │   │  GitHub    │   │  RabbitMQ  │   │  Work  │
│   API    │   │  Service   │   │            │   │ Service│
└────┬─────┘   └─────┬──────┘   └─────┬──────┘   └───┬────┘
     │               │                │               │
     │ 1. Webhook: pull_request.merged               │
     │──────────────▶│                │               │
     │               │                │               │
     │               │ 2. PullRequestMerged event    │
     │               │───────────────▶│               │
     │               │                │               │
     │               │                │ 3. Event      │
     │               │                │──────────────▶│
     │               │                │               │
     │               │                │               │ Issue → Closed
```

**Sonuç:** PR merge = Issue otomatik kapandı!

---

### Senaryo Özet Tablosu

| Senaryo | Tetikleyen | GitHub Service Rolü | Diğer Servisler |
|---------|-----------|---------------------|-----------------|
| App Kurulumu | Kullanıcı | Installation kaydet | - |
| Repo Bağlama | Kullanıcı | Repo listele, bağla | Work Service |
| AI Context | Issue oluşturma | Kod dosyalarını çek | AI Orchestrator |
| PR Açma | Plan onayı | Branch, commit, PR | Artifact Service |
| PR Merge | GitHub webhook | Event publish | Work Service |

---

## GitHub App Kurulumu

### Manuel Adımlar (GitHub.com)

1. **GitHub'da App oluştur:** https://github.com/settings/apps/new

2. **Temel Ayarlar:**
   - **App Name:** `ForgeFlow`
   - **Homepage URL:** `http://localhost:8090`
   - **Webhook URL:** `https://<public-url>/api/github/webhook`
   - **Webhook Secret:** Random string (güvenli sakla!)

3. **Permissions:**
   | Permission | Access | Açıklama |
   |------------|--------|----------|
   | Contents | Read & Write | Dosya okuma/yazma |
   | Pull requests | Read & Write | PR açma/okuma |
   | Metadata | Read | Repo bilgileri |
   | Webhooks | Read | Webhook yönetimi |

4. **Events to Subscribe:**
   - ✅ Push
   - ✅ Pull request
   - ✅ Installation

5. **Credentials Kaydet:**
   - **App ID:** Sayısal ID
   - **Private Key:** `.pem` dosyası olarak indir
   - **Webhook Secret:** Oluşturduğun secret

---

## Teknik Implementasyon

### Proje Yapısı

```
services/github/
├── ForgeFlow.GitHub.Api/
│   ├── Controllers/
│   │   ├── InstallationsController.cs
│   │   ├── RepositoriesController.cs
│   │   └── WebhookController.cs
│   ├── Program.cs
│   └── Dockerfile
├── ForgeFlow.GitHub.Application/
│   ├── Abstractions/
│   │   └── IGitHubClient.cs
│   ├── Installations/
│   │   ├── Commands/
│   │   └── Queries/
│   └── Repositories/
│       ├── Commands/
│       └── Queries/
├── ForgeFlow.GitHub.Infrastructure/
│   ├── GitHub/
│   │   └── OctokitGitHubClient.cs
│   └── Persistence/
│       └── GitHubDbContext.cs
└── ForgeFlow.GitHub.Domain/
    └── Entities/
        ├── GitHubInstallation.cs
        └── RepositoryConnection.cs
```

### Entity'ler

```csharp
// GitHubInstallation - GitHub App kurulumu
public class GitHubInstallation
{
    public Guid Id { get; set; }
    public long InstallationId { get; set; }      // GitHub'dan gelen
    public string AccountLogin { get; set; }       // org veya user name
    public string AccountType { get; set; }        // "Organization" | "User"
    public string? AccessToken { get; set; }       // Cached token (encrypted)
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime InstalledAtUtc { get; set; }
}

// RepositoryConnection - Proje ↔ Repo bağlantısı
public class RepositoryConnection
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }            // Work Service'deki proje
    public long RepositoryId { get; set; }         // GitHub repo ID
    public string FullName { get; set; }           // "owner/repo"
    public string DefaultBranch { get; set; }
    public Guid InstallationId { get; set; }       // FK
}
```

### API Endpoints

| Method | Endpoint | Açıklama | Auth |
|--------|----------|----------|------|
| GET | `/api/installations` | Kurulu GitHub App'ler | ✅ |
| GET | `/api/installations/{id}/repos` | Installation'daki repo'lar | ✅ |
| POST | `/api/repositories/connect` | Repo'yu projeye bağla | ✅ |
| GET | `/api/repositories/{id}/contents` | Dosya içeriği | ✅ |
| GET | `/api/repositories/{id}/tree` | Dosya ağacı | ✅ |
| POST | `/api/repositories/{id}/branches` | Branch oluştur | ✅ |
| POST | `/api/repositories/{id}/pulls` | PR aç | ✅ |
| POST | `/api/github/webhook` | GitHub webhook | ❌ (Public) |

### Event'ler (RabbitMQ)

```csharp
// GitHub → ForgeFlow Events
public record GitHubPushReceived(
    long RepositoryId,
    string Ref,
    string Before,
    string After,
    string[] ModifiedFiles
);

public record GitHubPullRequestOpened(
    long RepositoryId,
    int PullNumber,
    string Title,
    string HeadBranch,
    string BaseBranch
);

public record GitHubPullRequestMerged(
    long RepositoryId,
    int PullNumber,
    string IssueKey  // "FORGE-1"
);

public record GitHubInstallationCreated(
    long InstallationId,
    string AccountLogin,
    string AccountType
);

// ForgeFlow → GitHub Events
public record CreatePullRequestRequested(
    Guid ProjectId,
    string IssueKey,
    string Title,
    string Description,
    Dictionary<string, string> FileChanges
);

public record PullRequestCreated(
    Guid ProjectId,
    string IssueKey,
    int PullNumber,
    string Url
);
```

### Docker Compose

```yaml
github:
  build:
    context: ..
    dockerfile: services/github/ForgeFlow.GitHub.Api/Dockerfile
  container_name: forgeflow-github
  ports:
    - "8086:8080"
  environment:
    ASPNETCORE_URLS: http://+:8080
    ConnectionStrings__GitHubDb: "Server=mssql;Database=ForgeFlow_GitHub;..."
    GitHub__AppId: "${GITHUB_APP_ID}"
    GitHub__PrivateKey: "${GITHUB_PRIVATE_KEY}"
    GitHub__WebhookSecret: "${GITHUB_WEBHOOK_SECRET}"
    RabbitMq__Host: rabbitmq
    Seq__ServerUrl: http://seq
  depends_on:
    - mssql
    - rabbitmq
  networks:
    - forgeflow-net
```

### Gateway Routes

```json
{
  "github": {
    "ClusterId": "github",
    "Match": { "Path": "/api/github/{**remainder}" },
    "AuthorizationPolicy": "Authenticated"
  },
  "github_root": {
    "ClusterId": "github",
    "Match": { "Path": "/api/github" },
    "AuthorizationPolicy": "Authenticated"
  },
  "github_webhook": {
    "ClusterId": "github",
    "Match": { "Path": "/api/github/webhook" }
  },
  "installations": {
    "ClusterId": "github",
    "Match": { "Path": "/api/installations/{**remainder}" },
    "AuthorizationPolicy": "Authenticated"
  },
  "repositories": {
    "ClusterId": "github",
    "Match": { "Path": "/api/repositories/{**remainder}" },
    "AuthorizationPolicy": "Authenticated"
  }
}
```

### Dependencies

| Package | Version | Kullanım |
|---------|---------|----------|
| Octokit | 13.0.1 | GitHub API client |
| Octokit.Webhooks.AspNetCore | 2.0.0 | Webhook handling |

---

## Gelecek Planları

### Phase 1: Temel Entegrasyon (Öncelik)
- [x] GitHub App oluşturma (manuel)
- [ ] GitHub Service microservice
- [ ] Installation yönetimi
- [ ] Repo bağlama
- [ ] Dosya okuma (AI context)

### Phase 2: PR Oluşturma
- [ ] Branch oluşturma
- [ ] File commit
- [ ] PR açma
- [ ] PR template

### Phase 3: Webhook Handler
- [ ] Push event
- [ ] PR event (opened, merged, closed)
- [ ] Installation event

### Phase 4: GitHub OAuth (Opsiyonel)
- [ ] "GitHub ile Giriş Yap"
- [ ] Identity Service entegrasyonu
- [ ] User profile senkronizasyonu

### Phase 5: Advanced Features
- [ ] Code review suggestions
- [ ] Automated code quality checks
- [ ] Branch protection integration
- [ ] GitHub Actions trigger

---

## Önemli Notlar

> ⚠️ **Webhook URL:** Development için ngrok veya smee.io gibi tunnel servisi gerekli - localhost GitHub'dan erişilemez.

> ⚠️ **Credentials Güvenliği:** App ID ve Private Key güvenli şekilde saklanmalı. Production'da Azure Key Vault veya AWS Secrets Manager önerilir.

> ⚠️ **Rate Limiting:** GitHub API rate limit'lerine dikkat edilmeli. Installation token'ları cache'lenmeli.

---

## Referanslar

- [GitHub Apps Documentation](https://docs.github.com/en/apps)
- [Octokit.NET](https://github.com/octokit/octokit.net)
- [GitHub Webhooks](https://docs.github.com/en/webhooks)
