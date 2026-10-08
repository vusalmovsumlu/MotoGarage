import { Thumbnail } from "../Thumbnail.jsx";
import { Wrench, Store, MapPin } from "lucide-react";
import { money } from "../../../shared/utils/format.js";
import { Button } from "../../ui/Button.jsx";

export function BusinessProfileCard({
  state,
  mechanic,
  endpoint,
  confirm,
  resourceApi,
  toast,
}) {
  return (
    <div className="card merchant-detail">
      <Thumbnail src={state.data.imageUrl} icon={mechanic ? Wrench : Store} />
      <div>
        <h2>{state.data.businessName || state.data.storeName}</h2>
        <p className="muted">{state.data.description}</p>
        <p className="address">
          <MapPin size={16} />
          {state.data.address}
        </p>
        <p>
          <a href={`tel:${state.data.phoneNumber}`}>{state.data.phoneNumber}</a>
        </p>
        {state.data.hourlyRate != null && (
          <p>{money(state.data.hourlyRate)} / saat</p>
        )}
        <div className="actions">
          <a
            className="button secondary"
            href={`#${endpoint}/${state.data.slug}`}
          >
            Müştəri görünüşü
          </a>
          <Button
            variant="ghost"
            className="danger-text"
            onClick={() =>
              confirm({
                title: "Biznes profilini deaktiv etmək istəyirsiniz?",
                description:
                  "Profiliniz kataloqdan gizlənəcək. Əvvəlki sifariş və görüş tarixçəsi qalacaq.",
                action: async () => {
                  await resourceApi.deactivateProfile();
                  state.reload();
                  toast("Biznes profili deaktiv edildi.");
                },
              })
            }
          >
            Deaktiv et
          </Button>
        </div>
        {!state.data.isActive && (
          <p className="notice">
            Profil hazırda deaktivdir və müştəri kataloqunda görünmür.
          </p>
        )}
      </div>
    </div>
  );
}
