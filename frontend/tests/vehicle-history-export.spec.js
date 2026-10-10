import { test, expect } from "@playwright/test";
import { readFile } from "node:fs/promises";
import { mockApi, login } from "./fixtures.js";

const exportUrl = "**/api/Vehicle/by-slug/toyota-corolla-test/history.xlsx";
const emailUrl = "**/api/Vehicle/by-slug/toyota-corolla-test/history/email";
const workbookBytes = Buffer.from("PK\u0003\u0004browser-download-fixture");
const contentType =
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

async function openExport(page) {
  await login(page);
  await page.goto("/#/garage/toyota-corolla-test");
  await page.getByRole("button", { name: "Tarixçəni paylaş" }).click();
  return page.getByRole("dialog", { name: "Tarixçəni paylaş" });
}

test("History download preserves binary bytes, slug filename and authenticated request", async ({
  page,
}) => {
  const { auth } = await mockApi(page);
  let authorization;
  await page.route(exportUrl, (route) => {
    authorization = route.request().headers().authorization;
    return route.fulfill({ contentType, body: workbookBytes });
  });
  const dialog = await openExport(page);
  const downloadEvent = page.waitForEvent("download");
  await dialog.getByRole("button", { name: "Excel-i yüklə" }).click();
  const download = await downloadEvent;
  expect(download.suggestedFilename()).toBe("toyota-corolla-test-tarixce.xlsx");
  expect(await readFile(await download.path())).toEqual(workbookBytes);
  expect(authorization).toBe(`Bearer ${auth.accessToken}`);
  await expect(dialog).toBeVisible();
  await expect(
    dialog.getByText(/Xərc qeydlərində kilometr saxlanmır/),
  ).toBeVisible();
});

test("Buyer email requires consent tied to that address and sends exactly once", async ({
  page,
}) => {
  await mockApi(page);
  const submissions = [];
  let finish;
  const pending = new Promise((resolve) => {
    finish = resolve;
  });
  await page.route(emailUrl, async (route) => {
    submissions.push(route.request().postDataJSON());
    await pending;
    await route.fulfill({
      contentType: "application/json",
      body: '{"message":"Sent"}',
    });
  });
  const dialog = await openExport(page);
  const recipient = dialog.getByLabel("Alıcının emaili");
  const consent = dialog.getByRole("checkbox");
  const send = dialog.getByRole("button", { name: "Email ilə göndər" });
  await recipient.fill("first@example.invalid");
  await send.click();
  expect(submissions).toHaveLength(0);
  await consent.check();
  await recipient.fill("buyer@example.invalid");
  await expect(consent).not.toBeChecked();
  await consent.check();
  await send.click();
  await expect.poll(() => submissions.length).toBe(1);
  await expect(recipient).toBeDisabled();
  await expect(send).toBeDisabled();
  await expect(
    dialog.getByRole("button", { name: "Excel-i yüklə" }),
  ).toBeDisabled();
  await page.keyboard.press("Escape");
  await expect(dialog).toBeVisible();
  finish();
  await expect(dialog.getByRole("status")).toContainText(
    "buyer@example.invalid",
  );
  await expect(send).toBeDisabled();
  expect(submissions).toEqual([
    { recipientEmail: "buyer@example.invalid", confirmSharing: true },
  ]);
});

test("Failed download shows the API error instead of saving JSON as Excel", async ({
  page,
}) => {
  await mockApi(page);
  await page.route(exportUrl, (route) =>
    route.fulfill({
      status: 400,
      contentType: "application/json",
      body: JSON.stringify({ message: "Hesabat üçün qeyd limiti aşılıb." }),
    }),
  );
  let downloads = 0;
  page.on("download", () => {
    downloads++;
  });
  const dialog = await openExport(page);
  await dialog.getByRole("button", { name: "Excel-i yüklə" }).click();
  await expect(dialog.getByRole("alert")).toContainText("qeyd limiti aşılıb");
  expect(downloads).toBe(0);
  await expect(
    dialog.getByRole("button", { name: "Excel-i yüklə" }),
  ).toBeEnabled();
});

test("SMTP failure preserves recipient and consent and permits an explicit retry", async ({
  page,
}) => {
  await mockApi(page);
  let calls = 0;
  await page.route(emailUrl, (route) => {
    calls++;
    return route.fulfill({
      status: calls === 1 ? 500 : 200,
      contentType: "application/json",
      body: JSON.stringify({
        message: calls === 1 ? "Email göndərilə bilmədi." : "Sent",
      }),
    });
  });
  const dialog = await openExport(page);
  await dialog.getByLabel("Alıcının emaili").fill("buyer@example.invalid");
  await dialog.getByRole("checkbox").check();
  const send = dialog.getByRole("button", { name: "Email ilə göndər" });
  await send.click();
  await expect(dialog.getByRole("alert")).toContainText(
    "Email göndərilə bilmədi",
  );
  await expect(dialog.getByRole("status")).toHaveCount(0);
  await expect(dialog.getByLabel("Alıcının emaili")).toHaveValue(
    "buyer@example.invalid",
  );
  await expect(dialog.getByRole("checkbox")).toBeChecked();
  await send.click();
  await expect(dialog.getByRole("status")).toContainText(
    "buyer@example.invalid",
  );
  expect(calls).toBe(2);
});

test("Binary export retries once after JWT refresh through the shared API client", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  let calls = 0;
  await page.route(exportUrl, (route) => {
    calls++;
    return route.fulfill(
      calls === 1
        ? { status: 401, body: "" }
        : { contentType, body: workbookBytes },
    );
  });
  const dialog = await openExport(page);
  const downloadEvent = page.waitForEvent("download");
  await dialog.getByRole("button", { name: "Excel-i yüklə" }).click();
  const download = await downloadEvent;
  expect(await readFile(await download.path())).toEqual(workbookBytes);
  expect(calls).toBe(2);
  expect(
    requests.filter((item) => item.path === "/auth/refresh-token"),
  ).toHaveLength(1);
});

test("Unknown vehicle cannot open a sharing dialog", async ({ page }) => {
  await mockApi(page);
  await login(page);
  await page.goto("/#/garage/private-vehicle");
  await expect(page.getByRole("alert")).toContainText(
    "Nəqliyyat vasitəsi tapılmadı",
  );
  await expect(
    page.getByRole("button", { name: "Tarixçəni paylaş" }),
  ).toHaveCount(0);
});

for (const width of [320, 1440]) {
  test(`History sharing dialog fits at ${width}px in both themes`, async ({
    page,
  }) => {
    await mockApi(page);
    await page.setViewportSize({ width, height: 900 });
    const dialog = await openExport(page);
    for (const theme of ["light", "dark"]) {
      await expect(dialog.getByLabel("Alıcının emaili")).toBeVisible();
      expect(
        await dialog.evaluate(
          (element) => element.scrollWidth <= element.clientWidth,
        ),
      ).toBe(true);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
      await page.screenshot({
        path: `test-results/history-sharing-${width}-${theme}.png`,
      });
      await dialog
        .getByRole("button", { name: "Bağla", exact: true })
        .last()
        .click();
      if (theme === "light") {
        await page.getByRole("button", { name: "Temanı dəyiş" }).click();
        await page.getByRole("button", { name: "Tarixçəni paylaş" }).click();
      }
    }
  });
}
