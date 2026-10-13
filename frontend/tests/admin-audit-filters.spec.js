import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

const actions = {
  UserBlocked: "Hesab bloklandı",
  UserUnblocked: "Hesabın bloku götürüldü",
  UserDeleted: "Hesab silindi",
  ApplicationApproved: "Müraciət təsdiqləndi",
  ApplicationRejected: "Müraciət rədd edildi",
};
const records = Array.from({ length: 135 }, (_, index) => ({
  id: index + 1,
  adminName: "Admin Vüsal",
  action: Object.keys(actions)[index % 5],
  targetId: index + 1,
  targetName: `Hədəf ${index + 1}`,
  reason: `Qərar ${index + 1}`,
  occurredAtUtc: new Date(Date.UTC(2026, 9, 13, 8, index)).toISOString(),
})).reverse();

function resultFor(params, source = records) {
  const action = params.get("action");
  const filtered = source.filter((item) => !action || item.action === action);
  const page = Number(params.get("page") || 1);
  const pageSize = Number(params.get("pageSize") || 25);
  return {
    total: filtered.length,
    page,
    pageSize,
    items: filtered.slice((page - 1) * pageSize, page * pageSize),
  };
}
const respond = (route, result) =>
  route.fulfill({
    contentType: "application/json",
    body: JSON.stringify(result),
  });

test("All five audit filters change displayed mixed records and count, and reset pagination", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  await page.route("**/api/admin/audit-logs**", (route) =>
    respond(route, resultFor(new URL(route.request().url()).searchParams)),
  );
  await login(page);
  await page.goto("/#/admin/audit?keep=1");
  const rows = page.locator(".audit-table tbody tr");
  const filter = page.getByLabel("Əməliyyat", { exact: true });
  await expect(rows).toHaveCount(25);
  await expect(page.locator(".audit-pagination")).toContainText(
    "135 əməliyyat",
  );
  await expect(rows.first()).toContainText("Hədəf 135");
  for (const [action, label] of Object.entries(actions)) {
    await filter.selectOption(action);
    await expect(page.locator(".audit-pagination")).toContainText(
      "Səhifə 1 · 27 əməliyyat",
    );
    await expect(rows.locator("td:nth-child(3)")).toHaveText(
      Array(25).fill(label),
    );
    await page.getByRole("button", { name: "Növbəti", exact: true }).click();
    await expect(rows).toHaveCount(2);
    await expect(rows.locator("td:nth-child(3)")).toHaveText([label, label]);
    await expect(
      page.getByRole("button", { name: "Növbəti", exact: true }),
    ).toBeDisabled();
  }
  await page.reload();
  await expect(filter).toHaveValue("ApplicationRejected");
  await expect(rows).toHaveCount(2);
  await filter.selectOption("");
  await expect(rows).toHaveCount(25);
  await expect(page.locator(".audit-pagination")).toContainText(
    "Səhifə 1 · 135 əməliyyat",
  );
  await expect(page).toHaveURL(/#\/admin\/audit\?keep=1$/);
});

test("Empty audit filter explains the selected action and can be cleared", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  const source = records.filter((record) => record.action !== "UserDeleted");
  await page.route("**/api/admin/audit-logs**", (route) =>
    respond(
      route,
      resultFor(new URL(route.request().url()).searchParams, source),
    ),
  );
  await login(page);
  await page.goto("/#/admin/audit?action=UserDeleted&keep=1");
  await expect(
    page.getByRole("heading", { name: "Bu əməliyyat üzrə qeyd tapılmadı" }),
  ).toBeVisible();
  await expect(page.locator(".empty")).toContainText("Hesab silindi");
  await page.getByRole("button", { name: "Filteri təmizlə" }).click();
  await expect(page.locator(".audit-table tbody tr")).toHaveCount(25);
  await expect(page.locator(".audit-pagination")).toContainText(
    "108 əməliyyat",
  );
  await expect(page.getByLabel("Əməliyyat", { exact: true })).toHaveValue("");
  await expect(page).toHaveURL(/#\/admin\/audit\?keep=1$/);
});

test("A slow earlier audit response cannot replace the latest selected filter", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  let release;
  const gate = new Promise((resolve) => {
    release = resolve;
  });
  let waiting = false;
  let finished = false;
  await page.route("**/api/admin/audit-logs**", async (route) => {
    const params = new URL(route.request().url()).searchParams;
    if (params.get("action") === "UserBlocked") {
      waiting = true;
      await gate;
      await respond(route, resultFor(params)).catch(() => {}); // The old fetch may have been aborted.
      finished = true;
      return;
    }
    return respond(route, resultFor(params));
  });
  await login(page);
  await page.goto("/#/admin/audit");
  const filter = page.getByLabel("Əməliyyat", { exact: true });
  await expect(page.locator(".audit-table tbody tr")).toHaveCount(25);
  await filter.selectOption("UserBlocked");
  await expect.poll(() => waiting).toBe(true);
  await filter.selectOption("UserDeleted");
  const actionCells = page.locator(".audit-table tbody td:nth-child(3)");
  await expect(actionCells).toHaveText(Array(25).fill("Hesab silindi"));
  release();
  await expect.poll(() => finished).toBe(true);
  await expect(actionCells).toHaveText(Array(25).fill("Hesab silindi"));
  await expect(filter).toHaveValue("UserDeleted");
});

test("An obsolete audit page returns to the first page without losing its action filter", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  await page.route("**/api/admin/audit-logs**", (route) =>
    respond(route, resultFor(new URL(route.request().url()).searchParams)),
  );
  await login(page);
  await page.goto("/#/admin/audit?action=UserBlocked&page=99&keep=1");
  await expect(page.locator(".audit-table tbody tr")).toHaveCount(25);
  await expect(page.locator(".audit-pagination")).toContainText(
    "Səhifə 1 · 27 əməliyyat",
  );
  await expect(page.getByLabel("Əməliyyat", { exact: true })).toHaveValue(
    "UserBlocked",
  );
  await expect(page).toHaveURL(/#\/admin\/audit\?action=UserBlocked&keep=1$/);
});
