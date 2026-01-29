# AI Orchestrator - Mimari Açıklamalar

Bu doküman, AI Orchestrator servisinin mimari kararlarını ve tasarım prensiplerini açıklar.

---

## 🧅 Neden `Abstractions` Domain Katmanında?

Bu, **Onion Architecture** (veya Clean Architecture) prensibi olan **Dependency Inversion Principle (DIP)** ile doğrudan ilgilidir.

### Temel Kural
> "Dış katmanlar iç katmanlara bağımlı olabilir, ama iç katmanlar dış katmanlara **asla** bağımlı olamaz."

```
        ┌─────────────────────────────────────┐
        │         Worker / API                 │  ← En dış katman
        │  ┌─────────────────────────────┐    │
        │  │      Infrastructure         │    │  ← Dış katman
        │  │  ┌───────────────────────┐  │    │
        │  │  │     Application       │  │    │
        │  │  │  ┌─────────────────┐  │  │    │
        │  │  │  │     DOMAIN      │  │  │    │  ← Çekirdek (Core)
        │  │  │  │  (Abstractions) │  │  │    │
        │  │  │  └─────────────────┘  │  │    │
        │  │  └───────────────────────┘  │    │
        │  └─────────────────────────────┘    │
        └─────────────────────────────────────┘
```

---

## 📁 `Abstractions` Klasöründeki Dosyalar

### 1️⃣ `IAiService.cs` - LLM Soyutlaması

```csharp
public interface IAiService
{
    Task<AiResponse> GenerateContentAsync(AiRequest request, ...);
}
```

**Amaç:** Domain katmanı "AI'dan içerik üret" dediğinde, bunun Gemini mi, Groq mu, OpenAI mı olduğunu bilmiyor ve **bilmek zorunda da değil**.

- **Domain diyor ki:** "Bana `IAiService` ver, ben onunla konuşurum."
- **Infrastructure diyor ki:** "Tamam, sana `GeminiAiService` veriyorum. O da `IAiService` implement ediyor."

Bu sayede yarın Groq'a geçsen, Domain'e dokunmuyorsun.

---

### 2️⃣ `IContextProvider.cs` - Veri Kaynağı Soyutlaması

```csharp
public interface IContextProvider
{
    Task<AiContext> GetContextAsync(Guid projectId, string issueKey, ...);
}
```

**Amaç:** AI'a gönderilecek "bağlam" (proje bilgisi, issue detayı, kod dosyaları) birçok farklı kaynaktan gelebilir:
- **Şimdi:** `HttpWorkContextProvider` → Work Service API'sinden veri çeker.
- **Yarın:** `GitHubContextProvider` → GitHub'dan kod dosyalarını çeker.

Domain yine kaynağı bilmiyor, sadece "bana context ver" diyor.

---

### 3️⃣ `IAiAuditRepository.cs` - Veri Erişim Soyutlaması

```csharp
public interface IAiAuditRepository
{
    Task AddAsync(AiAuditLog auditLog, ...);
    Task<bool> IsRequestProcessedAsync(Guid requestId, ...);
}
```

**Amaç:** Audit logları veritabanına yazılacak. Ama Domain katmanı:
- Entity Framework bilmiyor.
- SQL Server mı, PostgreSQL mı bilmiyor.
- Sadece "bu logu kaydet" diyor.

Infrastructure tarafında `EfCoreAiAuditRepository` bunu implement edecek.

---

## 💡 Neden Application'da Değil de Domain'de?

İki farklı yaklaşım vardır:

| Yaklaşım | `Abstractions` Nerede? | Ne Zaman Kullanılır? |
|----------|------------------------|---------------------|
| **Clean Architecture (Uncle Bob)** | Application | Daha katı ayrım, Use Case'ler merkez |
| **Onion Architecture (Jeffrey Palermo)** | Domain | Entity'ler ve Interface'ler bir arada |

Biz **Onion** yaklaşımını seçtik çünkü:
1. `AiAuditLog` entity'si Domain'de → Repository interface'i de yanında durmalı.
2. `IAiService` dönüş tipi olan `AiResponse` Domain'de → Interface de orada olmalı.
3. Daha az circular dependency riski.

---

## 📊 Özet Tablo

