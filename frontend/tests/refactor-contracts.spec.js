import { test, expect } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { mockApi, login } from "./fixtures.js";

const sourceRoot = fileURLToPath(new URL("../src/", import.meta.url));
function sourceFiles(directory) {
  return fs
    .readdirSync(directory, { withFileTypes: true })
    .flatMap((entry) =>
      entry.isDirectory()
        ? sourceFiles(path.join(directory, entry.name))
        : [path.join(directory, entry.name)],
    );
}

test("Source imports resolve, shared views stay independent and the graph has no cycles", () => {
  const files = sourceFiles(sourceRoot).filter((file) => /\.jsx?$/.test(file));
  const graph = new Map();
  for (const file of files) {
    const source = fs.readFileSync(file, "utf8");
    const imports = [
      ...source.matchAll(/^import\s+(?:[^;]*?\sfrom\s+)?["']([^"']+)["'];/gm),
    ].map((match) => match[1]);
    const dependencies = imports
      .filter((value) => value.startsWith("."))
      .map((value) => path.resolve(path.dirname(file), value));
    for (const dependency of dependencies)
      expect(
        fs.existsSync(dependency),
        `${file} imports missing ${dependency}`,
      ).toBe(true);
    graph.set(
      file,
      dependencies.filter((dependency) => /\.jsx?$/.test(dependency)),
    );
    const relative = path.relative(sourceRoot, file).replaceAll("\\", "/");
    if (
      relative.startsWith("components/common/") ||
      relative.startsWith("components/ui/") ||
      relative.startsWith("components/feedback/")
    ) {
      expect(
        dependencies.filter((dependency) =>
          path
            .relative(sourceRoot, dependency)
            .replaceAll("\\", "/")
            .startsWith("features/"),
        ),
        `${relative} should receive feature adapters through props`,
      ).toEqual([]);
    }
    if (relative.startsWith("shared/")) {
      expect(
        dependencies.filter((dependency) =>
          path
            .relative(sourceRoot, dependency)
            .replaceAll("\\", "/")
            .startsWith("features/"),
        ),
      ).toEqual([]);
    }
  }
  const visited = new Set(),
    active = [];
  function visit(file) {
    if (active.includes(file))
      throw new Error(
        `Circular imports: ${[...active.slice(active.indexOf(file)), file].map((value) => path.relative(sourceRoot, value)).join(" -> ")}`,
      );
    if (visited.has(file)) return;
    active.push(file);
    for (const dependency of graph.get(file) || []) visit(dependency);
    active.pop();
    visited.add(file);
  }
  for (const file of graph.keys()) visit(file);
});

