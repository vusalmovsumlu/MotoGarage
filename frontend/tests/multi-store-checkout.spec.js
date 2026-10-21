import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

const products = [
  {
    id: 30,
    storeId: 20,
    storeName: "Garage Parts",
    name: "Mühərrik yağı",
    price: 45,
    stock: 2,
  },
  {
    id: 31,
    storeId: 21,
    storeName: "Brake Parts",
    name: "Əyləc dəsti",
    price: 75,
    stock: 4,
  },
  {
    id: 32,
    storeId: 22,
    storeName: "Light Parts",
    name: "Fara",
    price: 30,
    stock: 3,
  },
  {
    id: 33,
    storeId: 20,
    storeName: "Garage Parts",
    name: "Yağ filtri",
    price: 12.5,
    stock: 5,
  },
].map((product) => ({
  ...product,
  slug: `product-${product.id}`,
  isActive: true,
}));
const address = {
  recipientName: "Vüqar Məmmədov",
  deliveryPhone: "+994501234567",
  deliveryCity: "Bakı",
  deliveryAddress: "Nizami küçəsi 10",
  deliveryNote: "Girişdə zəng edin.",
};

async function cart(page, count = 3) {
  await mockApi(page);
  await page.route(
    (url) => url.pathname === "/api/products",
    (route) =>
      route.fulfill({
        contentType: "application/json",
        body: JSON.stringify(products),
      }),
  );
  await login(page);
  await page.goto("/#/products");
  for (let index = 0; index < count; index++)
    await page
      .getByRole("button", { name: "Səbətə əlavə et" })
      .nth(index)
      .click();
  await page.goto("/#/cart");
  await expect(page.locator(".cart-store-group")).toHaveCount(3);
}

async function fillAddress(page) {
  await page.getByLabel("Telefon nömrəsi").fill(address.deliveryPhone);
  await page.getByLabel("Şəhər / rayon").fill(address.deliveryCity);
  await page.getByLabel("Tam ünvan").fill(` ${address.deliveryAddress} `);
  await page.getByLabel("Çatdırılma qeydi").fill(` ${address.deliveryNote} `);
}

function result(body) {
  const orders = body.items.map((item, index) => {
    const product = products.find((value) => value.id === item.productId);
    return {
      ...body,
      id: 71 + index,
      userId: 1,
      storeId: product.storeId,
      storeName: product.storeName,
      totalAmount: product.price * item.quantity,
      status: "Pending",
      createdAt: new Date().toISOString(),
      items: [
        {
          id: index + 1,
          ...item,
          productName: product.name,
          totalPrice: product.price * item.quantity,
        },
      ],
    };
  });
  return {
    orders,
    totalAmount: orders.reduce((sum, order) => sum + order.totalAmount, 0),
  };
}

test("Three stores use one checkout request and one address; repeated submit cannot duplicate orders", async ({
  page,
}) => {
  await cart(page);
  await page.getByLabel("Mühərrik yağı sayı", { exact: true }).fill("2");
  await expect(
    page
      .locator(".checkout-card .summary-row")
      .filter({ hasText: "Ümumi məbləğ" }),
  ).toContainText("195,00");
  await fillAddress(page);
  const calls = [];
  let release;
  let created = [];
  const pending = new Promise((resolve) => {
    release = resolve;
  });
  await page.route("**/api/orders/checkout", async (route) => {
    calls.push(route.request().postDataJSON());
    await pending;
    const response = result(calls[0]);
    created = response.orders;
    await route.fulfill({
      status: 201,
      contentType: "application/json",
      body: JSON.stringify(response),
    });
  });
  await page.route("**/api/orders/my", (route) =>
    route.fulfill({
      contentType: "application/json",
      body: JSON.stringify(created),
    }),
  );
  await page.locator(".checkout-card form").evaluate((form) => {
    form.requestSubmit();
    form.requestSubmit();
  });
  await expect.poll(() => calls.length).toBe(1);
  await expect(
    page.getByRole("button", { name: "Sifarişi təsdiqlə" }),
  ).toBeDisabled();
  await expect(page.locator(".cart-store-group")).toHaveCount(3);
  release();
  await expect(
    page.getByRole("heading", { name: "Sifarişlərim", exact: true }),
  ).toBeVisible();
  for (const product of products.slice(0, 3))
    await expect(
      page.getByRole("heading", { name: product.storeName, exact: true }),
    ).toBeVisible();
  expect(calls).toEqual([
    {
      ...address,
      items: [
        { productId: 30, quantity: 2 },
        { productId: 31, quantity: 1 },
        { productId: 32, quantity: 1 },
      ],
    },
  ]);
  await page.goto("/#/cart");
  await expect(
    page.getByRole("heading", { name: "Səbətiniz hələ boşdur" }),
  ).toBeVisible();
});

