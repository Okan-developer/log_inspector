# LogInspector

Java/Spring Boot, .NET ve Oracle log dosyalarını tarayıcıda analiz eden, **bellek içi (memory-only)** çalışan bir ASP.NET Core MVC uygulaması.

## Amaç

Uzun log dosyalarında hataları elle aramak yerine; seviye dağılımını görmek, tekrarlayan exception'ları gruplamak, ilgili stack trace'e hızlıca ulaşmak ve sonucu CSV olarak dışa aktarmak. Loglar diske yazılmaz, veritabanı gerektirmez.

## Teknolojiler

| Alan | Kullanılan |
|------|------------|
| Platform | .NET 10, ASP.NET Core MVC (Razor views) |
| Dil | C# (nullable + implicit usings açık) |
| Arayüz | Sunucu taraflı Razor, tek CSS dosyası (`wwwroot/css/site.css`), JavaScript yok |
| Test | xUnit, `Microsoft.AspNetCore.Mvc.Testing` |
| CI | GitHub Actions (`.github/workflows/ci.yml`) |
| Ek bağımlılık | Uygulama tarafında yok (veritabanı, ORM, UI kütüphanesi kullanılmıyor) |

## Özellikler

| Özellik | Açıklama |
|---------|----------|
| Dosya yükleme | `.log` / `.txt`, en fazla 5 MB; uzantı, boyut, UTF-8 ve binary içerik kontrolü |
| Metin yapıştırma | Dosya yerine log metni doğrudan yapıştırılabilir (aynı doğrulama) |
| Özet kartları | Toplam satır ile ERROR / WARN / INFO / DEBUG sayıları |
| Exception gruplama | Aynı tipteki exception'lar sayılarıyla gruplanır; her grupta ilk 5 örnek ve ilişkili stack trace gösterilir |
| Hata açıklamaları | Bilinen exception tipleri ve ORA kodları için kısa Türkçe açıklama |
| Filtreleme | Seviye, metin arama (büyük/küçük harf duyarsız) ve tarih/saat aralığı |
| CSV export | Filtrelenmiş sonucun tamamı indirilir (`Line, Timestamp, Level, ExceptionType, Message`) |
| Örnek loglar | Tek tıkla sentetik Java/Spring Boot ve .NET örnek logu |
| Son analizler | Bellekte tutulan son analizler listelenir, tek tek silinebilir |
| Arayüz | Responsive koyu tema; sayfada en fazla 1000 satır gösterilir, CSV tüm eşleşmeleri içerir |

## Desteklenen log türleri

| Tür | Algılanan |
|-----|-----------|
| Java / Spring Boot | `Fully.Qualified.SomeException`, `...Error`, `at pkg.Class.method(File.java:10)`, `Caused by:`, `... N more` / `... N common frames omitted` |
| .NET | `System.XxxException`, kısa `XxxException` adları, `at Ns.Class.Method(...)`, `--- End of ...`, `--->`; `info:` / `warn:` / `fail:` / `crit:` / `dbug:` / `trce:` satır başı önekleri |
| Oracle | `ORA-NNNNN` kodları; açıklaması tanımlı kodlar: 00001, 00904, 00942, 01017, 01400, 01403, 01422, 01722, 01843, 02291, 02292, 04031, 12154, 12514, 12541 |

Seviye eşlemesi: `ERR`/`FATAL`/`FAIL`/`CRIT` → ERROR, `WARNING` → WARN, `TRACE`/`DBUG`/`TRCE` → DEBUG. Seviyesi bulunamayan satırlar OTHER olur; stack/exception satırları son bilinen seviyeyi devralır. Seviye, satırın ilk 120 karakterindeki **büyük harfli** token'dan alınır.

## Kurulum ve çalıştırma

**Gereksinimler:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (başka bir şey gerekmez).

```bash
dotnet restore LogInspector.slnx
dotnet build LogInspector.slnx -c Release
dotnet run --project LogInspector.csproj
dotnet test LogInspector.slnx -c Release
```

1. `dotnet run` çıktısındaki yerel adresi (örn. `https://localhost:xxxx`) tarayıcıda açın.
2. `.log`/`.txt` dosyası seçin, metin yapıştırın veya bir örnek log düğmesine tıklayın.
3. Analiz sayfasında exception gruplarını inceleyin, filtreleyin, **CSV indir** ile dışa aktarın.

Performans benchmark'ları (yalnızca ölçüm, CI'da çalışmaz):

```bash
dotnet test tests/LogInspector.Tests -c Release --filter Category=Perf --logger "console;verbosity=detailed"
```

