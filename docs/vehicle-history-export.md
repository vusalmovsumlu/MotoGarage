# Nəqliyyat vasitəsinin tarixçəsini Excel ilə paylaşmaq

Qarajda avtomobil və ya motosiklet detalını açın → **Tarixçəni paylaş**. **Excel-i yüklə** ilə hesabatı əvvəl yoxlamaq olar. Alıcının emailini daxil edin, paylaşmağa razılıq xanasını seçin və **Email ilə göndər** düyməsini basın. Gmail dəstəklənir; alıcının MotoGarage hesabı olması tələb olunmur. Email ünvanı dəyişəndə razılıq yenidən seçilməlidir.

Hesabatın dörd vərəqi var:

- **Avtomobil:** marka/model, il, növ, mühərrik, VIN, dövlət nömrəsi, cari odometr, hesabat tarixi və bölmələr üzrə məbləğlər.
- **Servis tarixçəsi:** tarix, dəqiq kilometr, görülən iş, açıqlama və məbləğ.
- **Xərclər:** tarix, kateqoriya, açıqlama və məbləğ. Mövcud Expense modelində kilometr yoxdur, buna görə hesabatda xərc kilometri təxmin edilmir.
- **Yanacaq:** tarix, kilometr, litr, litr qiyməti və məbləğ.

Qeydlər tarix üzrə sıralanır. Tarixlər UTC-dən Bakı vaxtına çevrilir və `dd.mm.yyyy hh:mm` 24 saatlıq formatda göstərilir. Pul vahidi AZN-dir. VIN və mətn sahələri Excel-də mətn kimi saxlanır; istifadəçi mətnləri formula kimi icra olunmur. Məbləğ cəmləri Excel formulaları ilə hesablanır. Eyni ödəniş fərqli bölmələrdə yazıla biləcəyi üçün servis/xərc/yanacaq məbləğləri ümumi bir cəmə birləşdirilmir.

## API və giriş nəzarəti

- `GET /api/Vehicle/by-slug/{slug}/history.xlsx` — `.xlsx` faylı, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`; istifadəçi üzrə dəqiqədə 10 sorğu.
- `POST /api/Vehicle/by-slug/{slug}/history/email` — istifadəçi üzrə saatda 5 sorğu.

```json
{
  "recipientEmail": "buyer@example.com",
  "confirmSharing": true
}
```

Hər iki endpoint JWT tələb edir. Backend avtomobili slug və giriş etmiş sahibin ID-si ilə seçir. Başqa istifadəçiyə məxsus və ya olmayan avtomobil üçün 404 qaytarılır. Export və email əməliyyatları avtomobilin sahibini dəyişmir, qeydləri silmir, alıcının emailini DB-yə yazmır. Hesabatda sahibin emaili, telefon nömrəsi və hesab məlumatları yoxdur; VIN və nömrə paylaşma pəncərəsində əvvəlcədən bildirilir.

Hər bölmə maksimum 10 000 qeyd, hazırlanmış fayl isə maksimum 10 MB ola bilər. Limit aşılarsa məlumatı səssiz kəsmək əvəzinə xəta qaytarılır. Tarixçələr ayrı, limitli `AsNoTracking` sorğuları ilə alınır.

## İşə salma və email

Migration tələb olunmur. Backend yeni build ilə yenidən başladılmalıdır. Mövcud `Email:SmtpHost`, `Email:SmtpPort`, `Email:Username`, `Email:Password` və `Email:FromName` konfiqurasiyası istifadə olunur; bu məlumatlar server secret/environment konfiqurasiyasında qalmalıdır. SMTP əlaqəsi StartTLS ilə qurulur və Excel MIME əlavəsi kimi göndərilir. [ClosedXML](https://github.com/ClosedXML/ClosedXML) hesabatı backend-də yaradır.

Göndərmə yalnız SMTP serverinin mesajı qəbul etməsindən sonra uğurlu sayılır; bu, alıcının inbox-a çatdırılmasına zəmanət deyil. SMTP xətasında UI xəta göstərir və əl ilə təkrar cəhdə imkan verir. Avtomatik SMTP retry yoxdur. Şəbəkə cavabı itərsə təkrar cəhd əlavə email yarada bilər; şübhəli halda əvvəl alıcının inbox/spam qovluğunu yoxlayın.

## Yoxlama

`VehicleHistoryExportTests` sahiblik, yalnız seçilmiş avtomobilin qeydləri, boş tarixçə, tarix/kilometr/pul tipləri, formulalar, etibarsız email, razılıq və SMTP xəta axınını yoxlayır. Brauzer testləri download bytes/filename, JWT refresh, razılığın emailə bağlı olması, göndərmə kilidi, xəta/retry və 320/1440 px ölçülərində hər iki temanı yoxlayır.

Testlərdə email fake xidməti işləyir, real alıcıya mesaj göndərilmir. Real SMTP/Gmail çatdırılması ayrıca, sizin seçdiyiniz test ünvanına UI-dən yoxlanmalıdır.
