import Editor from "../../../components/common/Editor.jsx";
import { profileApi } from "../api/profileApi.js";

export function ProfileEditor({ profile, setEdit, updateUser, state, toast }) {
  return (
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
        await profileApi.update(body);
        updateUser(body);
        setEdit(false);
        state.reload();
        toast("Profil yadda saxlanıldı.");
      }}
    />
  );
}
