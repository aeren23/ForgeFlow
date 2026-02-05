# GitHub Entegrasyonu Test Talimatları

Harika, test etmeye hazırsınız!

Yaptığım son incelemelere göre:
1.  **Environment Değişkenleri:** `infra/.env` dosyasındaki `GITHUB_APP_ID`, `PRIVATE_KEY` vb. değerlerin `docker-compose.yml` üzerinden `forgeflow-github` servisine doğru şekilde maplendiği doğrulanmıştır.
2.  **Otomatik Migration:** `ForgeFlow.GitHub.Api/Program.cs` içinde `db.Database.Migrate()` komutu bulunduğu için, servis ayağa kalkarken veritabanı tablolarınız otomatik oluşturulacaktır.
3.  **Frontend/Backend Uyumu:** Frontend'in çağırdığı `/api/installations` endpointleri ile Gateway ve Backend yapılandırması birbiriyle tam uyumludur.

## Test Öncesi Son Adım
Environment değişkenlerinin servise yüklenmesi için `github` (veya `forgeflow-github`) servisini yeniden başlatmanız (restart) gerekebilir:

```bash
docker-compose restart github
# veya
docker restart forgeflow-github
```

Kod tarafındaki (Logic ve Syntax) tüm engeller kaldırılmıştır. Gönül rahatlığıyla test edebilirsiniz. Başarılar!
