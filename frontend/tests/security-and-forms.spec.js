import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures";

test("Parallel refresh calls rotate a token once", async ({ page }) => {
  const { requests } = await mockApi(page);
  await login(page);
  const tokens = await page.evaluate(async () => {
    const client = await import("/src/lib/api.js");
    return Promise.all(
      Array.from({ length: 4 }, () => client.refreshSession()),
    );
  });
  expect(new Set(tokens).size).toBe(1);
  expect(
    requests.filter((item) => item.path === "/auth/refresh-token"),
  ).toHaveLength(1);
});

test("A late refresh response cannot restore a logged out session", async ({
  page,
}) => {
  const { auth } = await mockApi(page);
  await login(page);
  let release;
  const gate = new Promise((resolve) => {
    release = resolve;
  });
  await page.route("**/api/auth/refresh-token", async (route) => {
    await gate;
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(auth),
    });
  });
  const pending = page.waitForRequest("**/api/auth/refresh-token");
  await page.evaluate(async () => {
    const client = await import("/src/lib/api.js");
    window.refreshFinished = client
      .refreshSession()
      .catch((error) => error.status);
    client.setSession(null);
  });
  await pending;
  release();
  const result = await page.evaluate(async () => {
    await window.refreshFinished;
    return (await import("/src/lib/api.js")).getSession();
  });
  expect(result).toBeNull();
});

test("Vehicle dialog supports keyboard closing and locks while uploading", async ({
  page,
}) => {
  await mockApi(page);
  await login(page);
  await page.goto("/#/vehicles");
  const add = page.getByRole("button", { name: "Nəqliyyat əlavə et" });
  await add.click();
  await expect
    .poll(() =>
      page.evaluate(() =>
        document.querySelector("dialog").contains(document.activeElement),
      ),
    )
    .toBe(true);
  await page.keyboard.press("Tab");
  await expect
    .poll(() =>
      page.evaluate(() =>
        document.querySelector("dialog").contains(document.activeElement),
      ),
    )
    .toBe(true);
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(add).toBeFocused();
  await add.click();
  let release;
  const gate = new Promise((resolve) => {
    release = resolve;
  });
  await page.route("**/api/images/upload", async (route) => {
    await gate;
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        url: "/icons/garage.png",
        publicId: "test-upload",
      }),
    });
  });
  await page.getByLabel("Şəkil seç").setInputFiles({
    name: "test.png",
    mimeType: "image/png",
    buffer: Buffer.from(
      "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=",
      "base64",
    ),
  });
  await expect(
    page.getByRole("button", { name: "Yadda saxla" }),
  ).toBeDisabled();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toBeVisible();
  release();
  await expect(page.getByRole("button", { name: "Yadda saxla" })).toBeEnabled();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
});

test("Mobile authentication fits both themes", async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 320, height: 850 });
  for (const route of ["login", "register", "verify"]) {
    await page.goto(`/#/${route}`);
    await expect(page.locator(".auth-card")).toBeVisible();
    for (let theme = 0; theme < 2; theme++) {
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
      await page.getByRole("button", { name: "Temanı dəyiş" }).click();
    }
  }
});
