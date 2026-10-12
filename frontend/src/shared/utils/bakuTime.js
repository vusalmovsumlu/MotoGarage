const utcDate = (value) =>
  new Date(
    typeof value === "string" &&
      /T/.test(value) &&
      !/Z$|[+-]\d\d:\d\d$/.test(value)
      ? `${value}Z`
      : value,
  );
export const bakuTime = (value) =>
  new Intl.DateTimeFormat("az-AZ", {
    timeZone: "Asia/Baku",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(utcDate(value));
export function bakuDay(value = new Date()) {
  return new Intl.DateTimeFormat("sv-SE", {
    timeZone: "Asia/Baku",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(utcDate(value));
}
export function shiftDay(day, amount) {
  const date = new Date(`${day}T12:00:00Z`);
  date.setUTCDate(date.getUTCDate() + amount);
  return date.toISOString().slice(0, 10);
}
