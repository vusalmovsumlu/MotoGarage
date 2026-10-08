import PeekRating from "../../../components/ui/PeekRating.jsx";
import { date } from "../../../shared/utils/format.js";
import { IconButton } from "../../../components/ui/IconButton.jsx";
import { Pencil, Trash2 } from "lucide-react";

export function ReviewCard({
  value,
  user,
  setReview,
  confirm,
  reviewsApi,
  extras,
  state,
  toast,
}) {
  return (
    <article className="card review-card" key={value.id}>
      <div className="review-top">
        <strong>
          {value.userId === user.id
            ? "Sizin rəyiniz"
            : `İstifadəçi #${value.userId}`}
        </strong>
        <PeekRating
          value={value.rating}
          readOnly
          size={16}
          lift={0}
          idleColor="var(--line)"
        />
      </div>
      <p>{value.comment || "Şərh əlavə edilməyib."}</p>
      <div className="card-footer">
        <span className="meta">{date(value.createdAt)}</span>
        {value.userId === user.id && (
          <div className="actions">
            <IconButton label="Rəyi dəyiş" onClick={() => setReview(value)}>
              <Pencil size={15} />
            </IconButton>
            <IconButton
              label="Rəyi sil"
              onClick={() =>
                confirm({
                  title: "Rəyi silmək istəyirsiniz?",
                  action: async () => {
                    await reviewsApi.remove(value.id);
                    extras.reload();
                    state.reload();
                    toast("Rəy silindi.");
                  },
                })
              }
            >
              <Trash2 size={15} />
            </IconButton>
          </div>
        )}
      </div>
    </article>
  );
}
