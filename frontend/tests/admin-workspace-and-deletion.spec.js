import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

test("Admin workspace contains management tools, no personal links or personal API reads", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Admin");
  await login(page);
  const nav = page.getByRole("navigation", { name: "Əsas menyu", exact: true });
  await expect(nav.getByRole("link")).toHaveCount(4);
  for (const name of ["İş masası", "İstifadəçilər", "Biznes müraciətləri"])
    await expect(nav.getByRole("link", { name, exact: true })).toBeVisible();
  await expect(page.locator(".nav-more")).toHaveCount(0);
  await expect(
    page.getByRole("link", { name: /Səbət|Söhbətlər|Bildirişlər/ }),
  ).toHaveCount(0);
  await expect(
    page
      .locator("main")
      .getByRole("link", { name: /Qarajım|Sifarişlərim|Ustalar|Məhsullar/ }),
  ).toHaveCount(0);
  await expect(page.locator("main")).toContainText("Biznes müraciətləri");
  await expect(page.locator("main")).toContainText("Bloklanmış hesablar");
  for (const path of [
    "/vehicles",
    "/garage/toyota-corolla-test",
    "/records/fuel",
    "/products",
    "/cart",
    "/orders",
    "/appointments",
    "/chat",
    "/notifications",
  ]) {
    await page.goto(`/#${path}`);
    await expect(
      page.getByRole("heading", {
        name: "Bu bölmə admin hesabı üçün nəzərdə tutulmayıb",
      }),
    ).toBeVisible();
  }
  expect(
    requests.filter((request) =>
      [
        "/Vehicle",
        "/dashboard",
        "/orders",
        "/appointments",
        "/products",
        "/notifications/unread-count",
        "/chat/conversations",
      ].includes(request.path),
    ),
  ).toEqual([]);
});

test("Account deletion shows the target, requires confirmation and reloads the users list", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Admin");
  await login(page);
  await page.goto("/#/admin/users");
  const target = page.getByRole("row").filter({ hasText: "Aysel Əliyeva" });
  await target.getByRole("button", { name: "Sil", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toContainText("Aysel Əliyeva");
  await expect(dialog).toContainText("user@example.invalid");
  expect(requests.some((request) => request.method === "DELETE")).toBe(false);
  await dialog.getByRole("button", { name: "İmtina", exact: true }).click();
  await expect(target).toBeVisible();
  expect(requests.some((request) => request.method === "DELETE")).toBe(false);
  await target.getByRole("button", { name: "Sil", exact: true }).click();
  await dialog.getByRole("button", { name: "Hesabı sil", exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await expect(target).toHaveCount(0);
  expect(
    requests.filter(
      (request) =>
        request.method === "DELETE" && request.path === "/admin/users/2",
    ),
  ).toHaveLength(1);
  const self = page.getByRole("row").filter({ hasText: "Vüqar Məmmədov" });
  await expect(
    self.getByRole("button", { name: "Sil", exact: true }),
  ).toBeDisabled();
});

test("Failed account deletion preserves the row and dialog and prevents duplicate clicks while pending", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  await login(page);
  await page.goto("/#/admin/users");
  let release;
  let calls = 0;
  const gate = new Promise((resolve) => {
    release = resolve;
  });
  await page.route("**/api/admin/users/2", async (route) => {
    if (route.request().method() !== "DELETE") return route.fallback();
    calls++;
    await gate;
    await route.fulfill({
      status: 409,
      contentType: "application/json",
      body: JSON.stringify({
        message: "Hesabın aktiv sifariş və ya görüşləri var.",
      }),
    });
  });
  const target = page.getByRole("row").filter({ hasText: "Aysel Əliyeva" });
  await target.getByRole("button", { name: "Sil", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("button", { name: "Hesabı sil", exact: true }).click();
  await expect(
    dialog.getByRole("button", { name: "İmtina", exact: true }),
  ).toBeDisabled();
  expect(calls).toBe(1);
  release();
  await expect(dialog.getByRole("alert")).toContainText("aktiv sifariş");
  await expect(target).toBeVisible();
  await expect(
    dialog.getByRole("button", { name: "Hesabı sil", exact: true }),
  ).toBeEnabled();
});

for (const width of [320, 1440]) {
  test(`Admin navigation and delete confirmation fit ${width}px in both themes`, async ({
    page,
  }, testInfo) => {
    await page.setViewportSize({ width, height: 900 });
    await mockApi(page, "Admin");
    await login(page);
    for (const theme of ["light", "dark"]) {
      await page.goto("/#/admin/users");
      await page
        .getByRole("row")
        .filter({ hasText: "Aysel" })
        .getByRole("button", { name: "Sil", exact: true })
        .click();
      const dialog = page.getByRole("dialog");
      await expect(dialog).toBeVisible();
      const layout = await page.evaluate(() => ({
        width: document.documentElement.scrollWidth,
        viewport: innerWidth,
        overflowing: [...document.querySelectorAll("body *")]
          .filter(
            (element) => element.getBoundingClientRect().right > innerWidth,
          )
          .map((element) => `${element.tagName}.${element.className}`),
      }));
      expect(layout.width, JSON.stringify(layout)).toBe(width);
      const box = await dialog.boundingBox();
      expect(box.x).toBeGreaterThanOrEqual(0);
      expect(box.x + box.width).toBeLessThanOrEqual(width);
      await page.screenshot({
        path: testInfo.outputPath(`admin-delete-${theme}.png`),
      });
      await dialog.getByRole("button", { name: "İmtina", exact: true }).click();
      if (theme === "light")
        await page.getByRole("button", { name: "Temanı dəyiş" }).click();
    }
  });
}
