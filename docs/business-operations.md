# Təqvim, hesabatlar və biznes idarəetməsi

## İstifadə qaydası

- **Usta → Təqvim:** günlük və həftəlik görüşlər; “İş saatlarını dəyiş” ilə həftənin hər gününü açıq/bağlı etmək və interval təyin etmək mümkündür. İlkin cədvəl bazar ertəsi–şənbə 09:00–18:00, bazar bağlıdır. Saatlar Bakı vaxtıdır, 24 saatlıq formatdadır. İş saatları dəyişəndə əvvəlki rezervasiyalar saxlanılır.
- **Müştəri → Görüş yarat:** ustanın boş saatlarından seçim edilir. Ekrandakı görüşlər 60 dəqiqədir; API 15–240 dəqiqə müddəti, 15 dəqiqə addımla dəstəkləyir. Qarşıdakı 90 gün rezervasiya üçün açıqdır. Ustanın və eyni nəqliyyatın görüşlərinin üst-üstə düşməsi serverdə də yoxlanılır.
- **Görüş vaxtının dəyişməsi:** sahib və ya usta yeni vaxt təklif edə bilər. Qarşı tərəf qəbul edənədək əvvəlki vaxt qüvvədə qalır. Təklif edən öz təklifini təsdiqləyə bilməz, amma geri götürə bilər. Təklif yeni saatı rezerv etmir: təsdiqlənəndə saatın hələ boş olduğu yenidən yoxlanılır. Təsdiq email xatırlatma tarixini yeniləyir. Hazırda email göndərilirsə, onun tamamlanmasından sonra təsdiqi təkrarlamaq lazımdır.
- **Tamamlanmış görüş → Servis hesabatı:** usta işin adını, açıqlamanı, dəyişən hissələri, kilometr, məbləğ və servis tarixini yazır. Sahib təsdiqləyənə qədər servis tarixçəsi dəyişmir. Rədd ediləndə səbəb tələb olunur; usta düzəliş edib yenidən göndərə bilər. Köhnə hesabat versiyasına təsdiq qəbul edilmir. Təsdiqlənmiş hesabat dəyişdirilmir. Tarixçəyə əlavə edilmiş qeyd sahibin adi servis CRUD və Excel ixrac axınına daxildir. Cari odometr azaldılmır.
- **Mağaza → Məhsullarım:** hər məhsul üçün stok xəbərdarlıq həddi təyin edilir, ilkin hədd 5-dir. Həddə çatanda bir bildiriş yaradılır. Stok həddən yuxarı bərpa olunandan sonra yenidən düşərsə yeni bildiriş göndərilir. `0` həddi yalnız stok bitəndə xəbərdarlıq edir. Az stok və stoku bitən məhsullar ayrıca, URL-də saxlanan filterlərlə görünür. Bildiriş uyğun filterə aparır.
- **Usta/Mağaza → Statistika:** tarix intervalı üzrə nəticələr. Ustanın məbləği sahibin təsdiqlədiyi hesabatların servis tarixinə əsaslanır; tamamlanmış görüş sayı tamamlanma tarixinə əsaslanır. Reytinq/rəy sayı bütün dövrü əhatə edir. Mağazanın satış məbləği və ən çox satılan məhsullar yalnız tamamlanmış sifarişlərə, sifarişdə saxlanan qiymətə əsaslanır. Ləğvlər ayrıca hesablanır. Aktiv görüş/sifariş və stok göstəriciləri hazırkı vəziyyətdir.
- **Admin → Əməliyyat tarixçəsi:** hesabın bloklanması, blokdan çıxarılması, silinməsi və biznes müraciətinin təsdiqi/rəddi qeydə alınır. Admin adı, əməliyyat, hədəf, UTC vaxtı və səbəb saxlanılır; ekranda vaxt Bakı vaxtı ilə göstərilir. Hesab əməliyyatında əlavə admin qeydi yazmaq mümkündür. Tarixçə səhifələnir və əməliyyat üzrə filterlənir. Təkrar bloklama/silmə ayrıca təkrar qeyd yaratmır.

## API

| Method | Endpoint | İcazə |
| --- | --- | --- |
| GET / PUT | `/api/appointments/working-hours` | Öz usta profili |
| GET | `/api/appointments/calendar?from=YYYY-MM-DD&to=YYYY-MM-DD` | Öz usta profili, maksimum 31 gün |
| GET | `/api/appointments/availability?mechanicId=10&date=YYYY-MM-DD&durationMinutes=60` | Daxil olmuş qeyri-admin hesab |
| POST | `/api/appointments/{id}/reschedule` | Görüşün sahibi/ustası |
| POST | `/api/appointments/{id}/reschedule/decision` | Görüşün sahibi/ustası; təsdiq qarşı tərəfdən |
| GET / PUT | `/api/appointments/{id}/report` | Oxuma: iştirakçılar; yazma: görüşün ustası |
| POST | `/api/appointments/{id}/report/decision` | Görüşün və nəqliyyatın sahibi |
| GET | `/api/business/statistics?from=YYYY-MM-DD&to=YYYY-MM-DD` | Öz usta/mağaza profili, maksimum 366 gün |
| GET | `/api/admin/audit-logs?page=1&pageSize=25&action=UserBlocked` | Aktiv admin |

