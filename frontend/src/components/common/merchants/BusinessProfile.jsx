import { useBusinessProfile } from "../../../shared/hooks/useBusinessProfile.js";
import { Page } from "../../layout/Page.jsx";
import { Button } from "../../ui/Button.jsx";
import { Pencil, Wrench, Store, Plus } from "lucide-react";
import { Resource } from "../../feedback/Resource.jsx";
import { BusinessProfileCard } from "./BusinessProfileCard.jsx";
import { Empty } from "../../feedback/Empty.jsx";
import { BusinessProfileEditor } from "./BusinessProfileEditor.jsx";

export function BusinessProfile({ resourceApi }) {
  const { mechanic, state, setEdit, endpoint, confirm, toast, edit, user } =
    useBusinessProfile({ resourceApi });
  return (
    <Page
      title={mechanic ? "Usta profilim" : "Mağaza profilim"}
      description="Müştərilərinizin sizi tanıdığı yer."
      actions={
        state.data && (
          <Button variant="secondary" onClick={() => setEdit(state.data)}>
            <Pencil size={16} /> Profili dəyiş
          </Button>
        )
      }
    >
      <Resource state={state} allow404 skeleton="detail">
        {state.data ? (
          <BusinessProfileCard
            state={state}
            mechanic={mechanic}
            endpoint={endpoint}
            confirm={confirm}
            resourceApi={resourceApi}
            toast={toast}
          />
        ) : (
          <Empty
            icon={mechanic ? Wrench : Store}
            title="Biznesinizi tanıdaq"
            description="Ad, ünvan və əlaqə məlumatlarını əlavə edin."
            action={
              <Button onClick={() => setEdit({})}>
                <Plus size={16} /> Profil yarat
              </Button>
            }
          />
        )}
      </Resource>
      {edit && (
        <BusinessProfileEditor
          state={state}
          user={user}
          edit={edit}
          setEdit={setEdit}
          resourceApi={resourceApi}
          toast={toast}
        />
      )}
    </Page>
  );
}