| Interface | Implement Eden | Nerede? | Neden Abstract? |
|-----------|---------------|---------|-----------------|
| `IAiService` | `GeminiAiService`, `GroqAiService` | Infrastructure | LLM değişebilir |
| `IContextProvider` | `HttpWorkContextProvider`, `GitHubContextProvider` | Infrastructure | Veri kaynağı değişebilir |
| `IAiAuditRepository` | `EfCoreAiAuditRepository` | Infrastructure | DB teknolojisi değişebilir |

**Sonuç:** Domain "ne yapılacağını" tanımlar, Infrastructure "nasıl yapılacağını" implement eder. 🎯

---

# Phase 2: Infrastructure Katmanı

Bu bölüm, AI Orchestrator'ın dış dünya ile nasıl iletişim kurduğunu açıklar.

---

## 🔧 IOptions Pattern Nedir?

`IOptions<T>`, .NET'in **Configuration sistemi**nin parçasıdır. Amacı: Konfigürasyon değerlerini (API Key, URL vb.) **type-safe** şekilde kodda kullanmak.

### Kaynak Hiyerarşisi (Öncelik Sırası)
```
1. appsettings.json              (Base)
2. appsettings.Development.json  (Environment specific)
3. Environment Variables         (Docker/Kubernetes) ← EN ÖNCELİKLİ
4. Command line arguments
```

### Environment Variable → C# Binding
Docker Compose'da `__` (çift alt çizgi) nesne hiyerarşisi olarak yorumlanır:

```yaml
# docker-compose.yml
environment:
  Services__WorkApiUrl: "http://forgeflow-work:8080"
  AI__Providers__Gemini__ApiKey: "${GEMINI_API_KEY}"
```

Bu değerler otomatik olarak C# Options sınıflarına bind edilir:

```csharp
// DependencyInjection.cs
services.Configure<WorkServiceOptions>(configuration.GetSection("Services"));
services.Configure<AiOptions>(configuration.GetSection("AI"));

// HttpWorkContextProvider.cs
public HttpWorkContextProvider(IOptions<WorkServiceOptions> options, ...)
{
    _options.WorkApiUrl  // "http://forgeflow-work:8080"
}
```

---

## 📁 Infrastructure Dosya Açıklamaları

### `Options/AiOptions.cs`
**Amaç:** `appsettings.json` veya environment variable'lardan AI konfigürasyonlarını type-safe okumak.

| Kaynak | Hedef |
|--------|-------|
| `AI__DefaultProvider` | `AiOptions.DefaultProvider` |
| `AI__Providers__Gemini__ApiKey` | `GeminiOptions.ApiKey` |
| `AI__Providers__Groq__Model` | `GroqOptions.Model` |

---

### `AiServices/GeminiAiService.cs`
**Amaç:** Google Gemini API ile iletişim (REST API).

**İş Akışı:**
1. `AiRequest` alır (prompt, maxTokens, temperature)
2. Gemini REST API formatına çevirir (`generateContent` endpoint)
3. HTTP POST atar, yanıtı parse eder
4. Token kullanımı + süre ölçer
5. `AiResponse` döner

**Hata Yönetimi:**
- `429 Too Many Requests` → `QUOTA_EXCEEDED` error code
- `401 Unauthorized` → `UNAUTHORIZED` error code

---

### `AiServices/GroqAiService.cs`
**Amaç:** Groq API ile iletişim (Llama 3, Mixtral modelleri).

**Farkı:** OpenAI uyumlu `/chat/completions` endpoint kullanır.

**Rate Limit Header Okuma:**
```csharp
response.Headers.TryGetValues("x-ratelimit-remaining-requests", out var reqValues)
response.Headers.TryGetValues("x-ratelimit-remaining-tokens", out var tokValues)
```
Bu bilgiler `AiResponse`'a eklenir, kullanıcıya kalan hak gösterilebilir.

---

### `AiServices/AiServiceFactory.cs`
**Amaç:** Strategy Pattern ile doğru AI servisini seçmek.

```csharp
var service = factory.GetService(AiProviderType.Groq);  // Kullanıcı tercihi
var service = factory.GetService();                     // Default (config'den)
```

