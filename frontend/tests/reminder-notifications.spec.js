import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

const reminderNotification = (link) => ({
  id: 81,
  title: "Xatırlatma: Yağ dəyişimi",
  message: "CFMOTO 450 SR üçün odometr həddi çatıb.",
  link,
  createdAt: new Date().toISOString(),
  type: "Reminder",
  isRead: false,
});

test("Due reminder notifications appear automatically and open the correct vehicle", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  let due = false;
  await page.route("**/api/notifications/unread-count", (route) =>
    route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({ unreadCount: due ? 1 : 0 }),
    }),
  );
  await page.route(
    (url) => url.pathname === "/api/notifications",
    (route) =>
      route.fulfill({
        contentType: "application/json",
        body: JSON.stringify(
          due
            ? [reminderNotification("/garage/cfmoto-450-sr-test/reminders")]
            : [],
        ),
      }),
  );
  await page.clock.install();
  await login(page);
  await page.goto("/#/notifications");
  await expect(
    page.getByRole("heading", { name: "Hər şey qaydasındadır" }),
  ).toBeVisible();
  due = true;
  await page.clock.fastForward(30000);
  await expect(
    page.getByText("Xatırlatma: Yağ dəyişimi", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Bildirişlər, 1 oxunmamış" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Bax", exact: true }).click();
  await expect(page).toHaveURL(/#\/garage\/cfmoto-450-sr-test\/reminders$/);
  await expect(
    page.getByRole("combobox", { name: "Nəqliyyat vasitəsi" }),
  ).toHaveValue("2");
  expect(
    requests.some(
      (request) =>
        request.path === "/notifications/81/read" && request.method === "PATCH",
    ),
  ).toBe(true);
});

test("An existing reminder notification with a vehicle ID opens the canonical slug", async ({
  page,
}) => {
  await mockApi(page);
  await page.route(
    (url) => url.pathname === "/api/notifications",
    (route) =>
      route.fulfill({
        contentType: "application/json",
        body: JSON.stringify([reminderNotification("/reminders/50?vehicle=2")]),
      }),
  );
  await login(page);
  await page.goto("/#/notifications");
  await page.getByRole("button", { name: "Bax", exact: true }).click();
  await expect(page).toHaveURL(/#\/garage\/cfmoto-450-sr-test\/reminders$/);
  await expect(
    page.getByRole("combobox", { name: "Nəqliyyat vasitəsi" }),
  ).toHaveValue("2");
});

for (const link of ["/reminders/50", "/reminders/50?vehicle=invalid"]) {
  test(`Reminder notification retains safe legacy navigation: ${link}`, async ({
    page,
  }) => {
    await mockApi(page);
    await page.route(
      (url) => url.pathname === "/api/notifications",
      (route) =>
        route.fulfill({
          contentType: "application/json",
          body: JSON.stringify([reminderNotification(link)]),
        }),
    );
    await login(page);
    await page.goto("/#/notifications");
    await page.getByRole("button", { name: "Bax", exact: true }).click();
    await expect(page).toHaveURL(/#\/records\/reminders$/);
    await expect(
      page.getByRole("heading", { level: 1, name: "Xatırlatmalar" }),
    ).toBeVisible();
  });
}