test("Feature API adapters preserve mutation URLs, verbs, bodies and anonymous authentication", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  const initialCount = requests.length;
  await page.evaluate(async () => {
    const modules = await Promise.all([
      import("/src/features/auth/api/authApi.js"),
      import("/src/features/garage/api/garageApi.js"),
      import("/src/features/profile/api/profileApi.js"),
      import("/src/features/admin/api/adminApi.js"),
      import("/src/features/notifications/api/notificationsApi.js"),
      import("/src/features/chat/api/chatApi.js"),
      import("/src/features/mechanics/api/mechanicsApi.js"),
      import("/src/features/stores/api/storesApi.js"),
      import("/src/features/products/api/productsApi.js"),
      import("/src/features/reviews/api/reviewsApi.js"),
      import("/src/features/orders/api/ordersApi.js"),
      import("/src/features/appointments/api/appointmentsApi.js"),
    ]);
    const [
      auth,
      garage,
      profile,
      admin,
      notifications,
      chat,
      mechanics,
      stores,
      products,
      reviews,
      orders,
      appointments,
    ] = modules.map((module) => Object.values(module)[0]);
    const body = { description: "Contract fixture" };
    await auth.authenticate("login", {
      email: "a@example.invalid",
      password: "pass1",
    });
    await auth.authenticate("register", {
      fullName: "A",
      email: "a@example.invalid",
      password: "pass1",
      role: "User",
    });
    await auth.authenticate("verify", {
      email: "a@example.invalid",
      code: "123456",
    });
    await auth.google("test-google-id-token");
    await auth.resend("a@example.invalid");
    await auth.logout("test-refresh");
    await garage.save({}, body);
    await garage.save({ id: 7 }, body);
    await garage.remove(7);
    await profile.update(body);
    await admin.toggleBlock({ id: 8, isBlocked: false });
    await admin.toggleBlock({ id: 8, isBlocked: true });
    await notifications.readAll();
    await notifications.read(9);
    await chat.start("mechanic", 10);
    await chat.start("store", 20);
    await chat.read(90);
    await chat.send(90, "Salam");
    for (const api of [mechanics, stores]) {
      await api.saveProfile(null, body);
      await api.saveProfile({ id: 1 }, body);
      await api.deactivateProfile();
    }
    await products.save({}, body);
    await products.save({ id: 7, slug: "oil-7" }, body);
    await products.remove("oil-7");
    await products.updateStock("oil-7", { stock: 12 });
    await reviews.save({}, 10, { rating: "4", comment: "Test" });
    await reviews.save({ id: 9 }, 10, { rating: "3", comment: "Edit" });
    await reviews.remove(9);
    await orders.create({ items: [{ productId: 7, quantity: 2 }] });
    await orders.updateStatus(5, "Confirmed");
    await orders.cancel(5);
    await appointments.create({ vehicleId: 2, mechanicId: 10 });
    await appointments.updateStatus(6, "Accepted");
    await appointments.cancel(6);
    const records = await Promise.all([
      import("/src/features/fuel/api/fuelApi.js"),
      import("/src/features/expenses/api/expensesApi.js"),
      import("/src/features/service-records/api/serviceRecordsApi.js"),
      import("/src/features/reminders/api/remindersApi.js"),
    ]);
    for (const { recordApi } of records) {
      await recordApi.save({}, "2", body);
      await recordApi.save({ id: 11 }, "2", body);
      await recordApi.remove(11);
    }
    await records[3].recordApi.complete(11);
  });
  const body = { description: "Contract fixture" };
  const expected = [
    ["/auth/login", "POST", { email: "a@example.invalid", password: "pass1" }],
    [
      "/auth/register",
      "POST",
      {
        fullName: "A",
        email: "a@example.invalid",
        password: "pass1",
        role: "User",
      },
    ],
    [
      "/auth/verify-email",
      "POST",
      { email: "a@example.invalid", code: "123456" },
    ],
    ["/auth/google", "POST", { idToken: "test-google-id-token" }],
    ["/auth/resend-verification-code", "POST", { email: "a@example.invalid" }],
    ["/auth/logout", "POST", { refreshToken: "test-refresh" }],
    ["/Vehicle", "POST", body],
    ["/Vehicle/7", "PUT", body],
    ["/Vehicle/7", "DELETE", null],
    ["/profile", "PUT", body],
    ["/admin/users/8/block", "PATCH", null],
    ["/admin/users/8/unblock", "PATCH", null],
    ["/notifications/read-all", "PATCH", null],
    ["/notifications/9/read", "PATCH", null],
    ["/chat/mechanic/10", "POST", null],
    ["/chat/store/20", "POST", null],
    ["/chat/conversations/90/read", "PATCH", null],
    ["/chat/conversations/90/messages", "POST", { content: "Salam" }],
    ...["mechanics", "stores"].flatMap((type) => [
      [`/${type}`, "POST", body],
      [`/${type}/my-profile`, "PUT", body],
      [`/${type}/my-profile`, "DELETE", null],
    ]),
    ["/products", "POST", body],
    ["/products/oil-7", "PUT", body],
    ["/products/oil-7", "DELETE", null],
    ["/products/oil-7/stock", "PATCH", { stock: 12 }],
    ["/reviews", "POST", { rating: 4, comment: "Test", mechanicId: 10 }],
    ["/reviews/9", "PUT", { rating: 3, comment: "Edit" }],
    ["/reviews/9", "DELETE", null],
    ["/orders", "POST", { items: [{ productId: 7, quantity: 2 }] }],
    ["/orders/5/status", "PATCH", { status: "Confirmed" }],
    ["/orders/5/cancel", "PATCH", null],
    ["/appointments", "POST", { vehicleId: 2, mechanicId: 10 }],
    ["/appointments/6/status", "PATCH", { status: "Accepted" }],
    ["/appointments/6/cancel", "PATCH", null],
    ...["fuel-records", "expenses", "service-records", "reminders"].flatMap(
      (type) => [
        [`/${type}`, "POST", { ...body, vehicleId: 2 }],
        [`/${type}/11`, "PUT", body],
        [`/${type}/11`, "DELETE", null],
      ],
    ),
    ["/reminders/11/complete", "PATCH", null],
  ];
  const writes = requests
    .slice(initialCount)
    .filter((request) => request.method !== "GET");
  expect(writes.map(({ path, method, body }) => [path, method, body])).toEqual(
    expected,
  );
  for (const request of writes.slice(0, 5))
    expect(request.authorization).toBeUndefined();
  for (const request of writes.slice(5))
    expect(request.authorization).toMatch(/^Bearer /);
});

