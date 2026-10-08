import { decimal, description } from "../../../shared/utils/formFields.js";

export const recordDefinition = {
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
};