**Avantajları:**
- Kullanıcı model seçebilir
- Config'den default belirlenebilir
- Yeni model eklemek kolay (OCP prensibi)

---

### `ContextProviders/HttpWorkContextProvider.cs`
**Amaç:** AI'ya verilecek "bağlamı" Work Service'ten HTTP ile çekmek.

**İş Akışı:**
1. `projectId` ve `issueKey` alır
2. Work Service API'sine HTTP GET atar (`/api/projects/{id}`, `/api/issues/{key}`)
3. Project ve Issue detaylarını çeker
4. `AiContext` nesnesine paketler

**Neden Worker'dan Ayrı?**
- **Single Responsibility:** Worker orchestration yapar, ContextProvider sadece veri çeker
- **Testability:** Ayrı test edilebilir
- **Extensibility:** GitHub entegrasyonu gelince `GitHubContextProvider` eklenir, Worker değişmez

**GitHub-Ready:**
- `AiContext.SourceFiles` listesi şu an boş
- Gelecekte `GitHubContextProvider` kod dosyalarını dolduracak
- `CompositeContextProvider` ile birleştirilebilir

---

### `Persistence/AiOrchestratorDbContext.cs`
**Amaç:** EF Core DbContext - `AiAuditLogs` tablosu.

**Tablo:** Her AI çağrısının kaydı (prompt, response, token, süre, hata).

**Index'ler (Performans için):**
| Index | Kullanım Amacı |
|-------|----------------|
| `CorrelationId` | Distributed tracing |
| `RequestId` | Idempotency kontrolü |
| `IssueKey` | Issue bazlı sorgulama |
| `CreatedAtUtc` | Zaman bazlı raporlama |

---

### `Persistence/EfCoreAiAuditRepository.cs`
**Amaç:** `IAiAuditRepository` interface'inin EF Core implementasyonu.

**Kritik Metod - Idempotency:**
```csharp
public async Task<bool> IsRequestProcessedAsync(Guid requestId, ...)
{
    return await _context.AiAuditLogs
        .AnyAsync(log => log.RequestId == requestId && log.IsSuccess);
}
```
Bu metod aynı isteğin tekrar işlenmesini önler (RabbitMQ retry, duplicate message vb.).

---

### `DependencyInjection.cs`
**Amaç:** Tüm Infrastructure bileşenlerini IoC Container'a kaydetmek.

```csharp
// DbContext
services.AddDbContext<AiOrchestratorDbContext>(...);

// Repository
services.AddScoped<IAiAuditRepository, EfCoreAiAuditRepository>();

// AI Services (Named HttpClient ile)
services.AddHttpClient<IAiService, GeminiAiService>("Gemini");
services.AddHttpClient<IAiService, GroqAiService>("Groq");

// Factory
services.AddScoped<AiServiceFactory>();

// Context Provider
services.AddHttpClient<IContextProvider, HttpWorkContextProvider>("WorkService");
```

**Composition Root:** Tüm bağımlılıklar burada birleşir.

---

## 🐳 Docker Compose Entegrasyonu

Servisler arası iletişim container name üzerinden yapılır:

| Senaryo | URL | Açıklama |
|---------|-----|----------|
| Local Dev | `http://localhost:5002` | IDE'de çalışırken |
| Docker Compose | `http://forgeflow-work:8080` | Container içi iletişim |

```yaml
forgeflow-ai-orchestrator:
  environment:
    Services__WorkApiUrl: "http://forgeflow-work:8080"
    AI__Providers__Gemini__ApiKey: "${GEMINI_API_KEY}"
    AI__Providers__Groq__ApiKey: "${GROQ_API_KEY}"
    ConnectionStrings__AiOrchestratorDb: "Server=mssql;..."
```

Kod hiç değişmeden, sadece environment variable'lar ile davranış değişir.

---

# Phase 3: Application Katmanı (CQRS)

Bu bölüm, AI Orchestrator'ın iş mantığını ve MediatR pipeline'ını açıklar.

---

## 🎯 CQRS Nedir?

**Command Query Responsibility Segregation** - Okuma (Query) ve Yazma (Command) işlemlerini ayırma prensibi.

