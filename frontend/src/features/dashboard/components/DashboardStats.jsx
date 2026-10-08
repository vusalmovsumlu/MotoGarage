import { Stat } from "../../../components/common/Stat.jsx";
import { number, money } from "../../../shared/utils/format.js";
import {
  Users,
  Wrench,
  Package,
  ShoppingBag,
  CarFront,
  Wallet,
  Bell,
  CalendarDays,
} from "lucide-react";

export function DashboardStats({ admin, data }) {
  return (
    <div className="stats-grid">
      {admin ? (
        <>
          <Stat
            label="İstifadəçilər"
            value={number(data.totalUsers)}
            icon={Users}
          />
          <Stat
            label="Usta / mağaza"
            value={`${data.totalMechanics} / ${data.totalStores}`}
            icon={Wrench}
          />
          <Stat
            label="Məhsullar"
            value={number(data.totalProducts)}
            icon={Package}
          />
          <Stat
            label="Sifarişlər"
            value={number(data.totalOrders)}
            icon={ShoppingBag}
          />
        </>
      ) : (
        <>
          <Stat
            label="Qarajınızda"
            value={number(data.totalVehicles)}
            note="nəqliyyat vasitəsi"
            icon={CarFront}
          />
          <Stat
            label="Bu ayın xərcləri"
            value={money(data.thisMonthExpenses)}
            icon={Wallet}
          />
          <Stat
            label="Aktiv xatırlatmalar"
            value={number(data.activeRemindersCount)}
            icon={Bell}
          />
          <Stat
            label="Gözləyən görüşlər"
            value={number(data.pendingAppointmentsCount)}
            icon={CalendarDays}
          />
        </>
      )}
    </div>
  );
}
