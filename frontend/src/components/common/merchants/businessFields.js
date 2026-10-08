import {
  text,
  description,
  decimal,
} from "../../../shared/utils/formFields.js";

export const businessFields = (role) => [
  text(role === "Mechanic" ? "businessName" : "storeName", "Biznesin adı", 150),
  text("phoneNumber", "Telefon", 20),
  { ...text("address", "Ünvan", 250), wide: true },
  { ...description, required: true },
  ...(role === "Mechanic"
    ? [{ ...decimal("hourlyRate", "Saatlıq qiymət (₼)"), required: false }]
    : []),
];
