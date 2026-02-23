# Faz 5: Gelişmiş Özellikler — Taslak Plan

> ⚠️ Bu doküman **taslak** niteliğindedir. Faz 4 tamamlandıktan sonra detaylandırılacaktır.

---

## 5A. AI ile GitHub Actions Workflow Üretimi 🤖

### Problem
Kullanıcı CI/CD'nin ne olduğunu bilmeyebilir veya workflow dosyası yazmayı bilmeyebilir. ForgeFlow'un vizyonu "AI ile süreç üret" — bu, workflow dosyasını da kapsar.

### Çözüm
Kullanıcının reposunu analiz edip, uygun GitHub Actions workflow YAML'ı üreten AI destekli bir modül.

### Neden Sadece TechStack Yetmez?
- Aynı tech stack, farklı dosya yapıları (monorepo vs single project)
- Build dosyalarının konumu değişir (`.sln`, `package.json`, `Dockerfile`)
- Test projeleri farklı yerlerde olabilir
- Docker kullanılıyor olabilir ya da olmayabilir
- Birden fazla servis olabilir (microservice)

### Gerekli Veri Kaynakları

| Veri | GitHub API Endpoint | Kullanım |
|------|---------------------|----------|
| Dosya ağacı | `GET /repos/{owner}/{repo}/git/trees/{branch}?recursive=1` | Repo yapısını anlama |
| Solution dosyası (.sln) | `GET /repos/{owner}/{repo}/contents/{path}` | .NET projelerini tespit |
| package.json | Aynı endpoint | Frontend build/test script'leri |
| Dockerfile | Aynı endpoint | Docker build varlığı |
| docker-compose.yml | Aynı endpoint | Multi-service yapı |
| Mevcut workflow'lar | `.github/workflows/` dizin kontrolü | Duplicate önleme |
| .csproj dosyaları | Aynı endpoint | Target framework, test framework tespiti |

### Akış

```
1. Kullanıcı "AI ile Workflow Oluştur" butonuna tıklar
         │
         ▼
2. GitHub Service → GitHub API ile repo ağacını çeker
         │
         ▼
3. Kritik dosyaların içeriklerini çeker:
   - *.sln, package.json, Dockerfile, docker-compose.yml
   - *.csproj (test framework: xUnit, NUnit, MSTest?)
   - tsconfig.json, vite.config.ts, webpack.config.js
         │
         ▼
4. Bu bilgiyi AI Orchestrator'a gönderir:
   Event: WorkflowGenerationRequested
   Payload: {
     repoTree: [...],
     solutionContent: "...",
     packageJson: "...",
     dockerfiles: [...],
     techStack: ["C#", ".NET 8", "React"],
     existingWorkflows: []
   }
         │
         ▼
5. AI Orchestrator workflow YAML üretir:
   - Build adımları (hangi dizinde, hangi komutla)
   - Test adımları (test projeleri nerede?)
   - Docker build (Dockerfile varsa)
   - Multi-stage (frontend + backend ayrı job?)
   - Caching (NuGet, npm cache)
         │
         ▼
6. Sonuç kullanıcıya preview olarak gösterilir
         │
         ▼
7. Kullanıcı seçenekleri:
   a) "PR olarak gönder" → GitHub API ile PR açılır
   b) "Kopyala" → Clipboard'a kopyalar, kullanıcı kendisi ekler
   c) "Düzenle" → Inline editor ile değişiklik yapar, sonra gönderir
```

### AI Prompt Stratejisi

```
"Aşağıdaki repo yapısını analiz et ve GitHub Actions CI/CD 
workflow YAML dosyası oluştur.

Kurallar:
1. PR açılınca ve main/develop'a push yapılınca çalışsın
2. Build + Test adımları olsun
3. Dockerfile varsa Docker build ekle
4. Test framework'e uygun test komutu kullan
5. Caching ekle (NuGet/npm)
6. Monorepo ise path filter kullan

Repo bilgileri:
- Tech Stack: {techStack}
- Dosya ağacı: {repoTree}
- Solution: {solutionContent}
- Package.json: {packageJson}
- Dockerfile'lar: {dockerfiles}
- Mevcut workflow'lar: {existingWorkflows}"
```

### Servis Etkileri

| Servis | Değişiklik |
|--------|-----------|
| Contracts | `WorkflowGenerationRequested`, `WorkflowGenerationCompleted` event'leri |
| GitHub Service | Repo ağacı + dosya içeriği çekme endpoint/consumer |
| AI Orchestrator | Workflow generation prompt + handler |
| Artifact Service | Üretilen workflow'u artifact olarak saklama |
| Frontend | "AI ile Workflow Oluştur" butonu, preview UI, PR gönderme |

---

## 5B. Release Notes Otomasyonu 📝

### Problem
Sprint/dönem bittiğinde "neler yapıldı?" sorusuna cevap vermek zor. Done issue'ları tek tek toplayıp release notes yazmak zaman kaybı.

