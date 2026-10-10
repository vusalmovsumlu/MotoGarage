# MotoGarage frontend

React 19 və Vite ilə hazırlanmış Azərbaycan dilli interfeys. Avtomobil və motosikletlər üçün User, Mechanic, Store və Admin iş masaları mövcuddur. Dizayn quiet-workspace-ui skill-in Inter şriftindən, mavi-boz palitrasından və iş masası düzülüşündən istifadə edir; açıq və tünd tema dəstəklənir.

## Lokal işə salma

Repo kökündə:

```powershell
dotnet run --project MotoGarage.API --launch-profile https
```

Frontend qovluğunda:

```powershell
npm ci
npm run dev -- --host 127.0.0.1
```

İnterfeys: http://127.0.0.1:5173. Vite `/api` və `/hubs` sorğularını `https://localhost:7242` ünvanına ötürür. Lokal inkişaf proxy-si development sertifikatını qəbul edir; production üçün etibarlı HTTPS sertifikatı lazımdır.

`.env.example` əsasında `.env.local` yaradıla bilər. `VITE_GOOGLE_CLIENT_ID` yalnız Google-un açıq client ID-sidir. Google Console-da frontend origin-i qeyd edilməlidir. Google client secret, JWT signing key, SMTP, DB və Cloudinary secret frontend-ə yerləşdirilməməlidir: `VITE_*` dəyişənləri istifadəçinin brauzerinə ötürülür.

## Əhatə olunan axınlar

- Giriş, qeydiyyat, email OTP, kodun təkrar göndərilməsi, Google giriş, refresh-token rotasiyası və çıxış.
- Qeydiyyatda ayrı məcburi Ad/Soyad sahələri API-yə mövcud `fullName` kimi birləşdirilir. OTP sahəsində React Bits CodeSlots istifadə olunur: 6 rəqəm, klaviatura/paste, səhv kodun animasiya ilə təmizlənməsi, mobil ölçü və reduced-motion dəstəyi. Komponent yalnız OTP səhifəsində yüklənir.
- Usta/mağaza qeydiyyatında 3 fərqli real şəklin Cloudinary-yə yüklənməsi, email təsdiqindən sonra adminin biznes müraciətlərini qəbul/rədd etməsi və məcburi rədd şərhinin istifadəçiyə göstərilməsi. Detallar: [biznes müraciətləri](../docs/business-applications.md).
- Usta/mağaza qeydiyyatında 3 şəkli bir seçimlə əlavə etmək, önizləmə və ayrı şəkli əvəzləmək mümkündür. Admin qəbul pəncərəsi təsdiq ikonuyla göstərilir. Usta və mağaza menyuları yalnız öz biznes alətlərini göstərir; iş masası şəxsi qaraj statistikası yerinə gələn görüş/sifariş məlumatlarından istifadə edir.
- Qaraj CRUD, Car/Motorcycle seçimi, yanacaq və sərfiyyat göstəriciləri, xərc və servis qeydləri, xatırlatmalar.
- Qaraj detalında **Tarixçəni paylaş**: avtomobil/motosikletin servis, xərc və yanacaq tarixçəsini Excel olaraq yükləmək və alıcının emailinə əlavə kimi göndərmək. Kilometr servis və yanacaq qeydlərində göstərilir; mövcud xərc qeydlərində kilometr saxlanmır. [İstifadə və API qaydaları](../docs/vehicle-history-export.md).
- Usta, mağaza və məhsul kataloqları, axtarış/filterlər, slug üzrə detallar və rəylər.
- Usta rəylərində müəllifin adı və soyadı backend-in `userFullName` cavabından göstərilir; mövcud rəylər də bu məlumatı əlaqəli User-dən alır.
- Görüş yaratma/ləğv etmə, ustanın qəbul/rədd/tamamlama əməliyyatları.
- Aktiv görüşə təxminən 2 saat qalmış hesabın təsdiqlənmiş emailinə avtomatik xatırlatma. Brauzer bağlı olanda da backend işləyirsə göndərilir; tarix və saat Bakı vaxtı ilə göstərilir. [Migration və sazlamalar](../docs/appointment-email-reminders.md).
- Bir mağazaya aid səbət, sifariş və mağazanın icazə verilən status keçidləri, məhsul/stok idarəetməsi.
- Biznes profili, istifadəçi profili, bildirişlər, admin statistikası və istifadəçi bloklama/açma.
- Admin üçün yalnız idarəetmə menyusu, biznes müraciətləri və təsdiqlə hesab silinməsi. Şəxsi qaraj/alış/chat axınları adminə göstərilmir. Silinmə girişləri bağlayır və hesab məlumatlarını anonimləşdirir; əlaqəli tarixçə saxlanılır. [Qaydalar və tələb olunan migration](../docs/admin-accounts.md).
- JWT ilə SignalR chat, yenidən qoşulma, mesajların ID üzrə təkrarlanmasının qarşısının alınması, HTTP göndərmə/oxundu fallback-i.

