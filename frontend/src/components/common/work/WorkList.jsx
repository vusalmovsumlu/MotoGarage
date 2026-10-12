import { useWorkList } from "../../../shared/hooks/useWorkList.js";
import { Page } from "../../layout/Page.jsx";
import { Button } from "../../ui/Button.jsx";
import { Plus, ClipboardList, CalendarDays } from "lucide-react";
import { FilterTabs } from "../../ui/FilterTabs.jsx";
import { Resource } from "../../feedback/Resource.jsx";
import { WorkCard } from "./WorkCard.jsx";
import { Empty } from "../../feedback/Empty.jsx";

const statusTabs = [
  ["all", "Hamısı"],
  ["active", "Aktiv"],
  ["finished", "Bitmiş"],
];
export function WorkList({
  type,
  incoming,
  resourceApi,
  transitions,
  AppointmentEditor,
  renderExtra,
}) {
  const {
    title,
    orders,
    setCreate,
    filter,
    setFilter,
    state,
    items,
    pending,
    confirm,
    toast,
    transition,
    create,
  } = useWorkList({ type, incoming, resourceApi });
  return (
    <Page
      title={title}
      description={
        orders
          ? "Sifarişin ilk addımından tamamlanmasına qədər."
          : "Planınızı görün, görüşləri rahat idarə edin."
      }
      actions={
        !orders &&
        !incoming && (
          <Button onClick={() => setCreate(true)}>
            <Plus size={16} /> Görüş yarat
          </Button>
        )
      }
    >
      <FilterTabs
        items={statusTabs}
        value={filter}
        onChange={setFilter}
        label="Status üzrə filter"
      />
      <Resource state={state}>
        {items.length ? (
          <div className="work-list">
            {items.map((item) => (
              <WorkCard
                key={item.id}
                item={item}
                orders={orders}
                incoming={incoming}
                transitions={transitions}
                pending={pending}
                confirm={confirm}
                resourceApi={resourceApi}
                state={state}
                toast={toast}
                transition={transition}
                extra={renderExtra?.({ item, incoming, onDone: state.reload })}
              />
            ))}
          </div>
        ) : (
          <Empty
            icon={orders ? ClipboardList : CalendarDays}
            title="Bu siyahıda heç nə yoxdur"
            description={
              incoming
                ? "Yeni müraciətlər burada görünəcək."
                : "Yeni sifariş və görüşləriniz burada görünəcək."
            }
          />
        )}
      </Resource>
      {create && (
        <AppointmentEditor
          onClose={() => setCreate(false)}
          onDone={() => {
            state.reload();
            toast("Görüş yaradıldı.");
          }}
        />
      )}
    </Page>
  );
}
