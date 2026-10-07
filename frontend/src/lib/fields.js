const text = (key, label, maxLength, required = true) => ({
  key,
  label,
  maxLength,
  required,
});
const decimal = (key, label, min = 0, max = 999999999.99) => ({
  key,
  label,
  type: "number",
  min,
  max,
  step: "0.01",
  required: true,
});
const km = {
  key: "kilometer",
  label: "Odometr (km)",
  type: "number",
  min: 0,
  max: 10000000,
  required: true,
};
const description = {
  ...text("description", "Açıqlama", 500, false),
  type: "textarea",
  wide: true,
};
export const vehicleFields = [
  text("brand", "Marka", 50),
  text("model", "Model", 50),
  {
    key: "year",
    label: "İstehsal ili",
    type: "number",
    min: 1885,
    max: new Date().getFullYear() + 1,
    default: new Date().getFullYear(),
    required: true,
  },
  text("engine", "Mühərrik", 50),
  {
    key: "vehicleType",
    label: "Nəqliyyat növü",
    options: ["Motorcycle", "Car"],
    default: "Motorcycle",
    required: true,
  },
  {
    key: "fuelType",
    label: "Yanacaq növü",
    options: ["Petrol", "Diesel", "Hybrid", "Electric", "Gas"],
    default: "Petrol",
    required: true,
  },
  text("vin", "VIN", 50),
  text("plateNumber", "Qeydiyyat nişanı", 20),
  {
    ...km,
    key: "currentKm",
    label: "Cari odometr (km)",
    default: 0,
    wide: true,
  },
];
export const recordDefinitions = {
  fuel: {
    title: "Yanacaq qeydləri",
    singular: "Yanacaq qeydi",
    endpoint: "/fuel-records",
    dateKey: "fuelDate",
    fields: [
      decimal("liters", "Yanacaq (litr)", 0.01, 1000),
      decimal("pricePerLiter", "Litrin qiyməti (₼)", 0.01, 10000),
      km,
      {
        key: "fuelDate",
        label: "Doldurma tarixi",
        type: "datetime-local",
        required: true,
      },
    ],
  },
  expenses: {
    title: "Xərclər",
    singular: "Xərc",
    endpoint: "/expenses",
    dateKey: "expenseDate",
    fields: [
      {
        key: "expenseType",
        label: "Kateqoriya",
        options: ["Fuel", "Service", "SparePart", "Insurance", "Other"],
        default: "Service",
        required: true,
      },
      decimal("amount", "Məbləğ (₼)", 0.01),
      { ...description, required: true },
      {
        key: "expenseDate",
        label: "Xərc tarixi",
        type: "datetime-local",
        required: true,
      },
    ],
  },
  service: {
    title: "Servis tarixçəsi",
    singular: "Servis qeydi",
    endpoint: "/service-records",
    dateKey: "serviceDate",
    fields: [
      text("serviceName", "Servisin adı", 100),
      decimal("cost", "Xərc (₼)"),
      km,
      {
        key: "serviceDate",
        label: "Servis tarixi",
        type: "datetime-local",
        required: true,
      },
      description,
    ],
  },
  reminders: {
    title: "Xatırlatmalar",
    singular: "Xatırlatma",
    endpoint: "/reminders",
    dateKey: "reminderDate",
    fields: [
      { ...text("title", "Başlıq", 150), wide: true },
      {
        key: "reminderDate",
        label: "Tarix",
        type: "datetime-local",
        required: true,
      },
      {
        ...km,
        key: "reminderKm",
        label: "Odometr həddi (km)",
        required: false,
      },
      description,
    ],
  },
};
export const businessFields = (role) => [
  text(role === "Mechanic" ? "businessName" : "storeName", "Biznesin adı", 150),
  text("phoneNumber", "Telefon", 20),
  { ...text("address", "Ünvan", 250), wide: true },
  { ...description, required: true },
  ...(role === "Mechanic"
    ? [{ ...decimal("hourlyRate", "Saatlıq qiymət (₼)"), required: false }]
    : []),
];
export const productFields = [
  text("name", "Məhsulun adı", 150),
  decimal("price", "Qiymət (₼)"),
  text("brand", "Marka", 100, false),
  text("vehicleCompatibility", "Uyğun modellər", 200, false),
  description,
];
export const stockField = {
  key: "stock",
  label: "Stok sayı",
  type: "number",
  min: 0,
  max: 10000000,
  required: true,
  default: 0,
};
