# Usta və mağaza qeydiyyatının admin təsdiqi

Yeni `Mechanic` və `Store` hesabları `Pending` vəziyyətində yaranır. Qeydiyyat zamanı dəqiq 3 fərqli JPG/JPEG, PNG və ya WEBP faylı tələb olunur; hər fayl maksimum 5 MB ola bilər. Fayl başlığı və uzantısı uyğun olmalıdır; eyni faylın fərqli adla təkrar yüklənməsi SHA-256 ilə rədd edilir. Şəkillərin real iş yerini və ya məhsulları göstərməsini admin qiymətləndirir.

Şəkillər mövcud `CloudinaryImageService` ilə Cloudinary-yə yüklənir. DB-də şəkil faylları deyil, URL/public ID, sahiblik və qərar məlumatları saxlanır. Cloudinary açarları mövcud server secret konfiqurasiyasında qalır; frontendə göndərilmir. Yükləmə yarımçıq alınarsa yaradılmış hesab və yüklənmiş şəkillər kompensasiya ilə silinir; silmə alınmasa xəta loglanır.

Email OTP təsdiqindən sonra müraciət adminin `/#/admin/applications` səhifəsində görünür. Admin hər şəkli böyüdüb baxa, müraciəti qəbul və ya rədd edə bilər. Rədd səbəbi boş ola bilməz və maksimum 1000 simvoldur; əvvəl/son boşluqlar təmizlənir. Qərar, istifadəçinin canlı giriş statusu və bildiriş eyni DB əməliyyatı ilə saxlanır. Eyni müraciətə paralel qərar cəhdi 409 qaytarır.

Təsdiq gözləyən və rədd edilmiş biznes hesabları frontenddə müraciət statusu/şəkilləri/səbəbi görür. Status hər 30 saniyə yoxlanır və əl ilə yenilənə bilər. Backend JWT yoxlanışında statusu DB-dən oxuyur və bu istifadəçilərə biznes rolu ilə əməliyyat icazəsi vermir. Köhnə JWT təsdiq yoxlamasını keçə bilmir. Qəbuldan sonra mövcud sessiya biznes əməliyyatlarını aça bilər; email/password, OTP, Google və refresh axınları saxlanılıb.

## Endpoint-lər

| Metod | Endpoint | Contract / icazə |
| --- | --- | --- |
| POST | `/api/auth/register-business` | Anonymous, auth-register rate limit, multipart: `FullName`, `Email`, `Password`, `Role=Mechanic/Store`, 3 təkrarlanan `photos` faylı |
| POST | `/api/auth/register` | Mövcud JSON fərdi User qeydiyyatı; şəkilsiz biznes qeydiyyatı rədd edilir |
| GET | `/api/business-applications/my` | Authenticated, yalnız cari istifadəçinin müraciəti |
| GET | `/api/admin/business-applications?status=Pending` | Admin; Pending/Approved/Rejected, boş status bütün qərarlar |
| POST | `/api/admin/business-applications/{id}/accept` | Admin; body yoxdur, 204 |
| POST | `/api/admin/business-applications/{id}/decline` | Admin; JSON `{ "comment": "səbəb" }`, 204 |

Yeni migration: `AddBusinessRegistrationApproval`, yerli `MotoGarageDb` bazasına tətbiq edilib. Mövcud hesablar `Approved=0` kimi saxlanılır; onların işləyən profillərinə yenidən müraciət tələbi tətbiq edilmir. Yalnız yeni biznes qeydiyyatları 3 şəkil və admin təsdiqi tələb edir. Rədd edilmiş müraciətin yenidən təqdim edilməsi ayrıca axın kimi bu dəyişiklikdə əlavə edilməyib.

## Yoxlama

`dotnet test`, `npm run build`, `npm run lint`, `npm test`.

Yerli SQL inteqrasiya testləri üçün `MOTOGARAGE_RUN_SQL_TESTS=1` ilə `dotnet test` işə salınır. Onlar təsadüfi adlı ayrıca DB yaradır və yalnız həmin test DB-sini silir. Migration, paralel qərarlar və uğursuz bildirişin bütün qərarı geri qaytarması yoxlanır. Brauzer testləri API/şəkil fixture-ləri istifadə edir. Canlı Cloudinary/SMTP və real Google credential-ləri ilə ayrıca usta/mağaza qeydiyyatı–OTP–qəbul/rədd axını manual yoxlanmalıdır.
