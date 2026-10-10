import { useResource } from "./useResource.js";
import { useState } from "react";
import { useApp } from "./useApp.js";

export function useMerchantDetail({
  type,
  slug,
  resourceApi,
  extrasPath,
  preview,
}) {
  const mechanic = type === "mechanics";
  const state = useResource(
    preview ? resourceApi.ownPath() : resourceApi.detailPath(slug),
  );
  const item = state.data;
  const extras = useResource(item ? extrasPath(item, slug) : null);
  const [appointment, setAppointment] = useState(false);
  const [review, setReview] = useState(null);
  const { user, act, toast, confirm } = useApp();

  return {
    item,
    state,
    mechanic,
    user,
    setAppointment,
    act,
    extras,
    setReview,
    confirm,
    toast,
    appointment,
    review,
  };
}
