export const money = (value) =>
  `${new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(Number(value || 0))} ₼`;
export const number = (value) =>
  new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2 }).format(
    Number(value || 0),
  );
export function date(value, time = false) {
  if (!value) return "—";
  const parsed = new Date(
    /T/.test(value) && !/Z$|[+-]\d\d:\d\d$/.test(value) ? `${value}Z` : value,
  );
  if (Number.isNaN(parsed.getTime())) return "—";
  const months = [
    "yanvar",
    "fevral",
    "mart",
    "aprel",
    "may",
    "iyun",
    "iyul",
    "avqust",
    "sentyabr",
    "oktyabr",
    "noyabr",
    "dekabr",
  ];
  const parts = Object.fromEntries(
    new Intl.DateTimeFormat("en-GB", {
      day: "numeric",
      month: "numeric",
      year: "numeric",
      timeZone: "Asia/Baku",
      ...(time ? { hour: "2-digit", minute: "2-digit", hourCycle: "h23" } : {}),
    })
      .formatToParts(parsed)
      .map((part) => [part.type, part.value]),
  );
  return `${parts.day} ${months[Number(parts.month) - 1]} ${parts.year}${time ? ` · ${parts.hour}:${parts.minute}` : ""}`;
}
export function localInput(value) {
  const parsed = value
    ? new Date(
        /T/.test(value) && !/Z$|[+-]\d\d:\d\d$/.test(value)
          ? `${value}Z`
          : value,
      )
    : new Date();
  return new Date(parsed.getTime() - parsed.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);
}
export const roleNames = {
  User: "İstifadəçi",
  Mechanic: "Usta",
  Store: "Mağaza",
  Admin: "Administrator",
};
export const labels = {
  Pending: "Gözləmədə",
  Accepted: "Qəbul edildi",
  Rejected: "Rədd edildi",
  Confirmed: "Təsdiqləndi",
  Preparing: "Hazırlanır",
  Shipped: "Göndərildi",
  Completed: "Tamamlandı",
  Cancelled: "Ləğv edildi",
  Petrol: "Benzin",
  Diesel: "Dizel",
  Hybrid: "Hibrid",
  Electric: "Elektrik",
  Gas: "Qaz",
  Car: "Avtomobil",
  Motorcycle: "Motosiklet",
  Fuel: "Yanacaq",
  Service: "Servis",
  SparePart: "Ehtiyat hissəsi",
  Insurance: "Sığorta",
  Other: "Digər",
  Sent: "Göndərildi",
  Delivered: "Çatdırıldı",
  Read: "Oxundu",
};
export const orderTransitions = {
  Pending: ["Confirmed", "Cancelled"],
  Confirmed: ["Preparing", "Cancelled"],
  Preparing: ["Shipped", "Cancelled"],
  Shipped: ["Completed"],
};
export const appointmentTransitions = {
  Pending: ["Accepted", "Rejected"],
  Accepted: ["Completed"],
};