## Örnek

**Girdi:**

```text
2026-03-10 09:00:01 INFO Application started
2026-03-10 09:00:05 WARN Retrying payment call
2026-03-10 09:00:07 ERROR Query failed
java.sql.SQLException: ORA-00942: table or view does not exist
	at com.example.demo.OrderRepository.findAll(OrderRepository.java:42)
2026-03-10 09:00:09 ERROR ORA-00001: unique constraint violated
```

**Beklenen analiz** (parser kurallarından türetilmiştir):

| Kart | Değer |
|------|-------|
| Toplam satır | 6 |
| ERROR / WARN / INFO / DEBUG | 4 / 1 / 1 / 0 |

| Exception grubu | Adet | Açıklama |
|-----------------|-----:|----------|
| `java.sql.SQLException` | 1 | Veritabanı hatası; ORA/SQL koduna bakın. (stack trace: 1 satır) |
| `ORA-00001` | 1 | Benzersiz kısıt ihlali: aynı anahtarla kayıt zaten var. |

Not: Bir satırda hem Java exception sınıfı hem ORA kodu varsa grup, satırda önce geçen tipe göre (burada `java.sql.SQLException`) oluşturulur; ORA kodu mesajda görünmeye devam eder.

## Proje yapısı

```text
LogInspector/
├── Controllers/HomeController.cs      # Upload, Sample, Analysis, Export, Delete
├── Models/LogEntry.cs                 # LogEntry, ExceptionGroup, view model'ler
├── Services/
│   ├── LogParser.cs                   # satır ayrıştırma + exception gruplama
│   ├── LogFilter.cs                   # seviye / arama / tarih filtresi
│   ├── CsvExporter.cs                 # CSV üretimi (formula-injection koruması)
│   ├── UploadValidator.cs             # uzantı, boyut, UTF-8, binary kontrolü
│   ├── LogStore.cs                    # bellek içi depo (TTL + üst sınır)
│   ├── ErrorKnowledge.cs              # exception / ORA açıklamaları
│   └── SampleLogs.cs                  # sentetik örnek loglar
├── Views/                             # Home/Index, Home/Analysis, Shared/_Layout, Error
├── wwwroot/css/site.css
├── tests/LogInspector.Tests/          # xUnit testleri ve benchmark'lar
├── docs/                              # teknik belgeler
├── .github/workflows/ci.yml
├── Program.cs
└── LogInspector.slnx
```

## Mimari

```text
Browser ─ HomeController ─┬─ UploadValidator
                          ├─ LogStore (in-memory)
                          ├─ LogParser ─ ErrorKnowledge
                          ├─ LogFilter
                          └─ CsvExporter
```

| Rota | İşlev |
|------|-------|
| `GET /` | Yükleme formu, örnek log düğmeleri, son analizler |
| `POST Home/Upload` | Doğrula → belleğe kaydet → analiz sayfasına yönlendir |
| `POST Home/Sample` | Sentetik örnek log (`java` / `dotnet`) |
| `GET Home/Analysis/{id}` | Özet, exception grupları, filtreler (query string) |
| `GET Home/Export/{id}` | Filtrelenmiş CSV |
| `POST Home/Delete` | Kaydı bellekten siler |

Log her istekte yeniden ayrıştırılır (ölçümler için bkz. Performans). Ayrıntı: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Güvenlik ve gizlilik

- **Diske yazma yok:** `LogStore` bellek içidir (en fazla 20 kayıt, 60 dk TTL, yeniden başlatmada silinir). `FormOptions.MemoryBufferThreshold` yükleme limitinin üzerine ayarlıdır; framework geçici dosya oluşturmaz. Uygulama log içeriğini loglamaz.
- **Upload:** `.log`/`.txt` allow-list, 5 MB sınırı okunan gerçek bayt üzerinden uygulanır, strict UTF-8, NUL/kontrol karakteri reddi.
- **XSS:** Razor encoding, `Html.Raw` yok, CSP `default-src 'self'`.
- **CSRF:** Global `AutoValidateAntiforgeryToken`; durum değiştiren tüm işlemler POST.
- **CSV:** `= + - @ TAB CR` ile başlayan hücreler `'` ile etkisizleştirilir.
- **DoS:** Regex timeout (200 ms), satır başına 4000 karakter sınırı, 1000 satır gösterim sınırı.
- **Header'lar:** CSP, `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, HSTS (Development dışında).
- **Erişim:** Kayıtlar rastgele GUID ile adreslenir; kullanıcı dosya adı yalnızca görüntülenir ve temizlenir.

