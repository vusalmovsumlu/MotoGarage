import { CircleCheck, CircleAlert } from "lucide-react";
import SwipeToast from "./SwipeToast.jsx";

export function ToastRegion({ notices, setNotices }) {
  return (
    <div className="toast-region">
      {notices.map((item) => (
        <SwipeToast
          key={item.id}
          inline
          title={item.message}
          role={item.error ? "alert" : "status"}
          icon={item.error ? <CircleAlert /> : <CircleCheck />}
          background="var(--surface)"
          color="var(--text)"
          fuseColor={item.error ? "var(--red)" : "var(--primary)"}
          radius={16}
          duration={6500}
          closeButton
          className={item.error ? "swipe-toast--error" : "swipe-toast--success"}
          onClose={() =>
            setNotices((current) =>
              current.filter((value) => value.id !== item.id),
            )
          }
        />
      ))}
    </div>
  );
}
