import { api } from "../../../shared/api/client.js";

export const reviewsApi = {
  mechanicReviewsPath: (id) => `/reviews/mechanic/${id}`,
  remove: (id) => api(`/reviews/${id}`, { method: "DELETE" }),
  save: (review, mechanicId, values) =>
    api(review.id ? `/reviews/${review.id}` : "/reviews", {
      method: review.id ? "PUT" : "POST",
      body: {
        ...values,
        rating: Number(values.rating),
        ...(!review.id ? { mechanicId } : {}),
      },
    }),
};