Ayrıntı: [docs/SECURITY.md](docs/SECURITY.md).

## Test kapsamı

Son yerel sonuç (Release): **61 test başarılı, 0 başarısız**; ayrıca CI dışında tutulan 3 benchmark testi vardır.

| Test sınıfı | Kapsam |
|-------------|--------|
| `ParserTests` | boş/null girdi, seviyeler, yanlış pozitifler, `Caused by`, stack satırları, CRLF, bozuk/binary içerik |
| `GroupingTests`, `FilterTests`, `CsvTests` | gruplama + stack ilişkisi, ORA/.NET/Java tespiti, zaman damgası, filtreler, CSV kaçışları |
| `UploadTests` | uzantı, boş dosya, binary, geçersiz UTF-8, 5 MB sınırı, sahte uzunluk başlığı |
| `StoreTests` | bellek içi depo, TTL, kayıt üst sınırı, dosya adı temizleme |
| `WebTests` | entegrasyon: antiforgery, XSS (log metni ve dosya adı), 400/404, güvenlik header'ları, örnek → filtre → CSV, diske yazmama kontrolleri |

Ayrıntı: [docs/TESTING.md](docs/TESTING.md).

## Performans

Release build, 5 çalıştırmanın medyanı (ms), sentetik log:

| Satır | Eski parser (baseline) | Yeni parse | Gruplama | Filtre | CSV |
|------:|------:|------:|------:|------:|------:|
| 100 | 0.60 | 0.29 | 0.01 | 0.00 | 0.05 |
| 1000 | 6.17 | 2.78 | 0.05 | 0.05 | 0.32 |
| 5000 | 32.56 | 14.68 | 0.10 | 0.26 | 1.43 |

Gerçek bir darboğaz görülmediği için ek optimizasyon yapılmamıştır. Ayrıntı: [docs/PERFORMANCE.md](docs/PERFORMANCE.md).

## CI/CD

`.github/workflows/ci.yml`: `main` push'u ve pull request'te → checkout → .NET 10 kurulumu → `dotnet restore` → `dotnet build -c Release` → `dotnet test -c Release` (benchmark'lar hariç). Aynı adımlar yerelde doğrulanmıştır; **workflow henüz GitHub üzerinde çalıştırılmamıştır.** Deployment aşaması yoktur. Ayrıntı: [docs/CI_CD.md](docs/CI_CD.md).

## Teknik belgeler

| Belge | İçerik |
|-------|--------|
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Bileşenler, rotalar, tasarım kararları |
| [CODE_REVIEW.md](docs/CODE_REVIEW.md) | Architect/Security/QA inceleme bulguları ve durumları |
| [SECURITY.md](docs/SECURITY.md) | Güvenlik önlemleri ve sınırları |
| [TESTING.md](docs/TESTING.md) | Test kapsamı ve TDD kaydı |
| [PERFORMANCE.md](docs/PERFORMANCE.md) | Ölçüm yöntemi ve sonuçlar |
| [CI_CD.md](docs/CI_CD.md) | GitHub Actions süreci |

## Bilinen sınırlamalar

- Kimlik doğrulama ve kullanıcı bazlı ayrım yok; "son analizler" listesi tüm kullanıcılar için ortaktır. Yalnızca yerel veya güvenilir ağda çalıştırın.
- Veri süreç belleğindedir: yeniden başlatmada kaybolur, birden fazla instance arasında paylaşılmaz.
- Parser heuristiktir; yalnızca büyük harfli seviye token'ları ve yukarıda listelenen biçimler tanınır. Tarih için `yyyy-MM-dd HH:mm:ss` (veya `T` ayraçlı) biçimi aranır; zaman damgası olmayan satırlar tarih filtresinde elenir.
- UTF-8 dışı kodlamalar (örn. Windows-1254) reddedilir.
- Arayüz metinleri Türkçe sabit kodludur (localization yok).
- Hata açıklama sözlüğü sınırlıdır (yukarıdaki ORA kodları ve yaygın exception tipleri).
- OS bellek takası ve antiforgery anahtarları uygulamanın kontrolü dışında diske yazılabilir (log içeriği içermez).

## Geliştirme önerileri

- Kimlik doğrulama ve oturum bazlı kayıt izolasyonu
- Ek zaman damgası biçimleri ve çok satırlı olay modeli
- Genişletilebilir (veri güdümlü) hata açıklama sözlüğü
- Sayfalama ve daha büyük dosyalar için akış tabanlı ayrıştırma
- Yerelleştirme (i18n)
- CI'ın GitHub üzerinde çalıştırılıp doğrulanması
