import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

const urlFilters = (page) =>
  page.evaluate(() =>
    Object.fromEntries(new URLSearchParams(location.hash.split("?")[1])),
  );
const lastQuery = (requests, path) =>
  requests
    .filter((request) => request.method === "GET" && request.path === path)
    .at(-1)?.query;

for (const type of ["mechanics", "stores", "products"]) {
  test(`${type} searches restore from URL and back/forward, including drafts and encoded characters`, async ({
    page,
  }) => {
    const { requests } = await mockApi(page);
    await login(page);
    const searchLabel =
      type === "products" ? "Məhsul axtarın…" : "Ad və ya xidmət axtarın…";
    const extraLabel = type === "products" ? "Marka" : "Ünvan";
    const extraKey = type === "products" ? "brand" : "address";
    const initial = {
      search: "Əli & Moto + servis",
      [extraKey]: "Bakı / Nərimanov",
      keep: "1",
    };
    await page.goto(`/#/${type}?${new URLSearchParams(initial)}`);
    await expect(page.getByLabel(searchLabel, { exact: true })).toHaveValue(
      initial.search,
    );
    await expect(page.getByLabel(extraLabel, { exact: true })).toHaveValue(
      initial[extraKey],
    );
    await expect
      .poll(() => lastQuery(requests, `/${type}`)?.search)
      .toBe(initial.search);
    await page.getByLabel(searchLabel, { exact: true }).fill("ikinci axtarış");
    expect(await urlFilters(page)).toEqual(initial); // Drafts do not fetch or change history.
    await page.getByRole("button", { name: "Axtar", exact: true }).click();
    await expect
      .poll(() => urlFilters(page))
      .toEqual({ ...initial, search: "ikinci axtarış" });
    await page.goBack();
    await expect(page.getByLabel(searchLabel, { exact: true })).toHaveValue(
      initial.search,
    );
    await page.goForward();
    await expect(page.getByLabel(searchLabel, { exact: true })).toHaveValue(
      "ikinci axtarış",
    );
    await page.reload();
    await expect(page.getByLabel(searchLabel, { exact: true })).toHaveValue(
      "ikinci axtarış",
    );
    await page.getByLabel(searchLabel, { exact: true }).fill("   ");
    await page.getByLabel(extraLabel, { exact: true }).fill("");
    await page.getByRole("button", { name: "Axtar", exact: true }).click();
    await expect.poll(() => urlFilters(page)).toEqual({ keep: "1" });
  });
}

