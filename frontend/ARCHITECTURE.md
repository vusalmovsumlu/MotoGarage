# Frontend arxitekturası

Mövcud MotoGarage frontend-i yerində feature-based strukturda refactor edilib. Dizayn, responsive layout, CSS məzmunu, asset-lər və API contract-ları saxlanılıb. Backend və migration faylları dəyişdirilməyib. Yeni runtime dependency əlavə edilməyib.

## Qovluq ağacı

```text
src/
├── app/
│   ├── providers/
│   │   ├── AppContext.js
│   │   └── AppProvider.jsx
│   ├── router/
│   │   ├── AppRouter.jsx
│   │   ├── navigation.js
│   │   ├── recordRoutes.js
│   │   ├── resolveRoute.jsx
│   │   └── useRoute.js
│   └── App.jsx
├── assets/
│   ├── hero.png
│   ├── react.svg
│   └── vite.svg
├── components/
│   ├── common/
│   │   ├── catalog/
│   │   │   ├── Catalog.jsx
│   │   │   └── CatalogFilters.jsx
│   │   ├── merchants/
│   │   │   ├── businessFields.js
│   │   │   ├── BusinessProfile.jsx
│   │   │   ├── BusinessProfileCard.jsx
│   │   │   ├── BusinessProfileEditor.jsx
│   │   │   ├── MerchantCard.jsx
│   │   │   ├── MerchantDetail.jsx
│   │   │   └── MerchantHeader.jsx
│   │   ├── records/
│   │   │   ├── RecordEditor.jsx
│   │   │   ├── RecordFilters.jsx
│   │   │   ├── RecordRow.jsx
│   │   │   └── Records.jsx
│   │   ├── work/
│   │   │   ├── WorkCard.jsx
│   │   │   └── WorkList.jsx
│   │   ├── Editor.jsx
│   │   ├── EntityForm.jsx
│   │   ├── ImageUpload.jsx
│   │   ├── Stat.jsx
│   │   └── Thumbnail.jsx
│   ├── feedback/
│   │   ├── ConfirmDialog.jsx
│   │   ├── Empty.jsx
│   │   ├── Resource.jsx
│   │   ├── Skeleton.css
│   │   ├── Skeleton.jsx
│   │   └── ToastRegion.jsx
│   ├── layout/
│   │   ├── Page.jsx
│   │   ├── Section.jsx
│   │   └── Workspace.jsx
│   └── ui/
│       ├── Badge.jsx
│       ├── Button.jsx
│       ├── FilterTabs.jsx
│       ├── IconButton.jsx
│       ├── Modal.jsx
│       ├── PeekRating.css
│       ├── PeekRating.jsx
│       └── SearchBar.jsx
├── features/
│   ├── admin/
│   │   ├── api/
│   │   │   └── adminApi.js
│   │   ├── components/
│   │   │   ├── AdminUsersFilters.jsx
│   │   │   └── AdminUsersTable.jsx
│   │   ├── hooks/
│   │   │   └── useAdminUsers.js
│   │   └── pages/
│   │       └── AdminUsers.jsx
│   ├── appointments/
│   │   ├── api/
│   │   │   └── appointmentsApi.js
│   │   ├── components/
│   │   │   └── AppointmentEditor.jsx
│   │   ├── pages/
│   │   │   └── Appointments.jsx
│   │   └── utils/
│   │       └── appointmentTransitions.js
│   ├── auth/
│   │   ├── api/
│   │   │   └── authApi.js
│   │   ├── components/
│   │   │   └── AuthForm.jsx
│   │   ├── hooks/
│   │   │   ├── useAuth.js
│   │   │   └── useAuthSession.js
│   │   └── pages/
│   │       └── Auth.jsx
│   ├── chat/
│   │   ├── api/
│   │   │   └── chatApi.js
│   │   ├── components/
│   │   │   ├── ConversationList.jsx
│   │   │   ├── MessageComposer.jsx
│   │   │   └── MessageHistory.jsx
│   │   ├── hooks/
│   │   │   └── useChat.js
│   │   └── pages/
│   │       └── Chat.jsx
│   ├── dashboard/
│   │   ├── api/
│   │   │   └── dashboardApi.js
│   │   ├── components/
│   │   │   ├── DashboardDestinations.jsx
│   │   │   ├── DashboardStats.jsx
│   │   │   ├── DashboardSummary.jsx
│   │   │   └── Preview.jsx
│   │   ├── hooks/
│   │   │   └── useDashboard.js
│   │   ├── pages/
│   │   │   └── Home.jsx
│   │   └── utils/
│   │       └── destinations.js
│   ├── expenses/
│   │   ├── api/
│   │   │   └── expensesApi.js
│   │   ├── pages/
│   │   │   └── Expenses.jsx
│   │   └── utils/
│   │       └── recordDefinition.js
│   ├── fuel/
│   │   ├── api/
│   │   │   └── fuelApi.js
│   │   ├── components/
│   │   │   └── FuelStats.jsx
│   │   ├── pages/
│   │   │   └── FuelRecords.jsx
│   │   └── utils/
│   │       └── recordDefinition.js
│   ├── garage/
│   │   ├── api/
│   │   │   └── garageApi.js
│   │   ├── components/
│   │   │   ├── VehicleCard.jsx
│   │   │   └── VehicleEditor.jsx
│   │   ├── hooks/
│   │   │   └── useVehicles.js
│   │   ├── pages/
│   │   │   ├── VehicleDetail.jsx
│   │   │   └── Vehicles.jsx
│   │   └── utils/
│   │       └── vehicleFields.js
│   ├── mechanics/
│   │   ├── api/
│   │   │   └── mechanicsApi.js
│   │   └── pages/
│   │       ├── MechanicCatalog.jsx
│   │       ├── MechanicDetail.jsx
│   │       └── MechanicProfile.jsx
│   ├── notifications/
│   │   ├── api/
│   │   │   └── notificationsApi.js
│   │   ├── components/
│   │   │   └── NotificationRow.jsx
│   │   ├── hooks/
│   │   │   └── useNotifications.js
│   │   └── pages/
│   │       └── Notifications.jsx
│   ├── orders/
│   │   ├── api/
│   │   │   └── ordersApi.js
│   │   ├── components/
│   │   │   ├── CartItem.jsx
│   │   │   └── CheckoutSummary.jsx
│   │   ├── hooks/
│   │   │   ├── useCart.js
│   │   │   └── useCheckout.js
│   │   ├── pages/
│   │   │   ├── Cart.jsx
│   │   │   └── Orders.jsx
│   │   └── utils/
│   │       └── orderTransitions.js
│   ├── products/
│   │   ├── api/
│   │   │   └── productsApi.js
│   │   ├── components/
│   │   │   ├── InventoryRow.jsx
│   │   │   ├── ProductCard.jsx
│   │   │   ├── ProductEditor.jsx
│   │   │   └── StockEditor.jsx
│   │   ├── hooks/
│   │   │   └── useInventory.js
│   │   ├── pages/
│   │   │   ├── Inventory.jsx
│   │   │   ├── ProductCatalog.jsx
│   │   │   └── ProductDetail.jsx
│   │   └── utils/
│   │       └── productFields.js
│   ├── profile/
│   │   ├── api/
│   │   │   └── profileApi.js
│   │   ├── components/
│   │   │   └── ProfileEditor.jsx
│   │   ├── hooks/
│   │   │   └── useProfile.js
│   │   └── pages/
│   │       └── Profile.jsx
│   ├── reminders/
│   │   ├── api/
│   │   │   └── remindersApi.js
│   │   ├── pages/
│   │   │   └── Reminders.jsx
│   │   └── utils/
│   │       └── recordDefinition.js
│   ├── reviews/
│   │   ├── api/
│   │   │   └── reviewsApi.js
│   │   └── components/
│   │       ├── ReviewCard.jsx
│   │       └── ReviewEditor.jsx
│   ├── service-records/
│   │   ├── api/
│   │   │   └── serviceRecordsApi.js
│   │   ├── pages/
│   │   │   └── ServiceRecords.jsx
│   │   └── utils/
│   │       └── recordDefinition.js
│   └── stores/
│       ├── api/
│       │   └── storesApi.js
│       └── pages/
│           ├── StoreCatalog.jsx
│           ├── StoreDetail.jsx
│           └── StoreProfile.jsx
├── shared/
│   ├── api/
│   │   ├── client.js
│   │   ├── createVehicleRecordApi.js
│   │   ├── createWorkflowApi.js
│   │   └── imagesApi.js
│   ├── config/
│   │   └── environment.js
│   ├── constants/
│   │   └── labels.js
│   ├── hooks/
│   │   ├── useApp.js
│   │   ├── useBusinessProfile.js
│   │   ├── useCatalog.js
│   │   ├── useFeedback.js
│   │   ├── useMerchantDetail.js
│   │   ├── useResource.js
│   │   ├── useTheme.js
│   │   ├── useVehicleRecords.js
│   │   └── useWorkList.js
│   └── utils/
│       ├── format.js
│       ├── formFields.js
│       └── query.js
├── styles/
│   └── index.css
└── main.jsx
```

