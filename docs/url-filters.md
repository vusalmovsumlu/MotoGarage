# Axtarış və filterlərin URL-də saxlanması

Mövcud hash router saxlanılır. Axtarış formalarında **Axtar**, tarix aralığında **Filtrlə/Göstər** düyməsi seçiləndə tətbiq olunan qiymətlər query-string-ə yazılır. Status/stok tabları və select filterləri dərhal URL-i yeniləyir. Yalnız mətn yazmaq hələ yeni axtarış sorğusu göndərmir. Yeniləmə və brauzerin geri/irəli keçidi URL-dəki seçimləri və formanın görünən dəyərlərini bərpa edir.

| Bölmə | URL parametrləri | Nümunə |
| --- | --- | --- |
| Ustalar | `search`, `address` | `#/mechanics?search=Motarist&address=Bakı` |
| Mağazalar | `search`, `address` | `#/stores?search=Garage&address=Bakı` |
| Məhsullar | `search`, `brand`, `minPrice`, `maxPrice` | `#/products?search=yağ&brand=Honda&minPrice=0&maxPrice=100` |
| Görüşlərim / Gələn görüşlər | `status=active` və ya `finished` | `#/appointments?status=active`, `#/work/appointments?status=finished` |
| Sifarişlərim / Gələn sifarişlər | `status=active` və ya `finished` | `#/orders?status=active`, `#/work/orders?status=finished` |
| Yanacaq / Xərclər | `startDate`, `endDate`; avtomobil slug-u path-də | `#/garage/toyota-corolla-test/fuel?startDate=2026-10-01&endDate=2026-10-13` |
| Digər nəqliyyat qeydləri | seçilmiş avtomobil slug-u path-də | `#/garage/toyota-corolla-test/service`, `#/garage/toyota-corolla-test/reminders` |
| Bildirişlər | `unreadOnly=true` | `#/notifications?unreadOnly=true` |
| Mağazanın stoku | `stock=low` və ya `out` | `#/inventory?stock=low` |
| Admin istifadəçiləri | `search`, `role` | `#/admin/users?search=Əli&role=Mechanic` |
| Biznes müraciətləri | `status=Approved`, `Rejected`, `all` | `#/admin/applications?status=Rejected` |
| Admin əməliyyat tarixçəsi | `action`, `page` | `#/admin/audit?action=UserBlocked&page=2` |
| Usta/mağaza statistikası | `from`, `to` | `#/statistics?from=2026-10-01&to=2026-10-13` |
| Ustanın təqvimi | `date`, `view=day` | `#/work/calendar?date=2026-10-13&view=day` |

Query dəyərləri `URLSearchParams` ilə encode/decode edilir; Azərbaycan hərfləri, boşluq, `&`, `+` kimi işarələr qorunur. Boş/default filterlər URL-dən silinir, qiymət `0` isə saxlanılır. Biznes müraciətləri ilkin olaraq `Pending` göstərir; bütün müraciətlər üçün ayrıca `status=all` istifadə olunur. Təqvimdə ilkin görünüş həftəlikdir; `view=day` günlük görünüşdür. Açıq seçilmiş tarix URL-də saxlanır.

Filter dəyişiklikləri yalnız öz query açarlarını yeniləyir; başqa query parametrləri qorunur. Admin audit filteri dəyişəndə səhifə nömrəsi eyni history addımında 1-ə qaytarılır. Nəqliyyat dəyişəndə tətbiq olunmuş tarix filterləri yeni slug URL-də qalır. Köhnə `?vehicle=ID` linklərinin slug-a çevrilməsi də tarix query-lərini saxlayır. Eyni səhifədə query dəyişməsi tətbiq tərəfindən avtomatik yuxarı scroll yaratmır.

Admin auditində seçilmiş əməliyyat üçün qeyd yoxdursa həmin filterin adı göstərilir və **Filteri təmizlə** düyməsi ilə bütün əməliyyatlara qayıtmaq olur. URL-də artıq mövcud olmayan səhifə nömrəsi saxlanıbsa, seçilmiş əməliyyat qorunmaqla ilk səhifəyə qayıdılır; bu düzəliş əlavə brauzer history addımı yaratmır.

Səhv enum, səhifə nömrəsi, qiymət və tarix query-ləri uyğun təhlükəsiz ilkin seçimə düşür; həmin səhv dəyərlər API filterlərinə ötürülmür. Formada səhv qiymət/tarix aralığı yazılanda əvvəlki tətbiq edilmiş URL və nəticə saxlanılır. UI və API endpoint/request contract-ları qorunur. Axtarışa aid olmayan qeydiyyat, sifariş ünvanı və chat mətnləri query-string-ə yazılmır.

Paylaşılan link müvafiq hesabın giriş və rol/ownership icazələri çərçivəsində işləyir. URL filterləri əlavə icazə yaratmır. Backend və database modeli bu dəyişiklik üçün yenilənməyib.

Əsas kod: `shared/utils/routeQuery.js`, `shared/hooks/useUrlQuery.js`, `shared/hooks/useUrlFilterForm.js`. URL filterləri üçün 21 Playwright testi query bərpası, refresh, history, qiymət `0`, reset, slug, pagination və səhv dəyərləri yoxlayır.

Ümumi yoxlamada 174 frontend testi keçdi. `npm run build` və `npm run lint` uğurludur; mövcud bundle ölçüsü xəbərdarlığı qalır. Testlər mock API/SignalR ilə işləyib; backend işləmədiyi üçün ayrıca real development API smoke testi bu dəstə daxil edilməyib.

Admin auditinin ayrıca yoxlamasında `tests/admin-audit-filters.spec.js` qarışıq 135 qeyd ilə bütün beş filterin görünən sətirləri və sayını dəyişməsini, pagination, boş nəticəni, köhnə səhifəni və gecikən əvvəlki cavabı yoxlayır. Audit, URL və biznes əməliyyatları üzrə 40 frontend testi, həmçinin 3 backend testi keçdi. `AdminAuditFiltersTests` real HTTP controller sorğularını həm InMemory, həm ayrıca müvəqqəti LocalDB bazası ilə yoxlayır; tətbiqin bazasına test qeydləri yazılmır və email worker-ləri işə düşmür.