test("Product zero-price bounds stay in the URL; invalid price drafts never apply", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/products?minPrice=0&maxPrice=100&brand=Honda");
  await expect(page.getByLabel("Minimum qiymət")).toHaveValue("0");
  await expect(page.getByLabel("Maksimum qiymət")).toHaveValue("100");
  await expect.poll(() => lastQuery(requests, "/products")?.minPrice).toBe("0");
  await page.getByLabel("Minimum qiymət").fill("200");
  await page.getByRole("button", { name: "Axtar", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("Qiymət aralığını");
  expect((await urlFilters(page)).minPrice).toBe("0");
  await page.getByLabel("Minimum qiymət").fill("0");
  await page.getByLabel("Maksimum qiymət").fill("50");
  await page.getByRole("button", { name: "Axtar", exact: true }).click();
  await expect
    .poll(() => urlFilters(page))
    .toEqual({ minPrice: "0", maxPrice: "50", brand: "Honda" });
  await page.reload();
  await expect(page.getByLabel("Maksimum qiymət")).toHaveValue("50");
});

for (const kind of ["fuel", "expenses"]) {
  test(`${kind} dates and vehicle selection persist through slug routing, refresh and reset`, async ({
    page,
  }) => {
    const { requests } = await mockApi(page);
    await login(page);
    await page.goto(
      `/#/records/${kind}?vehicle=1&startDate=2026-09-01&endDate=2026-09-30&keep=1`,
    );
    await expect(page).toHaveURL(
      new RegExp(`#/garage/toyota-corolla-test/${kind}\\?`),
    );
    await expect(page.getByLabel("Başlanğıc tarixi")).toHaveValue("2026-09-01");
    await expect(page.getByLabel("Son tarix")).toHaveValue("2026-09-30");
    const apiPath = kind === "fuel" ? "/fuel-records/stats" : "/expenses";
    await expect
      .poll(() => lastQuery(requests, apiPath)?.startDate)
      .toBe("2026-09-01");
    await page.getByLabel("Nəqliyyat vasitəsi").selectOption("2");
    await expect(page).toHaveURL(
      new RegExp(`#/garage/cfmoto-450-sr-test/${kind}\\?`),
    );
    expect(await urlFilters(page)).toEqual({
      startDate: "2026-09-01",
      endDate: "2026-09-30",
      keep: "1",
    });
    await page.getByLabel("Başlanğıc tarixi").fill("2026-10-01");
    await page.getByLabel("Son tarix").fill("2026-10-10");
    await page.getByRole("button", { name: "Filtrlə", exact: true }).click();
    await expect
      .poll(() => urlFilters(page))
      .toEqual({ startDate: "2026-10-01", endDate: "2026-10-10", keep: "1" });
    await page.reload();
    await expect(page.getByLabel("Nəqliyyat vasitəsi")).toHaveValue("2");
    await expect(page.getByLabel("Başlanğıc tarixi")).toHaveValue("2026-10-01");
    await page.getByRole("button", { name: "Sıfırla", exact: true }).click();
    await expect.poll(() => urlFilters(page)).toEqual({ keep: "1" });
    await expect(page.getByLabel("Başlanğıc tarixi")).toHaveValue("");
  });
}

for (const [role, path] of [
  ["User", "/appointments"],
  ["User", "/orders"],
  ["Mechanic", "/work/appointments"],
  ["Store", "/work/orders"],
]) {
  test(`${role} ${path} status tabs restore from URL and browser history`, async ({
    page,
  }) => {
    await mockApi(page, role);
    await login(page);
    await page.goto(`/#${path}?status=active&keep=1`);
    await expect(
      page.getByRole("button", { name: "Aktiv", exact: true }),
    ).toHaveAttribute("aria-pressed", "true");
    await page.getByRole("button", { name: "Bitmiş", exact: true }).click();
    await expect
      .poll(() => urlFilters(page))
      .toEqual({ status: "finished", keep: "1" });
    await page.goBack();
    await expect(
      page.getByRole("button", { name: "Aktiv", exact: true }),
    ).toHaveAttribute("aria-pressed", "true");
    await page.goForward();
    await expect(
      page.getByRole("button", { name: "Bitmiş", exact: true }),
    ).toHaveAttribute("aria-pressed", "true");
    await page.reload();
    await expect(
      page.getByRole("button", { name: "Bitmiş", exact: true }),
    ).toHaveAttribute("aria-pressed", "true");
    await page.getByRole("button", { name: "Hamısı", exact: true }).click();
    await expect.poll(() => urlFilters(page)).toEqual({ keep: "1" });
  });
}

test("Unread notification filter persists and clearing it removes only its own parameter", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/notifications?unreadOnly=true&keep=1");
  await expect(
    page.getByRole("button", { name: "Oxunmamış", exact: true }),
  ).toHaveAttribute("aria-pressed", "true");
  await expect
    .poll(() => lastQuery(requests, "/notifications")?.unreadOnly)
    .toBe("true");
  await page.getByRole("button", { name: "Hamısı", exact: true }).click();
  await expect.poll(() => urlFilters(page)).toEqual({ keep: "1" });
  await page.goBack();
  await expect(
    page.getByRole("button", { name: "Oxunmamış", exact: true }),
  ).toHaveAttribute("aria-pressed", "true");
  await page.reload();
  await expect
    .poll(() => lastQuery(requests, "/notifications")?.unreadOnly)
    .toBe("true");
});

test("Admin user search and role filters share a restorable URL", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Admin");
  await login(page);
  await page.goto("/#/admin/users?search=Əli&role=Mechanic&keep=1");
  await expect(page.getByLabel("Ad və ya email axtarın…")).toHaveValue("Əli");
  await expect(page.getByLabel("Rol üzrə filter")).toHaveValue("Mechanic");
  await expect
    .poll(() => lastQuery(requests, "/admin/users")?.role)
    .toBe("Mechanic");
  await page.getByLabel("Ad və ya email axtarın…").fill("Aysel");
  await page.getByLabel("Rol üzrə filter").selectOption("User");
  await page.getByRole("button", { name: "Axtar", exact: true }).click();
  await expect
    .poll(() => urlFilters(page))
    .toEqual({ search: "Aysel", role: "User", keep: "1" });
  await page.goBack();
  await expect(page.getByLabel("Ad və ya email axtarın…")).toHaveValue("Əli");
  await expect(page.getByLabel("Rol üzrə filter")).toHaveValue("Mechanic");
});

