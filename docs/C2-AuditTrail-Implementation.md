# 🔍 ForgeFlow: Audit Trail Implementation

**Tarih:** 16 Ocak 2026  
**Kapsam:** C2 Milestone - Audit & Compliance  
**Geliştirici:** Ali Eren Oğuztaş

---

## 1. Problem Tanımı

Kurumsal yazılımlarda kritik soru: **"Kim, ne zaman, ne yaptı?"**

C1'de Idempotency ile "ne oldu" sorusunu cevaplayabildik. Ancak:
- Hangi kullanıcı bu işlemi tetikledi?
- Hangi event bu değişikliğe sebep oldu?
- Bir hatayı araştırırken işlemin tüm yaşam döngüsünü nasıl izleyeceğiz?

---

## 2. Audit Trail Neden Gereklidir?

| Senaryo | Audit Olmadan | Audit İle |
|---------|---------------|-----------|
| **Güvenlik İhlali** | "Kim silmiş bilmiyoruz" | UserId + Timestamp → Kesin tespit |
| **Hata Araştırma** | Log'larda kaybolmak | CorrelationId ile end-to-end trace |
| **Uyumluluk (Compliance)** | Denetçiye gösterecek kanıt yok | AuditLog tablosu = Kara kutu |
| **Rollback Kararı** | "Ne değişti?" belirsiz | Changes JSON → Diff görüntüleme |

### Enterprise Zorunluluğu

- **SOC 2, ISO 27001:** Audit trail zorunlu
- **GDPR:** Kişisel veri erişimi loglanmalı
- **PCI-DSS:** Finansal işlemler izlenebilir olmalı

---

## 3. Mimari Çözüm: MediatR Pipeline Behavior

### Neden Bu Yaklaşım?

❌ **Kötü yol:** Her handler'a `_db.AuditLogs.Add(...)` yazmak
- DRY ihlali
- Unutma riski
- Kod tekrarı

✅ **İyi yol:** Pipeline Behavior ile otomatik yakalama
- Separation of Concerns
- Tek noktadan yönetim
- Kolayca genişletilebilir

### Akış

```
┌────────────┐     ┌─────────────────┐     ┌──────────────┐     ┌──────────┐
│  Consumer  │────▶│  AuditBehavior  │────▶│   Handler    │────▶│    DB    │
│  (MediatR) │     │    (Pipeline)   │     │ (Upsert etc) │     │ (Write)  │
└────────────┘     └─────────────────┘     └──────────────┘     └──────────┘
                          │
                          ▼
                   ┌──────────────┐
                   │  AuditLogs   │
                   │    Table     │
                   └──────────────┘
```

---

## 4. Uygulanan Katmanlı Çözümler

### 🏗️ Contracts Layer

`EventEnvelope` ve `ArtifactGenerated`'a `UserId` eklendi:

```csharp
public record EventEnvelope<T>(
    ...
    string UserId,  // İşlemi başlatan kullanıcı
    ...
);
```

### 🏗️ Domain Layer

`AuditLog` entity oluşturuldu:

```csharp
public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityName { get; private set; }  // "Artifact"
    public string EntityId { get; private set; }    // GUID
    public string Action { get; private set; }      // "Create"
    public string UserId { get; private set; }      // Actor
    public string CorrelationId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? Changes { get; private set; }    // JSON diff (opsiyonel)
}
```

### 💡 Application Layer

**IAuditLoggable** interface: Bu interface'i implement eden Command'lar otomatik loglanır.

```csharp
public interface IAuditLoggable
{
    string EntityName { get; }
    string EntityId { get; }
    string Action { get; }
    string UserId { get; }
    string CorrelationId { get; }
}
```

**AuditBehavior** Pipeline: MediatR pipeline'ına eklendi, tüm `IAuditLoggable` command'ları yakalar.

```csharp
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next(); // İşlemi yap
        
        if (request is IAuditLoggable loggable)
        {
            // Otomatik audit log kaydet
            await _auditRepository.AddAsync(new AuditLog(...));
        }
        
        return response;
    }
}
```

### 🏗️ Infrastructure Layer

- `AuditLogRepository` implementasyonu
- `ArtifactDbContext`'e AuditLogs DbSet eklendi
- Sorgulama için indexler: `CorrelationId`, `UserId`, `CreatedAtUtc`, `EntityName+EntityId`

### 🚀 Work Service

`IssuesController` güncellendi - UserId artık event'e ekleniyor:

```csharp
var userId = "USR-1"; // TODO: JWT'den alınacak

var evt = new EventEnvelope<AiPlanRequested>(
    ...
    UserId: userId,  // Audit trail için
    ...
);
```

### 🤖 AI Orchestrator

`AiPlanRequestedConsumer` güncellendi - UserId zincir boyunca taşınıyor:

```csharp
// Structured logging scope
using var scope = _logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = msg.CorrelationId,
    ["UserId"] = msg.UserId
});

// UserId'yi bir sonraki event'e aktar
var outEvt = new EventEnvelope<ArtifactGenerated>(
    ...
    UserId: msg.UserId,  // Audit için aktar
    ...
);
```

---

## 5. End-to-End UserId Akışı

```
┌─────────────────┐     ┌─────────────────────┐     ┌───────────────────┐
│   Work Service  │────▶│   AI Orchestrator   │────▶│  Artifact Service │
│  UserId: USR-1  │     │  UserId: USR-1      │     │  UserId: USR-1    │
└─────────────────┘     └─────────────────────┘     └───────────────────┘
        │                        │                          │
   EventEnvelope            EventEnvelope              AuditLog
   .UserId = USR-1          .UserId = USR-1           .UserId = USR-1
```

---

## 6. Teknik Çıktılar

| Özellik | Açıklama |
|---------|----------|
| **Traceability** | CorrelationId ile Seq + DB'de end-to-end izleme |
| **Accountability** | Her işlemde UserId kaydı → Kim yaptı belli |
| **Compliance Ready** | AuditLog tablosu = Denetçiye kanıt |
| **Separation of Concerns** | İş mantığı vs audit mantığı ayrı |
| **DRY** | Tek behavior, tüm command'lar |

---

*Bu doküman, ForgeFlow projesinin C2 Milestone kapsamında yapılan Audit Trail çalışmalarını özetlemektedir.*

