# 📡 ForgeFlow Real-Time Notification Architecture

Bu doküman, ForgeFlow projesinin gerçek zamanlı bildirim ve olay güdümlü (event-driven) mimarisini, veri akışını ve bileşen etkileşimlerini detaylandırır.

## 🏗️ 1. Yüksek Seviye Mimari (Architecture Overview)

Sistem, mikroservislerin ürettiği olayları (events) son kullanıcıya milisaniyeler içinde iletmek için **RabbitMQ**, **MassTransit** ve **SignalR** teknolojilerini kullanır.

```mermaid
graph TD
    User((User)) -- 1. Trigger Action --> API[Work / AI API]
    API -- 2. Publish Event --> RabbitMQ{RabbitMQ Bus}
    
    subgraph "Backend Services"
        AI_Orch[AI Orchestrator]
        Work[Work Service]
    end
    
    subgraph "Notification System"
        NotifService[Notification Service]
        SignalR[SignalR Hub]
        Redis[(Redis Backplane)]
    end
    
    Backend Services -- Events --> RabbitMQ
    RabbitMQ -- Consume --> NotifService
    NotifService -- Push --> SignalR
    SignalR <-- Sync --> Redis
    SignalR -- WebSocket --> Client[Frontend App]
    
    Client -- Join Group --> SignalR
```

---

## 🔄 2. Veri Akış Şeması (Data Flow)

### Senaryo: AI Plan Oluşturma (AI Plan Generation)

```mermaid
sequenceDiagram
    participant U as User (Frontend)
    participant AI as AI Service
    participant MQ as RabbitMQ
    participant NS as Notification Service
    participant HUB as SignalR Hub
    participant G as Group (Project)

    Note over U: "Generate Plan" butonuna tıklar
    U->>AI: POST /generate-plan
    
    rect rgb(240, 248, 255)
        Note left of AI: İşlem Başladı
        AI->>MQ: Publish(AiProcessingProgress: 5%)
        MQ->>NS: Consume(AiProcessingProgress)
        NS->>HUB: Clients.Group(User & Project).SendAsync("AiProgress")
        HUB-->>U: WebSocket Push (5%)
        HUB-->>G: WebSocket Push (5%)
    end
    
    rect rgb(255, 250, 240)
        Note left of AI: %15... %50... %85
        AI->>MQ: Publish(AiProcessingProgress...)
        MQ...>>U: WebSocket Push (Updates)
    end

    rect rgb(230, 255, 230)
        Note left of AI: İşlem Tamamlandı
        AI->>MQ: Publish(AiProcessingProgress: 100%)
        AI->>MQ: Publish(UserNotification: "Plan Ready")
        MQ->>NS: Consume Events
        NS->>HUB: SendAsync("AiProgress", 100%)
        NS->>HUB: SendAsync("Notification", "Plan Ready")
        HUB-->>U: Trigger: Auto-Refresh Board
    end
```

---

## 🧩 3. Bileşen Detayları (Components)

### A. Publisher (Olay Üreticiler)

Olayları başlatan servislerdir.

| Servis | Olay (Event) | Açıklama |
| :--- | :--- | :--- |
| **AI Orchestrator** | `AiProcessingProgress` | AI işleminin ilerleme durumu (%10, %50, %100). |
| **Work Service** | `IssueStatusChanged` | Bir issue'nun durumu değiştiğinde (örn: ToDo -> Done). |
| **Any Service** | `UserNotification` | Kullanıcıya özel genel bildirimler (örn: "İşlem bitti"). |

### B. Broker & Consumer (İletim Katmanı)

*   **RabbitMQ:** Olayları `direct` veya `fanout` exchange ile ilgili kuyruklara (Queue) yönlendirir.
*   **Notification Service:** MassTransit `IConsumer<T>` arayüzü ile kuyrukları dinler. Mesaj geldiği anda SignalR Hub'ına iletir.

### C. SignalR Hub (Dağıtım Katmanı)

Kullanıcıları ve bağlantıları yönetir.

*   **Hub Rotası:** `/hubs/forge`
*   **Auth:** JWT Token (Query String üzerinden).
*   **Gruplar:**
    *   `user:{userId}` -> Kişiye özel mesajlar.
    *   `project:{projectId}` -> Proje bazlı ortak mesajlar.

### D. Frontend (Client Katmanı)

React uygulaması (`signalRService.ts` ve `notificationStore.ts`).

1.  **Bağlantı:** Uygulama açılınca WebSocket bağlantısı kurulur.
2.  **Odaya Giriş:** `ProjectBoard` açılınca `joinProject(id)` ile odaya girilir.
3.  **Deduplication (Önemli):** Race condition nedeniyle çift gelen mesajları filtreler.
4.  **UI Update:** Mesaj gelince, sayfayı yenilemeden (refresh-free) listeyi ve logları günceller.

---

## 🛠️ 4. Payload Şemaları (Data Schemas)

### Event: `AiProcessingProgress`

```json
{
  "requestId": "guid-uuid-v4",
  "projectId": "guid-uuid-v4",
  "userId": "user-123",
  "message": "Validating context...",
  "progressPercentage": 45,
  "timestamp": "2024-02-04T12:00:00Z"
}
```

### Event: `IssueStatusChanged`

```json
{
  "issueKey": "PRJ-101",
  "projectId": "guid-uuid-v4",
  "oldStatus": 0, // Open
  "newStatus": 1, // InProgress
  "updatedBy": "user-456",
  "timestamp": "2024-02-04T12:05:00Z"
}
```

---

## ⚡ 5. Performans ve Güvenlik

*   **Redis Backplane:** Notification servisi scale edildiğinde (birden fazla instance), instance'lar arası mesaj senkronizasyonunu Redis sağlar.
*   **Secure Handshake:** Token, `WSS` (WebSocket Secure) üzerinden şifreli iletilir.
*   **Auto Reconnect:** Bağlantı koptuğunda frontend otomatik olarak (0s, 2s, 10s...) yeniden bağlanmayı dener.

---

> *Bu doküman, ForgeFlow projesinin "Notification System Revizyonu" (Şubat 2026) sonrası güncel mimarisini yansıtmaktadır.*
