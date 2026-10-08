import { useState } from "react";
import { useApp } from "../../../shared/hooks/useApp.js";
import { useResource } from "../../../shared/hooks/useResource.js";
import { dashboardApi } from "../api/dashboardApi.js";
import { Users, Package } from "lucide-react";
import { destinations } from "../utils/destinations.js";

export function useDashboard() {
  const [today] = useState(() => new Date().toISOString());
  const { user } = useApp();
  const dashboard = useResource(dashboardApi.summaryPath(user.role));
  const work = useResource(dashboardApi.queuePath(user.role));
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
          title: "Ustalar",
          description: "Servis kataloqu",
          path: "/mechanics",
          art: "mechanic.png",
        },
        {
          title: "Məhsullar",
          description: "Məhsul kataloqu",
          path: "/products",
          art: "market.png",
        },
        {
          title: "Profil",
          description: "Hesabınız",
          path: "/profile",
          art: "settings.webp",
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
          ...destinations.filter((x) =>
            ["/vehicles", "/products", "/profile"].includes(x.path),
          ),
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
            ...destinations.filter((x) => x.path === "/vehicles"),
          ]
        : destinations;

  return { user, firstName, admin, today, dashboard, data, shortcuts, work };
}
