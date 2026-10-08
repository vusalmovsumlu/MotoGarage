import Editor from "../../../components/common/Editor.jsx";

export function ReviewEditor({
  review,
  setReview,
  reviewsApi,
  item,
  extras,
  state,
  toast,
}) {
  return (
    <Editor
      title={review.id ? "Rəyi dəyiş" : "Təcrübənizi bölüşün"}
      initial={review}
      fields={[
        {
          key: "rating",
          label: "Qiymət",
          type: "rating",
          wide: true,
          default: 5,
          required: true,
        },
        {
          key: "comment",
          label: "Rəyiniz",
          type: "textarea",
          wide: true,
          maxLength: 500,
        },
      ]}
      onClose={() => setReview(null)}
      onSave={async (values) => {
        await reviewsApi.save(review, item.id, values);
        setReview(null);
        extras.reload();
        state.reload();
        toast("Rəy yadda saxlanıldı.");
      }}
    />
  );
}
