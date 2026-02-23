# Faz 4: CI/CD Entegrasyon — Detaylı İmplementasyon Planı

ForgeFlow'un CI/CD pipeline durumunu (GitHub Actions) izlemesi, issue'lar ile ilişkilendirmesi ve Quality Gate mekanizmasıyla tam döngüyü kapatması.

---

## Genel Mimari — Nasıl Çalışacak?

```
GitHub Actions Workflow Çalışır
         │
         ▼
  GitHub Webhook (check_suite / workflow_run / check_run)
         │
         ▼
  WebhookController (GitHub Service)
    ├── Payload parse + branch'ten IssueKey çıkar
    └── Publish: CiCdStatusReceived event
              │
         ┌────┴─────┐
         ▼          ▼
  Work Service    Notification Service
    │                    │
    │  Issue'ya       SignalR: CiCdUpdate
    │  CiCdStatus       → Frontend
    │  alanını set        real-time
    │                    badge güncelle
    ▼
  Artifact Service
    │
    │  CI/CD sonuçlarını
    │  "CI_CD_RESULT"
    │  artifact olarak
    │  versiyonlayarak sakla
    ▼
  Quality Gate Kontrolü
    │
    │  AI Review ✅ + CI/CD ✅ → Done'a geçiş izni
    │  Herhangi biri ❌ → Done'a geçiş engeli
    ▼
  Frontend
    ├── Issue kartında CI/CD badge (✅ ❌ ⏳)
    ├── Issue detayda pipeline tab
    └── Quality Gate göstergesi
```

---

## Adım 1: Event Contracts — Shared Sözleşmeler

> **Prensip:** Tüm servisler arası iletişim `ForgeFlow.Contracts` üzerinden tip-güvenli olmalı.

### [MODIFY] `NotificationEvents.cs`

Yeni event record'ları:

```csharp
/// GitHub Actions workflow sonucu — WebhookController tarafından yayınlanır.
public record CiCdStatusReceived(
    string IssueKey,
    long RepositoryId,
    string WorkflowName,      // "build-and-test", "deploy-staging" vb.
    string Status,            // "queued", "in_progress", "completed"
    string? Conclusion,       // "success", "failure", "cancelled", "skipped" (status=completed ise)
    string BranchName,
    string CommitSha,
    string? HtmlUrl,          // GitHub Actions run URL
    long RunId,               // Workflow run ID (idempotency için)
    DateTime Timestamp
);

/// Work Service → Notification Service — Real-time CI/CD badge güncelleme
public record CiCdStatusUpdated(
    string IssueKey,
    Guid ProjectId,
    string WorkflowName,
    string Status,            // "queued", "in_progress", "success", "failure", "cancelled"
    string? HtmlUrl,
    DateTime Timestamp
);
```

**Neden bu yapı?**
- `CiCdStatusReceived`: Ham GitHub webhook verisini taşır → Work + Artifact tüketir
- `CiCdStatusUpdated`: İşlenmiş, issue'ya bağlanmış sonuç → Notification tüketir, SignalR ile frontend'e iletir
- Aynı pattern: `GitHubPullRequestOpened` → `IssueStatusChanged` → `BoardUpdate` akışı ile tutarlı

---

## Adım 2: GitHub Service — Webhook Handler

> **Prensip:** Mevcut `WebhookController.cs` pattern'ını koru. Yeni case → yeni handler metodu.

### [MODIFY] `WebhookController.cs`

`HandleWebhook` switch case'ine yeni event tipleri eklenir:

```csharp
case "check_suite":
    await HandleCheckSuiteEvent(payload);
    break;
case "workflow_run":
    await HandleWorkflowRunEvent(payload);
    break;
```

#### `HandleCheckSuiteEvent` Metodu

```
GitHub check_suite payload'ı:
  action: "requested" | "rerequested" | "completed"
  check_suite.head_branch → branch adı → IssueKey çıkar
  check_suite.conclusion → "success" | "failure" | "cancelled"
  check_suite.head_sha → commit SHA
  repository.id → RepositoryId
```

**Akış:**
1. `action` check: Sadece `"completed"` durumunda event publish et (noise azaltma)
2. `head_branch`'ten `ExtractIssueKey()` ile issue key çıkar (mevcut metod, değiştirilmez)
3. `CiCdStatusReceived` event publish et

