import { WorkList } from "../../../components/common/work/WorkList.jsx";
import { ordersApi } from "../api/ordersApi.js";
import { orderTransitions } from "../utils/orderTransitions.js";

export function Orders({ incoming }) {
  return (
    <WorkList
      type="orders"
      incoming={incoming}
      resourceApi={ordersApi}
      transitions={orderTransitions}
    />
  );
}
