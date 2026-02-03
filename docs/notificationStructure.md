# 🔔 Notification Service - Tam Mimari Açıklama

Real-time bildirim sistemi için detaylı teknik dokümantasyon.

## 🏗️ Genel Akış Diyagramı

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              TAM AKIŞ                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1. OLAY OLUŞUR (herhangi bir serviste)                                     │
│     ┌─────────────────┐                                                     │
│     │ AI Orchestrator │──┐                                                  │
│     │ Work Service    │──┼──→ RabbitMQ'ya Event Publish                     │
│     │ Identity Service│──┘                                                  │
│     └─────────────────┘                                                     │
│              │                                                               │
│              ▼                                                               │
│  2. RABBITMQ (Message Broker)                                               │
│     ┌─────────────────────────────────────────┐                             │
│     │  Queue: ai-processing-progress          │                             │
│     │  Queue: issue-status-changed            │                             │
│     │  Queue: user-notification               │                             │
│     └────────────────────┬────────────────────┘                             │
│                          │                                                   │
│                          ▼                                                   │
│  3. NOTIFICATION SERVICE (MassTransit Consumer)                             │
│     ┌─────────────────────────────────────────┐                             │
│     │  AiProgressConsumer                     │                             │
│     │  IssueChangedConsumer                   │──→ IHubContext.SendAsync()  │
│     │  NotificationConsumer                   │                             │
│     └────────────────────┬────────────────────┘                             │
│                          │                                                   │
│                          ▼                                                   │
│  4. SIGNALR HUB + REDIS BACKPLANE                                           │
│     ┌─────────────────────────────────────────┐                             │
│     │  ForgeHub (WebSocket Server)            │                             │
│     │     │                                   │                             │
│     │     └──→ Redis Pub/Sub ──→ Diğer Hub'lar│                             │
│     └────────────────────┬────────────────────┘                             │
│                          │                                                   │
│                          ▼                                                   │
│  5. GATEWAY (YARP + WebSocket)                                              │
│     ┌─────────────────────────────────────────┐                             │
│     │  /hubs/forge → forgeflow-notification   │                             │
│     │  (Protocol Upgrade: HTTP → WebSocket)   │                             │
│     └────────────────────┬────────────────────┘                             │
│                          │                                                   │
│                          ▼                                                   │
│  6. FRONTEND (React + SignalR Client)                                       │
│     ┌─────────────────────────────────────────┐                             │
│     │  signalRService.ts                      │                             │
│     │     │                                   │                             │
│     │     └──→ Zustand Store ──→ UI Update    │                             │
│     └─────────────────────────────────────────┘                             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📦 Bileşen Detayları

### 1️⃣ Program.cs - Servis Girişi

```csharp
// Redis Backplane Konfigürasyonu
var redisConnection = Environment.GetEnvironmentVariable("Redis__ConnectionString") ?? "localhost:6379";
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = "ForgeFlow";
    });
```

**Redis'in Rolü:**
- SignalR normalde **tek sunucu** için çalışır
- Birden fazla notification container'ı olduğunda, kullanıcı **hangi container'a bağlıysa** sadece oradan mesaj alır
- Redis **Pub/Sub** mekanizması ile tüm container'ları birbirine bağlar

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ Notification #1 │     │ Notification #2 │     │ Notification #3 │
│  User A bağlı   │     │  User B bağlı   │     │  User C bağlı   │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                          ┌──────▼──────┐
                          │    REDIS    │
                          │  Pub/Sub    │
                          └─────────────┘
