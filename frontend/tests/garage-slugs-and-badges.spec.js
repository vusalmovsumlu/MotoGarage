import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

test("Garage uses reloadable slugs and record navigation keeps the selected vehicle", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/vehicles");
  await page.getByRole("link", { name: "Toyota Corolla", exact: true }).click();
  await expect(page).toHaveURL(/#\/garage\/toyota-corolla-test$/);
  await expect(
    page.getByRole("heading", { name: "Toyota Corolla", level: 1 }),
  ).toBeVisible();
  await expect(page.getByRole("link", { name: "Qarajım" })).toHaveAttribute(
    "aria-current",
    "page",
  );
  await page.reload();
  await expect(
    page.getByRole("heading", { name: "Toyota Corolla", level: 1 }),
  ).toBeVisible();
  for (const [kind, name] of [
    ["fuel", "Yanacaq Doldurma və sərfiyyat"],
    ["service", "Servis tarixçəsi Görülən işlər və xərclər"],
    ["expenses", "Xərclər Büdcə və kateqoriyalar"],
    ["reminders", "Xatırlatmalar Növbəti texniki qulluq"],
  ]) {
    await page.goto("/#/garage/cfmoto-450-sr-test");
    await page.getByRole("link", { name, exact: true }).click();
    await expect(page).toHaveURL(
      new RegExp(`#/garage/cfmoto-450-sr-test/${kind}$`),
    );
    await expect(
      page.getByRole("combobox", { name: "Nəqliyyat vasitəsi" }),
    ).toHaveValue("2");
    await page
      .getByRole("combobox", { name: "Nəqliyyat vasitəsi" })
      .selectOption("1");
    await expect(page).toHaveURL(
      new RegExp(`#/garage/toyota-corolla-test/${kind}$`),
    );
    await expect(
      page.getByRole("combobox", { name: "Nəqliyyat vasitəsi" }),
    ).toHaveValue("1");
  }
  expect(
    requests.some(
      (request) => request.path === "/Vehicle/by-slug/cfmoto-450-sr-test",
    ),
  ).toBe(true);
  expect(
    requests.some((request) => request.path === "/reminders/vehicle/2"),
  ).toBe(true);
});

test("Legacy vehicle ID redirects to slug and an unknown slug never loads another vehicle's records", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/vehicles/2");
  await expect(page).toHaveURL(/#\/garage\/cfmoto-450-sr-test$/);
  const before = requests.length;
  await page.goto("/#/garage/missing-vehicle/reminders");
  await expect(page.getByRole("alert")).toContainText(
    "Nəqliyyat vasitəsi tapılmadı.",
  );
  expect(
    requests
      .slice(before)
      .some((request) => request.path.startsWith("/reminders/vehicle/")),
  ).toBe(false);
  await page.goto("/#/garage/missing-vehicle");
  await expect(page.getByRole("alert")).toContainText(
    "Nəqliyyat vasitəsi tapılmadı.",
  );
});

for (const width of [1440, 390, 320]) {
  test(`Header count badges stay clear of icons and fit at ${width}px`, async ({
    page,
  }) => {
    await mockApi(page);
    await page.route("**/api/notifications/unread-count", (route) =>
      route.fulfill({
        contentType: "application/json",
        body: JSON.stringify({ unreadCount: 150 }),
      }),
    );
    await page.setViewportSize({ width, height: 900 });
    await login(page);
    const button = page.getByRole("link", {
      name: "Bildirişlər, 150 oxunmamış",
    });
    await expect(button.locator(".count-dot")).toHaveText("99+");
    for (const theme of ["light", "dark"]) {
      const badge = await button.locator(".count-dot").boundingBox();
      const icon = await button.locator("svg").boundingBox();
      expect(badge.y + badge.height).toBeLessThanOrEqual(icon.y);
      expect(badge.x + badge.width).toBeLessThanOrEqual(width);
      expect(badge.y).toBeGreaterThanOrEqual(0);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
      await page.screenshot({
        path: `test-results/header-badge-${width}-${theme}.png`,
      });
      if (theme === "light")
        await page.getByRole("button", { name: "Temanı dəyiş" }).click();
    }
  });
}
