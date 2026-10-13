import { useResource } from "./useResource.js";
import { useState } from "react";
import { useApp } from "./useApp.js";
import { useUrlQuery } from "./useUrlQuery.js";
import { queryChoice } from "../utils/routeQuery.js";

export function useWorkList({ type, incoming, resourceApi }) {
  const orders = type === "orders";
  const state = useResource(resourceApi.listPath(incoming));
  const { params, update } = useUrlQuery();
  const filter = queryChoice(
    params,
    "status",
    ["all", "active", "finished"],
    "all",
  );
  const setFilter = (value) =>
    update({ status: value }, { defaults: { status: "all" } });
  const [create, setCreate] = useState(false);
  const [pending, setPending] = useState(null);
  const { act, confirm, toast } = useApp();
  const activeStatuses = orders
    ? ["Pending", "Confirmed", "Preparing", "Shipped"]
    : ["Pending", "Accepted"];
  const items = (state.data || []).filter(
    (item) =>
      filter === "all" ||
      (filter === "active"
        ? activeStatuses.includes(item.status)
        : !activeStatuses.includes(item.status)),
  );
  const title = orders
    ? incoming
      ? "Gələn sifarişlər"
      : "Sifarişlərim"
    : incoming
      ? "Gələn görüşlər"
      : "Görüşlərim";
  async function transition(item, status) {
    setPending(item.id);
    await act(async () => {
      await resourceApi.updateStatus(item.id, status);
      state.reload();
    }, "Status yeniləndi.");
    setPending(null);
  }

  return {
    title,
    orders,
    setCreate,
    filter,
    setFilter,
    state,
    items,
    pending,
    confirm,
    toast,
    transition,
    create,
  };
}