test("Business application all/pending/rejected filters use distinct restorable URLs", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Admin");
  await login(page);
  await page.goto("/#/admin/applications?status=Rejected&keep=1");
  await expect(page.getByLabel("Müraciət statusu")).toHaveValue("Rejected");
  await page.getByLabel("Müraciət statusu").selectOption("");
  await expect
    .poll(() => urlFilters(page))
    .toEqual({ status: "all", keep: "1" });
  await page.reload();
  await expect(page.getByLabel("Müraciət statusu")).toHaveValue("");
  await expect
    .poll(() => lastQuery(requests, "/admin/business-applications")?.status)
    .toBe("");
  await page.getByLabel("Müraciət statusu").selectOption("Pending");
  await expect.poll(() => urlFilters(page)).toEqual({ keep: "1" });
});

test("Audit action and page filters update atomically and restore with one back step", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  const calls = [];
  await page.route("**/api/admin/audit-logs**", (route) => {
    const params = new URL(route.request().url()).searchParams;
    calls.push(Object.fromEntries(params));
    return route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({
        total: 90,
        items: [
          {
            id: 1,
            action: params.get("action") || "UserBlocked",
            adminName: "Admin",
            targetName: "Test",
            reason: "Test",
            occurredAtUtc: new Date().toISOString(),
          },
        ],
      }),
    });
  });
  await login(page);
  await page.goto("/#/admin/audit?action=UserBlocked&page=2&keep=1");
  await expect(page.getByLabel("Əməliyyat", { exact: true })).toHaveValue(
    "UserBlocked",
  );
  await expect.poll(() => calls.at(-1)?.page).toBe("2");
  await page.getByRole("button", { name: "Növbəti", exact: true }).click();
  await expect
    .poll(() => urlFilters(page))
    .toEqual({ action: "UserBlocked", page: "3", keep: "1" });
  await page
    .getByLabel("Əməliyyat", { exact: true })
    .selectOption("UserDeleted");
  await expect
    .poll(() => urlFilters(page))
    .toEqual({ action: "UserDeleted", keep: "1" });
  await page.goBack();
  await expect
    .poll(() => urlFilters(page))
    .toEqual({ action: "UserBlocked", page: "3", keep: "1" });
  await expect(page.getByLabel("Əməliyyat", { exact: true })).toHaveValue(
    "UserBlocked",
  );
  await page.reload();
  await expect.poll(() => calls.at(-1)?.page).toBe("3");
});

for (const role of ["Mechanic", "Store"]) {
  test(`${role} statistics date ranges persist across refresh and history`, async ({
    page,
  }) => {
    const { requests } = await mockApi(page, role);
    await login(page);
    await page.goto("/#/statistics?from=2026-09-01&to=2026-09-30&keep=1");
    await expect(page.getByLabel("Başlanğıc", { exact: true })).toHaveValue(
      "2026-09-01",
    );
    await expect(page.getByLabel("Son gün", { exact: true })).toHaveValue(
      "2026-09-30",
    );
    await expect
      .poll(() => lastQuery(requests, "/business/statistics")?.from)
      .toBe("2026-09-01");
    await page.getByLabel("Başlanğıc", { exact: true }).fill("2026-10-01");
    await page.getByLabel("Son gün", { exact: true }).fill("2026-10-10");
    await page.getByRole("button", { name: "Göstər", exact: true }).click();
    await expect
      .poll(() => urlFilters(page))
      .toEqual({ from: "2026-10-01", to: "2026-10-10", keep: "1" });
    await page.goBack();
    await expect(page.getByLabel("Başlanğıc", { exact: true })).toHaveValue(
      "2026-09-01",
    );
    await page.goForward();
    await page.reload();
    await expect(page.getByLabel("Son gün", { exact: true })).toHaveValue(
      "2026-10-10",
    );
  });
}

