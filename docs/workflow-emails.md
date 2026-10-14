# Görüş və sifariş email-ləri

Müştərinin hesabındakı təsdiqlənmiş email ünvanına aşağıdakı əməliyyatlar üçün məktub növbəyə yazılır:

| Görüş | Sifariş |
| --- | --- |
| Yaradıldı, ustanın cavabı gözlənilir | Yaradıldı, mağazanın təsdiqi gözlənilir |
| Usta qəbul etdi | Mağaza təsdiqlədi |
| Usta rədd etdi | Hazırlanır |
| Tamamlandı | Göndərildi |
| Ləğv edildi | Tamamlandı |
| Yeni vaxt qarşılıqlı təsdiqləndi | Ləğv edildi |

Görüş məktubunda nəqliyyat, biznes adı, ustanın adı, ünvan və Bakı vaxtı ilə `HH:mm` göstərilir. Sifariş məktubunda mağaza, məhsullar, say, məbləğ və mövcuddursa çatdırılma ünvanı olur. Məktub əməliyyat anındakı məlumatı saxlayır və əməliyyat vaxtını göstərir. İki saat əvvəl göndərilən ayrıca görüş xatırlatması saxlanılıb.

## Göndəriş

`WorkflowEmailQueue` əməliyyatın `UnitOfWork` transaction-ında `WorkflowEmails` cədvəlinə yazır. Görüş, stok, sifariş və ya bildiriş yazılması geri qaytarılsa, email qeydi də geri qaytarılır. SMTP request transaction-ında çağırılmır.

`WorkflowEmailWorker` tətbiq işlədiyi müddətdə brauzerdən asılı olmadan növbəni yoxlayır. İlkin interval 10 saniyədir; SMTP cavabı və növbə uzunluğu faktiki çatdırılma vaxtına təsir edir. Mövcud `Email:SmtpHost`, `Email:SmtpPort`, `Email:Username`, `Email:Password` konfiqurasiyası istifadə edilir. Secret-lər konfiqurasiya faylına əlavə edilməyib.

Bir müştərinin məktubları növbə sırası ilə göndərilir. Hər status hadisəsinin unikal `EventKey`-i var; təkrarlanan çağırış ayrıca məktub yaratmır. Paralel worker-lər atomik lease ilə məktubu götürür. Yarımçıq göndəriş 5 dəqiqəlik lease bitəndən sonra yenidən götürülə bilər. Göndərişdən əvvəl hesabın təsdiqi, bloklanması və silinməsi yoxlanılır; email ünvanı hesabdan yenidən oxunur. Uyğun olmayan hesabın məktubu `DiscardedAtUtc` ilə bağlanır.

SMTP xətası yenidən yoxlanılır: ilkin gecikmə 60 saniyədir, sonrakı cəhdlərdə artır və 1 saatla məhdudlaşır. İlkin maksimum 10 cəhddən sonra `FailedAtUtc` yazılır və error log-a düşür; bu məktub sonrakıları saxlamır. Konfiqurasiya `WorkflowEmails` bölməsindədir. `Enabled=false` göndərişi saxlayır, yeni hadisələr bazada növbəyə yazılmağa davam edir.

Məktubun `DeliveryId`-si retry zamanı dəyişmir və sabit SMTP `Message-ID` yaradır. SMTP məktubu qəbul etdikdən dərhal sonra proses/database dayanarsa, təkrar çatdırılma mümkündür; SMTP tam exactly-once zəmanəti vermir.

## Migration və yoxlama

`20261014173032_AddWorkflowEmailOutbox` yalnız yeni cədvəl, indekslər və `Restrict` foreign key yaradır. Köhnə sifariş/görüşlərə geriyə dönük email hazırlanmayıb. Yeni hadisələr migration tətbiq edildikdən və backend yenidən başladıldıqdan sonra işləyir.

`WorkflowEmailTests` bütün statusları, müştəri və mağaza ləğvini, razılaşdırılmış yeni vaxtı, təkrarlanan çağırışları, uyğun olmayan hesabları, HTML escaping və saat formatını yoxlayır. `WorkflowEmailSqlTests` ayrıca təsadüfi adlı LocalDB bazasında transaction rollback, SMTP xətası, restart, ardıcıllıq, email ünvanının dəyişməsi, paralel worker-lər, lease bərpası və maksimum retry həddini yoxlayır. Testlər fake SMTP istifadə edir; real müştərilərə email göndərilmir.