test("Query builders preserve false/zero, filter order and encoded detail slugs", async ({
  page,
}) => {
  await mockApi(page);
  await page.goto("/#/login");
  const result = await page.evaluate(async () => {
    const { query } = await import("/src/shared/utils/query.js");
    const { productsApi } =
      await import("/src/features/products/api/productsApi.js");
    const { recordApi: expenses } =
      await import("/src/features/expenses/api/expensesApi.js");
    const { recordApi: fuel } =
      await import("/src/features/fuel/api/fuelApi.js");
    const { notificationsApi } =
      await import("/src/features/notifications/api/notificationsApi.js");
    const { ordersApi } = await import("/src/features/orders/api/ordersApi.js");
    const { appointmentsApi } =
      await import("/src/features/appointments/api/appointmentsApi.js");
    return [
      query({
        empty: "",
        nil: null,
        missing: undefined,
        zero: 0,
        flag: false,
        term: "yağ & filter",
      }),
      productsApi.listPath({
        search: "oil",
        brand: "A B",
        minPrice: 0,
        maxPrice: 20,
      }),
      productsApi.detailPath("yağ/x"),
      productsApi.storeProductsPath("mağaza/x"),
      productsApi.storeProductsPath("mağaza/x", false),
      expenses.listPath("2", {
        startDate: "2026-10-01",
        endDate: "2026-10-08",
      }),
      fuel.listPath("2"),
      fuel.statsPath("2", { startDate: "2026-10-01", endDate: "2026-10-08" }),
      notificationsApi.listPath(false),
      ordersApi.listPath(true),
      ordersApi.listPath(false),
      appointmentsApi.listPath(true),
      appointmentsApi.listPath(false),
    ];
  });
  expect(result).toEqual([
    "?zero=0&flag=false&term=ya%C4%9F+%26+filter",
    "/products?search=oil&brand=A+B&minPrice=0&maxPrice=20",
    "/products/ya%C4%9F%2Fx",
    "/products/store/ma%C4%9Faza%2Fx",
    "/products/store/mağaza/x",
    "/expenses?vehicleId=2&startDate=2026-10-01&endDate=2026-10-08",
    "/fuel-records/vehicle/2",
    "/fuel-records/stats?vehicleId=2&startDate=2026-10-01&endDate=2026-10-08",
    "/notifications?unreadOnly=false",
    "/orders/store",
    "/orders/my",
    "/appointments/mechanic",
    "/appointments/my",
  ]);
});

