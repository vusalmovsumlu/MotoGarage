import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

async function mount(page, props) {
  await mockApi(page);
  await page.goto("/#/login");
  await page.evaluate(async (options) => {
    const { mountToastHarness } = await import("/tests/swipe-toast-harness.js");
    window.toastTestRoot = mountToastHarness(options);
  }, props);
}

test("Existing success/error messages use SwipeToast; close and Escape remove them", async ({
  page,
}) => {
  await mockApi(page);
  await login(page);
  await page.goto("/#/products/engine-oil");
  const add = page.getByRole("button", { name: "Səbətə əlavə et" });
  await add.click();
  const region = page.locator(".toast-region");
  await expect(region.getByRole("status")).toContainText(
    "Məhsul səbətə əlavə edildi.",
  );
  await region.getByRole("button", { name: "Bildirişi bağla" }).click();
  await expect(region.locator(".swipe-toast")).toHaveCount(0);
  await add.click();
  await add.click();
  await expect(region.getByRole("alert")).toContainText(
    "Stokda olan saydan çox əlavə edə bilməzsiniz.",
  );
  await region.getByRole("alert").focus();
  await page.keyboard.press("Escape");
  await expect(region.getByRole("alert")).toHaveCount(0);
  await expect(region.getByRole("status")).toBeVisible();
});

test("Global feedback survives hover past its former 6.5 second timeout and keeps at most three messages", async ({
  page,
}) => {
  await mount(page, { feedback: true });
  const add = page.getByRole("button", { name: "Mesaj yarat" });
  await add.click();
  const card = page.locator(".toast-region .swipe-toast__card");
  await card.hover();
  await page.waitForTimeout(7000);
  await expect(card).toBeVisible();
  for (let i = 0; i < 4; i++) await add.click();
  await expect(card).toHaveCount(3);
});

test("Automatic timeout resumes after hover and keyboard focus leave", async ({
  page,
}) => {
  await mount(page, { duration: 1200 });
  const card = page.getByRole("status").filter({ hasText: "Sınaq bildirişi" });
  await card.hover();
  await page.waitForTimeout(1400);
  await expect(card).toBeVisible();
  await card.focus();
  await page.mouse.move(5, 5);
  await page.waitForTimeout(1400);
  await expect(card).toBeVisible();
  await card.evaluate((element) => element.blur());
  await expect(page.getByTestId("close-reason")).toHaveText("timeout");
});

test("A downward swipe dismisses a toast; a short drag returns it", async ({
  page,
}) => {
  await mount(page, { duration: 0 });
  const card = page.getByRole("status").filter({ hasText: "Sınaq bildirişi" });
  await expect(card).toBeVisible();
  await page.waitForTimeout(450);
  let box = await card.boundingBox();
  await page.mouse.move(box.x + 30, box.y + 20);
  await page.mouse.down();
  await page.mouse.move(box.x + 30, box.y + 32, { steps: 5 });
  await page.waitForTimeout(150);
  await page.mouse.up();
  await expect(card).toBeVisible();
  await page.waitForTimeout(550);
  box = await card.boundingBox();
  await page.mouse.move(box.x + 30, box.y + 10);
  await page.mouse.down();
  await page.mouse.move(box.x + 30, box.y + 85, { steps: 10 });
  await page.mouse.up();
  await expect(page.getByTestId("close-reason")).toHaveText("swipe");
});

test("Canceled pointer gestures keep the message and unmount cleans up animations", async ({
  page,
}) => {
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await mount(page, { duration: 0 });
  const card = page.getByRole("status").filter({ hasText: "Sınaq bildirişi" });
  await card.dispatchEvent("pointerdown", {
    pointerId: 9,
    button: 0,
    clientY: 50,
  });
  await card.dispatchEvent("pointermove", { pointerId: 9, clientY: 55 });
  await card.dispatchEvent("pointermove", { pointerId: 9, clientY: 120 });
  await card.dispatchEvent("pointercancel", { pointerId: 9, clientY: 120 });
  await page.waitForTimeout(550);
  await expect(card).toBeVisible();
  await expect(page.getByTestId("close-reason")).toBeEmpty();
  await page.evaluate(() => window.toastTestRoot.unmount());
  await page.waitForTimeout(250);
  expect(errors).toEqual([]);
});

for (const theme of ["light", "dark"]) {
  for (const width of [320, 1440]) {
    test(`Toast colors and layout match ${theme} theme at ${width}px`, async ({
      page,
    }, testInfo) => {
      await page.setViewportSize({ width, height: 900 });
      await page.emulateMedia({ reducedMotion: "reduce" });
      await mount(page, { feedback: true });
      await page.evaluate((value) => {
        document.documentElement.dataset.theme = value;
      }, theme);
      await page.getByRole("button", { name: "Mesaj yarat" }).click();
      const card = page.locator(".toast-region .swipe-toast__card");
      await expect(card).toBeVisible();
      const colors = await card.evaluate((element) => {
        const root = getComputedStyle(document.documentElement);
        const style = getComputedStyle(element);
        return {
          background: style.backgroundColor,
          color: style.color,
          surface: root.getPropertyValue("--surface").trim(),
          text: root.getPropertyValue("--text").trim(),
        };
      });
      const expectedColors = await page.evaluate(({ surface, text }) => {
        const probe = document.createElement("div");
        probe.style.cssText = `background:${surface};color:${text}`;
        document.body.append(probe);
        const result = {
          background: getComputedStyle(probe).backgroundColor,
          color: getComputedStyle(probe).color,
        };
        probe.remove();
        return result;
      }, colors);
      expect(colors.background).toBe(expectedColors.background);
      expect(colors.color).toBe(expectedColors.color);
      const box = await card.boundingBox();
      expect(box.x).toBeGreaterThanOrEqual(0);
      expect(box.x + box.width).toBeLessThanOrEqual(width);
      expect(box.y + box.height).toBeLessThanOrEqual(900);
      await page.screenshot({ path: testInfo.outputPath("toast.png") });
      await card.getByRole("button", { name: "Bildirişi bağla" }).click();
      await expect(card).toHaveCount(0);
    });
  }
}
