import { useEffect } from "react";
import { Page } from "../../../components/layout/Page.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { useResource } from "../../../shared/hooks/useResource.js";
import { date } from "../../../shared/utils/format.js";
import { adminApi } from "../api/adminApi.js";
import { useUrlQuery } from "../../../shared/hooks/useUrlQuery.js";
import { queryChoice, queryPage } from "../../../shared/utils/routeQuery.js";

const actions = {
  UserBlocked: "Hesab bloklandı",
  UserUnblocked: "Hesabın bloku götürüldü",
  UserDeleted: "Hesab silindi",
  ApplicationApproved: "Müraciət təsdiqləndi",
  ApplicationRejected: "Müraciət rədd edildi",
};
export function AdminAudit() {
  const { params, update } = useUrlQuery();
  const page = queryPage(params);
  const action = queryChoice(params, "action", Object.keys(actions), "");
  const setPage = (value) => update({ page: value }, { defaults: { page: 1 } });
  const state = useResource(adminApi.auditPath(page, action));
  const total = state.data?.total || 0;
  const pageSize = state.data?.pageSize || 25;
  useEffect(() => {
    if (state.data && page > 1 && (page - 1) * pageSize >= total)
      update({ page: null }, { replace: true });
  }, [state.data, page, pageSize, total, update]);
  return (
    <Page
      title="Əməliyyat tarixçəsi"
      description="Admin qərarları, vaxtı və səbəbi. Tarixçə bu funksiya əlavə ediləndən sonrakı əməliyyatları əhatə edir."
    >
      <label className="field audit-filter">
        Əməliyyat
        <select
          aria-label="Əməliyyat"
          value={action}
          onChange={(event) => {
            update({ action: event.target.value, page: null });
          }}
        >
          <option value="">Hamısı</option>
          {Object.entries(actions).map(([key, label]) => (
            <option key={key} value={key}>
              {label}
            </option>
          ))}
        </select>
      </label>
      <Resource state={state}>
        {state.data?.items?.length ? (
          <div className="card table-scroll">
            <table className="business-table audit-table">
              <thead>
                <tr>
                  <th>Vaxt</th>
                  <th>Admin</th>
                  <th>Əməliyyat</th>
                  <th>Hədəf</th>
                  <th>Səbəb</th>
                </tr>
              </thead>
              <tbody>
                {state.data.items.map((item) => (
                  <tr key={item.id}>
                    <td>{date(item.occurredAtUtc, true)}</td>
                    <td>{item.adminName}</td>
                    <td>{actions[item.action] || item.action}</td>
                    <td>
                      {item.targetName}
                      <small className="meta"> #{item.targetId}</small>
                    </td>
                    <td>{item.reason || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <Empty
            title={
              action
                ? "Bu əməliyyat üzrə qeyd tapılmadı"
                : "Əməliyyat tarixçəsi boşdur"
            }
            description={
              action
                ? `“${actions[action]}” filterinə uyğun əməliyyat yoxdur.`
                : "Admin qərarları verildikcə əməliyyatlar burada görünəcək."
            }
            action={
              action && (
                <Button
                  variant="secondary"
                  onClick={() => update({ action: null, page: null })}
                >
                  Filteri təmizlə
                </Button>
              )
            }
          />
        )}
        <div className="planning-toolbar audit-pagination">
          <Button
            variant="secondary"
            disabled={page <= 1}
            onClick={() => setPage(page - 1)}
          >
            Əvvəlki
          </Button>
          <span>
            Səhifə {page} · {total} əməliyyat
          </span>
          <Button
            variant="secondary"
            disabled={page * pageSize >= total}
            onClick={() => setPage(page + 1)}
          >
            Növbəti
          </Button>
        </div>
      </Resource>
    </Page>
  );
}
