import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures";

for (const role of ["User", "Mechanic", "Store", "Admin"]) {
  test(`${role}: role workspace and allowed routes render`, async ({
    page,
  }) => {
    const errors = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await mockApi(page, role);
    await login(page);
    const routes =
      role === "Admin"
        ? ["/home", "/admin/users", "/admin/applications", "/profile"]
        : role === "Mechanic"
          ? [
              "/home",
              "/business",
              "/work/appointments",
              "/profile",
              "/notifications",
              "/chat",
            ]
          : role === "Store"
            ? [
                "/home",
                "/business",
                "/inventory",
                "/work/orders",
                "/profile",
                "/notifications",
                "/chat",
              ]
            : [
                "/vehicles",
                "/products",
                "/mechanics",
                "/stores",
                "/appointments",
                "/orders",
                "/profile",
                "/notifications",
                "/records/fuel",
                "/records/expenses",
                "/records/service",
                "/records/reminders",
                "/chat",
              ];
    for (const route of routes) {
      await page.goto(`/#${route}`);
      await expect(page.locator("main h1")).toBeVisible();
      await expect(page.locator(".loading")).toHaveCount(0);
      await expect(page.locator(".error-panel")).toHaveCount(0);
    }
    expect(errors).toEqual([]);
  });
}
test("Car and motorcycle are both available; vehicle payload matches API", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/vehicles");
  await expect(
    page.getByRole("heading", { name: "Toyota Corolla" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "CFMOTO 450 SR" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Nəqliyyat əlavə et" }).click();
  await page.getByLabel("Nəqliyyat növü").selectOption("Car");
  await page.getByRole("textbox", { name: "Marka", exact: true }).fill("Honda");
  await page.getByRole("textbox", { name: "Model", exact: true }).fill("Civic");
  await page
    .getByRole("textbox", { name: "Mühərrik", exact: true })
    .fill("1.5");
  await page
    .getByRole("textbox", { name: "VIN", exact: true })
    .fill("TESTVIN789");
  await page.getByLabel("Qeydiyyat nişanı").fill("10-XY-999");
  await page.getByRole("button", { name: "Yadda saxla" }).click();
  await expect(
    page.getByRole("heading", { name: "Honda Civic" }),
  ).toBeVisible();
  expect(
    requests.find((item) => item.method === "POST" && item.path === "/Vehicle")
      .body.vehicleType,
  ).toBe("Car");
});
test("Shopping cart accepts different stores and checks out with delivery details and IDs/quantities", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/products");
  await page.getByRole("button", { name: "Səbətə əlavə et" }).first().click();
  await page.getByRole("button", { name: "Səbətə əlavə et" }).nth(1).click();
  await page.goto("/#/cart");
  await expect(page.locator(".cart-store-group")).toHaveCount(2);
  await expect(
    page.getByRole("heading", { name: "Garage Parts", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Brake Parts", exact: true }),
  ).toBeVisible();
  await page.getByLabel("Telefon nömrəsi").fill("+994501234567");
  await page.getByLabel("Şəhər / rayon").fill("Bakı");
  await page.getByLabel("Tam ünvan").fill("Test küçəsi 10");
  await page.getByRole("button", { name: "Sifarişi təsdiqlə" }).click();
  await expect(
    page.getByRole("heading", { name: "Sifarişlərim" }),
  ).toBeVisible();
  expect(
    requests.find(
      (item) => item.method === "POST" && item.path === "/orders/checkout",
    ).body,
  ).toEqual({
    items: [
      { productId: 30, quantity: 1 },
      { productId: 31, quantity: 1 },
    ],
    recipientName: "Vüqar Məmmədov",
    deliveryPhone: "+994501234567",
    deliveryCity: "Bakı",
    deliveryAddress: "Test küçəsi 10",
    deliveryNote: "",
  });
});
test("Mechanic only sees legal transitions for pending appointments", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Mechanic");
  await login(page);
  await page.goto("/#/work/appointments");
  await expect(page.getByRole("button", { name: "Tamamlandı" })).toHaveCount(0);
  await page.getByRole("button", { name: "Qəbul edildi" }).click();
  await expect
    .poll(() =>
      requests.some(
        (item) =>
          item.path === "/appointments/60/status" &&
          item.body?.status === "Accepted",
      ),
    )
    .toBe(true);
});
test("Admin block requires a reviewable confirmation", async ({ page }) => {
  const { requests } = await mockApi(page, "Admin");
  await login(page);
  await page.goto("/#/admin/users");
  await page
    .getByRole("row")
    .filter({ hasText: "Aysel" })
    .getByRole("button", { name: "Blokla" })
    .click();
  await expect(page.getByRole("dialog")).toBeVisible();
  expect(requests.some((item) => item.path.endsWith("/block"))).toBe(false);
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Blokla", exact: true })
    .click();
  await expect
    .poll(() => requests.some((item) => item.path === "/admin/users/2/block"))
    .toBe(true);
});
test("Chat remains functional with HTTP fallback and does not duplicate sent messages", async ({
  page,
}) => {
  await mockApi(page);
  await login(page);
  await page.goto("/#/chat/90");
  await expect(
    page.getByText("Salam. Sizə necə kömək edə bilərik?"),
  ).toBeVisible();
  await page.getByLabel("Mesajınız").fill("Salam, məhsul mövcuddur?");
  await page.getByRole("button", { name: "Göndər", exact: true }).click();
  await expect(
    page.getByText("Salam, məhsul mövcuddur?", { exact: true }),
  ).toHaveCount(1);
});
test("Registration and OTP complete into the authenticated workspace", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await page.goto("/#/register");
  await page.getByLabel("Ad", { exact: true }).fill("Test");
  await page.getByLabel("Soyad", { exact: true }).fill("User");
  await page.getByLabel("Hesab növü").selectOption("User");
  await page.getByLabel("Email ünvanı").fill("test@example.invalid");
  await page.getByLabel("Şifrə", { exact: true }).fill("testpass1");
  await page.getByRole("button", { name: "Hesab yarat" }).click();
  await expect(
    page.getByRole("heading", { name: "Emailinizi təsdiqləyin." }),
  ).toBeVisible();
  await expect(
    page.getByText("Təsdiqləmə kodu göndərildi.", { exact: true }),
  ).toBeVisible();
  await page.getByLabel("Təsdiqləmə kodu").fill("123456");
  await page.getByRole("button", { name: "Təsdiqlə və daxil ol" }).click();
  await expect(page.locator(".home-heading")).toBeVisible();
  expect(
    requests.find((item) => item.path === "/auth/register").body.role,
  ).toBe("User");
});
for (const width of [1440, 1000, 799, 390, 320]) {
  test(`Responsive ${width}px: light and dark have no horizontal overflow`, async ({
    page,
  }) => {
    await mockApi(page);
    await page.setViewportSize({ width, height: 1000 });
    await login(page);
    for (const route of [
      "/home",
      "/vehicles",
      "/products",
      "/mechanics/baku-servis",
      "/chat/90",
    ]) {
      await page.goto(`/#${route}`);
      await expect(page.locator(".loading")).toHaveCount(0);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
      ).toBe(true);
    }
    await page.goto("/#/home");
    await page.getByRole("button", { name: "Temanı dəyiş" }).click();
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= window.innerWidth,
      ),
    ).toBe(true);
    await page.screenshot({
      path: `test-results/home-${width}-dark.png`,
      fullPage: true,
    });
    await page.getByRole("button", { name: "Temanı dəyiş" }).click();
    await page.screenshot({
      path: `test-results/home-${width}-light.png`,
      fullPage: true,
    });
  });
}
test("Empty state and error state remain useful", async ({ page }) => {
  await mockApi(page, "User", { empty: true, fail: true });
  await login(page);
  await expect(
    page.getByText("Hazırda planlaşdırılmış görüşünüz yoxdur."),
  ).toBeVisible();
  await page.goto("/#/vehicles");
  await expect(page.getByRole("alert")).toContainText(
    "Test üçün server xətası.",
  );
  await expect(
    page.getByRole("button", { name: "Yenidən cəhd et" }),
  ).toBeVisible();
});
test("Unprivileged routes do not expose the admin UI", async ({ page }) => {
  await mockApi(page);
  await login(page);
  await page.goto("/#/admin/users");
  await expect(
    page.getByRole("heading", { name: "Səhifə əlçatan deyil" }),
  ).toBeVisible();
});
test("Real development API proxy returns authentication errors without database mutation", async ({
  page,
}) => {
  await page.goto("/#/login");
  await page
    .getByLabel("Email ünvanı")
    .fill(`frontend-probe-${crypto.randomUUID()}@example.invalid`);
  await page.getByLabel("Şifrə", { exact: true }).fill("testpass1");
  await page.getByRole("button", { name: "Daxil ol", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText(
    "Email və ya şifrə yanlışdır.",
  );
});
