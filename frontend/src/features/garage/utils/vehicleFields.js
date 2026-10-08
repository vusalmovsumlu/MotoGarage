import { text, km } from "../../../shared/utils/formFields.js";

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
