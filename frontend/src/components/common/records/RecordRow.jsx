import { number, money, date } from "../../../shared/utils/format.js";
import { labels } from "../../../shared/constants/labels.js";
import { Badge } from "../../ui/Badge.jsx";
import { Button } from "../../ui/Button.jsx";
import { IconButton } from "../../ui/IconButton.jsx";
import { Pencil, Trash2 } from "lucide-react";

export function RecordRow({
  item,
  Icon,
  kind,
  definition,
  act,
  recordApi,
  reload,
  setEdit,
  confirm,
  toast,
}) {
  return (
    <div className="record-row" key={item.id}>
      <span className="record-icon">
        <Icon size={19} />
      </span>
      <div className="record-info">
        <strong>
          {kind === "fuel"
            ? `${number(item.liters)} litr · ${money(item.pricePerLiter)}/L`
            : kind === "service"
              ? item.serviceName
              : kind === "expenses"
                ? item.description
                : item.title}
        </strong>
        <span className="meta">
          {date(item[definition.dateKey], true)}
          {item.kilometer != null && ` · ${number(item.kilometer)} km`}
          {kind === "expenses" && ` · ${labels[item.expenseType]}`}
          {item.reminderKm != null && ` · ${number(item.reminderKm)} km`}
        </span>
        {item.description && kind !== "expenses" && (
          <span className="meta">{item.description}</span>
        )}
        {kind === "service" && item.replacedParts && (
          <span className="meta">Dəyişən hissələr: {item.replacedParts}</span>
        )}
      </div>
      <strong className="row-value">
        {kind === "fuel" ? (
          money(item.totalPrice)
        ) : kind === "service" ? (
          money(item.cost)
        ) : kind === "expenses" ? (
          money(item.amount)
        ) : item.isCompleted ? (
          <Badge value="Completed" />
        ) : (
          <Badge value="Pending" />
        )}
      </strong>
      <div className="row-actions">
        {kind === "reminders" && !item.isCompleted && (
          <Button
            variant="ghost"
            onClick={() =>
              act(async () => {
                await recordApi.complete(item.id);
                reload();
              }, "Xatırlatma tamamlandı.")
            }
          >
            Tamamla
          </Button>
        )}
        <IconButton label="Qeydi dəyiş" onClick={() => setEdit(item)}>
          <Pencil size={16} />
        </IconButton>
        <IconButton
          label="Qeydi sil"
          onClick={() =>
            confirm({
              title: "Qeydi silmək istəyirsiniz?",
              description:
                "Bu əməliyyat geri qaytarıla bilməz. Cari odometr azaldılmayacaq.",
              action: async () => {
                await recordApi.remove(item.id);
                reload();
                toast("Qeyd silindi.");
              },
            })
          }
        >
          <Trash2 size={16} />
        </IconButton>
      </div>
    </div>
  );
}