test("Mechanic calendar keeps selected day and daily/weekly view in URL", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Mechanic");
  await login(page);
  await page.goto("/#/work/calendar?date=2026-10-20&view=day&keep=1");
  await expect(page.getByLabel("Təqvim başlanğıcı")).toHaveValue("2026-10-20");
  await expect(page.locator(".calendar-column")).toHaveCount(1);
  await expect
    .poll(() => lastQuery(requests, "/appointments/calendar")?.to)
    .toBe("2026-10-20");
  await page.getByRole("button", { name: "Həftəlik", exact: true }).click();
  await expect
    .poll(() => urlFilters(page))
    .toEqual({ date: "2026-10-20", keep: "1" });
  await expect(page.locator(".calendar-column")).toHaveCount(7);
  await expect
    .poll(() => lastQuery(requests, "/appointments/calendar")?.to)
    .toBe("2026-10-26");
  await page.goBack();
  await page.reload();
  await expect(
    page.getByRole("button", { name: "Günlük", exact: true }),
  ).toHaveAttribute("aria-pressed", "true");
});

test("Inventory filters preserve other query keys and default clears stock only", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Store");
  await login(page);
  await page.goto("/#/inventory?stock=out&keep=1");
  await expect(
    page.getByRole("button", { name: "Stok bitib", exact: true }),
  ).toHaveAttribute("aria-pressed", "true");
  await expect(page.locator(".loading")).toHaveCount(0);
  const count = requests.filter((request) =>
    request.path.startsWith("/products/store/"),
  ).length;
  await page.getByRole("button", { name: "Az stok", exact: true }).click();
  await expect
    .poll(() => urlFilters(page))
    .toEqual({ stock: "low", keep: "1" });
  await page.getByRole("button", { name: "Hamısı", exact: true }).click();
  await expect.poll(() => urlFilters(page)).toEqual({ keep: "1" });
  expect(
    requests.filter((request) => request.path.startsWith("/products/store/")),
  ).toHaveLength(count);
});

test("Malformed statistics dates use valid defaults instead of sending invalid API filters", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Store");
  await login(page);
  await page.goto("/#/statistics?from=2026-02-30&to=2099-12-31");
  await expect(page.getByLabel("Başlanğıc", { exact: true })).toHaveValue(
    /^\d{4}-\d{2}-\d{2}$/,
  );
  await expect
    .poll(() => lastQuery(requests, "/business/statistics")?.from)
    .toMatch(/^\d{4}-\d{2}-\d{2}$/);
  const params = lastQuery(requests, "/business/statistics");
  expect((new Date(params.to) - new Date(params.from)) / 86400000).toBe(29);
  await page.evaluate(async () => {
    const { updateRouteQuery } =
      await import("/src/shared/utils/routeQuery.js");
    updateRouteQuery(
      { stock: "low" },
      { path: "/inventory", remove: ["from", "to"] },
    );
  });
  await expect.poll(() => urlFilters(page)).toEqual({ stock: "low" });
});

test("Malformed price bounds never reach the product API", async ({ page }) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/products?minPrice=-5&maxPrice=NaN&search=oil");
  await expect(page.getByLabel("Minimum qiymət")).toHaveValue("");
  await expect(page.getByLabel("Maksimum qiymət")).toHaveValue("");
  await expect
    .poll(() => lastQuery(requests, "/products"))
    .toEqual({ search: "oil" });
});

test("Malformed admin role, audit action and page values use safe defaults", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Admin");
  await login(page);
  await page.goto("/#/admin/users?role=unknown&search=Admin");
  await expect(page.getByLabel("Rol üzrə filter")).toHaveValue("");
  await expect
    .poll(() => lastQuery(requests, "/admin/users"))
    .toEqual({ search: "Admin" });
  await page.goto("/#/admin/audit?page=-9&action=bad");
  await expect(page.getByLabel("Əməliyyat", { exact: true })).toHaveValue("");
  await expect
    .poll(() => lastQuery(requests, "/admin/audit-logs"))
    .toEqual({ page: "1", pageSize: "25" });
  await expect(
    page.getByRole("button", { name: "Əvvəlki", exact: true }),
  ).toBeDisabled();
});
