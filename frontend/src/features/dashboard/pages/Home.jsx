import { useDashboard } from "../hooks/useDashboard.js";
import { roleNames } from "../../../shared/constants/labels.js";
import { date } from "../../../shared/utils/format.js";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { DashboardStats } from "../components/DashboardStats.jsx";
import { DashboardSummary } from "../components/DashboardSummary.jsx";
import { DashboardDestinations } from "../components/DashboardDestinations.jsx";
import { BusinessDashboard } from "../components/BusinessDashboard.jsx";

export default function Home() {
  const {
    user,
    firstName,
    admin,
    business,
    today,
    dashboard,
    data,
    shortcuts,
    work,
  } = useDashboard();
  return (
    <div className="home-page">
      <header className="home-heading">
        <p className="eyebrow">{roleNames[user.role]} İŞ MASASI</p>
        <h1>Yenidən xoş gördük, {firstName}.</h1>
        <p className="muted">
          {admin
            ? "Platformanın vəziyyəti və idarəetmə alətləri."
            : business
              ? user.role === "Mechanic"
                ? "Servis görüşləriniz və biznesiniz — bir yerdə."
                : "Məhsullarınız, stokunuz və müştəri sifarişləriniz — bir yerdə."
              : "Qarajınız, planlarınız və növbəti yolunuz — bir yerdə."}
        </p>
        <span className="home-date">{date(today)}</span>
      </header>
      <Resource state={dashboard} skeleton="dashboard">
        {business ? (
          <BusinessDashboard user={user} shortcuts={shortcuts} work={work} />
        ) : (
          <>
            <DashboardStats admin={admin} data={data} />
            <div className="workspace-columns">
              <DashboardSummary admin={admin} data={data} />
              <DashboardDestinations
                admin={admin}
                shortcuts={shortcuts}
                user={user}
                work={work}
              />
            </div>
          </>
        )}
      </Resource>
    </div>
  );
}
