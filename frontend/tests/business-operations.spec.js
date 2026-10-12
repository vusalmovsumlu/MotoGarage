import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

const respond = (route, value, status = 200) =>
  route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(value),
  });
const appointment = (extra = {}) => ({
  id: 60,
  userId: 1,
  mechanicId: 10,
  vehicleId: 1,
  status: "Accepted",
  durationMinutes: 60,
  vehicleName: "Toyota Corolla",
  customerName: "Aysel Əliyeva",
  mechanicName: "Əli Məmmədov",
  description: "Yağ dəyişimi",
  appointmentDate: new Date(Date.now() + 86400000).toISOString(),
  ...extra,
});
const report = (extra = {}) => ({
  id: 1,
  appointmentId: 60,
  serviceName: "Yağ və filtr dəyişimi",
  replacedParts: "Yağ filtri",
  description: "Mühərrik qulluğu",
  kilometer: 25000,
  cost: 80,
  serviceDate: new Date(Date.now() - 3600000).toISOString(),
  revision: 1,
  status: "Pending",
  ...extra,
});

test("Baku calendar treats database timestamps without an offset as UTC", async ({
  page,
}) => {
  await mockApi(page);
  await login(page);
  const values = await page.evaluate(async () => {
    const { bakuTime, bakuDay } = await import("/src/shared/utils/bakuTime.js");
    return [
      bakuTime("2030-10-09T20:00:00"),
      bakuDay("2030-10-09T20:00:00"),
      bakuTime("2030-10-09T20:00:00Z"),
    ];
  });
  expect(values).toEqual(["00:00", "2030-10-10", "00:00"]);
});

test("Mechanic calendar switches day/week, reads only its calendar and saves 24 hour working hours", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Mechanic");
  await login(page);
  await page.goto("/#/work/calendar");
  await expect(page.locator(".calendar-column")).toHaveCount(7);
  await page.getByRole("button", { name: "Günlük", exact: true }).click();
  await expect(page.locator(".calendar-column")).toHaveCount(1);
  await page.getByRole("button", { name: "İş saatlarını dəyiş" }).click();
  const dialog = page.getByRole("dialog");
  await dialog
    .getByRole("textbox", { name: "Bazar ertəsi başlanğıc", exact: true })
    .fill("08:30");
  await dialog
    .getByRole("textbox", { name: "Bazar ertəsi bitmə", exact: true })
    .fill("17:45");
  await dialog
    .getByRole("button", { name: "Yadda saxla", exact: true })
    .click();
  await expect(dialog).toHaveCount(0);
  const save = requests.find(
    (request) =>
      request.method === "PUT" &&
      request.path === "/appointments/working-hours",
  );
  expect(save.body.find((hour) => hour.dayOfWeek === 1)).toMatchObject({
    start: "08:30:00",
    end: "17:45:00",
  });
  expect(save.body).toHaveLength(7);
  expect(
    requests.filter((request) =>
      ["/appointments/my", "/Vehicle"].includes(request.path),
    ),
  ).toHaveLength(0);
});

test("Booking exposes available Baku times and retains slot and dialog on a server conflict", async ({
  page,
}) => {
  await mockApi(page);
  let payload;
  await page.route("**/api/appointments", (route) => {
    payload = route.request().postDataJSON();
    return respond(route, { message: "Bu vaxt artıq tutulub." }, 409);
  });
  await login(page);
  await page.goto("/#/appointments");
  await page.getByRole("button", { name: "Görüş yarat", exact: true }).click();
  const dialog = page.getByRole("dialog");
  const day = await dialog.locator('input[type="date"]').inputValue();
  await dialog
    .getByRole("combobox", { name: "Boş saat" })
    .selectOption({ label: "10:00" });
  await dialog.getByRole("button", { name: "Yadda saxla" }).click();
  await expect(dialog.getByRole("alert")).toContainText(
    "Bu vaxt artıq tutulub.",
  );
  expect(payload.appointmentDate).toBe(
    new Date(`${day}T10:00:00+04:00`).toISOString(),
  );
  await expect(dialog.getByRole("combobox", { name: "Boş saat" })).toHaveValue(
    payload.appointmentDate,
  );
});

