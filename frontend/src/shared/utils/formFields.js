export const text = (key, label, maxLength, required = true) => ({
  key,
  label,
  maxLength,
  required,
});
export const decimal = (key, label, min = 0, max = 999999999.99) => ({
  key,
  label,
  type: "number",
  min,
  max,
  step: "0.01",
  required: true,
});
export const km = {
  key: "kilometer",
  label: "Odometr (km)",
  type: "number",
  min: 0,
  max: 10000000,
  required: true,
};
export const description = {
  ...text("description", "Açıqlama", 500, false),
  type: "textarea",
  wide: true,
};
