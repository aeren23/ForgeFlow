
# ForgeFlow 🚀  
**AI-Assisted Software Delivery Platform**

> From idea to production-ready plan — with AI, microservices, and event-driven architecture.

---

## 📌 Overview

**ForgeFlow**, yazılım geliştirme sürecinde (SDLC) fikirlerin ve gereksinimlerin;  
**plan**, **task**, **test planı**, **security checklist** ve **release artefact’larına**  
dönüştürülmesini sağlayan **AI destekli, event-driven bir delivery platformudur**.

Bu platformun amacı:
- Yazılım geliştirme sürecini **daha erken aşamada kaliteyle buluşturmak**
- AI’yı “kod yazan bot” değil, **süreç hızlandırıcı** olarak kullanmak
- Tek kişilik veya küçük ekiplerin **kurumsal seviye üretim standardına** ulaşmasını sağlamak

---

## 🎯 Problem Tanımı

Günümüz yazılım projelerinde sık görülen problemler:

- Issue’lar belirsiz ve eksik yazılıyor
- Test planı ve security kontrolleri geç aşamada düşünülüyor
- AI chat araçları kullanılıyor ama:
  - Çıktılar kayıt altına alınmıyor
  - Versiyonlanmıyor
  - Süreçle entegre edilmiyor
- Solo developer’lar PM, QA, Security ve DevOps rollerini tek başına üstlenmek zorunda

**ForgeFlow**, bu problemleri **süreç + otomasyon + kayıt** yaklaşımıyla çözer.

---

## 🌍 Vizyon

> AI destekli yazılım geliştirme süreçlerini **standartlaştıran**,  
> kaliteyi kod yazılmadan önce üreten,  
> modern ve güvenli bir delivery platformu oluşturmak.

---

## 🧭 Misyon

- AI’yı **SDLC artefact üretiminde** güvenilir ve tekrarlanabilir hale getirmek  
- Event-driven microservice mimarisiyle **ölçeklenebilir** bir yapı sunmak  
- Test, security ve audit kavramlarını sürecin **merkezine** yerleştirmek  
- Tek geliştiricinin bile **ekip standardında** üretim yapabilmesini sağlamak

---

## 🧠 Temel Kavramlar

### SDLC Artefact Nedir?
Yazılım geliştirme yaşam döngüsünde üretilen somut çıktılardır:

- Requirement / User Story
- Task Listesi
- Test Planı (Unit / Integration / E2E)
- Security Checklist (OWASP mapping)
- Release Notes
- Audit Log kayıtları

ForgeFlow, bu artefact’ları **AI ile üretir, doğrular, versiyonlar ve saklar**.

---

## 🧩 Platform Ne Yapar?

- Issue oluşturulmasını sağlar
- AI ile aşağıdaki artefact’ları üretir:
  - Plan & Task Breakdown
  - Test Planı
  - Security Checklist
  - Release Notes
- Üretilen çıktıları:
  - JSON Schema ile doğrular
  - Versiyonlayarak saklar
- Event-driven yapı ile servisleri gevşek bağlar
- CI/CD sonuçlarını ilişkilendirir
- Audit log ile tüm aksiyonları kayıt altına alır

---

## 🏗️ Mimari Yaklaşım

### Genel Mimari
```text
     Frontend
        ↓
API Gateway (YARP)
        ↓
+----------+   +------+   +-----------------+   +----------+
| Identity |   | Work |   | AI Orchestrator |   | Artifact |
+----------+   +------+   +-----------------+   +----------+
                    ↓               ↓                 ↓
                            RabbitMQ
```

### Mimari Prensipler
- **Microservice Architecture**
- **Event-Driven Communication**
- **Onion Architecture (her servis içinde)**
- **CQRS (Command / Query ayrımı)**
- **Loose Coupling (RabbitMQ)**
- **Infrastructure as Code (Docker Compose)**

---

## 🧪 Örnek Kullanım Akışı (Click-by-Click)