### Çözüm
Done durumuna geçmiş issue'lardan otomatik release notes üretimi.

### Akış

```
1. Kullanıcı "Release Notes Oluştur" butonuna tıklar
2. Tarih aralığı veya milestone seçer
3. ForgeFlow o aralıktaki Done issue'ları toplar:
   - Issue başlıkları
   - Issue tipleri (Feature, Bug, Task)
   - AI plan özetleri (Artifact Service'den)
   - PR merge tarihleri
4. AI bu bilgiyle structured release notes üretir:
   - Features, Bug Fixes, Improvements kategorileri
   - Breaking changes uyarısı
   - Contributor listesi
5. Sonuç markdown olarak gösterilir
6. Kullanıcı GitHub Release olarak publish edebilir
```

### Gerekli Veri

- Work Service: Done issue listesi (tarih filtreli)
- Artifact Service: Issue'lara bağlı AI plan özetleri
- GitHub Service: PR merge bilgileri, contributor listesi

---

## 5C. Sprint / Milestone Desteği 📅

### Problem
Issue'lar zamansız. "Bu hafta ne yapılacak?" sorusuna cevap yok.

### Çözüm
Issue'ları sprint'lere veya milestone'lara gruplama.

### Taslak Yapı

```
Sprint/Milestone entity:
  - Id, Name, Description
  - StartDate, EndDate
  - ProjectId
  - Status (Active, Completed, Planned)

Issue entity'ye eklenen:
  - SprintId (nullable FK)
```

### Frontend
- Sprint board view (sprint'e göre filtrelenmiş Kanban)
- Sprint planlama sayfası (drag-drop ile issue'ları sprint'e atama)
- Sprint burndown chart (opsiyonel)

---

## 5D. Dashboard & Analytics 📊

### Problem
Proje ilerlemesi hakkında görsel veri yok.

### Metrikler

| Metrik | Hesaplama | Görselleştirme |
|--------|-----------|---------------|
| **Velocity** | Sprint başına tamamlanan issue sayısı | Bar chart |
| **Cycle Time** | Issue Open → Done arası ortalama süre | Line chart |
| **AI Accuracy** | AI plan vs gerçek implementation uyumu | Score card |
| **CI/CD Success Rate** | Başarılı pipeline / toplam pipeline oranı | Pie chart |
| **Issue Distribution** | Tip (Bug/Feature/Task) ve öncelik dağılımı | Donut chart |
| **Team Productivity** | Kişi başı tamamlanan issue | Leaderboard |

### Veri Kaynakları
- Work Service: Issue metrikleri (status, dates, assignee)
- Artifact Service: AI plan verileri
- Work Service: CI/CD status verileri
- GitHub Service: PR metrikleri

---

## 5E. Test Plan Execution Eşleştirme 🧪

### Problem
AI test planı üretiyor ama gerçek test sonuçlarıyla eşleştirilemiyor.

### Çözüm

```
AI ürettiği test planı:
  ✅ "Login validasyonu test edilmeli"
  ✅ "Token expiry kontrol edilmeli"
  ❌ "Rate limiting test edilmeli" (henüz yazılmamış)
  
  Coverage: 2/3 (%67)
```

### Akış
1. AI test planı üretilir (mevcut)
2. CI/CD'den test raporu gelir (JUnit XML, TRX format)
3. AI, test planı maddelerini gerçek test isimleriyle eşleştirir
4. Coverage skoru hesaplanır
5. Eksik testler listelenir

> Bu özellik, CI/CD test raporu parsing gerektirir (Faz 4 altyapısına bağımlı).

---

## 5F. Diğer Özellikler

| Özellik | Zorluk | Öncelik | Bağımlılık |
|---------|--------|---------|-----------|
| **Multi-repo Support** | Orta | Düşük | GitHub Service refactor |
| **GitHub OAuth Login** | Orta | Düşük | Identity Service + OAuth flow |
| **Template Engine** | Orta | Orta | AI Orchestrator prompt management |
| **Notification Preferences** | Kolay | Düşük | Notification Service + Frontend |

---

## Tavsiye Edilen Sıralama

```
Faz 4 tamamlandıktan sonra:

1. 📝 Release Notes Otomasyonu    → Hızlı, görünür etki
2. 🤖 AI Workflow Üretimi         → ForgeFlow'un "killer feature"larından
3. 📊 Dashboard & Analytics       → Proje yöneticileri için değerli
4. 📅 Sprint/Milestone            → İş akışı olgunlaşması
5. 🧪 Test Plan Execution         → AI accuracy ölçümü
6. 🔧 Diğerleri                   → İhtiyaca göre
```

---

> 📌 **Not:** Bu taslak Faz 4 tamamlandıktan sonra güncellenecek ve detaylandırılacaktır. Her alt modül kendi implementation plan dokümanına sahip olacak.
