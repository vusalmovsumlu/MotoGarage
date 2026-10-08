# Qaraj slug routing

Nəqliyyat vasitəsinin detal ünvanı `#/garage/{slug}` şəklindədir; məsələn, `#/garage/toyota-corolla-<unikal-suffix>`. Yanacaq, servis, xərc və xatırlatma səhifələri müvafiq olaraq `/fuel`, `/service`, `/expenses`, `/reminders` əlavələri ilə açılır. Qaraj siyahısının ünvanı `#/vehicles` olaraq saxlanılıb.

Slug marka/modeldən və təsadüfi 32 simvolluq suffix-dən yaranır. ID, VIN, dövlət nömrəsi və istifadəçi məlumatı slug-a daxil edilmir. Slug DB-də required sahə və unique index ilə saxlanılır. Avtomobil redaktə olunanda dəyişmir. Eyni marka/modeldə olan müxtəlif avtomobillər ayrı slug alır.

`GET /api/Vehicle/by-slug/{slug}` yalnız giriş etmiş istifadəçinin öz nəqliyyat vasitəsini qaytarır; başqa sahibin və mövcud olmayan slug üçün 404 alınır. CRUD və record API-lərinin əvvəlki ID müqavilələri saxlanılıb; ID-lər brauzerin yeni navigation URL-lərinə çıxarılmır.

Köhnə `#/vehicles/{id}` və `#/records/{kind}?vehicle={id}` linkləri avtomobilin sahibə aid olduğu təsdiqləndikdən sonra history replace ilə slug ünvanına çevrilir. Köhnə xatırlatma bildirişləri də bu keçiddən istifadə edir. Tanınmayan slug başqa avtomobilə fallback etmir, xəta göstərir.

`20261008180601_AddVehicleSlugs` migration-ı əvvəlcə nullable sütun əlavə edir, mövcud nəqliyyat vasitələrinə marka/model əsasında unikal slug verir, sonra NOT NULL və unique index tətbiq edir. Yerli `MotoGarageDb` bazasına tətbiq olunub. Digər mühitlərdə `dotnet ef database update --project MotoGarage.Infrastructure --startup-project MotoGarage.API --context AppDbContext` işlədilməlidir. Backend yeni build ilə başladılmalıdır.

Sınaqlar yaradılma, eyni model üçün uniqueness, dəyişiklikdən sonra sabitlik, ownership, unknown slug, birbaşa açılış/reload, legacy redirect, bütün record səhifələrinə keçid və seçilmiş avtomobilin dəyişdirilməsini əhatə edir. Opt-in SQL sınağı təsadüfi adlı ayrıca test bazasında boş DB migration-ını, mövcud/unicode/eyni adlı qeydlərin backfill-ini və unique constraint-i yoxlayıb bazanı silir.

Header sayğacı ikonun üstündə ayrıca yer tutur, rəqəm mərkəzlənir və 99-dan böyük say `99+` göstərilir. 320, 390 və 1440 px-də hər iki tema üçün browser ölçü və screenshot yoxlamaları var.
