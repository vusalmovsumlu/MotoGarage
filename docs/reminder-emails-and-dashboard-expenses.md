# Xatırlatma emailləri və aylıq xərclər

Xatırlatmanın tarixi çatanda **və ya** avtomobilin/motosikletin tətbiqdəki cari odometri göstərilmiş həddə çatanda sahibinə tətbiqdaxili bildiriş və email hazırlanır. İki şərtdən birinin ödənməsi kifayətdir. Cari odometr tətbiqdə yenilənməlidir; sistem avtomobildən canlı kilometr oxumur.

## Avtomatik göndəriş

- `ReminderNotificationWorker` başlanğıcda və standart olaraq hər 30 saniyədə vaxtı çatmış, tamamlanmamış xatırlatmaları yoxlayır. Brauzerin açıq qalmasına ehtiyac yoxdur; backend işləməlidir.
- Bildiriş, emailin məzmunu və göndərmə işarələri eyni database əməliyyatında saxlanır. Paralel yoxlamalar eyni xatırlatma planını təkrar növbəyə əlavə etmir.
- Email mövcud `WorkflowEmails` növbəsindən `WorkflowEmailWorker` vasitəsilə göndərilir. Standart yoxlama intervalı 10 saniyədir. SMTP xətalarında 1 dəqiqədən başlayan artan fasilə ilə, maksimum 10 cəhd edilir; uğursuzluqlar loga yazılır. Göndəriş saniyəsinə zəmanət verilmir.
- Yalnız emaili təsdiqlənmiş, bloklanmamış və silinməmiş sahibə mail gedir. Ünvan göndəriş zamanı yenidən oxunur. Mövcud SMTP parametrləri istifadə olunur; yeni secret tələb olunmur.
- Emaildə xatırlatma, nəqliyyatın adı/nömrəsi, tarix, cari odometr və varsa odometr həddi/qeyd göstərilir. Saat Bakı vaxtı ilə 24 saatlıq formatdadır. HTML daxilində istifadəçi mətni escape edilir.
- Başlığın və ya qeydin redaktəsi yenidən email yaratmır. Tarix və ya kilometr həddi dəyişəndə planın reviziyası artır, növbəti çatma üçün yeni email mümkün olur.
- Silinmiş, tamamlanmış, planı dəyişmiş və ya artıq həddi ödəməyən xatırlatmanın köhnə növbə yazısı SMTP-yə göndərilməzdən əvvəl ləğv edilir. SMTP tərəfindən artıq qəbul edilmiş maili geri almaq mümkün deyil. SMTP çatdırılmasında mütləq tək göndəriş zəmanəti yoxdur; növbə və sabit Message-ID təkrarları məhdudlaşdırır.
- Migration-dan sonra əvvəl tətbiqdaxili bildiriş verilmiş, hələ tamamlanmamış və vaxtı çatmış xatırlatmalar üçün email bir dəfə növbəyə əlavə edilir; köhnə tətbiqdaxili bildiriş təkrarlanmır.

## Əsas ekran: “Bu ayın xərcləri”

Yalnız daxil olmuş istifadəçinin aşağıdakı məbləğləri toplanır:

| Mənbə | Məbləğ | Ay üçün əsas tarix |
| --- | --- | --- |
| Yanacaq qeydləri | `TotalPrice` | `FuelDate` |
| Ayrıca xərc qeydləri | `Amount` | `ExpenseDate` |
| Servis tarixçəsi | `Cost` | `ServiceDate` |
| Yalnız tamamlanmış mağaza sifarişləri | `TotalAmount` | `CompletedAtUtc`; köhnə yazılarda boşdursa `CreatedAt` |

Mövcud UTC təqvim ayı sərhədləri saxlanılır: ayın ilk anı daxil, növbəti ayın ilk anı xaric. Avtomobil və motosiklet qeydləri eyni cəmə daxildir. Sifariş nəqliyyata bağlanmasa da alıcının xərcinə daxildir. Gözləyən, təsdiqlənmiş, hazırlanan, göndərilmiş və ləğv edilmiş sifarişlər sayılmır.

Ustanın servis hesabatı sahibi tərəfindən təsdiqlənəndə servis tarixçəsi yaranır; hesabat ayrıca toplanmır. Beləliklə hesabat və tarixçə iki dəfə sayılmır. Eyni real ödəniş istifadəçi tərəfindən ayrıca iki fərqli bölməyə əl ilə daxil edilərsə, bunlar ayrı qeydlər sayılır; məbləğə görə avtomatik silinmə/deduplikasiya yoxdur.

## Quraşdırma və yoxlama

Migration: `AddReminderEmailScheduling` yalnız `Reminders` cədvəlinə nullable `EmailQueuedAtUtc`, standartı `0` olan `ScheduleRevision` və gözləyən emaillər üçün filterli indeks əlavə edir. Mövcud xatırlatmalar və əlaqələr saxlanılır.

```powershell
dotnet ef database update --project MotoGarage.Infrastructure --startup-project MotoGarage.API
dotnet test MotoGarage.Tests
```

SQL testləri yalnız local `MotoGarageDb` konfiqurasiyasından serveri götürüb ayrıca təsadüfi adlı test bazaları yaradır və sonda həmin bazaları silir. Əsas bazaya test qeydi yazılmır, real email göndərilmir:

```powershell
$env:MOTOGARAGE_RUN_SQL_TESTS = '1'
dotnet test MotoGarage.Tests --filter "FullyQualifiedName~ReminderEmailSqlTests|FullyQualifiedName~DashboardExpensesTests"
```

Əhatə: tarix/kilometr/birgə hədd, təkrar yoxlama, restart, planın redaktəsi, uyğun olmayan hesablar, catch-up, HTML və Bakı vaxtı; SQL-də paralel göndərmə, notification/email insert rollback, köhnə planın ləğvi və SMTP retry; dashboard-da sahiblik, tarix sərhədləri, bütün xərc mənbələri, sifariş statusları və servis hesabatının yalnız bir dəfə hesablanması.
