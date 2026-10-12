import { useState } from "react";
import { Page } from "../../../components/layout/Page.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { useResource } from "../../../shared/hooks/useResource.js";
import { date } from "../../../shared/utils/format.js";
import { adminApi } from "../api/adminApi.js";

const actions = {
  UserBlocked: "Hesab bloklandı",
  UserUnblocked: "Hesabın bloku götürüldü",
  UserDeleted: "Hesab silindi",
  ApplicationApproved: "Müraciət təsdiqləndi",
  ApplicationRejected: "Müraciət rədd edildi",
};
export function AdminAudit() {
  const [page, setPage] = useState(1);
  const [action, setAction] = useState("");
  const state = useResource(adminApi.auditPath(page, action));
  return (
    <Page
      title="Əməliyyat tarixçəsi"
      description="Admin qərarları, vaxtı və səbəbi. Tarixçə bu funksiya əlavə ediləndən sonrakı əməliyyatları əhatə edir."
    >
      <label className="field audit-filter">
        Əməliyyat
        <select
          value={action}
          onChange={(event) => {
            setAction(event.target.value);
            setPage(1);
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
          <Empty title="Əməliyyat tarixçəsi boşdur" />
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
            Səhifə {page} · {state.data?.total || 0} əməliyyat
          </span>
          <Button
            variant="secondary"
            disabled={page * 25 >= (state.data?.total || 0)}
            onClick={() => setPage(page + 1)}
          >
            Növbəti
          </Button>
        </div>
      </Resource>
    </Page>
  );
}
