# ForgeFlow Yol Haritası ve Gelecek Planları 🚀

> Bu doküman, ForgeFlow'un mevcut durumunu analiz ederek, sıradaki adımlar ve gelecek vizyonu için öneri ve faz planlaması içerir.

---

## 📍 Mevcut Durum (Şu An Neredeyiz?)

| Bileşen | Durum | Detay |
|---------|-------|-------|
| **Identity Service** | ✅ Tamamlandı | JWT Auth, kullanıcı yönetimi |
| **Work Service** | ✅ Tamamlandı | Project, Issue CRUD, Kanban board |
| **AI Orchestrator** | ✅ Tamamlandı | Gemini/Groq ile plan üretimi, idempotency, audit |
| **Artifact Service** | ✅ Tamamlandı | AI çıktıları saklama, versiyonlama |
| **GitHub Service** | ✅ Temel | Branch oluşturma (assign → In Progress) |
| **Notification Service** | ✅ Tamamlandı | SignalR + Redis backplane |
| **Gateway (YARP)** | ✅ Tamamlandı | Routing + Auth |
| **Frontend** | ✅ Temel | React + Vite, Kanban board |
| **İş Akışı** | ⚠️ Kısmi | To Do → In Progress (assign ile), Done (manuel) |

**Özet:** Faz 1 (Basit) tamamlandı. Assign edilince **In Progress** + **GitHub branch oluşturma** çalışıyor. Ancak **In Review** durumu, **Webhook** entegrasyonu ve **PR otomasyonu** henüz yok.

---

## 🤔 Sorularına Yanıtlar

### 1. Branch Oluşturma Opsiyonel Olmalı mı?

**Önerim: Evet, opsiyonel yapmalıyız.** Gerekçeler:

| Senaryo | Branch Gereksiz | Branch Gerekli |
|---------|----------------|----------------|
| Dokümantasyon değişikliği | ✅ | |
| UI renk düzeltmesi | ✅ | |
| Yeni feature geliştirme | | ✅ |
| Bug fix | | ✅ |
| Tasarım tartışması issue'su | ✅ | |

