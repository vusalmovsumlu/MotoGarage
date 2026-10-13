import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

test("Gooey navigation follows route, browser history, keyboard and role links", async ({
  page,
}) => {
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await mockApi(page);
  await login(page);
  const nav = page.getByRole("navigation", { name: "Əsas menyu" });
  await expect(nav.locator(".gooey-effect")).toBeVisible();
  await nav.getByRole("link", { name: "Qarajım", exact: true }).click();
  await expect(page).toHaveURL(/#\/vehicles$/);
  await expect(
    nav.getByRole("link", { name: "Qarajım", exact: true }),
  ).toHaveAttribute("aria-current", "page");
  await expect
    .poll(() => nav.locator(".gooey-particle").count())
    .toBeGreaterThan(0);
  await page.goto("/#/garage/toyota-corolla-test");
  await expect(
    nav.getByRole("link", { name: "Qarajım", exact: true }),
  ).toHaveAttribute("aria-current", "page");
  await nav.getByRole("link", { name: "Məhsullar", exact: true }).focus();
  await page.keyboard.press("Enter");
  await expect(page).toHaveURL(/#\/products$/);
  await page.goBack();
  await expect(
    nav.getByRole("link", { name: "Qarajım", exact: true }),
  ).toHaveAttribute("aria-current", "page");
  await expect.poll(() => nav.locator(".gooey-particle").count()).toBe(0);
  await nav.locator("summary").click();
  await nav.getByRole("link", { name: "Görüşlərim", exact: true }).click();
  await expect(page).toHaveURL(/#\/appointments$/);
  await expect(nav.locator(".gooey-effect")).toBeHidden();
  await page.emulateMedia({ reducedMotion: "reduce" });
  await nav.getByRole("link", { name: "Qarajım", exact: true }).click();
  await expect(nav.locator(".gooey-effect")).toHaveCSS(
    "transition-duration",
    "0s",
  );
  await expect(nav.locator(".gooey-particle")).toHaveCount(0);
  expect(errors).toEqual([]);
});

for (const width of [1440, 799, 390, 320]) {
  test(`Gooey nav fits both themes at ${width}px during transitions`, async ({
    page,
  }) => {
    await mockApi(page, "Store");
    await page.setViewportSize({ width, height: 950 });
    await login(page);
    const nav = page.getByRole("navigation", { name: "Əsas menyu" });
    for (const theme of ["light", "dark"]) {
      await nav.getByRole("link", { name: "Məhsullarım", exact: true }).click();
      await page.waitForTimeout(180);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
      await expect(
        nav.getByRole("link", { name: "Məhsullarım", exact: true }),
      ).toHaveCSS("color", "rgb(255, 255, 255)");
      await expect.poll(() => nav.locator(".gooey-particle").count()).toBe(0);
      const effect = await nav.locator(".gooey-effect").boundingBox();
      const link = await nav
        .getByRole("link", { name: "Məhsullarım", exact: true })
        .boundingBox();
      expect(Math.abs(effect.x - link.x)).toBeLessThan(2);
      expect(Math.abs(effect.y - link.y)).toBeLessThan(2);
      await page.screenshot({
        path: `test-results/gooey-nav-${width}-${theme}.png`,
        fullPage: true,
      });
      await nav.getByRole("link", { name: "İş masası", exact: true }).click();
      if (theme === "light")
        await page.getByRole("button", { name: "Temanı dəyiş" }).click();
    }
  });
}

for (const [kind, key, label, fields] of [
  [
    "fuel",
    "fuelDate",
    "Doldurma tarixi",
    [
      ["Yanacaq (litr)", "20"],
      ["Litrin qiyməti (₼)", "1.2"],
      ["Odometr (km)", "26000"],
    ],
  ],
  [
    "expenses",
    "expenseDate",
    "Xərc tarixi",
    [
      ["Məbləğ (₼)", "20"],
      ["Açıqlama", "Test xərc"],
    ],
  ],
  [
    "service",
    "serviceDate",
    "Servis tarixi",
    [
      ["Servisin adı", "Test servis"],
      ["Xərc (₼)", "20"],
      ["Odometr (km)", "26000"],
    ],
  ],
  ["reminders", "reminderDate", "Tarix", [["Başlıq", "Test xatırlatma"]]],
]) {
  test(`${kind}: explicit 24 hour input validates times and preserves UTC API payload`, async ({
    page,
  }) => {
    const { requests } = await mockApi(page);
    await login(page);
    await page.goto(`/#/garage/toyota-corolla-test/${kind}`);
    await page.getByRole("button", { name: "Qeyd əlavə et" }).click();
    const dialog = page.getByRole("dialog");
    for (const [name, value] of fields)
      await dialog.getByLabel(name).fill(value);
    await dialog.locator('input[type="date"]').fill("2026-10-09");
    const time = dialog.getByRole("textbox", {
      name: `${label} — saat (24 saat)`,
      exact: true,
    });
    await time.fill("24:00");
    expect(await time.evaluate((input) => input.checkValidity())).toBe(false);
    await time.fill("07:30");
    expect(await time.evaluate((input) => input.checkValidity())).toBe(true);
    await time.fill("0730");
    await expect(time).toHaveValue("07:30");
    await time.fill("12:60");
    expect(await time.evaluate((input) => input.checkValidity())).toBe(false);
    await time.fill("23:59");
    expect(await time.evaluate((input) => input.checkValidity())).toBe(true);
    await expect(
      dialog.locator('input[type="datetime-local"], input[type="time"]'),
    ).toHaveCount(0);
    const expected = await page.evaluate(() =>
      new Date("2026-10-09T23:59").toISOString(),
    );
    await dialog.getByRole("button", { name: "Yadda saxla" }).click();
    await expect(dialog).toHaveCount(0);
    const request = requests.find(
      (item) => item.method === "POST" && item.body?.[key],
    );
    expect(request.body[key]).toBe(expected);
  });
}

test("Appointments use available Baku slots and displayed midnight is 00:00", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/appointments");
  await page.getByRole("button", { name: "Görüş yarat", exact: true }).click();
  const dialog = page.getByRole("dialog");
  const day = await dialog.locator('input[type="date"]').inputValue();
  await expect(
    dialog
      .getByRole("combobox", { name: "Usta", exact: true })
      .getByRole("option", {
        name: "Baku Auto & Moto Servis · Əli Məmmədov",
        exact: true,
      }),
  ).toHaveCount(1);
  const expected = new Date(`${day}T11:00:00+04:00`).toISOString();
  await dialog
    .getByRole("combobox", { name: "Boş saat" })
    .selectOption({ label: "11:00" });
  await dialog.getByRole("button", { name: "Yadda saxla" }).click();
  await expect(dialog).toHaveCount(0);
  expect(
    requests.find(
      (request) =>
        request.path === "/appointments" && request.method === "POST",
    ).body.appointmentDate,
  ).toBe(expected);
  const formatted = await page.evaluate(async () => {
    const { date } = await import("/src/shared/utils/format.js");
    return [
      date("2026-10-08T20:00:00Z", true),
      date("2026-10-08T17:05:00Z", true),
    ];
  });
  expect(formatted[0]).toContain("00:00");
  expect(formatted[1]).toContain("21:05");
  expect(formatted.join(" ")).not.toMatch(/AM|PM/);
});

test("Date and 24 hour inputs fit a mobile dialog in both themes", async ({
  page,
}) => {
  await mockApi(page);
  await page.setViewportSize({ width: 320, height: 900 });
  await login(page);
  await page.goto("/#/garage/toyota-corolla-test/reminders");
  for (const theme of ["light", "dark"]) {
    await page.getByRole("button", { name: "Qeyd əlavə et" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.locator('input[type="date"]').fill("2026-10-09");
    await dialog
      .getByRole("textbox", { name: "Tarix — saat (24 saat)" })
      .fill("2359");
    await expect(
      dialog.getByRole("textbox", { name: "Tarix — saat (24 saat)" }),
    ).toHaveValue("23:59");
    const bounds = await dialog.boundingBox();
    for (const input of await dialog
      .locator(".datetime24-control input")
      .all()) {
      const box = await input.boundingBox();
      expect(box.x).toBeGreaterThanOrEqual(bounds.x);
      expect(box.x + box.width).toBeLessThanOrEqual(bounds.x + bounds.width);
    }
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
    await page.screenshot({ path: `test-results/datetime24-320-${theme}.png` });
    await dialog.getByRole("button", { name: "İmtina", exact: true }).click();
    if (theme === "light")
      await page.getByRole("button", { name: "Temanı dəyiş" }).click();
  }
});
