import { text, km, description } from "../../../shared/utils/formFields.js";

export const recordDefinition = {
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
};
