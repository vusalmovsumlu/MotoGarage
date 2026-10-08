import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures";

async function holdResponse(page, path) {
  let release;
  const gate = new Promise((resolve) => {
    release = resolve;
  });
  await page.route(`**/api${path}`, async (route) => {
    await gate;
    await route.fallback();
  });
  return release;
}

for (const width of [1440, 1000, 799, 390, 320]) {
  test(`Dashboard skeleton ${width}px fits both themes and resolves to real content`, async ({
    page,
  }) => {
    await mockApi(page);
    await page.setViewportSize({ width, height: 950 });
    const release = await holdResponse(page, "/dashboard");
    try {
      await login(page);
      const skeleton = page.locator(".skeleton-dashboard");
      await expect(skeleton).toBeVisible();
      await expect(skeleton).toHaveAttribute("aria-busy", "true");
      await expect(skeleton.locator(".spin")).toHaveCount(0);
      for (const theme of ["light", "dark"]) {
        expect(
          await page.evaluate(
            () => document.documentElement.scrollWidth <= innerWidth,
          ),
        ).toBe(true);
        await page.screenshot({
          path: `test-results/skeleton-${width}-${theme}.png`,
          fullPage: true,
        });
        if (theme === "light")
          await page.getByRole("button", { name: "Temanı dəyiş" }).click();
      }
    } finally {
      release();
    }
    await expect(page.locator(".skeleton-dashboard")).toHaveCount(0);
    await expect(page.getByText("180,00 ₼", { exact: true })).toBeVisible();
  });
}

test("Page skeletons match cards, details, profile, table and chat content", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  await login(page);
  await page.setViewportSize({ width: 320, height: 900 });
  for (const [route, path, variant] of [
    ["/vehicles", "/Vehicle", "cards"],
    [
      "/garage/toyota-corolla-test",
      "/Vehicle/by-slug/toyota-corolla-test",
      "detail",
    ],
    ["/profile", "/profile", "profile"],
    ["/admin/users", "/admin/users*", "table"],
    ["/chat/90", "/chat/conversations/90/messages", "messages"],
  ]) {
    const release = await holdResponse(page, path);
    try {
      await page.goto(`/#${route}`);
      await expect(page.locator(`.skeleton-${variant}`)).toBeVisible();
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
    } finally {
      release();
    }
    await expect(page.locator(`.skeleton-${variant}`)).toHaveCount(0);
    await page.unroute(`**/api${path}`);
  }
});

test("Skeleton exits on failure and reduced motion disables shimmer", async ({
  page,
}) => {
  await mockApi(page, "User", { fail: true });
  await login(page);
  await page.emulateMedia({ reducedMotion: "reduce" });
  const release = await holdResponse(page, "/Vehicle");
  try {
    await page.goto("/#/vehicles");
    await expect(page.locator(".skeleton-cards")).toBeVisible();
    const animation = await page
      .locator(".skeleton-block")
      .first()
      .evaluate(
        (element) => getComputedStyle(element, "::after").animationName,
      );
    expect(animation).toBe("none");
  } finally {
    release();
  }
  await expect(page.locator(".skeleton-loading")).toHaveCount(0);
  await expect(page.getByRole("alert")).toContainText(
    "Test üçün server xətası.",
  );
});

test("Peek Rating previews on hover, commits by keyboard and sends numeric review rating", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/mechanics/baku-servis");
  await page.getByRole("button", { name: "Rəy yaz" }).click();
  const group = page.getByRole("radiogroup", { name: "Qiymət" });
  const fourth = group.getByRole("radio", {
    name: "4/5 ulduz, Yaxşı",
    exact: true,
  });
  await fourth.hover();
  await expect(group.locator(".peek-rating__tip")).toHaveText("Yaxşı");
  await expect(group.locator(".peek-rating__tip")).toHaveAttribute(
    "data-show",
    "true",
  );
  await expect(
    group.getByRole("radio", { checked: true }),
  ).toHaveAccessibleName("5/5 ulduz, Əla");
  await fourth.click();
  await expect(fourth).toHaveAttribute("aria-checked", "true");
  await fourth.focus();
  await page.keyboard.press("ArrowLeft");
  await expect(
    group.getByRole("radio", { checked: true }),
  ).toHaveAccessibleName("3/5 ulduz, Orta");
  await page.keyboard.press("Home");
  await page.keyboard.press("Backspace");
  await expect(
    group.getByRole("radio", { checked: true }),
  ).toHaveAccessibleName("1/5 ulduz, Çox zəif");
  await page.keyboard.press("ArrowRight");
  await page.getByRole("textbox", { name: "Rəyiniz" }).fill("Test rəyidir.");
  await page.getByRole("button", { name: "Yadda saxla" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  expect(
    requests.find((item) => item.method === "POST" && item.path === "/reviews")
      .body,
  ).toEqual({ mechanicId: 10, rating: 2, comment: "Test rəyidir." });
});

test("Peek Rating edits existing ratings and uses read-only review stars", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await page.route("**/api/reviews/mechanic/10", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify([
        {
          id: 43,
          userId: 1,
          mechanicId: 10,
          rating: 2,
          comment: "Əvvəlki rəy",
          createdAt: new Date().toISOString(),
        },
      ]),
    }),
  );
  await login(page);
  await page.goto("/#/mechanics/baku-servis");
  await expect(page.getByRole("img", { name: "2/5 ulduz" })).toBeVisible();
  await expect(page.getByRole("radiogroup")).toHaveCount(0);
  await page.getByRole("button", { name: "Rəyi dəyiş" }).click();
  const group = page.getByRole("radiogroup", { name: "Qiymət" });
  await expect(
    group.getByRole("radio", { checked: true }),
  ).toHaveAccessibleName("2/5 ulduz, Zəif");
  await group.getByRole("radio", { checked: true }).focus();
  await page.keyboard.press("End");
  await page.getByRole("button", { name: "Yadda saxla" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  expect(
    requests.find(
      (item) => item.method === "PUT" && item.path === "/reviews/43",
    ).body.rating,
  ).toBe(5);
});

test("Peek Rating supports touch and mobile layout in both themes with reduced motion", async ({
  browser,
}) => {
  const context = await browser.newContext({
    viewport: { width: 320, height: 850 },
    hasTouch: true,
    reducedMotion: "reduce",
  });
  const page = await context.newPage();
  try {
    await mockApi(page);
    await login(page);
    await page.goto("/#/mechanics/baku-servis");
    for (const theme of ["light", "dark"]) {
      if (theme === "dark")
        await page.getByRole("button", { name: "Temanı dəyiş" }).click();
      await page.getByRole("button", { name: "Rəy yaz" }).click();
      const group = page.getByRole("radiogroup", { name: "Qiymət" });
      const star = group.getByRole("radio", {
        name: "4/5 ulduz, Yaxşı",
        exact: true,
      });
      await star.tap();
      await expect(star).toHaveAttribute("aria-checked", "true");
      expect(
        await group
          .locator(".peek-rating__lift")
          .nth(3)
          .evaluate((element) => getComputedStyle(element).transform),
      ).toBe("matrix(1, 0, 0, 1, 0, 0)");
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
      await page.screenshot({
        path: `test-results/rating-320-${theme}.png`,
        fullPage: true,
      });
      await page.getByRole("button", { name: "İmtina" }).click();
    }
  } finally {
    await context.close();
  }
});
