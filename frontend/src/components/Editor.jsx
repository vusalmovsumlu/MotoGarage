import { useState } from "react";
import { EntityForm, Modal } from "./ui";
export default function Editor({
  title,
  fields,
  initial,
  image,
  onSave,
  onClose,
  description,
}) {
  const [busy, setBusy] = useState(false);
  return (
    <Modal
      title={title}
      description={description}
      busy={busy}
      onClose={onClose}
    >
      <EntityForm
        fields={fields}
        initial={initial}
        image={image}
        onCancel={onClose}
        onBusyChange={setBusy}
        onSave={async (values) => {
          setBusy(true);
          try {
            await onSave(values);
          } finally {
            setBusy(false);
          }
        }}
      />
    </Modal>
  );
}
