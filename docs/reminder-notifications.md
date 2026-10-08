# Avtomatik xatırlatma bildirişləri

Backend işə düşəndə və sonra hər 30 saniyədə tamamlanmamış xatırlatmaları yoxlayır. Tarix keçibsə **və ya** nəqliyyat vasitəsinin cari odometri `ReminderKm` həddinə çatıbsa, nəqliyyat vasitəsinin sahibinə tətbiqdaxili bildiriş yaradır. Odometrlə bağlı yoxlama bazadakı `CurrentKm` qiymətindən istifadə edir.

Bildiriş yaratmaq üçün istifadəçinin brauzerinin açıq olması lazım deyil. Backend dayanıbsa, gecikmiş xatırlatmalar növbəti açılışda yoxlanılır. Backend işləyən zaman bildiriş siyahısı və başlıqdakı sayğac da 30 saniyəlik intervalla yenilənir; görünən brauzerdə bildiriş adətən bir dəqiqə ərzində görünür. Bu mexanizm email, SMS və ya browser push göndərmir.

## Təkrar göndərilmə qaydası

- `NotificationSentAtUtc` bazada saxlanılır; backend yenidən başladıqda eyni xatırlatma təkrar göndərilmir.
- Bildirişi oxunmuş kimi işarələmək göndərilmə qeydini sıfırlamır.
- Xatırlatmanın tarixini və ya kilometr həddini dəyişmək onu yeni qrafik üçün yenidən aktivləşdirir. Başlığı dəyişmək bunu etmir.
- Tamamlanmış xatırlatma üçün bildiriş yaradılmır. Qrafiki dəyişmək tamamlanmış xatırlatmanı yenidən açmır.
- Sistem avtomatik təkrarlanan yeni xatırlatmalar yaratmır.

Bildirişdəki keçid `/garage/{vehicleSlug}/reminders` şəklindədir. Frontend bunu uyğun nəqliyyat vasitəsi seçilmiş xatırlatmalar səhifəsinə yönləndirir. Əvvəlki `vehicle` ID-li keçidlər slug ünvanına yönləndirilir; query-string olmayan keçidlər də işləyir.

## Paralel icra və xəta halları

Bildiriş və göndərilmə qeydi eyni `SaveChangesAsync` əməliyyatının tranzaksiyasında saxlanılır. Tarix, kilometr həddi, tamamlanma və göndərilmə qeydi concurrency token-ləridir. İki worker eyni xatırlatmanı seçdikdə yalnız birinin tranzaksiyası uğurlu olur. Seçimdən sonra xatırlatma təxirə salınarsa və ya tamamlanarsa, köhnə bildirişin tranzaksiyası geri qaytarılır.

Concurrency toqquşmasında məlumat yenidən oxunur, bir batch üçün ən çox üç cəhd edilir. Digər xətalar log-a yazılır və növbəti yoxlamada yenidən cəhd olunur. Hər batch ayrıca DI scope istifadə edir və tətbiqin dayanma siqnalını qəbul edir.

## Konfiqurasiya

Əlavə konfiqurasiya olmadan aşağıdakı dəyərlər tətbiq olunur:

```json
{
  "ReminderNotifications": {
    "PollIntervalSeconds": 30,
    "BatchSize": 100,
    "MaxBatchesPerPass": 10
  }
}
```

İnterval 5–3600 saniyə, batch ölçüsü 1–1000, bir yoxlamadakı batch sayı 1–100 ola bilər. Yanlış konfiqurasiya startup validation tərəfindən rədd edilir. Standart dəyərlərlə bir yoxlamada ən çox 1000 bildiriş işlənir; qalanlar növbəti yoxlamaya keçir.

## Migration və işə salma

`20261008174618_AddAutomaticReminderNotifications` migration-ı `Reminders` cədvəlinə nullable `NotificationSentAtUtc` sütunu və göndərilməmiş, tamamlanmamış qeydlər üçün filtered index əlavə edir. Yerli `MotoGarageDb` bazasına tətbiq olunub. Digər mühitlərdə ayrıca tətbiq edilməlidir:

```powershell
dotnet ef database update --project MotoGarage.Infrastructure --startup-project MotoGarage.API --context AppDbContext
dotnet run --project MotoGarage.API --launch-profile https
```

Yeni build ilə backend işlədikdə worker avtomatik başlayır. Mövcud reminder və notification HTTP request/response müqavilələri saxlanılıb.

## Yoxlamalar

40 backend sınağı uğurludur. Bunlara tarix/kilometr hədləri, tamamlanma, təkrar göndərilməmə, yenidən planlaşdırma, batch, cancellation, worker startup və xəta sonrası davam etmə daxildir. SQL Server sınağı paralel worker, aralıqda təxirə salınma/tamamlanma və uğursuz notification insert zamanı tranzaksiyanın geri qaytarılmasını yoxlayır.

SQL sınağı yalnız açıq şəkildə aktivləşdirildikdə işləyir, lokal bağlantını yoxlayır və ayrıca təsadüfi adlı test bazası yaradıb sonunda silir. Tətbiqin bazasındakı məlumatları dəyişdirmir. Aktivləşdirmək üçün:

```powershell
$env:MOTOGARAGE_RUN_SQL_TESTS = '1'
dotnet test MotoGarage.Tests/MotoGarage.Tests.csproj --no-restore
```

Bu dəyişən olmadıqda SQL sınağı skip edilir. Frontend-də 43 browser fixture sınağı, `npm run build` və `npm run lint` uğurludur. Yeni sınaqlar avtomatik bildiriş/sayğac yenilənməsini və uyğun nəqliyyat vasitəsinə keçidi yoxlayır. Backend bu iş zamanı işə salınmadığından real API ilə browser inteqrasiyası ayrıca manual yoxlanmalıdır.
