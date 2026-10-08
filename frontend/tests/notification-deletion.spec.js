import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

test("Deleting the last notification confirms first, updates unread count and shows the empty state", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/notifications");
  const remove = page.getByRole("button", {
    name: "Bildirişi sil: Görüşünüz yaradıldı",
    exact: true,
  });
  await remove.click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "İmtina", exact: true })
    .click();
  expect(requests.some((request) => request.method === "DELETE")).toBe(false);
  await remove.click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Sil", exact: true })
    .click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.locator(".notification-row")).toHaveCount(0);
  await expect(
    page.getByRole("heading", { name: "Hər şey qaydasındadır" }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Bildirişlər, 0 oxunmamış", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Hamısını sil", exact: true }),
  ).toBeDisabled();
  expect(
    requests.filter(
      (request) =>
        request.path === "/notifications/80" && request.method === "DELETE",
    ),
  ).toHaveLength(1);
});

for (const fail of [false, true]) {
  test(`Bulk notification deletion handles ${fail ? "failure without removing rows" : "read and unread items"}`, async ({
    page,
  }) => {
    await mockApi(page);
    const items = [
      {
        id: 80,
        title: "Oxunmamış bildiriş",
        message: "Test",
        createdAt: new Date().toISOString(),
        isRead: false,
      },
      {
        id: 81,
        title: "Oxunmuş bildiriş",
        message: "Test",
        createdAt: new Date().toISOString(),
        isRead: true,
      },
    ];
    let deleted = 0;
    await page.route(
      (url) =>
        url.pathname === "/api/notifications" ||
        url.pathname === "/api/notifications/unread-count",
      async (route) => {
        if (route.request().method() === "DELETE") {
          deleted++;
          if (fail)
            return route.fulfill({
              status: 500,
              contentType: "application/json",
              body: JSON.stringify({ message: "Silmə alınmadı." }),
            });
          items.splice(0);
          return route.fulfill({ status: 204 });
        }
        const url = new URL(route.request().url());
        const data = url.pathname.endsWith("unread-count")
          ? { unreadCount: items.filter((item) => !item.isRead).length }
          : url.searchParams.get("unreadOnly") === "true"
            ? items.filter((item) => !item.isRead)
            : items;
        return route.fulfill({
          contentType: "application/json",
          body: JSON.stringify(data),
        });
      },
    );
    await page.setViewportSize({ width: 320, height: 900 });
    await login(page);
    await page.goto("/#/notifications");
    await expect(page.locator(".notification-row")).toHaveCount(2);
    await page
      .getByRole("button", { name: "Hamısını sil", exact: true })
      .click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toContainText(
      "Oxunmuş və oxunmamış bütün bildirişləriniz silinəcək.",
    );
    await dialog
      .getByRole("button", { name: "Hamısını sil", exact: true })
      .click();
    if (fail) {
      await expect(dialog.getByRole("alert")).toHaveText("Silmə alınmadı.");
      await expect(page.locator(".notification-row")).toHaveCount(2);
      await dialog.getByRole("button", { name: "İmtina", exact: true }).click();
    } else {
      await expect(dialog).toHaveCount(0);
      await expect(page.locator(".notification-row")).toHaveCount(0);
      await expect(
        page.getByRole("link", {
          name: "Bildirişlər, 0 oxunmamış",
          exact: true,
        }),
      ).toBeVisible();
    }
    expect(deleted).toBe(1);
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
  });
}