Qərar body-si: `{ "revision": 1, "approve": true, "comment": null }`.
Vaxt təklifi: `{ "appointmentDate": "2026-10-15T06:00:00Z" }`.
Admin hesab əməliyyatları əvvəlki body-siz request-i qəbul edir; əlavə səbəb üçün `{ "reason": "Adminin qeydi" }` göndərilə bilər.

## Database və təhlükəsizlik

`20261011130417_BusinessOperations` migration-u yeni cədvəlləri, stok həddini, görüş müddətini, tarix göstəricilərini və hissələr sahəsini əlavə edir. Yeni FK-lar `Restrict` istifadə edir. Mövcud hesab və əməliyyat məlumatları silinmir. Stok həddi `0` SQL-də düzgün saxlanılır; EF sentinel dəyəri database default-u ilə qarışmır.

Rezervasiya, vaxt qərarı, hesabat qərarı və stok dəyişiklikləri mövcud `SERIALIZABLE` transaction və concurrency mexanizmlərindən istifadə edir. Bildiriş və audit qeydi əlaqəli əməliyyatla birlikdə commit olunur. SMTP həmin transaction-ların daxilində işləmir.

Əvvəlki qeydlərin real tamamlanma/ləğv vaxtı saxlanılmadığı üçün statistikada həmin qeydlər üçün görüş tarixi və ya sifarişin yaradılma tarixi istifadə edilir. Yeni qeydlərdə faktiki status dəyişmə vaxtı saxlanılır. Əvvəlki admin qərarlarının tarixçəsi geriyə doğru yaradılmır. İş cədvəli bir gün daxilində bir interval dəstəkləyir; gecəyarısını keçən növbə və fasilə intervalları bu versiyada yoxdur.

11.10.2026 tarixində layihənin konfiqurasiya olunmuş `MotoGarageDb` bazasına migration tətbiq edildi. Əvvəl `COPY_ONLY, CHECKSUM` backup alındı:

`C:\Users\VUQAR\AppData\Local\MotoGarage\DatabaseBackups\MotoGarage-before-BusinessOperations-20261011-172734-1902b396.bak`

Hesab sayı 7, görüş sayı 2 olaraq qaldı. Testlər ayrıca təsadüfi adlı LocalDB bazalarında işlədi; real email/Cloudinary çağırışı edilmədi.

## Avtomatik yoxlama nəticələri

- Backend: 128 test keçdi; ayrıca LocalDB bazalarında migration, paralel rezervasiya, hesabat təsdiqi və stok transaction-ları da yoxlanıldı.
- Frontend: mock API ilə 138 Playwright testi keçdi; role routing, mövcud axınlar və yeni ekranların mobil/açıq/tünd tema yoxlamaları daxildir.
- `npm run build` və `npm run lint` keçdi. Build-də 592 kB əsas JavaScript bundle üçün ölçü xəbərdarlığı qalır.
- EF modelində migration-a düşməmiş dəyişiklik yoxdur. Real development API smoke testi və real SMTP/Cloudinary göndərişi bu yoxlamaya daxil deyil.

## Manual yoxlama

1. Usta iş saatını dəyişir; iki ayrı müştəri eyni intervalı seçir. Yalnız biri rezervasiya edə bilməlidir.
2. Vaxt təklifini qarşı tərəf qəbul/rədd edir; əvvəlki vaxt, yeni vaxt və qarşı tərəfin bildirişini yoxlayın.
3. Tamamlanmış görüşə hesabat göndərin, sahibin rədd səbəbini yazın, düzəlişdən sonra təsdiqləyin. Servis tarixçəsi və Excel-də hissələri/kilometri yoxlayın.
4. Məhsulun stokunu həddə endirin, yenidən endirin, həddən yuxarı artırıb bir daha endirin. Hər yeni epizod üçün bir bildiriş olmalıdır. Stok `0` və xəbərdarlıq həddi `0` hallarını yoxlayın.
5. Sifarişi tamamlayın/ləğv edin; statistikada məbləğ, satılan say və ləğv sayını yoxlayın.
6. Admin səbəb yazaraq hesabı bloklasın/açsın və biznes müraciətinə qərar versin. Auditdə qərar, admin, vaxt və səbəb görünməlidir.
7. SMTP konfiqurasiyası ilə vaxtı dəyişmiş görüşün iki saatlıq email xatırlatmasını ayrıca yoxlayın. Avtomatik testlər real mail göndərmir.

Başqa mühitdə migration tətbiqi:

```powershell
dotnet ef database update --project MotoGarage.Infrastructure --startup-project MotoGarage.API
```
