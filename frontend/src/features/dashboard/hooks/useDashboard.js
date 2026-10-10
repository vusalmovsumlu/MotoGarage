import { useState } from "react";
import { useApp } from "../../../shared/hooks/useApp.js";
import { useResource } from "../../../shared/hooks/useResource.js";
import { dashboardApi } from "../api/dashboardApi.js";
import { Users, Package, ShieldCheck, Settings } from "lucide-react";
import { destinations } from "../utils/destinations.js";

export function useDashboard() {
  const [today] = useState(() => new Date().toISOString());
  const { user } = useApp();
  const summary = useResource(dashboardApi.summaryPath(user.role));
  const work = useResource(dashboardApi.queuePath(user.role));
  const business = ["Mechanic", "Store"].includes(user.role);
  const dashboard = business ? work : summary;
  const data = dashboard.data || {};
  const admin = user.role === "Admin";
  const firstName = user.fullName.split(" ")[0];
  const shortcuts = admin
    ? [
        {
          title: "İstifadəçilər",
          description: "Hesablar və icazələr",
          path: "/admin/users",
          icon: Users,
        },
        {
          title: "Biznes müraciətləri",
          description: "Usta və mağazaları yoxlayın",
          path: "/admin/applications",
          icon: ShieldCheck,
        },
        {
          title: "Profil",
          description: "Hesabınız",
          path: "/profile",
          icon: Settings,
        },
      ]
    : user.role === "Mechanic"
      ? [
          {
            title: "Gələn görüşlər",
            description: "Növbəti servis işləri",
            path: "/work/appointments",
            art: "schedule.webp",
          },
          {
            title: "Usta profilim",
            description: "Biznes məlumatları",
            path: "/business",
            art: "mechanic.png",
          },
        ]
      : user.role === "Store"
        ? [
            {
              title: "Məhsullarım",
              description: "Kataloq və stok",
              path: "/inventory",
              art: "market.png",
            },
            {
              title: "Gələn sifarişlər",
              description: "Sifarişləri idarə edin",
              path: "/work/orders",
              icon: Package,
            },
            {
              title: "Mağaza profilim",
              description: "Biznes məlumatları",
              path: "/business",
              art: "settings.webp",
            },
          ]
        : destinations;

  return {
    user,
    firstName,
    admin,
    business,
    today,
    dashboard,
    data,
    shortcuts,
    work,
  };
}
