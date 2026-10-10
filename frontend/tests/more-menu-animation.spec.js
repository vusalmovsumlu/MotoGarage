import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

test("More menu animates out before hiding, disables closing links and smoothly reverses", async ({
  page,
}) => {
  await mockApi(page);
  await login(page);
  const menu = page.locator(".nav-more");
  const panel = menu.locator(".more-menu-panel");
  const summary = menu.locator("summary");
  await summary.click();
  await expect(panel).toHaveCSS("opacity", "1");
  const closing = await menu.evaluate(async (element) => {
    element.querySelector("summary").click();
    await new Promise(requestAnimationFrame);
    const panel = element.querySelector(".more-menu-panel");
    return {
      present: element.open,
      state: panel.dataset.state,
      inert: panel.inert,
      duration: panel.getAnimations()[0]?.effect.getTiming().duration,
    };
  });
  expect(closing).toEqual({
    present: true,
    state: "closed",
    inert: true,
    duration: 180,
  });
  await expect(menu.getByRole("link")).toHaveCount(0);
  await expect(panel).toBeHidden();
  await summary.click();
  await expect(panel).toHaveCSS("opacity", "1");
  await menu.evaluate(async (element) => {
    const summary = element.querySelector("summary");
    summary.click();
    await new Promise((resolve) => setTimeout(resolve, 40));
    summary.click();
  });
  await expect(panel).toHaveCSS("opacity", "1");
  await expect(summary).toHaveAttribute("aria-expanded", "true");
  await expect(menu).toHaveAttribute("open", "");
  await expect(
    menu.getByRole("link", { name: "Görüşlərim", exact: true }),
  ).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(panel).toBeHidden();
  await expect(summary).toBeFocused();
});

test("Reduced motion opens and closes the menu immediately on mobile", async ({
  page,
}) => {
  await page.setViewportSize({ width: 320, height: 650 });
  await page.emulateMedia({ reducedMotion: "reduce" });
  await mockApi(page);
  await login(page);
  const menu = page.locator(".nav-more");
  const panel = menu.locator(".more-menu-panel");
  const summary = menu.locator("summary");
  await summary.click();
  await expect(panel).toHaveCSS("opacity", "1");
  expect(
    await panel.evaluate((element) =>
      element
        .getAnimations()
        .every((animation) => animation.effect.getTiming().duration === 0),
    ),
  ).toBe(true);
  await summary.click();
  await expect(panel).toBeHidden();
  await expect(summary).toHaveAttribute("aria-expanded", "false");
});
