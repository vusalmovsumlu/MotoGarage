# Admin iş masası və hesab silinməsi

Admin menyusunda iş masası, istifadəçilər və biznes müraciətləri var. Profil/şifrə və tema hesab menyusundan idarə olunur. Şəxsi qaraj, yanacaq, xərc, servis, xatırlatma, səbət, sifariş, görüş, chat və kataloq səhifələri admin üçün açılmır. Şəxsi backend controller-ləri və SignalR hub da `User,Mechanic,Store` rollarını tələb edir. Digər rolların menyuları saxlanılıb.

İş masasında platformanın statistikası, gözləyən biznes müraciətləri və bloklanmış hesablar göstərilir. Usta/mağaza müraciətlərinin qəbul/rədd axını saxlanılıb.

## Silinmə

`DELETE /api/admin/users/{id}` yalnız aktiv, emaili təsdiqlənmiş admin tərəfindən çağırıla bilər. Frontend seçilmiş istifadəçinin adını və emailini göstərən təsdiq pəncərəsi açır. Sorğu bitənədək düymələr kilidlənir; xəta olduqda istifadəçi və pəncərə saxlanılır.

- Öz admin hesabını və digər admin hesablarını silmək olmaz.
- Müştəri və ya mağaza kimi `Pending`, `Confirmed`, `Preparing`, `Shipped` sifarişləri, yaxud müştəri/usta kimi `Pending`, `Accepted` görüşləri varsa `409` qaytarılır. Əvvəl bu əməliyyatlar tamamlanmalı və ya ləğv edilməlidir.
- Uğurlu sorğu `204` qaytarır. Eyni silinmiş hesab üçün təkrar sorğu təhlükəsizdir. Mövcud olmayan ID `404` qaytarır.

Silinmə hesabı geri qaytarılmayacaq şəkildə anonimləşdirir: ad dəyişir, email/telefon/şifrə silinir, username və security stamp dəyişir. Refresh token-lər ləğv edilir, OTP, Google login əlaqəsi, Identity token/claim/role qeydləri və bildirişlər silinir. Mövcud JWT-lər bazadakı vəziyyət yoxlamasından keçmir; chat bağlantıları transaction uğurla bitəndən sonra kəsilir. Usta/mağaza və həmin mağazanın məhsulları deaktiv edilir. Silinmiş hesab blok açma və biznes təsdiqi ilə yenidən aktivləşmir, istifadəçi siyahısından/statistikasından çıxır, yeni qaraj xatırlatmaları almır.

Əlaqələrin pozulmaması üçün User sətri, tamamlanmış/ləğv edilmiş sifariş və görüşlər, qaraj və servis qeydləri, rəylər və chat tarixçəsi bazada saxlanılır. Cloudinary şəkilləri, əvvəlki biznes müraciətləri və onların foto qeydləri də saxlanılır. Bu əməliyyat bütün şəxsi məlumatların və media fayllarının fiziki təmizlənməsi deyil. Köhnə email ilə yeni qeydiyyat ayrıca istifadəçi ID-si yaradır və əvvəlki tarixçəni ona keçirmir.

Silinmə və yeni sifariş/görüş yaradılması mövcud serializable transaction ilə işləyir. Yaradan hesab transaction daxilində yenidən yoxlanılır: eyni anda silinən müştərinin aktiv sifarişinin qalmasına yol verilmir. `DeletedAtUtc` concurrency token-i paralel köhnə hesab yeniləməsinin silinmiş hesabı bərpa etməsini dayandırır.

## Migration və yoxlama

Yeni `20261010200253_AddAdminAccountDeletion` migration-ı `DeletedAtUtc`, `DeletedByAdminId` sütunlarını və adminə olan restrict əlaqəsini əlavə edir. Mövcud hesabları silmir. Layihənin kökündən işlədin və backend-i yenidən başladın:

```powershell
dotnet ef database update --project MotoGarage.Infrastructure --startup-project MotoGarage.API
```

Bu dəyişiklik hazırlanarkən tətbiqin bazasına migration tətbiq edilməyib və real hesab silinməyib. Unit testlər yaddaş bazasında, SQL testlər isə ayrıca təsadüfi adlı lokal test bazalarında işləyir; SMTP və real Cloudinary əməliyyatı aparılmır. SQL test migration-ın əvvəlki hesabı qorumasını və hesab silinməsi ilə paralel sifariş yaradılmasını yoxlayır. Browser fixture testləri admin menyusunu, şəxsi URL blokunu, təsdiq/imtina, təkrar klik, API xətası və 320/1440 px açıq/tünd görünüşü yoxlayır.

Real backend ilə ayrıca test hesablarında admin login, digər rollar üçün DELETE icazəsizliyi, aktiv sifariş/görüş üçün `409`, uğurlu silinmədən sonra köhnə JWT/refresh/Google girişinin bağlanması və açıq SignalR bağlantısının kəsilməsi yoxlanmalıdır. Real istifadəçi hesabını bu yoxlamalarda istifadə etməyin.
