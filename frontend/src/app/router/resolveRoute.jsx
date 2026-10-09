import { MechanicCatalog } from "../../features/mechanics/pages/MechanicCatalog.jsx";
import { StoreCatalog } from "../../features/stores/pages/StoreCatalog.jsx";
import { ProductCatalog } from "../../features/products/pages/ProductCatalog.jsx";
import { MechanicDetail } from "../../features/mechanics/pages/MechanicDetail.jsx";
import { StoreDetail } from "../../features/stores/pages/StoreDetail.jsx";
import { Orders } from "../../features/orders/pages/Orders.jsx";
import { Appointments } from "../../features/appointments/pages/Appointments.jsx";
import { recordPages } from "./recordRoutes.js";
import { MechanicProfile } from "../../features/mechanics/pages/MechanicProfile.jsx";
import { StoreProfile } from "../../features/stores/pages/StoreProfile.jsx";
import Home from "../../features/dashboard/pages/Home.jsx";
import { Vehicles } from "../../features/garage/pages/Vehicles.jsx";
import { VehicleDetail } from "../../features/garage/pages/VehicleDetail.jsx";
import { ProductDetail } from "../../features/products/pages/ProductDetail.jsx";
import { Cart } from "../../features/orders/pages/Cart.jsx";
import { Profile } from "../../features/profile/pages/Profile.jsx";
import { Notifications } from "../../features/notifications/pages/Notifications.jsx";
import Chat from "../../features/chat/pages/Chat.jsx";
import { AdminUsers } from "../../features/admin/pages/AdminUsers.jsx";
import { BusinessApplications } from "../../features/admin/pages/BusinessApplications.jsx";
import { Inventory } from "../../features/products/pages/Inventory.jsx";
import { Page } from "../../components/layout/Page.jsx";
import { Empty } from "../../components/feedback/Empty.jsx";
import { vehicleSlugFromRoute } from "../../shared/utils/vehicleRoutes.js";

const catalogPages = {
  mechanics: MechanicCatalog,
  stores: StoreCatalog,
  products: ProductCatalog,
};
const merchantPages = { mechanics: MechanicDetail, stores: StoreDetail };
const workPages = { orders: Orders, appointments: Appointments };
export function resolveRoute(route, user) {
  const path = route.split("?")[0];
  const parts = path.split("/").filter(Boolean);
  const RecordPage = recordPages[parts[1]];
  const CatalogPage = catalogPages[parts[0]];
  const MerchantPage = merchantPages[parts[0]];
  const WorkPage = workPages[parts[0]];
  const BusinessPage =
    user.role === "Mechanic" ? MechanicProfile : StoreProfile;
  if (path === "/home") return <Home />;
  if (path === "/vehicles") return <Vehicles />;
  if (parts[0] === "vehicles" && /^\d+$/.test(parts[1] || ""))
    return <VehicleDetail key={path} id={parts[1]} />;
  if (parts[0] === "garage" && vehicleSlugFromRoute(route)) {
    if (parts.length === 2)
      return <VehicleDetail key={path} slug={vehicleSlugFromRoute(route)} />;
    const GarageRecordPage = recordPages[parts[2]];
    if (parts.length === 3 && GarageRecordPage)
      return <GarageRecordPage key={route} route={route} />;
  }
  if (
    parts[0] === "records" &&
    ["fuel", "expenses", "service", "reminders"].includes(parts[1])
  )
    return <RecordPage key={route} route={route} />;
  if (
    ["mechanics", "stores", "products"].includes(parts[0]) &&
    parts.length === 1
  )
    return <CatalogPage key={parts[0]} />;
  if (["mechanics", "stores"].includes(parts[0]) && parts[1])
    return <MerchantPage key={path} slug={parts[1]} />;
  if (parts[0] === "products" && parts[1])
    return <ProductDetail key={path} slug={parts[1]} />;
  if (path === "/cart") return <Cart />;
  if (path === "/orders" || path === "/appointments")
    return <WorkPage key={path} />;
  if (path === "/profile") return <Profile />;
  if (path === "/notifications") return <Notifications />;
  if (parts[0] === "chat" && (!parts[1] || /^\d+$/.test(parts[1])))
    return <Chat key={path} selectedId={parts[1]} />;
  if (path === "/admin/users" && user.role === "Admin") return <AdminUsers />;
  if (path === "/admin/applications" && user.role === "Admin")
    return <BusinessApplications />;
  if (path === "/business" && ["Mechanic", "Store"].includes(user.role))
    return <BusinessPage />;
  if (path === "/inventory" && user.role === "Store") return <Inventory />;
  if (path === "/work/orders" && user.role === "Store")
    return <Orders key={path} incoming />;
  if (path === "/work/appointments" && user.role === "Mechanic")
    return <Appointments key={path} incoming />;
  return (
    <Page title="Səhifə əlçatan deyil">
      <Empty
        title="Bu səhifə tapılmadı və ya rolunuza uyğun deyil."
        action={
          <a href="#/home" className="button primary">
            İş masasına keç
          </a>
        }
      />
    </Page>
  );
}