test("Navigation links preserve slug destinations and the selected vehicle query across record features", async ({
  page,
}) => {
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/vehicles/2");
  await page
    .getByRole("link", { name: "Yanacaq Doldurma və sərfiyyat" })
    .click();
  await expect(page).toHaveURL(/#\/garage\/cfmoto-450-sr-test\/fuel$/);
  await expect(
    page.getByRole("combobox", { name: "Nəqliyyat vasitəsi" }),
  ).toHaveValue("2");
  for (const kind of ["expenses", "service", "reminders"]) {
    await page.goto(`/#/records/${kind}?vehicle=2`);
    await expect(
      page.getByRole("combobox", { name: "Nəqliyyat vasitəsi" }),
    ).toHaveValue("2");
    await expect(page.locator(".loading")).toHaveCount(0);
    await expect(page.locator(".error-panel")).toHaveCount(0);
  }
  expect(
    requests.some(
      (request) =>
        request.path === "/expenses" && request.query.vehicleId === "2",
    ),
  ).toBe(true);
  expect(
    requests.some((request) => request.path === "/service-records/vehicle/2"),
  ).toBe(true);
  expect(
    requests.some((request) => request.path === "/reminders/vehicle/2"),
  ).toBe(true);
  await page.goto("/#/mechanics");
  await page.getByRole("link", { name: "Ətraflı" }).click();
  await expect(page).toHaveURL(/#\/mechanics\/baku-servis$/);
  await expect(
    page.getByRole("heading", { level: 1, name: "Baku Auto & Moto Servis" }),
  ).toBeVisible();
  await page.goto("/#/stores");
  await page.getByRole("link", { name: "Ətraflı" }).click();
  await expect(page).toHaveURL(/#\/stores\/garage-parts$/);
  await expect(
    page.getByRole("heading", { level: 1, name: "Garage Parts" }),
  ).toBeVisible();
  expect(errors).toEqual([]);
});

for (const [kind, endpoint, id, fields, changedField] of [
  [
    "fuel",
    "/fuel-records",
    40,
    [
      ["Yanacaq (litr)", "20"],
      ["Litrin qiyməti (₼)", "1.2"],
      ["Odometr (km)", "25000"],
    ],
    ["Yanacaq (litr)", "35"],
  ],
  [
    "expenses",
    "/expenses",
    41,
    [
      ["Məbləğ (₼)", "80"],
      ["Açıqlama", "Test xərc"],
    ],
    ["Məbləğ (₼)", "90"],
  ],
  [
    "service",
    "/service-records",
    42,
    [
      ["Servisin adı", "Yağ dəyişimi"],
      ["Xərc (₼)", "80"],
      ["Odometr (km)", "25000"],
    ],
    ["Xərc (₼)", "100"],
  ],
  [
    "reminders",
    "/reminders",
    50,
    [
      ["Başlıq", "Texniki baxış"],
      ["Odometr həddi (km)", "26000"],
    ],
    ["Başlıq", "Növbəti texniki baxış"],
  ],
]) {
  test(`${kind}: shared record editor preserves create/update/delete contracts and vehicle ownership fields`, async ({
    page,
  }) => {
    const { requests } = await mockApi(page);
    await login(page);
    await page.goto(`/#/records/${kind}?vehicle=2`);
    await page.getByRole("button", { name: "Qeyd əlavə et" }).click();
    const dialog = page.getByRole("dialog");
    for (const [label, value] of fields)
      await dialog.getByLabel(label).fill(value);
    await dialog.getByRole("button", { name: "Yadda saxla" }).click();
    await expect(dialog).toHaveCount(0);
    const created = requests.find(
      (request) => request.path === endpoint && request.method === "POST",
    );
    expect(created.body.vehicleId).toBe(2);
    expect(created.body.id).toBeUndefined();
    await page.getByRole("button", { name: "Qeydi dəyiş" }).first().click();
    await dialog.getByLabel(changedField[0]).fill(changedField[1]);
    await dialog.getByRole("button", { name: "Yadda saxla" }).click();
    await expect(dialog).toHaveCount(0);
    const updated = requests.find(
      (request) =>
        request.path === `${endpoint}/${id}` && request.method === "PUT",
    );
    expect(updated).toBeDefined();
    expect(updated.body.vehicleId).toBeUndefined();
    if (kind === "reminders") {
      await page.getByRole("button", { name: "Tamamla", exact: true }).click();
      await expect
        .poll(() =>
          requests.some(
            (request) =>
              request.path === `/reminders/${id}/complete` &&
              request.method === "PATCH",
          ),
        )
        .toBe(true);
    }
    await page.getByRole("button", { name: "Qeydi sil" }).first().click();
    expect(requests.some((request) => request.method === "DELETE")).toBe(false);
    await dialog.getByRole("button", { name: "Təsdiqlə" }).click();
    await expect(dialog).toHaveCount(0);
    expect(
      requests.some(
        (request) =>
          request.path === `${endpoint}/${id}` && request.method === "DELETE",
      ),
    ).toBe(true);
  });
}

test("Date filters validate the range and preserve expense/fuel request queries", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  for (const kind of ["expenses", "fuel"]) {
    await page.goto(`/#/records/${kind}?vehicle=2`);
    await page.getByLabel("Başlanğıc tarixi").fill("2026-01-01");
    await page.getByRole("button", { name: "Filtrlə", exact: true }).click();
    await expect(page.getByRole("alert")).toHaveText(
      "Hər iki tarixi düzgün ardıcıllıqla seçin.",
    );
    await page.getByLabel("Son tarix").fill("2026-12-31");
    await page.getByRole("button", { name: "Filtrlə", exact: true }).click();
    await expect(page.getByRole("alert")).toHaveCount(0);
    const endpoint = kind === "expenses" ? "/expenses" : "/fuel-records/stats";
    await expect
      .poll(() =>
        requests.some(
          (request) =>
            request.path === endpoint &&
            request.query.vehicleId === "2" &&
            request.query.startDate === "2026-01-01" &&
            request.query.endDate === "2026-12-31",
        ),
      )
      .toBe(true);
    await page.getByRole("button", { name: "Sıfırla", exact: true }).click();
    await expect(page.getByLabel("Başlanğıc tarixi")).toHaveValue("");
    await expect(page.getByLabel("Son tarix")).toHaveValue("");
  }
});