test("Only the other participant sees acceptance; proposing retains old date until confirmed", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  let item = appointment();
  await page.route("**/api/appointments/my", (route) => respond(route, [item]));
  await page.route("**/api/appointments/60/reschedule", (route) => {
    item = {
      ...item,
      proposedAppointmentDateUtc: route.request().postDataJSON()
        .appointmentDate,
      rescheduleProposedByUserId: 1,
      rescheduleRevision: 1,
    };
    return route.fulfill({ status: 204 });
  });
  await login(page);
  await page.goto("/#/appointments");
  await page
    .getByRole("button", { name: "Vaxt dəyişikliyi təklif et" })
    .click();
  const dialog = page.getByRole("dialog");
  await dialog
    .getByRole("combobox", { name: "Boş saat" })
    .selectOption({ label: "11:00" });
  const oldDate = item.appointmentDate;
  await dialog.getByRole("button", { name: "Təklif göndər" }).click();
  await expect(dialog).toHaveCount(0);
  await expect(
    page.getByText("Qarşı tərəfin cavabı gözlənilir.", { exact: false }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Yeni vaxtı qəbul et" }),
  ).toHaveCount(0);
  expect(item.appointmentDate).toBe(oldDate);
  item.rescheduleProposedByUserId = 20;
  await page.reload();
  await expect(
    page.getByRole("button", { name: "Yeni vaxtı qəbul et" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Yeni vaxtı qəbul et" }).click();
  await expect
    .poll(
      () =>
        requests.find((request) =>
          request.path.endsWith("/reschedule/decision"),
        )?.body,
    )
    .toMatchObject({ approve: true, revision: 1 });
});

test("Mechanic submits report parts and kilometer, and cannot write approved reports", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Mechanic");
  let current = null;
  let submitted;
  await page.route("**/api/appointments/mechanic", (route) =>
    respond(route, [appointment({ status: "Completed" })]),
  );
  await page.route("**/api/appointments/60/report", (route) => {
    if (route.request().method() === "GET") return respond(route, current);
    submitted = route.request().postDataJSON();
    return route.fulfill({ status: 204 });
  });
  await login(page);
  await page.goto("/#/work/appointments");
  await page
    .getByRole("button", { name: "Servis hesabatı", exact: true })
    .click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Görülən iş", { exact: false }).fill("Yağ dəyişimi");
  await dialog.getByLabel("Kilometr", { exact: false }).fill("25000");
  await dialog.getByLabel("Məbləğ (AZN)", { exact: false }).fill("80");
  await dialog.getByLabel("Dəyişdirilən hissələr").fill("Yağ filtri");
  await dialog.getByRole("button", { name: "Hesabatı göndər" }).click();
  await expect(dialog).toHaveCount(0);
  expect(submitted).toMatchObject({
    serviceName: "Yağ dəyişimi",
    replacedParts: "Yağ filtri",
    kilometer: 25000,
    cost: 80,
    revision: 0,
  });
  // Route overrides bypass the common fixture request log.
  expect(
    requests.filter((request) => request.path === "/service-records"),
  ).toHaveLength(0);
  current = report({ status: "Approved" });
  await page
    .getByRole("button", { name: "Servis hesabatı", exact: true })
    .click();
  await expect(dialog.getByText("Tarixçəyə əlavə edilib")).toBeVisible();
  await expect(
    dialog.getByRole("button", { name: "Hesabatı göndər" }),
  ).toHaveCount(0);
});

test("Customer report decision requires rejection comment and submits the visible revision", async ({
  page,
}) => {
  await mockApi(page);
  let decision;
  await page.route("**/api/appointments/my", (route) =>
    respond(route, [appointment({ status: "Completed" })]),
  );
  await page.route("**/api/appointments/60/report", (route) =>
    respond(route, report({ revision: 3 })),
  );
  await page.route("**/api/appointments/60/report/decision", (route) => {
    decision = route.request().postDataJSON();
    return route.fulfill({ status: 204 });
  });
  await login(page);
  await page.goto("/#/appointments");
  await page.getByRole("button", { name: "Servis hesabatına bax" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByText("Yağ filtri", { exact: true })).toBeVisible();
  await dialog.getByRole("button", { name: "Düzəlişə qaytar" }).click();
  await expect(dialog.getByRole("alert")).toContainText("Rədd səbəbini yazın.");
  expect(decision).toBeUndefined();
  await dialog
    .getByRole("button", { name: "Təsdiqlə və tarixçəyə əlavə et" })
    .click();
  await expect(dialog).toHaveCount(0);
  expect(decision).toMatchObject({ approve: true, revision: 3 });
});

test("Inventory stock filters survive URL reload and thresholds are configurable", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Store");
  await page.route("**/api/products/store/garage-parts", (route) =>
    respond(route, [
      {
        id: 1,
        slug: "low",
        name: "Az qalan yağ",
        stock: 2,
        lowStockThreshold: 3,
        price: 10,
      },
      {
        id: 2,
        slug: "out",
        name: "Bitən filtr",
        stock: 0,
        lowStockThreshold: 5,
        price: 10,
      },
      {
        id: 3,
        slug: "full",
        name: "Çox qalan detal",
        stock: 20,
        lowStockThreshold: 5,
        price: 10,
      },
    ]),
  );
  await login(page);
  await page.goto("/#/inventory?stock=out");
  await expect(page.locator(".inventory-row")).toHaveCount(1);
  await expect(page.locator(".inventory-row")).toContainText("Bitən filtr");
  await page.getByRole("button", { name: "Az stok", exact: true }).click();
  await expect(page).toHaveURL(/stock=low/);
  await page.reload();
  await expect(page.locator(".inventory-row")).toContainText("Az qalan yağ");
  await page
    .getByRole("button", { name: "Məhsulu dəyiş", exact: true })
    .click();
  const dialog = page.getByRole("dialog");
  await expect(
    dialog.getByLabel("Stok xəbərdarlıq həddi", { exact: false }),
  ).toHaveValue("3");
  await dialog.getByLabel("Stok xəbərdarlıq həddi", { exact: false }).fill("4");
  await dialog.getByRole("button", { name: "Yadda saxla" }).click();
  await expect(dialog).toHaveCount(0);
  expect(
    requests.find(
      (request) => request.path === "/products/low" && request.method === "PUT",
    ).body.lowStockThreshold,
  ).toBe(4);
});

test("Statistics expose completed sales and best sellers with stock links", async ({
  page,
}) => {
  await mockApi(page, "Store");
  await login(page);
  await page.goto("/#/statistics");
  await expect(page.locator(".business-metrics").first()).toContainText(
    "120,00",
  );
  await expect(page.locator(".business-table")).toContainText("Motor oil");
  await page.getByRole("link", { name: /Stoku bitən məhsullar/ }).click();
  await expect(page).toHaveURL(/inventory\?stock=out/);
});

test("Admin account decision stores the entered audit reason", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Admin");
  await login(page);
  await page.goto("/#/admin/users");
  const row = page.getByRole("row").filter({ hasText: "Aysel Əliyeva" });
  await row.getByRole("button", { name: "Blokla", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog
    .getByLabel("Admin qeydi (istəyə bağlı)")
    .fill("  Qaydaları pozduğu üçün  ");
  await dialog.getByRole("button", { name: "Blokla", exact: true }).click();
  await expect(dialog).toHaveCount(0);
  expect(
    requests.find((request) => request.path === "/admin/users/2/block").body,
  ).toEqual({ reason: "Qaydaları pozduğu üçün" });
});

test("Admin audit filters reach the paged API and display actor, reason and time", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Admin");
  const auditQueries = [];
  await page.route("**/api/admin/audit-logs?**", (route) => {
    auditQueries.push(
      new URL(route.request().url()).searchParams.get("action"),
    );
    return respond(route, {
      total: 1,
      items: [
        {
          id: 1,
          adminName: "Admin Vüsal",
          action: "ApplicationRejected",
          targetName: "Baku Garage",
          targetId: 7,
          reason: "Real qaraj şəkilləri lazımdır",
          occurredAtUtc: new Date().toISOString(),
        },
      ],
    });
  });
  await login(page);
  await page.goto("/#/admin/audit");
  await expect(page.locator(".audit-table")).toContainText("Admin Vüsal");
  await expect(page.locator(".audit-table")).toContainText(
    "Real qaraj şəkilləri lazımdır",
  );
  await page.getByRole("combobox").selectOption("ApplicationRejected");
  await expect.poll(() => auditQueries).toContain("ApplicationRejected");
  await expect(
    page.getByRole("button", { name: "Növbəti", exact: true }),
  ).toBeDisabled();
  expect(
    requests.filter((request) =>
      ["/Vehicle", "/dashboard"].includes(request.path),
    ),
  ).toHaveLength(0);
});

for (const role of ["User", "Store"]) {
  test(`${role} cannot open mechanic calendar or admin audit`, async ({
    page,
  }) => {
    const { requests } = await mockApi(page, role);
    await login(page);
    await page.goto("/#/work/calendar");
    await expect(page.locator(".calendar-column")).toHaveCount(0);
    await page.goto("/#/admin/audit");
    await expect(page.locator(".audit-table")).toHaveCount(0);
    expect(
      requests.filter((request) =>
        ["/appointments/calendar", "/admin/audit-logs"].includes(request.path),
      ),
    ).toHaveLength(0);
  });
}

for (const [role, path, target] of [
  ["Mechanic", "/work/calendar", ".appointment-calendar"],
  ["Store", "/statistics", ".business-metrics"],
  ["Admin", "/admin/audit", ".audit-pagination"],
]) {
  test(`${role} new tools fit 320px in both themes`, async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 900 });
    await mockApi(page, role);
    await login(page);
    await page.goto(`/#${path}`);
    await expect(page.locator(target).first()).toBeVisible();
    for (const theme of ["light", "dark"]) {
      if (theme === "dark")
        await page.getByRole("button", { name: "Temanı dəyiş" }).click();
      await expect
        .poll(async () =>
          page.evaluate(() => {
            const active = document
              .querySelector('.main-nav a[aria-current="page"]')
              ?.getBoundingClientRect();
            const effect = document
              .querySelector(".gooey-effect")
              ?.getBoundingClientRect();
            return (
              active &&
              effect &&
              Math.abs(active.left - effect.left) < 1 &&
              Math.abs(active.top - effect.top) < 1
            );
          }),
        )
        .toBe(true);
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth > innerWidth + 1,
      );
      expect(overflow).toBe(false);
      await page.screenshot({
        path: `test-results/business-${role}-${theme}-320.png`,
        fullPage: true,
      });
    }
  });
}