```

---

### 2️⃣ ForgeHub.cs - SignalR Hub

```csharp
[Authorize]  // JWT ile korumalı!
public class ForgeHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;  // JWT'den "sub" claim'i
        
        // Otomatik olarak kendi grubuna katıl
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    }
}
```

**Grup Yapısı:**
- `user:{userId}` → Kişisel bildirimler için
- `project:{projectId}` → Proje board güncellemeleri için

---

### 3️⃣ MassTransit Consumers

```csharp
public class AiProgressConsumer : IConsumer<AiProcessingProgress>
{
    public async Task Consume(ConsumeContext<AiProcessingProgress> context)
    {
        var msg = context.Message;

        // SignalR üzerinden kullanıcıya push
        await _hubContext.Clients
            .Group($"user:{msg.UserId}")
            .SendAsync("AiProgress", progressMessage);
    }
}
```

---

### 4️⃣ JWT Authentication (WebSocket Özel)

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        // WebSocket'te Authorization header yok!
        // Token query string'den gelir: /hubs/forge?access_token=xxx
        var accessToken = context.Request.Query["access_token"];
        
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

---

### 5️⃣ Frontend SignalR Client

```typescript
connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, {
        accessTokenFactory: () => accessToken,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build();

connection.on('AiProgress', (msg) => {
    eventHandlers.aiProgress.forEach(cb => cb(msg));
});
```

---

## ⚠️ EVENT PUBLISH DURUMU

### Mevcut Durum (2026-02-03)

| Event | Consumer Var mı? | Publisher Var mı? | Durum |
|-------|------------------|-------------------|-------|
| `AiProcessingProgress` | ✅ Var | ❌ **YOK** | Eksik |
| `IssueStatusChanged` | ✅ Var | ❌ **YOK** | Eksik |
| `UserNotification` | ✅ Var | ❌ **YOK** | Eksik |
| `AiPlanGenerated` | ❌ Yok | ✅ Var | Consumer eklenebilir |
| `AiPlanFailed` | ❌ Yok | ✅ Var | Consumer eklenebilir |

### Yapılması Gerekenler

#### 1. AI Orchestrator'a Progress Event Eklenmeli
```csharp
// GenerateAiPlanHandler.cs'de işlem sırasında:
await _publishEndpoint.Publish(new AiProcessingProgress(
    RequestId: command.RequestId,
    ProjectId: command.ProjectId,
    UserId: command.UserId,
    Message: "Analyzing issue context...",
    ProgressPercentage: 25,
    Timestamp: DateTime.UtcNow
));
```

#### 2. Work Service'e IssueStatusChanged Eklenmeli
```csharp
// ChangeIssueStatusHandler.cs'de:
await _publishEndpoint.Publish(new IssueStatusChanged(
    IssueKey: issue.Key,
    ProjectId: issue.ProjectId,
    OldStatus: (int)oldStatus,
    NewStatus: (int)newStatus,
    UpdatedByUserId: userId,
    Timestamp: DateTime.UtcNow
));
```

#### 3. Mevcut Event'ler İçin Consumer Eklenmeli
```csharp
// AiPlanGenerated için consumer eklenerek
// kullanıcıya "Plan hazır!" bildirimi gönderilebilir
```

---

## 🔴 Redis Yapılandırması

```yaml
# docker-compose.yml
redis:
  image: redis:7-alpine
  container_name: forgeflow-redis
  ports:
    - "6379:6379"
  networks:
    - forgeflow-net

notification:
  environment:
    Redis__ConnectionString: redis:6379
```

**Redis içinde ne var?**
```
ForgeFlow:user:abc123        → Bu kullanıcıya mesaj var
ForgeFlow:project:proj-456   → Bu projeye mesaj var
```

Redis sadece **geçici mesaj yönlendirme** için kullanılıyor, kalıcı veri saklamıyor.

---

## ❓ SSS

**S: Redis olmadan çalışır mı?**
A: Evet, tek instance için. Ama scale-out yapınca mesajlar kaybolur.

**S: RabbitMQ ve Redis farkı ne?**
- RabbitMQ: Servisler arası asenkron iletişim (durable, persistent)
- Redis: SignalR instance'ları arası anlık mesaj dağıtımı (in-memory, fast)

**S: WebSocket bağlantısı düşerse ne olur?**
A: `withAutomaticReconnect([0, 2000, 5000, 10000, 30000])` ile yeniden bağlanır.

---

*Son Güncelleme: 2026-02-03*