#### `HandleWorkflowRunEvent` Metodu

```
GitHub workflow_run payload'ı:
  action: "requested" | "in_progress" | "completed"
  workflow_run.name → "Build & Test" gibi workflow adı
  workflow_run.head_branch → branch adı
  workflow_run.conclusion → "success" | "failure" | "cancelled"
  workflow_run.html_url → GitHub Actions run linkiö
  workflow_run.id → run ID (idempotency)
  workflow_run.head_sha → commit SHA
```

**Akış:**
1. Tüm `action` değerlerini işle (queued → in_progress → completed takibi):
   - `"requested"` → status: `"queued"`
   - `"in_progress"` → status: `"in_progress"`
   - `"completed"` → status: `"completed"`, conclusion alanı dolu
2. `head_branch`'ten `ExtractIssueKey()` ile issue key çıkar
3. IssueKey boşsa → log + return (ForgeFlow ile ilişkili değil)
4. `CiCdStatusReceived` event publish et

> [!IMPORTANT]
> **GitHub App Permission Gereksinimi:** GitHub App ayarlarından `Actions: Read` ve `Checks: Read` permission'ları eklenecek. Webhook subscription'a `check_suite` ve `workflow_run` event'leri eklenecek.

---

## Adım 3: Work Service — CI/CD Status Takibi

> **Prensip:** Onion Architecture. Domain entity'ye yeni alan → Application'a command → Consumer MediatR ile dispatch.

### [MODIFY] `Issue.cs` (Domain Entity)

Issue entity'sine CI/CD durumu alanları eklenir:

```csharp
// ========== CI/CD Integration ==========

/// <summary>
/// Son CI/CD pipeline durumu (success, failure, in_progress, queued)
/// </summary>
public string? CiCdStatus { get; set; }

/// <summary>
/// Son CI/CD pipeline workflow adı
/// </summary>
public string? CiCdWorkflowName { get; set; }

/// <summary>
/// GitHub Actions run URL'i
/// </summary>
public string? CiCdRunUrl { get; set; }

/// <summary>
/// Son CI/CD güncelleme zamanı
/// </summary>
public DateTime? CiCdUpdatedAtUtc { get; set; }
```

**Neden ayrı tablo değil?** Issue entity'sine ekleme sebebi:
- CI/CD durumu issue'nun doğrudan bir property'si (1:1 ilişki)
- Kanban board'da gösterilmesi gerekiyor (ayrı sorgu gerektirmez)
- Quality Gate kontrolünde hızlı erişim

### [NEW] `UpdateCiCdStatusCommand.cs` (Application Layer)

```csharp
public record UpdateCiCdStatusCommand(
    string IssueKey,
    string WorkflowName,
    string Status,        // "queued", "in_progress", "success", "failure", "cancelled"
    string? HtmlUrl,
    string CommitSha,
    long RunId
) : IRequest<UpdateCiCdStatusResult>;

public record UpdateCiCdStatusResult(
    string IssueKey,
    Guid ProjectId,
    string Status
);
```

### [NEW] `UpdateCiCdStatusHandler.cs` (Application Layer)

