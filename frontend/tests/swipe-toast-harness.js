// Browser-only harness; excluded from the runtime application and production build.
import { createElement as h, StrictMode, useState } from "react";
import { createRoot } from "react-dom/client";
import SwipeToast from "../src/components/feedback/SwipeToast.jsx";
import { ToastRegion } from "../src/components/feedback/ToastRegion.jsx";
import { useFeedback } from "../src/shared/hooks/useFeedback.js";

function FeedbackHarness() {
  const feedback = useFeedback(() => {});
  return h(
    "div",
    null,
    h(
      "button",
      { onClick: () => feedback.toast(`Mesaj ${crypto.randomUUID()}`) },
      "Mesaj yarat",
    ),
    h(ToastRegion, feedback),
  );
}

function ComponentHarness(props) {
  const [reason, setReason] = useState(null);
  return h(
    "div",
    null,
    reason === null &&
      h(SwipeToast, {
        title: "Sınaq bildirişi",
        closeButton: true,
        ...props,
        onClose: setReason,
      }),
    h("output", { "data-testid": "close-reason" }, reason),
  );
}

export function mountToastHarness({ feedback = false, ...props } = {}) {
  const target = document.createElement("div");
  target.id = "toast-test-harness";
  document.body.append(target);
  const root = createRoot(target);
  root.render(
    h(
      StrictMode,
      null,
      h(feedback ? FeedbackHarness : ComponentHarness, props),
    ),
  );
  return root;
}
