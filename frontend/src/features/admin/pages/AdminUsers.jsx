import { useAdminUsers } from "../hooks/useAdminUsers.js";
import { Page } from "../../../components/layout/Page.jsx";
import { AdminUsersFilters } from "../components/AdminUsersFilters.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { AdminUsersTable } from "../components/AdminUsersTable.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { Users } from "lucide-react";

export function AdminUsers() {
  const {
    setFilters,
    search,
    role,
    setSearch,
    setRole,
    state,
    user,
    confirm,
    toast,
  } = useAdminUsers();
  return (
    <Page
      title="İstifadəçilər"
      eyebrow="ADMİN İDARƏETMƏSİ"
      description="Hesabların vəziyyətinə nəzarət edin."
      actions={
        <a href="#/admin/applications" className="button primary">
          Usta və mağaza müraciətləri
        </a>
      }
    >
      <AdminUsersFilters
        setFilters={setFilters}
        search={search}
        role={role}
        setSearch={setSearch}
        setRole={setRole}
      />
      <Resource state={state} skeleton="table">
        {state.data?.length ? (
          <AdminUsersTable
            state={state}
            user={user}
            confirm={confirm}
            toast={toast}
          />
        ) : (
          <Empty
            icon={Users}
            title="İstifadəçi tapılmadı"
            description="Filter və ya axtarışı dəyişin."
          />
        )}
      </Resource>
    </Page>
  );
}
