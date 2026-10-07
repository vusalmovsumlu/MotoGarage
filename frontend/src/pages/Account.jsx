import { useState } from "react";
import { Bell, CheckCheck, Pencil, ShieldCheck, Users } from "lucide-react";
import { api, query } from "../lib/api";
import { navigate, useResource } from "../lib/hooks";
import { date, roleNames } from "../lib/format";
import {
  Badge,
  Button,
  Empty,
  Page,
  Resource,
  SearchBar,
} from "../components/ui";
import Editor from "../components/Editor";
import { useApp } from "../context";

export function Profile() {
  const state = useResource("/profile");
  const [edit, setEdit] = useState(false);
  const { toast, updateUser } = useApp();
  const profile = state.data;
  return (
    <Page
      title="Profiliniz"
      description="Hesabınızın əsas məlumatları burada."
      actions={
        profile && (
          <Button variant="secondary" onClick={() => setEdit(true)}>
            <Pencil size={16} /> Profili dəyiş
          </Button>
        )
      }
    >
      <Resource state={state} skeleton="profile">
        {profile && (
          <section className="card profile-card">
            <div className="profile-avatar">
              {profile.fullName
                .split(" ")
                .slice(0, 2)
                .map((name) => name[0])
                .join("")}
            </div>
            <h2>{profile.fullName}</h2>
            <p className="muted">{profile.email}</p>
            <Badge value="Accepted">{roleNames[profile.role]}</Badge>
            <dl className="profile-details">
              <div>
                <dt>Telefon</dt>
                <dd>{profile.phoneNumber || "Əlavə edilməyib"}</dd>
              </div>
              <div>
                <dt>Email</dt>
                <dd>
                  {profile.emailConfirmed ? (
                    <span className="verified">
                      <ShieldCheck size={15} /> Təsdiqlənib
                    </span>
                  ) : (
                    "Təsdiqlənməyib"
                  )}
                </dd>
              </div>
              <div>
                <dt>Qeydiyyat tarixi</dt>
                <dd>{date(profile.createdAt)}</dd>
              </div>
            </dl>
          </section>
        )}
      </Resource>
      {edit && (
        <Editor
          title="Profil məlumatları"
          initial={profile}
          fields={[
            {
              key: "fullName",
              label: "Ad və soyad",
              maxLength: 100,
              required: true,
              autoComplete: "name",
              wide: true,
            },
            {
              key: "phoneNumber",
              label: "Telefon nömrəsi",
              maxLength: 20,
              autoComplete: "tel",
              wide: true,
            },
          ]}
          onClose={() => setEdit(false)}
          onSave={async (body) => {
            await api("/profile", { method: "PUT", body });
            updateUser(body);
            setEdit(false);
            state.reload();
            toast("Profil yadda saxlanıldı.");
          }}
        />
      )}
    </Page>
  );
}
export function Notifications() {
  const [unread, setUnread] = useState(false);
  const state = useResource(`/notifications${query({ unreadOnly: unread })}`);
  const { act, notificationReload, user } = useApp();
  function destination(item) {
    const link = item.link || "";
    if (link.startsWith("/chat/")) return link;
    if (link.startsWith("/appointments"))
      return user.role === "Mechanic" ? "/work/appointments" : "/appointments";
    if (link.startsWith("/orders"))
      return user.role === "Store" ? "/work/orders" : "/orders";
    if (link.startsWith("/reminders")) return "/records/reminders";
    return null;
  }
  return (
    <Page
      title="Bildirişlər"
      description="Görüş, sifariş və söhbətlərdən xəbərdar olun."
      actions={
        <Button
          variant="secondary"
          disabled={!state.data?.some((item) => !item.isRead)}
          onClick={() =>
            act(async () => {
              await api("/notifications/read-all", { method: "PATCH" });
              state.reload();
              notificationReload();
            }, "Bildirişlər oxundu.")
          }
        >
          <CheckCheck size={16} /> Hamısını oxunmuş et
        </Button>
      }
    >
      <div className="tabs">
        <button
          className={!unread ? "active" : ""}
          aria-pressed={!unread}
          onClick={() => setUnread(false)}
        >
          Hamısı
        </button>
        <button
          className={unread ? "active" : ""}
          aria-pressed={unread}
          onClick={() => setUnread(true)}
        >
          Oxunmamış
        </button>
      </div>
      <Resource state={state}>
        {state.data?.length ? (
          <div className="card rows-card">
            {state.data.map((item) => (
              <div
                className={`notification-row ${!item.isRead ? "unread" : ""}`}
                key={item.id}
              >
                <span className="record-icon">
                  <Bell size={19} />
                </span>
                <div className="record-info">
                  <strong>{item.title}</strong>
                  <p>{item.message}</p>
                  <span className="meta">{date(item.createdAt, true)}</span>
                </div>
                <div className="notification-actions">
                  {destination(item) && (
                    <Button
                      variant="ghost"
                      onClick={() =>
                        act(async () => {
                          if (!item.isRead)
                            await api(`/notifications/${item.id}/read`, {
                              method: "PATCH",
                            });
                          notificationReload();
                          navigate(destination(item));
                        })
                      }
                    >
                      Bax
                    </Button>
                  )}
                  {!item.isRead && (
                    <Button
                      variant="secondary"
                      onClick={() =>
                        act(async () => {
                          await api(`/notifications/${item.id}/read`, {
                            method: "PATCH",
                          });
                          state.reload();
                          notificationReload();
                        })
                      }
                    >
                      Oxundu
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <Empty
            icon={Bell}
            title="Hər şey qaydasındadır"
            description="Yeni bildirişlər burada görünəcək."
          />
        )}
      </Resource>
    </Page>
  );
}
export function AdminUsers() {
  const [search, setSearch] = useState("");
  const [role, setRole] = useState("");
  const [filters, setFilters] = useState({});
  const state = useResource(`/admin/users${query(filters)}`);
  const { user, confirm, toast } = useApp();
  return (
    <Page
      title="İstifadəçilər"
      eyebrow="ADMİN İDARƏETMƏSİ"
      description="Hesabların vəziyyətinə nəzarət edin."
    >
      <form
        onSubmit={(event) => {
          event.preventDefault();
          setFilters({ search, role });
        }}
      >
        <SearchBar
          value={search}
          onChange={setSearch}
          placeholder="Ad və ya email axtarın…"
        >
          <select
            aria-label="Rol üzrə filter"
            value={role}
            onChange={(event) => setRole(event.target.value)}
          >
            <option value="">Bütün rollar</option>
            {Object.entries(roleNames).map(([value, title]) => (
              <option value={value} key={value}>
                {title}
              </option>
            ))}
          </select>
          <Button type="submit">Axtar</Button>
        </SearchBar>
      </form>
      <Resource state={state} skeleton="table">
        {state.data?.length ? (
          <div className="card users-card">
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>İstifadəçi</th>
                    <th>Rol</th>
                    <th>Email</th>
                    <th>Status</th>
                    <th>
                      <span className="visually-hidden">Əməliyyat</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {state.data.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <strong>{item.fullName}</strong>
                        <span className="meta">
                          #{item.id} · {date(item.createdAt)}
                        </span>
                      </td>
                      <td>{roleNames[item.role]}</td>
                      <td>
                        <span>{item.email}</span>
                        <small
                          className={item.emailConfirmed ? "verified" : "meta"}
                        >
                          {item.emailConfirmed
                            ? "Təsdiqlənib"
                            : "Təsdiqlənməyib"}
                        </small>
                      </td>
                      <td>
                        <Badge value={item.isBlocked ? "Rejected" : "Accepted"}>
                          {item.isBlocked ? "Bloklanıb" : "Aktiv"}
                        </Badge>
                      </td>
                      <td>
                        <Button
                          variant="secondary"
                          disabled={
                            item.id === user.id || item.role === "Admin"
                          }
                          onClick={() =>
                            confirm({
                              title: item.isBlocked
                                ? "Hesabın blokunu açmaq istəyirsiniz?"
                                : "İstifadəçini bloklamaq istəyirsiniz?",
                              description: item.isBlocked
                                ? "İstifadəçi yenidən giriş edə biləcək. Əvvəlki sessiyaları bərpa olunmayacaq."
                                : "Aktiv sessiyalar və chat bağlantıları dayandırılacaq.",
                              label: item.isBlocked ? "Bloku aç" : "Blokla",
                              action: async () => {
                                await api(
                                  `/admin/users/${item.id}/${item.isBlocked ? "unblock" : "block"}`,
                                  { method: "PATCH" },
                                );
                                state.reload();
                                toast("Hesabın vəziyyəti yeniləndi.");
                              },
                            })
                          }
                        >
                          {item.isBlocked ? "Bloku aç" : "Blokla"}
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
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