```
┌─────────────────────────────────────────────────────────┐
│                      MediatR                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐  │
│  │  Command    │ ─► │  Behaviors  │ ─► │   Handler   │  │
│  │  (Request)  │    │  (Pipeline) │    │  (Logic)    │  │
│  └─────────────┘    └─────────────┘    └─────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## 📁 Application Dosya Açıklamaları

### `Commands/GenerateAiPlanCommand.cs`
**Amaç:** AI plan oluşturma isteğini tanımlamak (CQRS Command).

```csharp
public class GenerateAiPlanCommand : IRequest<GenerateAiPlanResult>
{
    public Guid RequestId { get; init; }      // Idempotency için
    public Guid CorrelationId { get; init; }  // Distributed tracing
    public Guid ProjectId { get; init; }
    public string IssueKey { get; init; }
    public string UserId { get; init; }
    public AiProviderType? PreferredProvider { get; init; } // Kullanıcı seçimi
}
```

**GenerateAiPlanResult:** Başarı/Hata durumu + Token metrikleri + Rate limit bilgisi.

---

### `Commands/GenerateAiPlanCommandHandler.cs`
**Amaç:** Ana orchestration logic - AI plan üretim akışını yönetmek.

**İş Akışı:**
```
1. Context Provider'dan proje/issue bilgisi çek
2. Prompt'ları oluştur (System + User + Code Context)
3. AI Service Factory'den uygun servisi al
4. AI'ya istek at
5. Sonucu döndür
```

**Prompt Yapısı:**
- **System Prompt:** Architect rolü, Clean Architecture kuralları
- **User Prompt:** Proje + Issue + Kod Bağlamı (GitHub'dan)
- **Code Context:** Dosya ağacı + Kaynak kodlar (max 10 dosya, max 500 satır)

---

### `Behaviors/IdempotencyBehavior.cs`
**Amaç:** Aynı isteğin tekrar işlenmesini önlemek.

```
Request ──► IdempotencyBehavior ──► [Already processed?]
                                          │
                              ┌───────────┴───────────┐
                              │ YES                   │ NO
                              ▼                       ▼
                    Return CACHED Response      Continue to next
                    (from DB, not empty!)
```

**Nasıl Çalışır:**
1. `GetByRequestIdAsync(requestId)` ile DB'de arama yap
2. Daha önce başarılı işlendiyse → **Cached response'u dön** (AI çağrısı yapma)
3. İlk kez geliyorsa → Devam et

**At-Least-Once Delivery Garantisi:**
- RabbitMQ retry durumunda bile aynı sonuç dönülür
- Consumer, cached response'u sanki yeni üretilmiş gibi RabbitMQ'ya basabilir
- Token tasarrufu sağlanır (AI tekrar çağrılmaz)

**Use Case:** RabbitMQ retry, network glitch, duplicate message.

---

### `Behaviors/AuditLoggingBehavior.cs`
**Amaç:** Her AI işlemini otomatik kayıt altına almak.

```csharp
// Handler çalışmadan ÖNCE: Stopwatch başlat
// Handler çalıştıktan SONRA: AuditLog oluştur ve kaydet
```

**Kaydedilen Bilgiler:**
| Alan | Açıklama |
|------|----------|
| `CorrelationId` | Distributed tracing |
| `RequestId` | Idempotency |
| `Provider` | Gemini/Groq |
| `PromptTokens` | Giden token |
| `CompletionTokens` | Gelen token |
| `DurationMs` | İşlem süresi |
| `IsSuccess` | Başarı durumu |
| `ErrorCode` | Hata kodu (varsa) |

---

### `DependencyInjection.cs`
**Amaç:** MediatR ve Pipeline Behavior'ları IoC'ye kaydetmek.

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(...);
    
    // Sıralama önemli!
    cfg.AddBehavior<IdempotencyBehavior>();   // 1. Duplicate check
    cfg.AddBehavior<AuditLoggingBehavior>();  // 2. Log everything
});
```

**Pipeline Sırası:**
```
Request ──► Idempotency ──► AuditLogging ──► Handler ──► Response
            (1st)           (2nd)            (3rd)
```

---

## 🔗 Clean Architecture Korunması

Handler'da Infrastructure bağımlılığı olmaması için:

