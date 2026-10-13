# Çatdırılma ünvanı və chat faylları

## İstifadə

Səbətdə sifarişi təsdiqləməzdən əvvəl alıcının adı, telefon, şəhər/rayon və tam ünvan yazılmalıdır. Çatdırılma qeydi istəyə bağlıdır. Alıcının adı cari hesabdan ilkin olaraq doldurulur və dəyişdirilə bilər. Telefon 7–15 rəqəm və standart nömrə simvolları ilə yoxlanılır. Bu məlumatlar sifarişin özündə saxlanılır; profilin sonrakı dəyişməsi əvvəlki sifarişin ünvanını dəyişmir. Müştəri sifarişində, mağaza isə “Gələn sifarişlər” bölməsində məlumatı görür. Ünvan olmadan yeni sifariş API tərəfindən də rədd edilir və stok dəyişmir.

Köhnə sifarişlər silinmir; onlarda ünvan sütunları boş qalır və ekranda əvvəlki sifarişdə məlumatın saxlanılmadığı göstərilir. Ünvanlar ayrıca açıq endpoint-ə əlavə edilməyib; mövcud sifariş ownership yoxlamaları qüvvədədir.

Chatda əlavə işarəsi ilə bir şəkil və ya video seçilə bilər. Şəkillər JPG/JPEG, PNG, WEBP formatlarında maksimum 5 MB; videolar MP4, WEBM formatlarında maksimum 25 MB qəbul edilir. Faylın uzantısı, MIME tipi, ilkin məzmun imzası və faktiki ölçüsü serverdə yoxlanılır. Önizləmə göstərilir, yükləmə tamamlanana qədər göndərmə bağlıdır. Faylı təkbaşına və ya mətnlə göndərmək mümkündür. Göndərilməmiş əlavəni silmək olar; göndərilmiş əlavələr bu əməliyyatla silinmir.

Şəkillər görünən sahəyə gələndə yüklənir. Video üçün “Videonu aç” seçimi var; video istifadəçi açmadan endirilmir. Native video controls və 24 saatlıq mövcud mesaj tarixləri saxlanılır. Canlı bağlantı olduqda SignalR, olmadıqda HTTP fallback işləyir. Eyni mesajın broadcast və cavabda iki dəfə gəlməsi tarixçədə dublikat yaratmır.

## Faylların saxlanması

Chat faylları Cloudinary-də `authenticated` media kimi saxlanılır. Bu rejimdə orijinal və törəmə fayllar imzasız URL ilə açılmır. [Cloudinary-nin rəsmi qaydası](https://cloudinary.com/documentation/control_access_to_media).

İmzalı URL və Cloudinary public ID-si frontend-ə qaytarılmır. Backend söhbətin iştirakçısını yoxlayır, qorunan Cloudinary faylını alır və autentifikasiya edilmiş API cavabı ilə ötürür. Brauzer həmin cavabdan lokal blob URL yaradır. Recipient göndərilməmiş faylı aça bilməz; başqa söhbətdə olan və ya başqa istifadəçinin yüklədiyi fayl mesaja əlavə edilə bilməz. Bir əlavə yalnız bir mesaja bağlanır. Oxuma cavabı `private, no-store` və `nosniff` istifadə edir.

Faylı mesaja bağlamaq, mesajın yazılması və bildirişin yaradılması eyni transaction-da baş verir. Uğursuz göndəriş əlavəni istifadə edilmiş vəziyyətdə saxlamır. Remote upload-un cavabı kəsilsə də server əvvəlcədən seçdiyi public ID ilə cleanup edə bilir. Göndərilməmiş və 24 saatdan köhnə əlavələr saatlıq worker tərəfindən maksimum 20 qeyddən ibarət batch-lərlə silinir. Göndərilmiş fayllar bu cleanup-a daxil deyil. Mövcud profil/məhsul Cloudinary şəkillərinin axını dəyişdirilməyib.

## API

- `POST /api/orders`: əvvəlki `items` ilə birlikdə `recipientName`, `deliveryPhone`, `deliveryCity`, `deliveryAddress` tələb olunur; `deliveryNote` istəyə bağlıdır. Qiymət və stok serverdən hesablanır.
- `POST /api/chat/conversations/{id}/attachments`: multipart `file`; yalnız söhbətin iştirakçısı. Metadata qaytarır, açıq Cloudinary URL-i qaytarmır.
- `GET /api/chat/conversations/{id}/attachments/{guid}/content`: iştirakçı və fayl əlaqəsi yoxlandıqdan sonra bytes. Göndərilməmiş faylı yalnız uploader oxuya bilər.
- `DELETE /api/chat/conversations/{id}/attachments/{guid}`: yalnız həmin söhbətdəki öz göndərilməmiş əlavəsi.
- `POST /api/chat/conversations/{id}/messages`: `{ "content": "", "attachmentId": "guid" }`; boş mətn yalnız əlavə olduqda qəbul edilir. Mətn mesajlarının əvvəlki contract-ı işləyir.
- SignalR `SendMediaMessage(conversationId, dto)`: eyni validation və transaction axını; `ReceiveMessage` metadata-nı daşıyır. Mövcud mətn üçün `SendMessage` dəyişdirilməyib.

## Migration və yoxlama

`20261013115724_OrderDeliveryAndChatMedia` migration-u 13.10.2026 tarixində LocalDB `MotoGarageDb` bazasına tətbiq edildi. Əvvəl native `COPY_ONLY, CHECKSUM` backup alındı və `RESTORE VERIFYONLY` ilə yoxlanıldı:

`C:\Users\VUQAR\AppData\Local\MotoGarage\DatabaseBackups\MotoGarage-before-DeliveryMedia-20261013-160419-72eb4940.bak`

Hesab sayı 7, sifariş sayı 2, mesaj sayı 4 olaraq qaldı. Yeni əlavələr cədvəli, nullable legacy sütunları, unique attachment index-i və restrict FK-lar əlavə edilib. EF modelində migration-a düşməmiş dəyişiklik yoxdur.

142 backend testi keçdi; real SQL testləri ayrıca təsadüfi adlı LocalDB bazalarında migration, legacy qeydlər, paralel attachment göndərişi və notification uğursuzluğunda rollback-i yoxladı. 153 frontend testi keçdi. Build və lint uğurludur; əsas bundle ölçüsü xəbərdarlığı qalır. Mobil 320px açıq/tünd tema ekranları da yoxlanıldı.

Real Cloudinary smoke yoxlamasında test PNG və qısa WEBM yükləndi, qorunan oxuma uğurlu oldu, imzasız giriş rədd edildi və müvəqqəti cloud faylları silindi. Test faylları heç bir real söhbətə göndərilmədi. Frontend testləri mock API/SignalR ilə işləyib; real development API login smoke testi backend işləmədiyi üçün ümumi brauzer dəstinə daxil edilməyib.

Yeni endpoint və worker-lərin tətbiqi üçün backend-i yenidən başlatmaq, frontend səhifəsini yeniləmək lazımdır. İki real hesabla ünvanın mağazaya çatmasını, media-nın qarşı tərəfdə açılmasını və istifadə ediləcək video codec-lərinin brauzerdə oynadılmasını manual yoxlamaq faydalıdır.
