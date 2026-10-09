import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures";

test("Separate required first and last names preserve the registration FullName contract", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await page.goto("/#/register");
  await page.getByLabel("Ad", { exact: true }).fill("  Vüsal  ");
  await page.getByLabel("Email ünvanı").fill("name@example.invalid");
  await page.getByLabel("Şifrə", { exact: true }).fill("testpass1");
  await page.getByRole("button", { name: "Hesab yarat" }).click();
  expect(requests.some((item) => item.path === "/auth/register")).toBe(false);
  await page.getByLabel("Soyad", { exact: true }).fill("  ");
  await page.getByRole("button", { name: "Hesab yarat" }).click();
  await expect(page.getByRole("alert")).toContainText("Ad və soyadınızı ayrı");
  await page.getByLabel("Soyad", { exact: true }).fill("  Mövsümlü  ");
  await page.getByRole("button", { name: "Hesab yarat" }).click();
  await expect(
    page.getByRole("heading", { name: "Emailinizi təsdiqləyin." }),
  ).toBeVisible();
  expect(
    requests.find((item) => item.path === "/auth/register").body,
  ).toMatchObject({ fullName: "Vüsal Mövsümlü", role: "User" });
});

test("CodeSlots supports keyboard edits and numeric paste, and sends only a full six digit code", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await page.goto("/#/verify");
  await page.getByLabel("Email ünvanı").fill("test@example.invalid");
  const input = page.getByLabel("Təsdiqləmə kodu");
  await input.focus();
  await input.pressSequentially("12");
  await page.getByRole("button", { name: "Təsdiqlə və daxil ol" }).click();
  await expect(page.getByRole("alert")).toContainText("6 rəqəmli");
  expect(requests.some((item) => item.path === "/auth/verify-email")).toBe(
    false,
  );
  await input.focus();
  await page.keyboard.press("ArrowLeft");
  await page.keyboard.press("Backspace");
  await expect(page.locator(".code-slots__slot[data-filled]")).toHaveCount(1);
  await input.evaluate((element) => {
    const data = new DataTransfer();
    data.setData("text/plain", "a123-456z");
    element.dispatchEvent(
      new ClipboardEvent("paste", {
        clipboardData: data,
        bubbles: true,
        cancelable: true,
      }),
    );
  });
  await expect(page.locator(".code-slots__slot[data-filled]")).toHaveCount(6);
  await page.getByRole("button", { name: "Təsdiqlə və daxil ol" }).click();
  await expect(page.locator(".home-heading")).toBeVisible();
  expect(
    requests.find((item) => item.path === "/auth/verify-email").body,
  ).toEqual({ email: "test@example.invalid", code: "123456" });
});

test("Rejected OTP animates out, clears, allows retry and disables edits during verification", async ({
  page,
}) => {
  const { auth } = await mockApi(page);
  let submitted = 0;
  let release;
  const waiting = new Promise((resolve) => {
    release = resolve;
  });
  await page.route("**/api/auth/verify-email", async (route) => {
    submitted++;
    if (submitted === 1)
      return route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({ message: "Təsdiqləmə kodu yanlışdır." }),
      });
    await waiting;
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(auth),
    });
  });
  await page.goto("/#/verify");
  await page.getByLabel("Email ünvanı").fill("test@example.invalid");
  const input = page.getByLabel("Təsdiqləmə kodu");
  await input.fill("111111");
  await page.getByRole("button", { name: "Təsdiqlə və daxil ol" }).click();
  await expect(page.getByRole("alert")).toContainText("kodu yanlışdır");
  await expect(page.locator(".code-slots__slot[data-filled]")).toHaveCount(0);
  await input.fill("123456");
  await page.getByRole("button", { name: "Təsdiqlə və daxil ol" }).click();
  await expect(input).toBeDisabled();
  await expect(
    page.getByRole("button", { name: "Təsdiqlə və daxil ol" }),
  ).toBeDisabled();
  release();
  await expect(page.locator(".home-heading")).toBeVisible();
  expect(submitted).toBe(2);
});

test("CodeSlots fits small screens in both themes and honors reduced motion", async ({
  page,
}) => {
  await mockApi(page);
  await page.setViewportSize({ width: 320, height: 850 });
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/#/verify");
  await page.getByLabel("Təsdiqləmə kodu").fill("123456");
  for (let theme = 0; theme < 2; theme++) {
    await expect(page.locator(".code-slots__slot[data-filled]")).toHaveCount(6);
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
    expect(
      await page
        .locator(".code-slots__caret-line")
        .evaluate((element) => getComputedStyle(element).animationName),
    ).toBe("none");
    await page.screenshot({
      path: `test-results/otp-320-${theme}.png`,
      fullPage: true,
    });
    await page.getByRole("button", { name: "Temanı dəyiş" }).click();
  }
});

test("Mechanic reviews display actual authors including the signed in user", async ({
  page,
}) => {
  await mockApi(page);
  await page.route("**/api/reviews/mechanic/10", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify([
        {
          id: 43,
          userId: 2,
          userFullName: "Vüsal Mövsümlü",
          mechanicId: 10,
          rating: 5,
          comment: "Yaxşı xidmət",
          createdAt: new Date().toISOString(),
        },
        {
          id: 44,
          userId: 1,
          userFullName: "Vüqar Məmmədov",
          mechanicId: 10,
          rating: 4,
          comment: "Mənim rəyim",
          createdAt: new Date().toISOString(),
        },
      ]),
    }),
  );
  await login(page);
  await page.goto("/#/mechanics/baku-servis");
  const cards = page.locator(".review-card");
  await expect(cards.locator("strong")).toHaveText([
    "Vüsal Mövsümlü",
    "Vüqar Məmmədov",
  ]);
  await expect(cards.getByText(/İstifadəçi #|Sizin rəyiniz/)).toHaveCount(0);
  await expect(
    cards.nth(0).getByRole("button", { name: "Rəyi dəyiş" }),
  ).toHaveCount(0);
  await expect(
    cards.nth(1).getByRole("button", { name: "Rəyi dəyiş" }),
  ).toBeVisible();
});