```
Application ──► IAiServiceFactory (Domain interface)
                        ▲
Infrastructure ─────────┘ (AiServiceFactory implementation)
```

Bu sayede Application katmanı test edilebilir ve Infrastructure'dan bağımsız.

---

# Phase 4: Worker Integration & Orchestration

Bu bölüm, AI Orchestrator Worker servisinin MassTransit ve MediatR entegrasyonunu açıklar.

---

## 📁 Worker Dosya Açıklamaları

### `Consumers/AiPlanRequestedConsumer.cs`
**Amaç:** RabbitMQ'dan gelen `AiPlanRequested` eventlerini işlemek.

**Akış:**
```csharp
public async Task Consume(ConsumeContext<EventEnvelope<AiPlanRequested>> context)
{
    // 1. Event'ten Command oluştur
    var command = new GenerateAiPlanCommand { ... };
    
    // 2. MediatR ile işle (Idempotency → AuditLogging → Handler)
    var result = await _mediator.Send(command);
    
    // 3. Sonuca göre event yayınla
    if (result.IsSuccess)
        await context.Publish(new AiPlanGenerated(...));
    else
        await context.Publish(new AiPlanFailed(...));
}
```

---

### `Program.cs`
**Amaç:** Worker servisinin DI yapılandırması ve başlatılması.

**Kayıtlar:**
```csharp
// Serilog + Seq
builder.Services.AddSerilog(...);

// Application layer (MediatR + Behaviors)
builder.Services.AddAiOrchestratorApplication();

// Infrastructure layer (AI Services, Repositories)
builder.Services.AddAiOrchestratorInfrastructure(configuration);

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x => { ... });
```

---

### `appsettings.json`
**Amaç:** Tüm konfigürasyonları barındırmak.

| Bölüm | Açıklama |
|-------|----------|
| `Seq.ServerUrl` | Seq loglama sunucusu |
| `RabbitMq.*` | MassTransit bağlantısı |
| `ConnectionStrings.AiOrchestratorDb` | Audit log DB |
| `Services.WorkApiUrl` | Work Service API |
| `AI.DefaultProvider` | Varsayılan LLM |
| `AI.Providers.Gemini.*` | Gemini API yapılandırması |
| `AI.Providers.Groq.*` | Groq API yapılandırması |

---

### Events: `AiPlanGenerated` / `AiPlanFailed`
**Amaç:** AI işlem sonucunu diğer servislere bildirmek.

```csharp
// Başarılı
public record AiPlanGenerated(
    string IssueId,
    string ProjectId,
    string GeneratedContent,  // AI'ın ürettiği plan
    string UsedProvider,      // "Gemini" veya "Groq"
    int PromptTokens,
    int CompletionTokens,
    long DurationMs
);

// Başarısız
public record AiPlanFailed(
    string IssueId,
    string ProjectId,
    string ErrorCode,         // "QUOTA_EXCEEDED", "UNAUTHORIZED" vb.
    string ErrorMessage,
    string UsedProvider
);
```

---

# 🎯 AI Integration - Tam İş Akışı

## End-to-End Sequence Diagram

```
┌──────────┐    ┌──────────┐    ┌──────────────────┐    ┌──────────────┐
│  Client  │    │   Work   │    │  AI Orchestrator │    │   Gemini/    │
│  (Web)   │    │ Service  │    │     (Worker)     │    │    Groq      │
└────┬─────┘    └────┬─────┘    └────────┬─────────┘    └──────┬───────┘
     │               │                   │                     │
     │ Generate Plan │                   │                     │
     │──────────────►│                   │                     │
     │               │                   │                     │
     │               │ AiPlanRequested   │                     │
     │               │──────────────────►│ (via RabbitMQ)      │
     │               │                   │                     │
     │               │                   │──── Idempotency ────│
     │               │                   │                     │
     │               │                   │── Get Context ──────│
     │               │◄──────────────────│ (HTTP GET)          │
     │               │ Project+Issue     │                     │
     │               │──────────────────►│                     │
     │               │                   │                     │
     │               │                   │── Build Prompt ─────│
     │               │                   │                     │
     │               │                   │── AI Call ─────────►│
     │               │                   │                     │
     │               │                   │◄─── Generated Plan ─│
     │               │                   │                     │
     │               │                   │── Audit Log ────────│
     │               │                   │                     │
     │               │ AiPlanGenerated   │                     │
     │               │◄──────────────────│ (via RabbitMQ)      │
     │               │                   │                     │
     │ Plan Ready    │                   │                     │
     │◄──────────────│                   │                     │
     │               │                   │                     │
```

