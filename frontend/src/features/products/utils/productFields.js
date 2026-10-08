import {
  text,
  decimal,
  description,
} from "../../../shared/utils/formFields.js";

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
