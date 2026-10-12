import {
  text,
  decimal,
  km,
  description,
} from "../../../shared/utils/formFields.js";

export const recordDefinition = {
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
    {
      key: "replacedParts",
      label: "Dəyişdirilən hissələr",
      type: "textarea",
      maxLength: 1000,
      wide: true,
    },
  ],
};