---

## 🔄 MediatR Pipeline Flow

```
Request ──► IdempotencyBehavior ──► AuditLoggingBehavior ──► Handler ──► Response
                │                          │                    │
                ▼                          ▼                    ▼
           [Check DB]              [Start Stopwatch]      [Orchestrate]
           Already processed?      Log after handler       - Context
                │                          │                - Prompt
        ┌───────┴───────┐                  │                - AI Call
        │ YES           │ NO               ▼                - Return
        ▼               ▼             [Save AuditLog]
   Return early     Continue              to DB
```

---

## 📊 Proje Yapısı

```
services/ai-orchestrator/
├── ForgeFlow.AiOrchestrator.Domain/           # Core (Interfaces, Models)
│   ├── Abstractions/
│   │   ├── IAiService.cs
│   │   ├── IAiServiceFactory.cs
│   │   ├── IContextProvider.cs
│   │   └── IAiAuditRepository.cs
│   ├── Entities/
│   │   └── AiAuditLog.cs
│   ├── Enums/
│   │   └── AiProviderType.cs
│   └── Models/
│       ├── AiRequest.cs
│       ├── AiResponse.cs
│       └── AiContext.cs
│
├── ForgeFlow.AiOrchestrator.Application/      # Business Logic (CQRS)
│   ├── Commands/
│   │   ├── GenerateAiPlanCommand.cs
│   │   └── GenerateAiPlanCommandHandler.cs
│   ├── Behaviors/
│   │   ├── IdempotencyBehavior.cs
│   │   └── AuditLoggingBehavior.cs
│   └── DependencyInjection.cs
│
├── ForgeFlow.AiOrchestrator.Infrastructure/   # External Integrations
│   ├── AiServices/
│   │   ├── GeminiAiService.cs
│   │   ├── GroqAiService.cs
│   │   └── AiServiceFactory.cs
│   ├── ContextProviders/
│   │   └── HttpWorkContextProvider.cs
│   ├── Persistence/
│   │   ├── AiOrchestratorDbContext.cs
│   │   └── EfCoreAiAuditRepository.cs
│   ├── Options/
│   │   └── AiOptions.cs
│   └── DependencyInjection.cs
│
└── ForgeFlow.AiOrchestrator.Worker/           # Entry Point
    ├── Consumers/
    │   └── AiPlanRequestedConsumer.cs
    ├── Program.cs
    └── appsettings.json
```

---

## 🔐 API Key Yönetimi

```bash
# infra/.env dosyası oluştur
GEMINI_API_KEY=AIzaSy...
GROQ_API_KEY=gsk_...
```

```yaml
# docker-compose.yml
ai-orchestrator:
  environment:
    AI__Providers__Gemini__ApiKey: "${GEMINI_API_KEY:-}"
    AI__Providers__Groq__ApiKey: "${GROQ_API_KEY:-}"
```

---

## 📈 Monitoring (Seq)

Tüm AI işlemleri Seq'te aşağıdaki bilgilerle loglanır:

| Property | Açıklama |
|----------|----------|
| `CorrelationId` | End-to-end tracing |
| `IssueId` | Hangi issue için plan üretildi |
| `Provider` | Gemini / Groq |
| `PromptTokens` | Giden token sayısı |
| `CompletionTokens` | Gelen token sayısı |
| `DurationMs` | İşlem süresi |
| `IsSuccess` | Başarı durumu |
| `ErrorCode` | Hata kodu (varsa) |