**Akış:**
1. Issue'yu `Key` ile bul (mevcut repository pattern)
2. `CiCdStatus`, `CiCdWorkflowName`, `CiCdRunUrl`, `CiCdUpdatedAtUtc` güncelle
3. Status mapping: `completed` + `conclusion` birleşimi → `success` / `failure` / `cancelled`
4. `SaveChangesAsync()`
5. `CiCdStatusUpdated` event publish et (Notification Service'e bildir)
6. Return `UpdateCiCdStatusResult`

**Validasyonlar:**
- Issue bulunamadı → `InvalidOperationException` (mevcut pattern ile tutarlı)
- Aynı `RunId` ile tekrar gelirse → idempotent: sadece güncelle, hata verme

### [NEW] `CiCdStatusReceivedConsumer.cs` (Work.Api)

Mevcut `PullRequestOpenedConsumer` pattern'ını takip eder:

```csharp
public class CiCdStatusReceivedConsumer : IConsumer<CiCdStatusReceived>
{
    // IMediator + ILogger injection (mevcut pattern)
    
    public async Task Consume(ConsumeContext<CiCdStatusReceived> context)
    {
        var msg = context.Message;
        
        // Status mapping
        var status = msg.Status == "completed" 
            ? (msg.Conclusion ?? "unknown") 
            : msg.Status;
        
        var result = await _mediator.Send(new UpdateCiCdStatusCommand(
            Key: msg.IssueKey,
            WorkflowName: msg.WorkflowName,
            Status: status,
            HtmlUrl: msg.HtmlUrl,
            CommitSha: msg.CommitSha,
            RunId: msg.RunId
        ));
    }
}
```

### [MODIFY] `Program.cs` (Work.Api)

Consumer kaydı:
```csharp
x.AddConsumer<CiCdStatusReceivedConsumer>();
```

### [MODIFY] EF Migration

`Issue` entity'sine yeni alanlar eklendiği için migration gerekli:
```bash
dotnet ef migrations add AddCiCdFields -p ForgeFlow.Work.Infrastructure -s ForgeFlow.Work.Api
```

---

## Adım 4: Artifact Service — CI/CD Sonuç Saklama

> **Prensip:** Mevcut `AiPlanGenerated` → `UpsertArtifactRevision` pattern'ını takip et.

### [NEW] `CiCdStatusReceivedConsumer.cs` (Artifact.Api)

CI/CD sonuçlarını artifact olarak saklar:

**Akış:**
1. `CiCdStatusReceived` event'i tüket
2. Sadece `status == "completed"` olanları sakla (ara durumlar artifact olmaz)
3. Content JSON oluştur:
   ```json
   {
     "workflowName": "Build & Test",
     "conclusion": "success",
     "commitSha": "abc123",
     "htmlUrl": "https://github.com/.../actions/runs/123",
     "runId": 123456,
     "timestamp": "2026-02-20T15:00:00Z"
   }
   ```
4. `UpsertArtifactRevisionCommand` ile kaydet:
   - `ArtifactType`: `"CI_CD_RESULT"`
   - `CorrelationId`: `"run-{RunId}"` (idempotent upsert)
   - `IssueKey`: webhook'tan gelen key
5. İçerik hash ile dedup (mevcut mekanizma)

### [MODIFY] `Program.cs` (Artifact.Api)

Consumer kaydı eklenir.

---

## Adım 5: Notification Service — Real-time CI/CD Badge

> **Prensip:** Mevcut `IssueChangedConsumer` → SignalR → `BoardUpdate` pattern'ını takip et.

### [NEW] `CiCdStatusUpdatedConsumer.cs`

```csharp
public class CiCdStatusUpdatedConsumer : IConsumer<CiCdStatusUpdated>
{
    // IHubContext<ForgeHub> + ILogger (mevcut pattern)
    
    public async Task Consume(ConsumeContext<CiCdStatusUpdated> context)
    {
        var msg = context.Message;
        
        // Project key çıkar (issue key'den: "PROJ-123" → "PROJ")
        var projectKey = msg.IssueKey.Split('-')[0];
        
        // SignalR ile frontend'e bildir
        await _hubContext.Clients
            .Group($"project:{projectKey}")
            .SendAsync("CiCdUpdate", new {
                IssueKey = msg.IssueKey,
                ProjectId = msg.ProjectId,
                WorkflowName = msg.WorkflowName,
                Status = msg.Status,
                HtmlUrl = msg.HtmlUrl,
                Timestamp = msg.Timestamp
            });
    }
}
```

### [MODIFY] `Program.cs` (Notification.Service)

Consumer kaydı eklenir.

---

## Adım 6: Frontend — CI/CD Status Görüntüleme

> **Prensip:** Mevcut `signalRService.ts` → `IssueCard.tsx` / `IssueDetailModal.tsx` pattern'ını takip et.

### [MODIFY] `signalRService.ts`

Yeni event tipi ve handler:

```typescript
interface CiCdUpdateMessage {
  issueKey: string;
  projectId: string;
  workflowName: string;
  status: string;     // "queued" | "in_progress" | "success" | "failure" | "cancelled"
  htmlUrl?: string;
  timestamp: string;
}

// Connection setup'a ekle:
connection.on("CiCdUpdate", handler);

// Subscribe method:
onCiCdUpdate(callback): () => void
```

### [MODIFY] `IssueCard.tsx` — Kanban Kartında CI/CD Badge

Issue kartının sağ alt köşesine küçük bir CI/CD badge eklenir:

| Status | Badge | Renk |
|--------|-------|------|
| `queued` | ⏳ | Gri |
| `in_progress` | 🔄 | Sarı (animasyonlu) |
| `success` | ✅ | Yeşil |
| `failure` | ❌ | Kırmızı |
| `cancelled` | ⊘ | Gri |

Badge'e tıklanınca GitHub Actions URL'ine yönlendirir (`htmlUrl`).

### [MODIFY] `IssueDetailModal.tsx` — Pipeline Tab

Mevcut "Reviews" tab'ının yanına **"Pipeline"** tab'ı eklenir:

**Tab İçeriği:**
- Son pipeline durumu kartı (büyük badge + workflow adı + commit SHA)
- Eğer artifact varsa: Geçmiş pipeline sonuçları listesi (Artifact Service'ten çekilir)
- GitHub Actions linki (dış link ikonu ile)
- Quality Gate durumu (bkz. Adım 7)

### [MODIFY] `ProjectBoard.tsx`

Kanban board'a CI/CD status verisi eklenir:
- Mevcut issue fetch API response'una `ciCdStatus` alanı dahil edilir
- `CiCdUpdate` SignalR event'i dinlenerek real-time güncelleme yapılır

---

## Adım 7: Quality Gate Mekanizması

> **Prensip:** Mevcut `ChangeIssueStatusCommand` flow'unu genişlet. Yeni bir servis veya entity gerektirmez.

### Nasıl Çalışır?

Quality Gate, issue'nun Done'a geçişini belirli koşullara bağlar:

```
Done'a geçiş kontrolü:
  1. CI/CD Status == "success" ?      → ✅ / ❌
  2. AI Code Review mevcut mu?        → ✅ / ❌  (opsiyonel)
  3. Tüm koşullar sağlanıyor mu?      → Done'a geçiş izni / engel
```

### [MODIFY] `ChangeIssueStatusHandler.cs` (Work.Application)

Mevcut handler'a Quality Gate kontrolü eklenir:

```csharp
// Eğer yeni durum "Done" ise ve bu bir manual geçiş ise:
if (command.NewStatus == IssueStatus.Done && !command.IsSystemAction)
{
    // Quality Gate kontrolü
    if (issue.CiCdStatus != null && issue.CiCdStatus != "success")
    {
        throw new InvalidOperationException(
            $"Quality Gate: CI/CD pipeline durumu '{issue.CiCdStatus}'. " +
            "Done'a geçiş için CI/CD pipeline'ın başarılı olması gerekiyor.");
    }
}
```

**Önemli Noktalar:**
- `IsSystemAction == true` → Quality Gate atlanır (PR merge ile otomatik Done geçişi engellenMEZ)
- CI/CD status `null` → Kontrol atlanır (CI/CD entegrasyonu yoksa engelleme yapılmaz)
- Sadece "failure" durumunda engel → "in_progress" veya "queued" durumunda da engeller (pipeline bitmeli)
- Admin override: Gelecekte `BypassQualityGate` flag eklenebilir

### [MODIFY] Frontend — Quality Gate UI

`IssueDetailModal.tsx` "Pipeline" tabında Quality Gate durumu gösterilir:

```
┌─────────────────────────────────────────┐
│ 🛡️ Quality Gate                       │
│                                         │
│  CI/CD Pipeline    ✅ success           │
│  AI Code Review    ✅ 8.5/10           │
│                                         │
│  Status: ✅ Ready to merge              │
│  (veya)                                 │
│  Status: ❌ Pipeline failed — Done'a    │
│           geçiş engellendi              │
└─────────────────────────────────────────┘
```

---

## Adım 8: Work Service API — Issue DTO Güncelleme

### [MODIFY] Issue DTO'ları

CI/CD alanlarını frontend'e döndür:

```csharp
// GetIssueByIdHandler / GetIssuesByProjectHandler response'larına ekle:
public string? CiCdStatus { get; set; }
public string? CiCdWorkflowName { get; set; }
public string? CiCdRunUrl { get; set; }
public DateTime? CiCdUpdatedAtUtc { get; set; }
```

---

## Dosya Değişiklik Özeti

| # | Servis | Dosya | İşlem |
|---|--------|-------|-------|
| 1 | Contracts | `NotificationEvents.cs` | `CiCdStatusReceived` + `CiCdStatusUpdated` event'leri ekle |
| 2 | GitHub Service | `WebhookController.cs` | `check_suite` + `workflow_run` handler'ları ekle |
| 3 | Work Service | `Issue.cs` | `CiCdStatus`, `CiCdWorkflowName`, `CiCdRunUrl`, `CiCdUpdatedAtUtc` alanları ekle |
| 4 | Work Service | `UpdateCiCdStatusCommand.cs` | [NEW] CQRS command + result |
| 5 | Work Service | `UpdateCiCdStatusHandler.cs` | [NEW] Command handler |
| 6 | Work Service | `CiCdStatusReceivedConsumer.cs` | [NEW] MassTransit consumer |
| 7 | Work Service | `ChangeIssueStatusHandler.cs` | Quality Gate kontrolü ekle |
| 8 | Work Service | `Program.cs` | Consumer kaydı |
| 9 | Work Service | Issue DTO'ları | CI/CD alanlarını ekle |
| 10 | Work Service | EF Migration | Yeni alanlar için migration |
| 11 | Artifact Service | `CiCdStatusReceivedConsumer.cs` | [NEW] CI/CD sonuçlarını artifact olarak sakla |
| 12 | Artifact Service | `Program.cs` | Consumer kaydı |
| 13 | Notification Service | `CiCdStatusUpdatedConsumer.cs` | [NEW] SignalR broadcast |
| 14 | Notification Service | `Program.cs` | Consumer kaydı |
| 15 | Frontend | `signalRService.ts` | `CiCdUpdate` event handler + subscribe |
| 16 | Frontend | `IssueCard.tsx` | CI/CD badge ekle |
| 17 | Frontend | `IssueDetailModal.tsx` | Pipeline tab + Quality Gate UI |
| 18 | Frontend | `ProjectBoard.tsx` | Real-time CI/CD güncelleme |

---

## GitHub App Permission Gereksinimleri

Mevcut GitHub App ayarlarına eklenmesi gereken permission'lar:

| Permission | Level | Neden |
|-----------|-------|-------|
| `Actions` | Read | Workflow run bilgilerine erişim |
| `Checks` | Read | Check suite sonuçlarına erişim |

Webhook subscription'a eklenmesi gereken event'ler:

| Event | Neden |
|-------|-------|
| `check_suite` | CI/CD pipeline tamamlanma bildirimi |
| `workflow_run` | Workflow lifecycle takibi (queued → in_progress → completed) |

---

## Verification Plan

### Automated (Build)
```bash
dotnet build     # Tüm backend servisleri
cd frontend && npm run build  # Frontend
docker compose build          # Docker image'ları
```

### Manual Test Senaryoları
1. **Happy Path:** PR ile push → GitHub Actions çalışır → ForgeFlow'da issue kartında ✅ badge görünür
2. **Failure Case:** CI fail → Issue kartında ❌ badge → Done'a geçiş engellenir
3. **Real-time:** Pipeline başlarken ⏳ → çalışırken 🔄 → bitince ✅/❌ badge'i real-time güncellenir
4. **Quality Gate:** CI failed issue'yu Done'a sürükleme → hata mesajı gösterilir
5. **System Override:** PR merge → Done'a otomatik geçiş Quality Gate'e takılmaz (IsSystemAction=true)
6. **Idempotency:** Aynı RunId ile tekrar webhook gelirse → sadece güncelleme, duplicate yok

---

## Uygulama Sırası (Tavsiye)

1. **Event Contracts** → Tüm servisler bu sözleşmelere bağımlı
2. **GitHub Service Webhook** → Veri kaynağı, her şey buradan başlıyor
3. **Work Service (Entity + Command + Consumer)** → İş mantığının merkezi
4. **Artifact Service Consumer** → Kalıcı saklama
5. **Notification Service Consumer** → Real-time bildirim
6. **Frontend (SignalR + Badge + Pipeline Tab)** → Görsel katman
7. **Quality Gate** → Son adım, tüm altyapı hazır olduktan sonra
8. **EF Migration + Build + Test** → Doğrulama
