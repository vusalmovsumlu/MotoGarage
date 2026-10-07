import { useState } from "react";
import {
  ArrowRight,
  Bell,
  CalendarDays,
  CarFront,
  ClipboardList,
  MessageCircle,
  Package,
  ShoppingBag,
  Users,
  Wallet,
  Wrench,
} from "lucide-react";
import { Resource, Section, Stat } from "../components/ui";
import { useResource } from "../lib/hooks";
import { date, money, number, roleNames } from "../lib/format";
import { useApp } from "../context";

const destinations = [
  {
    title: "Qarajım",
    description: "Nəqliyyatınız və tarixçəsi",
    path: "/vehicles",
    art: "garage.png",
  },
  {
    title: "Ustalar",
    description: "Doğru ustanı tapın",
    path: "/mechanics",
    art: "mechanic.png",
  },
  {
    title: "Mağaza",
    description: "Yol üçün lazım olanlar",
    path: "/products",
    art: "market.png",
  },
  {
    title: "Görüşlərim",
    description: "Servis planınızı izləyin",
    path: "/appointments",
    art: "schedule.webp",
  },
  {
    title: "Xatırlatmalar",
    description: "Heç nəyi unutmayın",
    path: "/records/reminders",
    art: "homework.webp",
  },
  {
    title: "Profil",
    description: "Hesabınızı idarə edin",
    path: "/profile",
    art: "settings.webp",
  },
];
function Preview({ items, render, empty }) {
  return items?.length ? (
    <div className="preview-list">{items.map(render)}</div>
  ) : (
    <p className="quiet-empty">{empty}</p>
  );
}
export default function Home() {
  const [today] = useState(() => new Date().toISOString());
  const { user } = useApp();
  const dashboard = useResource(
    user.role === "Admin" ? "/admin/dashboard" : "/dashboard",
  );
  const work = useResource(
    user.role === "Mechanic"
      ? "/appointments/mechanic"
      : user.role === "Store"
        ? "/orders/store"
        : null,
  );
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
  return (
    <div className="home-page">
      <header className="home-heading">
        <p className="eyebrow">{roleNames[user.role]} İŞ MASASI</p>
        <h1>Yenidən xoş gördük, {firstName}.</h1>
        <p className="muted">
          {admin
            ? "Platformanın vəziyyəti və idarəetmə alətləri."
            : "Qarajınız, planlarınız və növbəti yolunuz — bir yerdə."}
        </p>
        <span className="home-date">{date(today)}</span>
      </header>
      <Resource state={dashboard} skeleton="dashboard">
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
        <div className="workspace-columns">
          <aside className="summary-column">
            {admin ? (
              <Section title="Diqqət gözləyənlər">
                <div className="summary-row">
                  <span>Gözləyən sifarişlər</span>
                  <strong>{data.pendingOrders}</strong>
                </div>
                <div className="summary-row">
                  <span>Gözləyən görüşlər</span>
                  <strong>{data.pendingAppointments}</strong>
                </div>
                <a className="text-link" href="#/admin/users">
                  İstifadəçiləri idarə et <ArrowRight size={14} />
                </a>
              </Section>
            ) : (
              <>
                <Section title="Növbəti görüşlər" link="/appointments">
                  <Preview
                    items={data.upcomingAppointments}
                    empty="Hazırda planlaşdırılmış görüşünüz yoxdur."
                    render={(item) => (
                      <a
                        href="#/appointments"
                        key={item.id}
                        className="preview-row"
                      >
                        <span className="preview-time">
                          {date(item.appointmentDate, true)}
                        </span>
                        <strong>
                          {item.description || `Servis görüşü #${item.id}`}
                        </strong>
                        <span className="meta">Usta #{item.mechanicId}</span>
                      </a>
                    )}
                  />
                </Section>
                <Section title="Yadda saxlayın" link="/records/reminders">
                  <Preview
                    items={data.upcomingReminders}
                    empty="Aktiv xatırlatma yoxdur. Yolunuz rahat olsun."
                    render={(item) => (
                      <a
                        href={`#/records/reminders?vehicle=${item.vehicleId}`}
                        key={item.id}
                        className="preview-row"
                      >
                        <strong>{item.title}</strong>
                        <span className="meta">
                          {date(item.reminderDate)}
                          {item.reminderKm != null &&
                            ` · ${number(item.reminderKm)} km`}
                        </span>
                      </a>
                    )}
                  />
                </Section>
                <Section title="Son yanacaq qeydləri" link="/records/fuel">
                  <Preview
                    items={data.recentFuelRecords}
                    empty="İlk doldurmanı qeyd edib sərfiyyatı izləyin."
                    render={(item) => (
                      <a
                        href={`#/records/fuel?vehicle=${item.vehicleId}`}
                        key={item.id}
                        className="preview-row"
                      >
                        <strong>
                          {number(item.liters)} litr{" "}
                          <span className="row-amount">
                            {money(item.totalPrice)}
                          </span>
                        </strong>
                        <span className="meta">
                          {date(item.fuelDate)} · {number(item.kilometer)} km
                        </span>
                      </a>
                    )}
                  />
                </Section>
              </>
            )}
          </aside>
          <div className="destinations-column">
            <div className="section-heading">
              <h2>{admin ? "İdarəetmə" : "Hər şey əlçatan olsun."}</h2>
              <span className="meta">Sizin alətləriniz</span>
            </div>
            <div className="app-shortcuts">
              {shortcuts.map((item) => (
                <a
                  href={`#${item.path}`}
                  className="app-shortcut"
                  key={item.path}
                >
                  {item.art ? (
                    <img src={`/icons/${item.art}`} alt="" />
                  ) : (
                    <span className="shortcut-native">
                      <item.icon size={35} strokeWidth={1.5} />
                    </span>
                  )}
                  <strong>{item.title}</strong>
                  <span className="meta">{item.description}</span>
                </a>
              ))}
            </div>
            <div className="quick-links">
              <a href="#/chat">
                <MessageCircle size={20} />
                <div>
                  <strong>Söhbətlər</strong>
                  <span className="meta">
                    Ustanız və mağazanızla əlaqədə qalın.
                  </span>
                </div>
                <ArrowRight size={17} />
              </a>
              <a href="#/orders">
                <ClipboardList size={20} />
                <div>
                  <strong>Sifarişlərim</strong>
                  <span className="meta">
                    Alışlarınızın gedişatını izləyin.
                  </span>
                </div>
                <ArrowRight size={17} />
              </a>
            </div>
            {["Mechanic", "Store"].includes(user.role) && (
              <Section
                title={
                  user.role === "Mechanic"
                    ? "Servis növbəniz"
                    : "Sifariş növbəniz"
                }
                link={
                  user.role === "Mechanic"
                    ? "/work/appointments"
                    : "/work/orders"
                }
              >
                <Resource state={work} skeleton="compact">
                  {work.data?.filter((x) =>
                    [
                      "Pending",
                      "Accepted",
                      "Confirmed",
                      "Preparing",
                      "Shipped",
                    ].includes(x.status),
                  ).length ? (
                    <p>
                      {
                        work.data.filter((x) =>
                          [
                            "Pending",
                            "Accepted",
                            "Confirmed",
                            "Preparing",
                            "Shipped",
                          ].includes(x.status),
                        ).length
                      }{" "}
                      aktiv iş diqqətinizi gözləyir.
                    </p>
                  ) : (
                    <p className="quiet-empty">Hazırda aktiv iş yoxdur.</p>
                  )}
                </Resource>
              </Section>
            )}
            <div className="workspace-note">
              <span className="live-dot" />
              <p>Bu iş masası hesabınızdakı real məlumatlarla yenilənir.</p>
            </div>
          </div>
        </div>
      </Resource>
    </div>
  );
}
