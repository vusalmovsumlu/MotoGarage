import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

for (const role of ["Mechanic", "Store"]) {
  test(`${role} workspace exposes only its own work tools and blocks personal routes before API reads`, async ({
    page,
  }) => {
    const { requests } = await mockApi(page, role);
    await login(page);
    const nav = page.getByRole("navigation", {
      name: "Əsas menyu",
      exact: true,
    });
    await expect(nav.getByRole("link")).toHaveText(
      role === "Mechanic"
        ? ["İş masası", "Gələn görüşlər", "Biznes profilim"]
        : ["İş masası", "Məhsullarım", "Gələn sifarişlər", "Biznes profilim"],
    );
    await expect(page.locator(".nav-more")).toHaveCount(0);
    await expect(page.getByRole("link", { name: /^Səbət/ })).toHaveCount(0);
    await expect(
      page.getByRole("link", { name: "Söhbətlər", exact: true }).first(),
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: /^Bildirişlər,/ }),
    ).toBeVisible();
    await expect(page.locator(".stat").first()).toContainText(
      role === "Mechanic" ? "Yeni görüşlər" : "Yeni sifarişlər",
    );
    await expect(page.locator(".stat").first().locator("strong")).toHaveText(
      "1",
    );
    await expect(page.getByText("Qarajınızda", { exact: true })).toHaveCount(0);
    await expect(
      page.locator(
        "main a[href='#/vehicles'], main a[href='#/orders'], main a[href='#/appointments'], main a[href='#/products']",
      ),
    ).toHaveCount(0);
    for (const path of [
      "/vehicles",
      "/garage/toyota-corolla-test",
      "/cart",
      "/products",
      "/mechanics/baku-servis",
      "/stores",
      "/records/fuel?vehicle=1",
      "/appointments",
      "/orders",
      role === "Mechanic" ? "/work/orders" : "/work/appointments",
      role === "Mechanic" ? "/inventory" : "/records/service",
    ]) {
      await page.goto(`/#${path}`);
      await expect(
        page.getByRole("heading", {
          name: "Bu bölmə biznes hesabı üçün nəzərdə tutulmayıb",
        }),
      ).toBeVisible();
    }
    expect(
      requests.filter((request) =>
        [
          "/dashboard",
          "/Vehicle",
          "/appointments",
          "/orders",
          "/products",
          "/stores",
          "/mechanics",
        ].includes(request.path),
      ),
    ).toEqual([]);
    await page.goto(
      role === "Mechanic" ? "/#/work/appointments" : "/#/work/orders",
    );
    await expect(page.locator(".work-card")).toBeVisible();
    await expect(page.locator(".error-panel")).toHaveCount(0);
    await page.goto("/#/business");
    await page
      .getByRole("link", { name: "Müştəri görünüşü", exact: true })
      .click();
    await expect(page).toHaveURL(/#\/business\/preview$/);
    await expect(page.locator("main h1")).toHaveText(
      role === "Mechanic" ? "Baku Auto & Moto Servis" : "Garage Parts",
    );
    await expect(page.locator(".error-panel")).toHaveCount(0);
    await expect(
      page.getByRole("button", {
        name: /Səbətə əlavə et|Görüş planlaşdır|Mesaj yaz|Rəy yaz|Rəyi dəyiş|Rəyi sil/,
      }),
    ).toHaveCount(0);
    await expect(page.locator("main a[href^='#/products/']")).toHaveCount(0);
    if (role === "Mechanic")
      await expect(page.locator(".review-card")).toBeVisible();
    else {
      await expect(page.locator(".product-card")).toBeVisible();
      await page.goto("/#/inventory");
      await page
        .getByRole("button", { name: "Mühərrik yağı 5W-40", exact: true })
        .click();
      await expect(page.getByRole("dialog")).toBeVisible();
      await page.keyboard.press("Escape");
    }
    expect(
      requests.filter((request) =>
        [
          "/mechanics/undefined",
          "/stores/undefined",
          "/mechanics/baku-servis",
          "/stores/garage-parts",
        ].includes(request.path),
      ),
    ).toEqual([]);
  });

  test(`${role} empty chat only offers business messaging guidance`, async ({
    page,
  }) => {
    await mockApi(page, role, { empty: true });
    await login(page);
    await page.goto("/#/chat");
    await expect(
      page.getByText("Müştərilərinizin mesajları burada görünəcək.", {
        exact: true,
      }),
    ).toBeVisible();
    await expect(page.locator("main a[href='#/mechanics']")).toHaveCount(0);
  });

  test(`${role} business dashboard uses its real work statuses, retains errors and has an empty state`, async ({
    page,
  }) => {
    await mockApi(page, role);
    const endpoint =
      role === "Mechanic" ? "/appointments/mechanic" : "/orders/store";
    let fail = false;
    let items = (
      role === "Mechanic"
        ? [
            "Pending",
            "Pending",
            "Accepted",
            "Completed",
            "Rejected",
            "Cancelled",
          ]
        : [
            "Pending",
            "Pending",
            "Confirmed",
            "Preparing",
            "Shipped",
            "Completed",
            "Cancelled",
          ]
    ).map((status, index) => ({
      id: index + 1,
      status,
      appointmentDate: new Date(
        Date.now() + (index + 1) * 3600000,
      ).toISOString(),
      createdAt: new Date().toISOString(),
      totalAmount: 45,
      customerName: "Aysel Əliyeva",
      vehicleName: "Toyota Corolla",
      description: "Yağ dəyişimi",
    }));
    await page.route(`**/api${endpoint}`, (route) =>
      route.fulfill({
        status: fail ? 500 : 200,
        contentType: "application/json",
        body: JSON.stringify(fail ? { message: "Test işi yüklənmədi" } : items),
      }),
    );
    await login(page);
    await expect(page.locator(".stat strong")).toHaveText(
      role === "Mechanic" ? ["2", "1", "1", "1"] : ["2", "2", "1", "1"],
    );
    if (role === "Mechanic")
      await expect(page.locator(".summary-column")).toContainText(
        "Aysel Əliyeva · Toyota Corolla",
      );
    fail = true;
    await page.reload();
    await expect(page.locator(".error-panel")).toContainText(
      "Test işi yüklənmədi",
    );
    await expect(page.locator(".stat")).toHaveCount(0);
    fail = false;
    items = [];
    await page.getByRole("button", { name: "Yenidən cəhd et" }).click();
    await expect(page.locator(".stat strong")).toHaveText(["0", "0", "0", "0"]);
    await expect(page.locator(".summary-column")).toContainText(
      role === "Mechanic"
        ? "Hazırda aktiv servis görüşü yoxdur."
        : "Hazırda aktiv sifariş yoxdur.",
    );
  });

  for (const width of [320, 1440]) {
    test(`${role} business workspace fits ${width}px in both themes`, async ({
      page,
    }, testInfo) => {
      await page.setViewportSize({ width, height: 900 });
      await mockApi(page, role);
      await login(page);
      for (const theme of ["light", "dark"]) {
        await expect(page.locator(".stat")).toHaveCount(4);
        expect(
          await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
          ),
        ).toBe(true);
        await page.screenshot({
          path: testInfo.outputPath(`workspace-${theme}.png`),
          fullPage: true,
        });
        if (theme === "light")
          await page.getByRole("button", { name: "Temanı dəyiş" }).click();
      }
    });
  }
}
