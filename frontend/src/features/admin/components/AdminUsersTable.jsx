import { date } from "../../../shared/utils/format.js";
import { roleNames } from "../../../shared/constants/labels.js";
import { Badge } from "../../../components/ui/Badge.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { adminApi } from "../api/adminApi.js";

export function AdminUsersTable({ state, user, confirm, toast }) {
  return (
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
                  <small className={item.emailConfirmed ? "verified" : "meta"}>
                    {item.emailConfirmed ? "Təsdiqlənib" : "Təsdiqlənməyib"}
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
                    disabled={item.id === user.id || item.role === "Admin"}
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
                          await adminApi.toggleBlock(item);
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
  );
}
