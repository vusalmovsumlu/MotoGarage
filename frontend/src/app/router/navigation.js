import {
  Home as HomeIcon,
  CarFront,
  Wrench,
  ShoppingBag,
  Users,
  ShieldCheck,
  CalendarDays,
  Package,
  BriefcaseBusiness,
} from "lucide-react";

export const adminLinks = [
  { path: "/home", title: "İş masası", icon: HomeIcon },
  { path: "/admin/users", title: "İstifadəçilər", icon: Users },
  {
    path: "/admin/applications",
    title: "Biznes müraciətləri",
    icon: ShieldCheck,
  },
];

export const links = [
  { path: "/home", title: "İş masası", icon: HomeIcon },
  { path: "/vehicles", title: "Qarajım", icon: CarFront },
  { path: "/mechanics", title: "Ustalar", icon: Wrench },
  { path: "/products", title: "Məhsullar", icon: ShoppingBag },
];

export const businessLinks = {
  Mechanic: [
    links[0],
    { path: "/work/appointments", title: "Gələn görüşlər", icon: CalendarDays },
    { path: "/business", title: "Biznes profilim", icon: BriefcaseBusiness },
  ],
  Store: [
    links[0],
    { path: "/inventory", title: "Məhsullarım", icon: Package },
    { path: "/work/orders", title: "Gələn sifarişlər", icon: ShoppingBag },
    { path: "/business", title: "Biznes profilim", icon: BriefcaseBusiness },
  ],
};

export function isBusinessRoute(path, role) {
  return (
    businessLinks[role]?.some((item) => item.path === path) ||
    ["/profile", "/notifications", "/chat", "/business/preview"].includes(
      path,
    ) ||
    /^\/chat\/\d+$/.test(path)
  );
}