Runtime məlumatları API-dən gəlir. JWT-dəki rol menyunu müəyyən edir; icazə və ownership üçün son qərarı backend verir. Tokenlər tabın sessionStorage yaddaşında saxlanır. Eyni səhifədə paralel refresh sorğuları birləşdirilir; gecikmiş refresh cavabı çıxış edilmiş sessiyanı bərpa etmir. Native dialog-lar, klaviatura fokusları, forma limitləri, upload zamanı save/close kilidi və destructive əməliyyatlar üçün təsdiq var.

## Yoxlama

```powershell
npm run lint
npm run build
npx playwright install chromium
npm test
```

`tests/fixtures.js` yalnız browser testlərinə aid açıq fixture-lərdir. Testlər rol səhifələrini, avtomobil/motosiklet formalarını, API payload-larını, səbət qaydalarını, status keçidlərini, OTP, admin təsdiqi, chat fallback, boş/xəta halları və 1440/1000/799/390/320 px ölçülərində hər iki temanı yoxlayır. Screenshot-lar `test-results/` altında saxlanır. Real API ilə login xətası/proxy ayrıca yoxlanır; testlərdə real hesab yaradılmır və istifadəçi məlumatları dəyişdirilmir.

Production-dan əvvəl real test hesabları ilə hər rolun girişini, SMTP OTP-ni, Google girişini, Cloudinary upload-u və iki ayrı hesab arasında SignalR mesajlaşmasını yoxlayın. Sifariş/stok və görüş əməliyyatlarının eyni vaxtda müxtəlif brauzerlərdə icrasını backend ilə birlikdə yoxlayın. Fixture testləri bu inteqrasiyaların işlək olduğunu təsdiqləmir.

## Production yerləşdirməsi

`npm run build` nəticəsi `dist/` qovluğudur. Hash routing tətbiq olunur. Eyni origin-də `/api/*` backend-ə, `/hubs/*` isə WebSocket upgrade dəstəyi ilə backend-ə yönləndirilməlidir. Ayrı origin üçün `VITE_API_URL`/`VITE_HUB_URL` və backend-in CORS origin-lərini uyğunlaşdırın. Vite development proxy-si dist daxilində işləmir. HTTPS, təhlükəsizlik başlıqları və CSP-ni Google GSI/Cloudinary domenləri ilə uyğun qurun.

## Vizual mənbələr

Qısa əməliyyat mesajları istifadəçinin təqdim etdiyi React Bits SwipeToast əsasında göstərilir. Mövcud `toast(message, error)` çağırışları saxlanılıb; bütün səhifələr eyni feedback region-dan istifadə edir. Açıq/tünd temada kart səthi və mətn rəngləri tətbiqin palitrasından, uğurlu mesajın vurğusu mavi və xəta vurğusu qırmızı rəngdən gəlir. Aşağı sürüşdürmək, bağlama düyməsi və fokuslanmış kartda Escape ilə bağlanır. Normal halda əvvəlki kimi 6.5 saniyə sonra bağlanır; mouse hover, klaviatura fokusu, sürüşdürmə və gizli tab zamanı sayğac dayanır. Maksimum üç mesaj, slot bağlanma animasiyası və reduced-motion dəstəyi var. Mənbə lisenziyası `public/licenses/react-bits.txt` daxilində saxlanır.

