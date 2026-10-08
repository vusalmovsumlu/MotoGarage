import { labels } from "../../shared/constants/labels.js";

export function Badge({ value, children }) {
  return (
    <span className={`badge status-${String(value).toLowerCase()}`}>
      {children || labels[value] || value}
    </span>
  );
}
