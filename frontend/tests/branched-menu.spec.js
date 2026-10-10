import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

const commonLinks = [
  ["Görüşlərim", "/appointments"],
  ["Sifarişlərim", "/orders"],
  ["Mağazalar", "/stores"],
  ["Yanacaq", "/records/fuel"],
  ["Xərclər", "/records/expenses"],
  ["Servis tarixçəsi", "/records/service"],
  ["Xatırlatmalar", "/records/reminders"],
];
for (const role of ["User"]) {
  test(`${role}: branched More navigation preserves all permitted destinations`, async ({
    page,
  }) => {
    await mockApi(page, role);
    await login(page);
    const menu = page.locator(".nav-more");
    await menu.locator("summary").click();
    const links = commonLinks;
    await expect(menu.getByRole("link")).toHaveCount(links.length);
    for (const [name, path] of links) {
      await expect(
        menu.getByRole("link", { name, exact: true }),
      ).toHaveAttribute("href", `#${path}`);
    }
    const [lastName, lastPath] = links.at(-1);
    await menu.getByRole("link", { name: lastName, exact: true }).click();
    await expect(page).toHaveURL(new RegExp(`#${lastPath}$`));
    await expect(menu).not.toHaveAttribute("open", "");
    await menu.locator("summary").click();
    await expect(
      menu.getByRole("link", { name: lastName, exact: true }),
    ).toHaveAttribute("aria-current", "page");
    await expect(page.locator(".gooey-effect")).toBeHidden();
    await expect(menu.locator(".branched-menu__marker")).toHaveAttribute(
      "data-on",
      "",
    );
  });
}

test("Disclosure supports keyboard, folding, Escape, outside click and URL-driven active state", async ({
  page,
}) => {
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await mockApi(page);
  await login(page);
  const menu = page.locator(".nav-more");
  const summary = menu.locator("summary");
  await summary.focus();
  await page.keyboard.press("Enter");
  const section = menu.getByRole("button", { name: "Planlar və alış-veriş" });
  await section.focus();
  await page.keyboard.press("Enter");
  await expect(section).toHaveAttribute("aria-expanded", "false");
  await expect(
    menu.getByRole("link", { name: "Görüşlərim", exact: true }),
  ).toHaveCount(0);
  await page.keyboard.press("Tab");
  await expect(
    menu.getByRole("button", { name: "Nəqliyyat qeydləri" }),
  ).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(summary).toBeFocused();
  await expect(summary).toHaveAttribute("aria-expanded", "false");
  await summary.click();
  await page.getByRole("heading", { level: 1 }).click();
  await expect(summary).toHaveAttribute("aria-expanded", "false");
  await page.goto("/#/records/fuel?vehicle=1");
  await expect(page).toHaveURL(/#\/garage\/toyota-corolla-test\/fuel$/);
  await summary.click();
  await expect(
    menu.getByRole("link", { name: "Yanacaq", exact: true }),
  ).toHaveAttribute("aria-current", "page");
  await expect(
    menu.getByRole("link", { name: "Xərclər", exact: true }),
  ).not.toHaveAttribute("aria-current");
  await expect(page.locator(".gooey-effect")).toBeVisible();
  await page
    .getByRole("navigation", { name: "Əsas menyu", exact: true })
    .getByRole("link", { name: "Qarajım", exact: true })
    .click();
  await expect(summary).toHaveAttribute("aria-expanded", "false");
  await page.goBack();
  await summary.click();
  await expect(
    menu.getByRole("link", { name: "Yanacaq", exact: true }),
  ).toHaveAttribute("aria-current", "page");
  expect(errors).toEqual([]);
});

for (const width of [1440, 799, 390, 320]) {
  test(`Open More menu stays inside ${width}px viewport in both themes and scrolls when short`, async ({
    page,
  }, testInfo) => {
    await mockApi(page);
    await page.setViewportSize({ width, height: 650 });
    await login(page);
    await page.goto("/#/records/service");
    const menu = page.locator(".nav-more");
    for (const theme of ["light", "dark"]) {
      await menu.locator("summary").click();
      const panel = menu.locator(".more-menu-panel");
      await expect(panel).toBeVisible();
      await page.waitForTimeout(300);
      const box = await panel.boundingBox();
      expect(box.x).toBeGreaterThanOrEqual(15);
      expect(box.x + box.width).toBeLessThanOrEqual(width - 15);
      expect(box.y + box.height).toBeLessThanOrEqual(650 - 15);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
      await expect(
        menu.getByRole("link", { name: "Servis tarixçəsi", exact: true }),
      ).toHaveAttribute("aria-current", "page");
      await page.screenshot({ path: testInfo.outputPath(`menu-${theme}.png`) });
      await page.emulateMedia({ reducedMotion: "reduce" });
      await expect(panel).toHaveCSS("animation-name", "none");
      await expect(menu.locator(".branched-menu__reach").first()).toHaveCSS(
        "transition-duration",
        "0s",
      );
      await page.emulateMedia({ reducedMotion: "no-preference" });
      await menu
        .getByRole("link", { name: "Sifarişlərim", exact: true })
        .click();
      await expect(page).toHaveURL(/#\/orders$/);
      if (theme === "light")
        await page.getByRole("button", { name: "Temanı dəyiş" }).click();
      await page.goto("/#/records/service");
    }
    await menu.locator("summary").click();
    await page.setViewportSize({ width: 320, height: 480 });
    await expect
      .poll(async () => {
        const box = await menu.locator(".more-menu-panel").boundingBox();
        return (
          box.x >= 15 && box.x + box.width <= 305 && box.y + box.height <= 465
        );
      })
      .toBe(true);
    await menu.getByRole("link", { name: "Sifarişlərim", exact: true }).click();
    await expect(page).toHaveURL(/#\/orders$/);
  });
}