**Uygulama Önerisi:**
- **Proje ayarlarında** `autoCreateBranch: true/false` seçeneği olsun
- Assign edildiğinde proje ayarına bakılsın
- Alternatif: Issue üzerinde "Start Working" butonu ile **manuel** branch oluşturma (workflow-design.md'de zaten önerilmiş)

> [!TIP]
> MVP için en pragmatik yol: Proje seviyesinde bir toggle eklemek. Branch oluşturma **varsayılan açık** olsun ama kapanabilsin. Detaylı issue-bazlı kontrol Phase 2'ye bırakılabilir.

---

### 2. Issue Bittikten Sonra Review Süreci

Mevcut durumda **In Review** durumu yok. Bu büyük bir eksiklik çünkü ForgeFlow'un vizyonu **süreç yönetimi**. Önerim:

```
In Progress ──► "Ready for Review" ──► In Review ──► Done
                    (buton ile)          (PR merge ile)
```

**Kod Review Otomasyonu İçin Seçenekler:**

| Yöntem | Açıklama | Zorluk |
|--------|----------|--------|
| **AI Code Review** | PR açıldığında AI diff'i analiz eder, yorum bırakır | Orta |
| **Checklist Validation** | Security checklist, test coverage kontrolü | Kolay |
| **Automated Testing** | PR branch'inde testleri çalıştır, sonucu issue'ya yaz | Orta |
| **Quality Gate** | Tüm checkler geçmeden merge engellensin | Zor |

---

### 3. PR Açıldığında Otomatik Review

Bu ForgeFlow'un en güçlü diferansiyatörlerinden biri olabilir:

```
PR Açıldı (GitHub Webhook)
    │
    ├── 1. ForgeFlow issue'yu "In Review" yap
    │
    ├── 2. AI Code Review başlat
    │       ├── Diff analizi
    │       ├── Original plan ile karşılaştırma
    │       ├── Security checklist kontrolü
    │       └── PR'a yorum olarak yaz
    │
    ├── 3. Test sonuçlarını izle (CI/CD webhook)
    │
    └── 4. Tüm checkler OK → "Ready to Merge" etiketle
```

---

## 📋 Önerilen Faz Planı

### Faz 2: İş Akışı Olgunlaştırma ⭐ (Öncelik)

Bu faz, mevcut temel akışı tamamlayarak gerçek bir SDLC sürecine dönüştürür.

#### 2A. Branch Oluşturmayı Opsiyonel Yapma
- Proje entity'sine `AutoCreateBranch` (bool) alanı ekle
- `AssignIssueHandler`'da bu ayarı kontrol et
- Frontend proje ayarlarına toggle ekle

#### 2B. "In Review" Durumu Ekleme
- `IssueStatus` enum'una `InReview` ekle
- "Ready for Review" butonu (frontend + backend)
- Kanban board'da yeni sütun
- Yetki kontrolü: Sadece assignee veya Admin "In Review"e geçebilsin

#### 2C. GitHub Webhook Entegrasyonu
- PR açıldığında → Issue otomatik "In Review"
- PR merge edildiğinde → Issue otomatik "Done"
- PR kapatıldığında (merge olmadan) → Issue "In Progress"a geri dön
- Webhook endpoint'i Gateway'e ekleme

#### 2D. Manuel Done Kaldırma (Opsiyonel)
- "Done" durumuna geçiş sadece PR merge veya Admin onayı ile olsun

---

### Faz 3: AI-Powered Code Review 🧠

ForgeFlow'un en büyük katma değer noktası.

#### 3A. PR Diff Analizi
- GitHub Service üzerinden PR diff'ini çek
- AI Orchestrator'a `CodeReviewRequested` event'i ekle
- AI diff'i analiz etsin:
  - Bug risk
  - Security açıkları
  - Performance iyileştirmeleri
  - Clean Architecture uyumu

#### 3B. Plan-Code Karşılaştırma
- Orijinal AI planı (Artifact Service'den) ile gerçek PR'ı karşılaştır
- "Plan dışı değişiklikler" raporla
- Eksik implementasyonları işaretle

#### 3C. Sonuçları GitHub'a Yazma
- PR'a yorum olarak review sonuçlarını yaz (`forgeflow[bot]` olarak)
- Review'u "Approve", "Request Changes" veya "Comment" olarak işaretle
- Review sonuçlarını ForgeFlow dashboard'da da göster

---

### Faz 4: CI/CD Entegrasyonu 🔄

#### 4A. GitHub Actions Webhook'ları
- `workflow_run` ve `check_suite` event'lerini dinle
- Build/Test durumunu ForgeFlow'da göster
- Issue'ya CI/CD durumu badge'i ekle

#### 4B. Quality Gate
- Tüm checkler (AI review + CI/CD + test coverage) geçmeden Done'a geçmeyi engelle
- Dashboard'da "health score" göster

---

### Faz 5: Gelişmiş Özellikler 🌟

| Özellik | Açıklama | Öncelik |
|---------|----------|---------|
| **Release Notes Otomasyonu** | Done olan issue'lardan otomatik release notes üret | Yüksek |
| **Sprint/Milestone** | Issue'ları sprint'lere grupla | Orta |
| **Test Plan Execution** | AI'ın ürettiği test planını gerçek test sonuçlarıyla eşleştir | Yüksek |
| **Dashboard & Analytics** | Proje metrikler (velocity, cycle time, AI accuracy) | Orta |
| **Multi-repo Support** | Bir projeye birden fazla repo bağlama | Düşük |
| **GitHub OAuth Login** | "GitHub ile Giriş Yap" | Düşük |
| **Template Engine** | Farklı proje tipleri için AI prompt şablonları | Orta |
| **Notification Preferences** | Kullanıcı bazlı bildirim tercihleri | Düşük |

---

## 🎯 Öncelik Matrisi

```
                    ETKİ
            Düşük          Yüksek
         ┌──────────┬──────────────┐
  Kolay  │ Notif.   │ ⭐ Branch    │
         │ Prefs    │ opsiyonel    │
ZORLUK   ├──────────┼──────────────┤
         │ Multi-   │ ⭐ In Review │
  Orta   │ repo     │ ⭐ Webhook   │
         │          │ ⭐ AI Review  │
         ├──────────┼──────────────┤
  Zor    │ GitHub   │ CI/CD Gate   │
         │ OAuth    │ Full Quality │
         └──────────┴──────────────┘
```

**Sonuç:** Sağ üst köşedeki (kolay + yüksek etki) özelliklerle başlamak en mantıklısı.

---

## 🏁 Tavsiye Edilen Sıralama

1. **Branch oluşturmayı opsiyonel yap** → Küçük değişiklik, hemen yapılabilir
2. **In Review durumu ekle** → Temel iş akışını tamamlar
3. **GitHub Webhook (PR events)** → Otomasyonun temeli
4. **AI Code Review** → ForgeFlow'un "killer feature"ı
5. **CI/CD entegrasyonu** → Tam döngüyü kapatır

> [!IMPORTANT]
> En kritik adım **Faz 2**'dir. Bu faz tamamlandığında ForgeFlow gerçek bir SDLC aracı haline gelir. Faz 3 (AI Code Review) ise projenin piyasadaki diferansiyatörü olacaktır.