1. Kullanıcı giriş yapar
2. Yeni bir Issue oluşturur  
   > “Refresh token support eklensin”
3. “Generate Plan” butonuna basar
4. Sistem:
   - `ai.plan.requested` event’i yayınlar
5. AI Orchestrator:
   - Plan + Task + Test + Security üretir
   - `artifact.generated` event’i yayınlar
6. Artifact Service:
   - JSON Schema validation yapar
   - Artefact’ı **Revision 1** olarak kaydeder
7. Kullanıcı artefact’ları dashboard’da görüntüler
8. Plan onaylanır ve audit log’a düşer

---

## 🔄 Event-Driven Akış (RabbitMQ)

| Event | Producer | Consumer |
|------|---------|----------|
| issue.created | Work Service | Audit / Notification |
| ai.plan.requested | Work Service | AI Orchestrator |
| artifact.generated | AI Orchestrator | Artifact Service |
| audit.event | All Services | Audit Service |

RabbitMQ burada:
- Servisleri birbirinden bağımsızlaştırır
- Arka plan işlerini güvenli şekilde yürütür
- Retry ve dayanıklılık sağlar

---

## 🔐 Güvenlik Yaklaşımı

- JWT Authentication + Refresh Token
- RBAC (Admin / Developer / Viewer)
- Rate Limiting
- Input Validation
- Audit Logging
- OWASP Top 10 checklist üretimi
- Hassas veriler için masked logging

---

## 🧰 Tech Stack

### Backend
- **.NET 8**
- ASP.NET Core Web API
- MassTransit (RabbitMQ)
- MediatR (CQRS)
- Entity Framework Core
- PostgreSQL

### Messaging
- RabbitMQ (Topic Exchange)
- Event Envelope + Correlation ID

### API Gateway
- YARP (ASP.NET Core Reverse Proxy)

### Frontend
- React (Vite + TypeScript)
- (Opsiyonel) Flowbite / Tailwind

### DevOps
- Docker & Docker Compose
- GitHub Actions (CI)
- Nginx (opsiyonel reverse proxy)

### Observability (MVP+)
- Serilog
- Health Checks
- (Opsiyonel) Seq / Prometheus

---

## 🧪 CI/CD (Özet)

- Push / PR sonrası:
  - Build
  - Test
  - (Opsiyonel) Security scan
- Pipeline sonucu:
  - Platform tarafından izlenebilir
  - Issue ve artefact’larla ilişkilendirilebilir

---

## 📦 Artefact Versiyonlama

- Her AI çıktısı **revision** olarak saklanır
- Eski versiyonlar korunur
- Plan revize edilebilir
- Onaylanan versiyonlar audit ile kayıt altına alınır

---

## 🧠 Neden ChatGPT Yerine ForgeFlow?

| ChatGPT | ForgeFlow |
|------|---------|
| Konuşma bazlı | Süreç bazlı |
| Kayıt yok | Versiyonlu artefact |
| Tek seferlik çıktı | Tekrarlanabilir standart |
| Audit yok | Audit & compliance |
| CI/CD entegrasyonu yok | SDLC entegrasyonu |

---

## 🚀 Hedef Kitle

- Solo developer & freelancer
- Startup ekipleri
- Junior ekipler
- Bootcamp / Üniversite projeleri
- Regulated domain projeleri (FinTech, Health)

---

## 📈 Roadmap (Özet)

### MVP
- Issue → AI artefact üretimi
- Artifact versioning
- RabbitMQ event flow
- Gateway + Auth
- Docker Compose

### V2
- CI pipeline entegrasyonu
- Notification service
- Audit service
- Observability dashboard

---

## 🧠 Proje Felsefesi

> “Kod yazmadan önce kalite üret.”

ForgeFlow, AI’yı yazılım sürecinin merkezine koyar  
ama **kontrolü ve güvenliği insanda bırakır**.

---

## 📜 Lisans
MIT (placeholder)


