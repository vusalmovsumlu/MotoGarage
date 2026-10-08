import { useProfile } from "../hooks/useProfile.js";
import { Page } from "../../../components/layout/Page.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { Pencil, ShieldCheck } from "lucide-react";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Badge } from "../../../components/ui/Badge.jsx";
import { roleNames } from "../../../shared/constants/labels.js";
import { date } from "../../../shared/utils/format.js";
import { ProfileEditor } from "../components/ProfileEditor.jsx";

export function Profile() {
  const { profile, setEdit, state, edit, updateUser, toast } = useProfile();
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
        <ProfileEditor
          profile={profile}
          setEdit={setEdit}
          updateUser={updateUser}
          state={state}
          toast={toast}
        />
      )}
    </Page>
  );
}
