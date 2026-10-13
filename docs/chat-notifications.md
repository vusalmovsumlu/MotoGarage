# Chat adları, URL-lər və canlı bildirişlər

- Usta və mağaza chat başlığında və söhbət siyahısında müştərinin tam adı göstərilir. Müştəri tərəfində biznesin adı görünür. Adlar birbaşa icazəli chat API cavabından gəlir; əlavə usta/mağaza kataloqu sorğusu edilmir.
- URL ad əsasında açılır: məsələn, `#/chat/vusal-movsumlu`. Eyni adlı şəxslər varsa slug-a sabit fərqləndirici əlavə olunur. Köhnə `#/chat/10` linkləri, o cümlədən əvvəlki bildirişlər, uyğun ad URL-inə yönləndirilir. API və SignalR daxilində söhbət ID-si istifadə edilir və iştirakçı icazələri yoxlanılır. Naməlum slug başqa söhbətin mesajlarını göstərmir.
- İstifadəçi, usta və mağaza üçün chat səhifəsindən asılı olmayan SignalR bağlantısı var. Yeni bildiriş yarananda navbar sayğacı və açıq bildiriş siyahısı yenilənir. Bağlantı bərpa olunanda və səhifəyə qayıdanda buraxılmış yeniləmələr API-dən alınır. Çıxış zamanı bağlantılar bağlanır.
- Serverdə EF interceptor-ları bildiriş əlavə/silmə/oxunma dəyişikliklərini toplayır. Explicit transaction üçün yalnız commit-dən sonra, digər SaveChanges üçün uğurlu tamamlanmadan sonra istifadəçinin öz qrupuna `NotificationsChanged` siqnalı göndərilir. Stok və xatırlatma kimi birbaşa DbContext vasitəsilə yazılan bildirişlər də əhatə olunur. Rollback və uğursuz yazı üçün siqnal göndərilmir.
- Siqnal bildiriş məzmununu daşımır. Məlumat mövcud autentifikasiya və ownership yoxlaması olan API-dən alınır. Əvvəlki 30 saniyəlik yeniləmə bağlantı itkisində ehtiyat mexanizmi kimi saxlanılır. Növbə davamlı saxlanc deyil; bildirişlərin özü database-də saxlanılır. Bir neçə API instance ilə deploy üçün SignalR backplane/Azure SignalR ayrıca konfiqurasiya edilməlidir.

Database modeli dəyişməyib; yeni migration lazım deyil. Dəyişikliklərin tətbiqi üçün backend yenidən başladılmalı və frontend yenilənməlidir.

## Yoxlama

1. Usta və mağaza hesabında söhbəti açın: başlıqda müştərinin adı, URL-də həmin adın slug-ı görünməlidir. Köhnə rəqəmli link və səhifənin yenilənməsi işləməlidir.
2. Usta/mağaza chatdan başqa səhifədə olarkən digər hesabdan mesaj, görüş və ya sifariş yaradın. Bildiriş sayğacı səhifəni yeniləmədən dəyişməlidir.
3. Bildiriş səhifəsi açıqdırsa yeni qeyd dərhal siyahıya gəlməlidir. Eyni hesabın iki tabında da yenilənmə işləməlidir.
4. İnterneti müvəqqəti kəsib bərpa edin; buraxılmış qeydlər yenilənməlidir. Çıxışdan sonra əvvəlki hesabın siqnalı yeni hesaba təsir etməməlidir.

Avtomatik backend yoxlamaları real SQL commit/rollback, failed save, bulk delete və real WebSocket-də hesabların ayrılmasını əhatə edir. Brauzer yoxlamaları slug, ad, HTTP fallback, canlı sayğac/siyahı və çıxış zamanı bağlantıların bağlanmasını yoxlayır. Production JWT/proxy ilə iki real hesabdan manual yoxlama ayrıca faydalıdır.

Son yoxlamada 135 backend və 144 frontend testi keçdi. Build və lint uğurludur; əvvəlki bundle ölçüsü xəbərdarlığı qalır. EF modelində migration-a düşməmiş dəyişiklik yoxdur. Backend testləri ayrıca təsadüfi adlı LocalDB bazalarında və izolə olunmuş WebSocket serverində işləyib; real hesab məlumatları dəyişdirilməyib. Frontend testləri mock API/SignalR ilə işləyib; real development API smoke testi bu dəstə daxil deyil.