## Məsuliyyət və import qaydaları

- `app/App.jsx` yalnız provider və router-i birləşdirir. `app/router` hash routing, role guards, route keys və navigation-u saxlayır. `app/providers` auth, tema, səbət, toast və confirmation state-lərini birləşdirir.
- `features/<feature>` domain səhifələri, komponentləri, API adapter-ləri, hook-ları və forma/status tərifləridir. Qovluqlar yalnız real fayllar lazım olduqda yaradılıb.
- `components/ui` primitive-lər, `components/layout` səhifə karkası, `components/feedback` skeleton/empty/error/toast/confirmation, `components/common` isə ümumi formalar və paylaşılmış CRUD görünüşləridir.
- `shared/api/client.js` yeganə HTTP/session client-dir: JWT header, refresh single-flight, logout revision guard və API error normalization burada qalır. Feature adapter-ləri URL/method/payload-ları müəyyən edir. `useResource` əvvəlki AbortController/loading/error/reload davranışını saxlayır.
- Shared catalog/merchant/record/work görünüşləri feature API və komponentlərini props vasitəsilə qəbul edir. Common/UI/feedback və shared modulları feature-ləri import etmir. Feature giriş səhifələri əlaqəli review/appointment/product/chat leaf modullarını birləşdirir.
- `useApp` yalnız leaf `AppContext.js` kontraktına istinad edir; provider-i import etmir. Navigation router-in leaf `useRoute.js` modulundan istifadə edir. Circular dependency yoxdur.
- Relative import-lar, named/default export-lar və JSX/JS ayrımı ardıcıl saxlanılıb. Barrel export-lar və yeni routing kitabxanası əlavə edilməyib.
- Global CSS `styles/index.css`, skeleton/rating CSS-ləri öz komponentlərinin yanında yerləşir. Public icon/font/license yolları əvvəlki kimidir.

