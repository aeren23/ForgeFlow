# ForgeFlow - Issue Workflow & Permission Design

Bu doküman, ForgeFlow'daki issue iş akışı ve yetki sisteminin tasarımını içerir.

## Mevcut Durum

| İşlem | Nasıl Çalışıyor |
|-------|-----------------|
| To Do → In Progress | Manuel sürükle-bırak |
| In Progress → Done | Manuel sürükle-bırak |
| Assign | Manuel dropdown |

---

## Önerilen Gelişmiş Sistem

```
┌─────────────────────────────────────────────────────────────────┐
│                        TO DO                                     │
│  • Herkes oluşturabilir (Viewer hariç)                          │
│  • Backlog olarak bekler                                        │
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼ (Assign yapılınca OTOMATİK)
┌─────────────────────────────────────────────────────────────────┐
│                     IN PROGRESS                                  │
│  • Sadece assignee veya Admin/Owner sürükleyebilir              │
│  • GitHub'da branch oluşturulunca otomatik geçiş (opsiyonel)    │
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼ (PR merge edilince OTOMATİK)
┌─────────────────────────────────────────────────────────────────┐
│                    IN REVIEW (Yeni!)                            │
│  • "Ready for Review" butonu ile geçiş                          │
│  • GitHub PR açılınca otomatik                                  │
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼ (PR onay + merge)
┌─────────────────────────────────────────────────────────────────┐
│                        DONE                                      │
│  • PR merge → Otomatik Done                                     │
│  • Veya Admin/Owner manuel kapatabilir                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## Yetki Matrisi

| İşlem | Viewer | Member | Admin | Owner |
|-------|--------|--------|-------|-------|
| To Do → In Progress (kendi issue'su) | ❌ | ✅ | ✅ | ✅ |
| To Do → In Progress (başkasının) | ❌ | ❌ | ✅ | ✅ |
| In Progress → In Review | ❌ | ✅ (sadece assignee) | ✅ | ✅ |
| In Review → Done (manuel) | ❌ | ❌ | ✅ | ✅ |
| Herhangi bir geri alma | ❌ | ✅ (kendi) | ✅ | ✅ |

---

## GitHub Entegrasyonu ile Akış

```
1. Issue assign edilir (To Do → In Progress)
         │
         ▼
2. Kullanıcı "Start Working" tıklar
   → Backend: feature/{issue-key}-{slug} branch oluşturur
   → Örnek: feature/BLOG-42-add-dark-mode
         │
         ▼
3. Geliştirici commit'ler yapar
         │
         ▼
4. PR açılır (In Progress → In Review)
   → GitHub Webhook → ForgeFlow'a bildirir
   → Issue otomatik "In Review" olur
         │
         ▼
5. PR merge edilir
   → GitHub Webhook → ForgeFlow
   → Issue otomatik "Done" olur
   → (Opsiyonel) Branch silinir
```

---

## Git Flow Entegrasyonu

```
main ────────────────────────────────────────────────► production
  │
  └── develop ────────────────────────────────────────► staging
         │
         ├── feature/BLOG-42-dark-mode ──┐
         │                               │ PR → merge
         ├── feature/BLOG-43-auth ───────┤
         │                               ▼
         └────────────────────────────────────────────► develop
```

**ForgeFlow'un rolü:**
- Issue key'i branch adına otomatik eklemek
- PR description'a issue linkini koymak
- PR merge edilince issue'yu kapatmak

---

## Uygulama Fazları

### Faz 1 (Basit) ✅
1. **Assign = In Progress**: Birini atadığında otomatik "In Progress"
2. **Yetki kontrolü**: Sadece assignee veya Admin/Owner sürükleyebilsin
3. **Manuel Done**: Şimdilik el ile kapatma

### Faz 2 (GitHub Entegrasyonu ile)
1. "Start Working" butonu → Branch oluştur
2. PR açılınca → "In Review" durumu ekle
3. PR merge → Otomatik "Done"

### Faz 3 (Gelişmiş)
1. Git Flow tam entegrasyon
2. CI/CD pipeline durumu gösterimi
3. Deployment tracking

---

## Teknik Notlar

### Backend Değişiklikleri (Faz 1)
- `AssignIssueHandler`: Assign yapılırken status'u "InProgress" yap
- `ChangeIssueStatusHandler`: Yetki kontrolü ekle

### Frontend Değişiklikleri (Faz 1)
- `ProjectBoard.tsx`: Drag-drop yetki kontrolü
- Toast mesajları ile feedback

---

*Son Güncelleme: 2026-02-02*
