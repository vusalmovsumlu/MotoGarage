import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

test("Appointments display vehicle and mechanic names, including on the dashboard", async ({
  page,
}) => {
  await mockApi(page);
  await login(page);
  await expect(
    page.locator(".preview-row").filter({ hasText: "Əli Məmmədov" }),
  ).toBeVisible();
  await page.goto("/#/appointments");
  const details = page.locator(".work-card p.meta");
  await expect(details).toContainText("Toyota Corolla");
  await expect(details).toContainText("Əli Məmmədov");
  await expect(details).not.toContainText(/Nəqliyyat #|Usta #/);
  await page.getByRole("button", { name: "Aktiv", exact: true }).click();
  await expect(details).toContainText("Toyota Corolla");
  await expect(
    page.getByRole("button", { name: "Ləğv et", exact: true }),
  ).toBeVisible();
});

test("Mechanic's incoming appointments show the vehicle and customer name", async ({
  page,
}) => {
  await mockApi(page, "Mechanic");
  await login(page);
  await page.goto("/#/work/appointments");
  const details = page.locator(".work-card p.meta");
  await expect(details).toContainText("Toyota Corolla");
  await expect(details).toContainText("Vüqar Məmmədov");
  await expect(details).not.toContainText(/Nəqliyyat #|Müştəri #/);
  await expect(
    page.getByRole("button", { name: "Qəbul edildi", exact: true }),
  ).toBeVisible();
});