**Seq Query Örneği:**
```
CorrelationId = "abc-123" | Provider = "Gemini" | @Level = "Error"
```
{
    "project_name": "ForgeFlow",
    "issue_id": "FORGE-3",
    "issue_description": "Implement user authentication (login/register endpoints with JWT)",
    "architectural_principles": [
        "Clean Architecture",
        "SOLID Principles"
    ],
    "tech_stack_assumptions": [
        ".NET 8",
        "ASP.NET Core Web API",
        "Entity Framework Core (for data persistence)",
        "MediatR (for command/query dispatching)",
        "BCrypt.Net-NEXT (for password hashing)"
    ],
    "implementation_plan": {
        "summary": "This plan outlines the implementation of user authentication (registration and login) using JWT tokens, strictly adhering to Clean Architecture and SOLID principles. The solution will separate concerns into Domain, Application, Infrastructure, and Presentation layers, ensuring maintainability, testability, and scalability. MediatR will be used for command/query dispatching, and Entity Framework Core for data persistence. Password hashing will be implemented using BCrypt.",
        "list_of_changes": [
            "**Domain Layer:** Define core User entity, interfaces for user repository and token service, and custom domain exceptions.",
            "**Application Layer:** Define DTOs for authentication results, commands for user registration and login, and their respective handlers. Introduce an abstraction for password hashing. Implement validation for commands.",
            "**Infrastructure Layer:** Provide concrete implementations for `IUserRepository` (using EF Core) and `ITokenService` (JWT generation). Implement the concrete password hasher. Configure EF Core DbContext and migrations.",
            "**Presentation Layer (Web API):** Create an `AuthController` with `/register` and `/login` endpoints. Configure JWT authentication middleware in `Program.cs`. Set up dependency injection.",
            "**Configuration:** Add JWT settings to `appsettings.json`.",
            "**NuGet Packages:** Add necessary packages (e.g., `Microsoft.AspNetCore.Authentication.JwtBearer`, `MediatR`, `MediatR.Extensions.Microsoft.DependencyInjection`, `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `BCrypt.Net-NEXT`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`)."
        ],
        "file_by_file_changes": [
            {
                "layer": "ForgeFlow.Domain",
                "purpose": "Core business entities, value objects, and interfaces, independent of infrastructure.",
                "files": [
                    {
                        "filename": "Entities/User.cs",
                        "description": "Represents the user entity with properties like Id, Username, Email, PasswordHash, and Timestamp.",
                        "code_example": "```csharp\nnamespace ForgeFlow.Domain.Entities\n{\n    public class User\n    {\n        public Guid Id { get; private set; }\n        public string Username { get; private set; }\n        public string Email { get; private set; }\n        public string PasswordHash { get; private set; }\n        public DateTime CreatedAt { get; private set; }\n\n        private User() { } // Required for EF Core\n\n        public User(string username, string email, string passwordHash)\n        {\n            Id = Guid.NewGuid();\n            Username = username ?? throw new ArgumentNullException(nameof(username));\n            Email = email ?? throw new ArgumentNullException(nameof(email));\n            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));\n            CreatedAt = DateTime.UtcNow;\n        }\n\n        public void UpdatePasswordHash(string newPasswordHash)\n        {\n            if (string.IsNullOrWhiteSpace(newPasswordHash))\n            {\n                throw new ArgumentException(\"Password hash cannot be empty.\", nameof(newPasswordHash));\n            }\n            PasswordHash = newPasswordHash;\n        }\n    }\n}\n```"
                    },
                    {
                        "filename": "Interfaces/IUserRepository.cs",
                        "description": "Abstraction for user data access operations. Follows DIP.",
                        "code_example": "```csharp\nusing ForgeFlow.Domain.Entities;\n\nnamespace ForgeFlow.Domain.Interfaces\n{\n    public interface IUserRepository\n    {\n        Task<User?> GetByUsernameAsync(string username);\n        Task<User?> GetByEmailAsync(string email);\n        Task AddAsync(User user);\n        Task UpdateAsync(User user);\n        Task<bool> ExistsByUsernameAsync(string username);\n        Task<bool> ExistsByEmailAsync(string email);\n    }\n}\n```"
                    },
                    {
                        "filename": "Interfaces/ITokenService.cs",
                        "description": "Abstraction for JWT token generation. Follows DIP.",
                        "code_example": "```csharp\nusing ForgeFlow.Domain.Entities;\n\nnamespace ForgeFlow.Domain.Interfaces\n{\n    public interface ITokenService\n    {\n        string GenerateToken(User user);\n    }\n}\n```"
                    }
                ]
            }
        ]
    }
}
