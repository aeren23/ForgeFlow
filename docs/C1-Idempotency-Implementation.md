# 🛡️ ForgeFlow: Resilience & Idempotency Implementation

**Tarih:** 16 Ocak 2026  
**Kapsam:** C1 Milestone - Event-Driven Reliability  
**Geliştirici:** Ali Eren Oğuztaş

---

## 1. Problem Tanımı (The "At-Least-Once" Challenge)

ForgeFlow, RabbitMQ üzerinden asenkron mesajlaşma kullanmaktadır. Dağıtık sistemlerde ağ kesintileri veya servis hataları durumunda **"en az bir kere teslimat" (at-least-once delivery)** garantisi nedeniyle aynı mesajın (event) tekrar işlenme riski bulunur. 

Bu durum:
- Veritabanında **mükerrer kayıtlar** (duplicate revisions) oluşmasına
- **Veri bütünlüğünün** bozulmasına

yol açar.

---

## 2. Idempotency Neden Gereklidir?

**Idempotency (Eşgüçlülük)**, bir işlemin birden fazla kez çalıştırılsa bile aynı sonucu üretmesini garanti eden özelliktir.

### Neden Kritik?

| Senaryo | Idempotency Olmadan | Idempotency İle |
|---------|---------------------|-----------------|
| **Ağ kesintisi** | Retry → Duplicate kayıt | Retry → Aynı kayıt, yan etki yok |
| **Consumer crash** | Mesaj tekrar işlenir → Mükerrer veri | CorrelationId check → Skip |
| **Manuel replay** | Tüm eventler tekrar yazılır | Sadece yeni eventler işlenir |

### Gerçek Hayat Senaryosu

```
1. Consumer "ArtifactGenerated" event alır
2. DB'ye revision yazar
3. ACK göndermeden önce crash olur
4. RabbitMQ mesajı tekrar teslim eder
5. ❌ Idempotency yok → 2. revision (duplicate!)
   ✅ Idempotency var → CorrelationId check → Skip
```

### Enterprise Sistemlerde Zorunluluk

- **Finansal sistemler:** Aynı ödeme 2 kez çekilmemeli
- **E-ticaret:** Aynı sipariş 2 kez oluşturulmamalı
- **SDLC (ForgeFlow):** Aynı artifact revision mükerrer kaydedilmemeli

---

## 3. Mimari Çözüm: CorrelationId & Idempotency

Bu sorunu çözmek için **"Exactly-Once Processing"** simülasyonu sağlayan **Idempotency** stratejisi uygulanmıştır. Her event, bir `EventEnvelope` içerisinde benzersiz bir `CorrelationId` ile taşınır.

### Uygulanan Katmanlı Çözümler

#### 🏗️ Domain Layer

- `ArtifactRevision` entity'si, her revizyonun hangi event tetiklemesiyle oluştuğunu bilmesi için `CorrelationId` alanıyla genişletildi.
- Domain logic seviyesinde revizyon ekleme metotları bu kimliği zorunlu/isteğe bağlı olarak kabul edecek şekilde güncellendi.

```csharp
public string? CorrelationId { get; private set; }

public ArtifactRevision AddRevision(string contentJson, string contentHash, string? correlationId = null)
```

#### 💡 Application Layer (CQRS & Validation)

- `IArtifactRepository` arayüzüne `RevisionExistsByCorrelationIdAsync` metodu eklendi.
- `UpsertArtifactRevisionHandler` içerisinde **"Check-then-Act"** paterni uygulandı: İşlem yapılmadan önce bu `CorrelationId` ile daha önce bir kayıt atılıp atılmadığı kontrol edilir.
- Mükerrer istek durumunda işlem durdurularak sistemin yan etki üretmesi engellendi.

```csharp
var existingRevision = await _repository.RevisionExistsByCorrelationIdAsync(request.CorrelationId, ct);
if (existingRevision)
{
    _logger.LogWarning("Duplicate event detected. CorrelationId: {CorrelationId}", request.CorrelationId);
    return -1; // Skip processing
}
```

#### 🏗️ Infrastructure Layer (Persistence)

- **EF Core Unique Index:** DB seviyesinde son kale olarak `CorrelationId` kolonuna Unique Index eklendi.
- **Partial Indexing:** Eski kayıtları bozmamak ve esneklik sağlamak için `[CorrelationId] IS NOT NULL` filtresiyle index performansı ve tutarlılığı optimize edildi.

```csharp
b.HasIndex(x => x.CorrelationId)
    .IsUnique()
    .HasFilter("[CorrelationId] IS NOT NULL");
```

#### 🚀 API & Messaging Layer

- **MassTransit Retry Policy:** Geçici hatalar (DB timeout vb.) için **"Incremental Retry"** (1s, 3s, 5s) politikası belirlendi.
- **Structured Logging (Seq):** `CorrelationId` üzerinden dağıtık izleme (distributed tracing) sağlandı. Seq üzerinden bir işlemin tüm yaşam döngüsü tek bir ID ile takip edilebilir hale getirildi.

```csharp
e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

using var scope = _logger.BeginScope(new Dictionary<string, object> 
{ 
    ["CorrelationId"] = msg.CorrelationId,
    ["EventId"] = msg.EventId 
});
```

---

## 4. Akış Diyagramı

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐     ┌────────────┐
│  RabbitMQ   │────▶│   Consumer   │────▶│   Handler   │────▶│  Database  │
│   Event     │     │  (Logging)   │     │ (Check-Act) │     │ (Unique)   │
└─────────────┘     └──────────────┘     └─────────────┘     └────────────┘
       │                   │                    │                   │
       │              CorrelationId        Idempotency          Last Line
       │               Scope Log             Check              of Defense
       │                   │                    │                   │
       ▼                   ▼                    ▼                   ▼
   at-least-once    Seq Tracing         Code Level          DB Level
    guarantee       ile izleme            kontrol            kontrol
```

---

## 5. Teknik Çıktılar (Key Takeaways)

| Özellik | Açıklama |
|---------|----------|
| **Reliability** | Mesaj tekrar gelse bile veritabanı tutarlılığı korunur |
| **Observability** | Seq entegrasyonu sayesinde "Duplicate event detected" uyarıları anlık izlenebilir |
| **Fault Tolerance** | Artan aralıklı (incremental) retry mekanizması ile sistem geçici kesintileri kullanıcıya hissettirmeden aşar |

---

*Bu doküman, ForgeFlow projesinin C1 Milestone kapsamında yapılan Resilience & Idempotency çalışmalarını özetlemektedir.*