test("Failed checkout keeps every store's cart and address so the customer can retry", async ({
  page,
}) => {
  await cart(page);
  await fillAddress(page);
  let retry = false;
  const calls = [];
  await page.route("**/api/orders/checkout", (route) => {
    const body = route.request().postDataJSON();
    calls.push(body);
    return route.fulfill({
      status: retry ? 201 : 400,
      contentType: "application/json",
      body: JSON.stringify(
        retry
          ? result(body)
          : { message: "Əyləc dəsti üçün kifayət qədər stok yoxdur." },
      ),
    });
  });
  await page.getByRole("button", { name: "Sifarişi təsdiqlə" }).click();
  await expect(page.getByRole("alert")).toContainText(
    "kifayət qədər stok yoxdur",
  );
  await expect(page.locator(".cart-row")).toHaveCount(3);
  await expect(page.locator(".cart-store-group")).toHaveCount(3);
  await expect(page.getByLabel("Tam ünvan")).toHaveValue(
    ` ${address.deliveryAddress} `,
  );
  await expect(
    page.getByRole("button", { name: "Sifarişi təsdiqlə" }),
  ).toBeEnabled();
  expect(calls).toHaveLength(1);
  retry = true;
  await page.getByRole("button", { name: "Sifarişi təsdiqlə" }).click();
  await expect(
    page.getByRole("heading", { name: "Sifarişlərim", exact: true }),
  ).toBeVisible();
  expect(calls).toHaveLength(2);
  expect(calls[1]).toEqual(calls[0]);
  await page.goto("/#/cart");
  await expect(
    page.getByRole("heading", { name: "Səbətiniz hələ boşdur" }),
  ).toBeVisible();
});

test("Store totals combine that store's products and removing one group preserves other stores", async ({
  page,
}) => {
  await cart(page, 4);
  const garage = page.getByRole("region", {
    name: "Garage Parts",
    exact: true,
  });
  await expect(garage.locator(".cart-row")).toHaveCount(2);
  await expect(garage.locator(".cart-store-header")).toContainText("57,50");
  await garage.getByLabel("Mühərrik yağı sayı", { exact: true }).fill("2");
  await expect(garage.locator(".cart-store-header")).toContainText("102,50");
  await garage
    .getByRole("button", { name: "Məhsulu səbətdən çıxar" })
    .first()
    .click();
  await expect(garage.locator(".cart-row")).toHaveCount(1);
  await garage.getByRole("button", { name: "Məhsulu səbətdən çıxar" }).click();
  await expect(page.locator(".cart-store-group")).toHaveCount(2);
  await expect(
    page.getByLabel("Əyləc dəsti sayı", { exact: true }),
  ).toHaveValue("1");
  await expect(page.getByLabel("Fara sayı", { exact: true })).toHaveValue("1");
  await expect(
    page
      .locator(".checkout-card .summary-row")
      .filter({ hasText: "Ümumi məbləğ" }),
  ).toContainText("105,00");
});

for (const width of [320, 1440]) {
  test(`Multi-store cart fits ${width}px in light and dark themes`, async ({
    page,
  }) => {
    await page.setViewportSize({ width, height: 1000 });
    await cart(page, 4);
    for (const theme of ["light", "dark"]) {
      if (theme === "dark")
        await page.getByRole("button", { name: "Temanı dəyiş" }).click();
      await expect(page.locator(".cart-store-group")).toHaveCount(3);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
      ).toBe(true);
      await page.screenshot({
        path: `test-results/cart-${width}-${theme}.png`,
        fullPage: true,
      });
    }
  });
}
