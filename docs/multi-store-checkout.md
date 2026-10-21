# Bir səbətdə fərqli mağazalar

Müştəri fərqli mağazaların məhsullarını eyni səbətə əlavə edib bir dəfə təsdiqləyir.
Səbətdə mağazalar və onların məbləğləri ayrıca göstərilir. Bir çatdırılma ünvanı
bütün sifarişlərə tətbiq olunur.

`POST /api/orders/checkout` mövcud `CreateOrderDto` formasını qəbul edir:
çatdırılma məlumatları və `items: [{ productId, quantity }]`. Qiymət, mağaza və
sifariş sahibi serverdə müəyyənləşdirilir. Cavab `201` statusu ilə
`{ orders: OrderDto[], totalAmount: number }` qaytarır.

Hər mağaza üçün ayrıca `Pending` sifariş yaradılır. Sifarişlərin statusu,
ləğvi, stokun bərpası və mağazanın giriş icazələri ayrıca idarə edilir.
Hər mağazaya öz sifarişinin bildirişi, müştəriyə isə hər sifariş üçün mövcud
email növbəsi vasitəsilə ayrıca status məktubu hazırlanır.

Bütün mağazaların sifarişləri, stok dəyişiklikləri, bildirişlər və email
snapshot-ları eyni `Serializable` UnitOfWork transaction daxilində yazılır.
İstənilən mağazada xəta yaranarsa bütün checkout geri qaytarılır; frontend
səbəti və ünvanı saxlayır. Məhsullar sabit ID ardıcıllığında oxunur və yenilənir;
mövcud rowversion və deadlock retry qoruması işləyir.

Səbət üzrə maksimum 100 fərqli məhsul və hər məhsul üçün 1–10000 ədəd limiti
saxlanılıb. Təkrarlanan məhsul ID-ləri, aktiv olmayan məhsul/mağaza,
bloklanmış və silinmiş hesablar, çatışmayan stok və yanlış ünvan rədd olunur.

`POST /api/orders` əvvəlki tək mağazalı sorğu/cavab formasını saxlayır.
Frontend yeni checkout endpoint-indən istifadə edir. Backend və frontend
birlikdə yenilənməlidir. Database migration tələb olunmur.

Testlər: `MultiStoreCheckoutTests`, `MultiStoreCheckoutSqlTests` və
`frontend/tests/multi-store-checkout.spec.js`. SQL testi ayrıca təsadüfi adlı
LocalDB bazasında üç mağazanın sifarişlərini, son mağazada bildiriş xətasının
tam rollback etməsini və son stok üçün paralel checkout yarışını yoxlayır.
Əsas istifadəçi bazasına sifariş yazılmır və real email göndərilmir.