API məlumatları yüklənərkən səhifəyə uyğun skeleton göstərilir: iş masası, kartlar, detal, profil, forma, siyahı, admin cədvəli və chat. Skeleton animasiyası reduced-motion seçiminə uyğun dayanır. Rəy yaratma və redaktə formasında [React Bits Peek Rating](https://reactbits.dev/micro/peek-rating) istifadə edilir; 1–5 ulduz seçimi mouse, touch və klaviatura ilə işləyir. Rəy kartlarında həmin komponentin read-only variantı var. Komponent mövcud Lucide ikonlarına, Azərbaycan dilinə və tema rənglərinə uyğunlaşdırılıb; lisenziya `public/licenses/react-bits.txt` daxilində saxlanır.

Inter şriftləri `public/fonts/LICENSE` altında lisenziyalanır. Schedule, homework və settings ikonları istifadəçinin verdiyi skill-dəndir. `garage.png`, `mechanic.png`, `market.png` builtin imagegen vasitəsilə yaradılıb. Qaraj ikonu istifadəçinin təqdim etdiyi motosiklet fotosuna əsasən image edit rejimində yenilənib: öndən baxış, qara aerodinamik korpus, iti ağ LED faralar və qızılı amortizatorlar; mavi həcmli tile və şəffaf fon saxlanıb. Digər prompt mövzuları: teal tile üzərində çarpaz servis alətləri; amber tile üzərində dişli və alış-veriş çantası. Heç bir əlavə mətn/brend nişanı yaradılmayıb. Fayllar `public/icons/` altındadır.

SignalR inteqrasiyası üçün [Microsoft sənədləri](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-8.0), Google giriş üçün [Google GSI API](https://developers.google.com/identity/gsi/web/reference/js-reference) istifadə olunub.

## Frontend arxitekturası

Mövcud kod feature-based strukturda saxlanılır: app setup/router/providers, ümumi UI/layout/feedback komponentləri, feature pages/components/hooks/api/utils və shared client/hooks/utils/config. Tam qovluq ağacı, məsuliyyət qaydaları, refactor xəritəsi və yoxlama nəticələri [ARCHITECTURE.md](./ARCHITECTURE.md) daxilindədir.

Backend işləmirsə yalnız fixture sınaqları üçün `npm test -- --reporter=line --grep-invert 'Real development API'` istifadə edin. Bu sınaqlar real hesab və DB məlumatlarını dəyişdirmir.

## Avtomatik xatırlatma bildirişləri

Backend tarix və ya odometr həddi çatmış xatırlatmaları avtomatik bildirişə çevirir. Bildiriş siyahısı görünən brauzerdə hər 30 saniyədə yenilənir; “Bax” uyğun nəqliyyat vasitəsinin xatırlatmalarını açır. Təkrar göndərilmə, konfiqurasiya, migration və sınaq qaydaları [reminder-notifications.md](../docs/reminder-notifications.md) daxilindədir.

Hər bildirişdə “Sil”, səhifədə “Hamısını sil” düyməsi var. Silmə təsdiq pəncərəsindən sonra icra olunur; xəta olduqda pəncərə açıq qalır və siyahı saxlanılır. Uğurlu əməliyyatdan sonra siyahı və unread sayğacı yenilənir. `DELETE /api/notifications/{id}` yalnız sahibin bildirişini silir; `DELETE /api/notifications` giriş etmiş istifadəçinin oxunmuş və oxunmamış bütün bildirişlərini bir SQL sorğusunda silir. Başqa istifadəçilərin bildirişləri saxlanılır. Xatırlatma bildirişinin silinməsi göndərilmə qeydini sıfırlamır, ona görə eyni xatırlatma təkrar göndərilmir. Bu dəyişiklik üçün migration lazım deyil; backend yeni build ilə başladılmalıdır.

Qaraj detallarında və nəqliyyat vasitəsinin qeydlərində ID əvəzinə sabit slug ünvanları istifadə olunur. Routing, ownership, köhnə keçidlərin yönləndirilməsi və migration qaydaları [vehicle-slugs.md](../docs/vehicle-slugs.md) daxilindədir.

## Navbar və saat formatı

“Digər” menyusu istifadəçinin təqdim etdiyi React Bits BranchedMenu əsasında qurulub: planlar/alış-veriş, nəqliyyat qeydləri və rola uyğun biznes/idarəetmə bölmələri. SVG budaqlarının aktiv xətti, açılıb-yığılma və panel animasiyası mövcud tema rənglərindən istifadə edir. Keçidlər əvvəlki hash URL-lərini saxlayır; aktiv seçim URL və nəqliyyatın slug-lu qeyd səhifələri ilə sinxron qalır. Bölmə başlıqları Enter/Space ilə işləyir, bağlı bölmələr fokusdan çıxarılır, Escape fokusu “Digər”ə qaytarır və kənara klik paneli bağlayır. Panel viewport daxilində yerləşir, qısa ekranlarda öz daxilində scroll edir və reduced-motion dəstəyi var. Mənbə lisenziyası `public/licenses/react-bits.txt` daxilində saxlanır.

Əsas navbar [React Bits Gooey Nav](https://reactbits.dev/components/gooey-nav) əsasında uyğunlaşdırılıb. Aktiv keçid hash route-dan hesablanır; rol keçidləri, Digər menyusu, klaviatura ilə Enter və brauzer history-si saxlanılıb. Animasiya mövcud tema palitrasından istifadə edir, mobil wrap/resize vəziyyətlərinə uyğunlaşır və reduced-motion seçimini nəzərə alır. Animasiya timer-ləri unmount zamanı təmizlənir. Mənbə lisenziyası `public/licenses/react-bits.txt` daxilindədir.

Yanacaq, xərc, servis, xatırlatma və görüş formalarında tarix ayrı calendar sahəsində, saat isə `HH:mm` formatında daxil edilir. AM/PM istifadə olunmur. Mobil numeric klaviatura ilə `2130` yazanda `21:30` yaranır; `24:00` və `12:60` kimi yanlış saatlar form validation tərəfindən rədd edilir. Giriş əvvəlki kimi cihazın yerli saat qurşağından UTC ISO formatına çevrilir, API müqavilələri saxlanılıb. Görünən tarix-saatlar mövcud `Asia/Baku`/`h23` formatter-i ilə göstərilir.
