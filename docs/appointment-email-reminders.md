# Görüşdən 2 saat əvvəl avtomatik email

Backend `AppointmentEmailReminderWorker` vasitəsilə hər 60 saniyədə yaxınlaşan görüşləri yoxlayır. Görüşün vaxtına 2 saat və ya daha az qalıbsa, hesabın təsdiqlənmiş emailinə bir xatırlatma göndərilir. Brauzerin açıq olması tələb olunmur; backend prosesi işləməlidir.

Emaildə görüşün tarixi/saatı, avtomobilin marka-modeli, ustanın ad-soyadı, ünvan və status göstərilir. Saat Bakı vaxtı ilə, 24 saatlıq `HH:mm` formatındadır. Gözləmədə olan görüşdə ustanın hələ təsdiq vermədiyi ayrıca yazılır. HTML daxilində istifadəçi mətnləri encode olunur; plain-text variantı da var.

## Göndərmə qaydaları

- `Pending` və `Accepted` görüşlər əhatə olunur. `Cancelled`, `Rejected`, `Completed`, başlamış və keçmiş görüşlər üçün email göndərilmir.
- Hesabın emaili təsdiqlənmiş olmalı və admin tərəfindən bloklanmamalıdır.
- Görüş yaradılan anda 2 saatdan az qalıbsa və ya backend gec başlayıbsa, vaxtı hələ keçməyən görüş növbəti yoxlamada göndərilir. İşləməyən backend vaxtı keçmiş görüşə sonradan email göndərmir.
- Uğurlu göndərmənin `EmailReminderSentAtUtc` qeydi DB-də saxlanır. Normal restart və `Pending → Accepted` keçidi eyni emaili təkrar göndərmir.
- SMTP xətasında uğur qeydi yazılmır; 5 dəqiqə sonra, görüş başlamayıbsa, yenidən cəhd edilir. Bir görüşdə SMTP xəta digər görüşlərin göndərilməsini dayandırmır.
- Birdən çox backend instansiyası üçün atomik, şərtli SQL `UPDATE` ilə 5 dəqiqəlik lease götürülür. Göndərmədən əvvəl status, vaxt və hesab yenidən yoxlanır. SMTP əməliyyatı lease müddətini keçməyən timeout ilə işləyir.

SMTP tranzaksional deyil: server emaili qəbul etdikdən sonra proses/DB sıradan çıxarsa və ya SMTP cavabı itərsə, növbəti cəhddə təkrar email mümkündür. Bu mexanizm normal paralel işləmə və restart təkrarlarını önləyir, SMTP üçün tam “exactly once” zəmanəti vermir. Email SMTP-yə təhvil verildikdən sonra görüş ləğv edilərsə artıq göndərilmiş məktubu geri çağırmaq mümkün deyil. Alıcının inbox/spam çatdırılması SMTP provayderindən asılıdır.

## Migration və işə salma

Repo kökündən:

```powershell
dotnet ef database update --project MotoGarage.Infrastructure --startup-project MotoGarage.API
dotnet run --project MotoGarage.API --launch-profile https
```

`20261010152751_AddAppointmentEmailReminders` migration-ı Appointments cədvəlinə dörd nullable sütun və yalnız göndərilməmiş aktiv görüşlərə aid tarix indeksi əlavə edir. Köhnə görüş məlumatları qorunur. Migration hazırkı tətbiq DB-sinə avtomatik tətbiq edilməyib; SQL sınaqlarında təsadüfi adlı ayrı test DB-lərinə tətbiq olunub.

Mövcud server `Email:SmtpHost`, `Email:SmtpPort`, `Email:Username`, `Email:Password`, `Email:FromName` sazlamaları və StartTLS email xidməti istifadə olunur. Secret-lər frontend-ə və git-ə yerləşdirilməməlidir.

`AppointmentEmailReminders` bölməsinin standartları: `Enabled=true`, `PollIntervalSeconds=60`, `LeadTimeHours=2`, `BatchSize=25`, `LeaseMinutes=5`, `RetryMinutes=5`. Environment override nümunəsi: `AppointmentEmailReminders__Enabled=false`. Fəaliyyət üçün SQL migration və SMTP konfiqurasiyası tələb olunur. Bir neçə instansiyada UTC saatları uyğun olmalıdır.

Hazırkı API görüş tarixinin dəyişdirilməsini dəstəkləmir. Gələcək reschedule əməliyyatında bu dörd göndərmə/lease/retry sahəsi yeni tarix üçün sıfırlanmalıdır.

## Sınaq

`AppointmentEmailReminderTests` vaxt/status/account seçimlərini, Bakı saatını, HTML təhlükəsizliyini və brauzersiz worker icrasını yoxlayır. `AppointmentEmailReminderSqlTests` ayrı SQL DB-də köhnə məlumatların migration-dan sonra qalmasını, 2 saat sərhədini, paralel claim-ləri, expired lease bərpasını, restartı, retry-ni və claim-dən dərhal sonra ləğvetməni fake email xidməti ilə yoxlayır. Real şəxsə email göndərilmir.

Real çatdırılma üçün öz test hesabınızda 2 saatlıq pəncərədə bir görüş yaradın; backend işləyərkən email/spam qovluğunu yoxlayın. SQL sınaqları üçün `MOTOGARAGE_RUN_SQL_TESTS=1` və işləyən lokal SQL bağlantısı tələb olunur; testlər tətbiqin DB-sini dəyişdirmir.
