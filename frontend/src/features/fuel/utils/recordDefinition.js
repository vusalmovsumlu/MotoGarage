import { decimal, km } from "../../../shared/utils/formFields.js";

export const recordDefinition = {
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
};