## Bölünən fayllar

| Əvvəlki fayl                               | Sətir | Yeni məsuliyyətlər                                                                                                                                         |
| ------------------------------------------ | ----: | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| App.jsx                                    |   489 | App setup, provider/context, router/navigation; auth session/theme/cart/feedback hook-ları və ToastRegion                                                  |
| components/ui.jsx                          |   534 | UI, layout, shared form və feedback komponentləri                                                                                                          |
| pages/Catalog.jsx                          |   640 | Mechanics/stores/products səhifələri; Catalog/Filters və merchant komponentləri; ProductCard/Detail; Cart/CheckoutSummary; ReviewCard/Editor               |
| pages/Garage.jsx                           |   522 | Vehicles/Detail/Card/Editor/useVehicles; fuel/expenses/service/reminders giriş səhifələri və API-ləri; shared Records/Filters/Row/Editor/useVehicleRecords |
| pages/Account.jsx                          |   345 | Profile/Editor/hook; Notifications/Row/hook; AdminUsers/Filters/Table/hook                                                                                 |
| pages/Business.jsx                         |   266 | MechanicProfile/StoreProfile, shared BusinessProfile/Editor/Card; Inventory/Row/ProductEditor/StockEditor/hook                                             |
| pages/Work.jsx                             |   308 | Appointments/Orders, AppointmentEditor; shared WorkList/WorkCard/hook; status transition-ları                                                              |
| pages/Home.jsx                             |   395 | Home, DashboardStats/Summary/Destinations, Preview və useDashboard                                                                                         |
| pages/Chat.jsx                             |   393 | Chat, useChat daxilində SignalR lifecycle/HTTP fallback; ConversationList/MessageHistory/MessageComposer                                                   |
| pages/Auth.jsx                             |   376 | Auth, AuthForm, useAuth; OTP pending state, Google loader və resend davranışı qorunub                                                                      |
| lib/api.js, hooks.js, format.js, fields.js |     — | Shared client/config/hooks/utils/constants və feature forma tərifləri                                                                                      |

`App.jsx` 11 sətirdir. Ən böyük JSX faylı mövcud PeekRating-dir (352 sətir); vahid interaksiya/animasiya state-ni idarə etdiyi üçün əlavə xırdalanmayıb. SignalR state məntiqi `useChat.js` daxilindədir (247 sətir).

## Reusable hissələr və təkrar kod

Mövcud UI primitive-ləri ayrıca fayllara çıxarılıb. `FilterTabs` status və unread filterlərini eyni DOM ilə göstərir. `Catalog`, `MerchantDetail`, `BusinessProfile`, `Records` və `WorkList` paylaşılmış görünüşlərdir; feature adapter/config-ləri dəyişir.

`createVehicleRecordApi` dörd qeyd növünün ortaq save/delete kontraktını, `createWorkflowApi` sifariş/görüş list/status/cancel sorğularını birləşdirir. Aktiv iş filteri bir dəfə hesablanır. Forma field helper-ləri bir shared moduldadır. İstifadə olunmayan import-lar, köhnə monolit fayllar və artıq lazım olmayan StatusTabs wrapper-i silinib.

Numeric ID/rating çevrilməsi, slug encoding fərqləri, query parametr sırası, false/zero dəyərləri, HTTP method-lar, anonymous auth və image FormData kontraktları saxlanılıb. State-lər props ilə ötürülür; əlavə DOM wrapper-i yaradılmayıb.

## Yoxlama

İlkin vəziyyətdə build, lint və 31 fixture browser sınağı keçib. Yekun nəticələr:

- `npm run build`: uğurlu production build.
- `npm run lint`: uğurlu, warning/error yoxdur. Lint əvvəl də mövcud idi; browser/Node globals və no-undef/no-unused-vars error qaydaları dəqiqləşdirilib.
- `npm test -- --reporter=line --grep-invert 'Real development API'`: 40 fixture sınağı; əvvəlki 31 və 9 yeni regression yoxlaması.
- Bütün local import-lar mövcuddur, circular dependency yoxdur; shared/common UI feature-ləri import etmir.
- 50 mutation sorğusu üçün URL/method/payload və anonymous/Bearer header; query/slug builder-ləri; dörd record feature-in create/edit/delete formaları, reminder complete və date-filter validation yoxlanılıb.
- Rol route-ları, slug navigation, vehicle query, OTP, refresh/logout guard, cart, status keçidləri, admin confirmation, HTTP chat fallback, modal keyboard/upload lock, skeleton/error və PeekRating yoxlanılıb.
- 20 səhifə/rol kombinasiyası × 1440/390 px = 40 əvvəl/sonra screenshot byte müqayisəsində eynidir. Mövcud responsive sınaqlar 1440/1000/799/390/320 px və hər iki temanı əhatə edir.
- Üç CSS faylının məzmunu byte səviyyəsində saxlanılıb; production CSS hash-i dəyişməyib. Backend SHA-256 müqayisəsində dəyişiklik yoxdur.

Backend olmadan fixture yoxlaması:

```powershell
npm run build
npm run lint
npm test -- --reporter=line --grep-invert 'Real development API'
```

Fixture yalnız origin-in `/api/` yolunu intercept edir; yeni `/src/.../api/...` modulları backend fixture kimi tutulmur. Security sınaqları köçürülmüş singleton client yolunu import edir. Test config və runtime dependencies dəyişdirilməyib.

## Avtomatik təsdiqlənməyən inteqrasiyalar

Real backend işləmədiyinə görə mövcud real API login/proxy probe bu keçiddə exclude edilib. Real Google OAuth, SMTP OTP çatdırılması, Cloudinary upload, iki hesab arasında canlı SignalR, backend role/ownership/block enforcement və paralel order/stock/appointment əməliyyatları fixture sınaqları ilə təsdiqlənmir. Backend işlədikdə bunlar real test hesabları ilə manual integration test tələb edir. Refactor zamanı DB məlumatları dəyişdirilməyib.
