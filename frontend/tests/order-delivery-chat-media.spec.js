import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

const png = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=",
  "base64",
);
const id = "1bf2b48e-e9de-476a-93dc-947f9e5646cb";
const address = {
  recipientName: "Vüqar Məmmədov",
  deliveryPhone: "+994501234567",
  deliveryCity: "Bakı",
  deliveryAddress: "Nizami küçəsi 10, mənzil 3",
  deliveryNote: "Girişdə zəng edin.",
};
async function fillAddress(page) {
  await page.getByLabel("Telefon nömrəsi").fill(address.deliveryPhone);
  await page.getByLabel("Şəhər / rayon").fill(address.deliveryCity);
  await page.getByLabel("Tam ünvan").fill(` ${address.deliveryAddress} `);
  await page.getByLabel("Çatdırılma qeydi").fill(` ${address.deliveryNote} `);
}

test("Checkout requires a real address and sends trimmed delivery details without prices", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/products");
  await page.getByRole("button", { name: "Səbətə əlavə et" }).first().click();
  await page.goto("/#/cart");
  await page.getByRole("button", { name: "Sifarişi təsdiqlə" }).click();
  expect(
    requests.some(
      (request) =>
        request.path === "/orders/checkout" && request.method === "POST",
    ),
  ).toBe(false);
  await fillAddress(page);
  await page.getByLabel("Telefon nömrəsi").fill("-------");
  await page.getByRole("button", { name: "Sifarişi təsdiqlə" }).click();
  await expect(page.getByRole("alert")).toContainText("Düzgün telefon");
  expect(
    requests.some(
      (request) =>
        request.path === "/orders/checkout" && request.method === "POST",
    ),
  ).toBe(false);
  await page.getByLabel("Telefon nömrəsi").fill(address.deliveryPhone);
  await page.getByRole("button", { name: "Sifarişi təsdiqlə" }).click();
  await expect(
    page.getByRole("heading", { name: "Sifarişlərim" }),
  ).toBeVisible();
  expect(
    requests.find(
      (request) =>
        request.path === "/orders/checkout" && request.method === "POST",
    ).body,
  ).toEqual({ ...address, items: [{ productId: 30, quantity: 1 }] });
});

for (const role of ["User", "Store"]) {
  test(`${role} sees order delivery snapshot with phone and notes`, async ({
    page,
  }) => {
    await mockApi(page, role);
    await page.route(
      (url) =>
        url.pathname === "/api/orders/my" ||
        url.pathname === "/api/orders/store",
      (route) =>
        route.fulfill({
          contentType: "application/json",
          body: JSON.stringify([
            {
              id: 70,
              userId: 1,
              storeId: 20,
              totalAmount: 45,
              status: "Pending",
              createdAt: new Date().toISOString(),
              items: [],
              ...address,
            },
          ]),
        }),
    );
    await login(page);
    await page.goto(role === "Store" ? "/#/work/orders" : "/#/orders");
    const delivery = page.getByRole("region", {
      name: "Çatdırılma məlumatları",
    });
    await expect(delivery).toContainText(address.deliveryAddress);
    await expect(delivery).toContainText(address.recipientName);
    await expect(delivery).toContainText(address.deliveryNote);
    await expect(
      delivery.getByRole("link", { name: address.deliveryPhone }),
    ).toHaveAttribute("href", `tel:${address.deliveryPhone}`);
  });
}

async function attachmentApi(page, kind = "image", uploadFails = false) {
  const calls = [];
  const file = {
    id,
    kind,
    contentType: kind === "video" ? "video/mp4" : "image/png",
    fileName: kind === "video" ? "clip.mp4" : "photo.png",
    sizeBytes: kind === "video" ? 12 : png.length,
  };
  const record = {
    id: 200,
    conversationId: 90,
    senderUserId: 1,
    content: "",
    sentAt: new Date().toISOString(),
    status: "Sent",
    attachment: file,
  };
  await page.route(
    "**/api/chat/conversations/90/attachments**",
    async (route) => {
      calls.push({
        method: route.request().method(),
        path: new URL(route.request().url()).pathname,
        headers: route.request().headers(),
      });
      if (route.request().method() === "POST")
        return route.fulfill({
          status: uploadFails ? 500 : 200,
          contentType: "application/json",
          body: JSON.stringify(
            uploadFails ? { message: "Faylı yükləmək mümkün olmadı." } : file,
          ),
        });
      if (route.request().method() === "DELETE")
        return route.fulfill({ status: 204 });
      return route.fulfill({
        contentType: file.contentType,
        body:
          kind === "video"
            ? Buffer.from([0, 0, 0, 24, 102, 116, 121, 112, 109, 112, 52, 50])
            : png,
      });
    },
  );
  await page.route("**/api/chat/conversations/90/messages", (route) => {
    if (route.request().method() === "GET")
      return route.fulfill({ contentType: "application/json", body: "[]" });
    const body = route.request().postDataJSON();
    calls.push({ method: "SEND", body });
    return route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({ ...record, content: body.content }),
    });
  });
  return { file, calls, record };
}

for (const kind of ["image", "video"]) {
  test(`Chat sends ${kind} without text and loads it through authenticated API`, async ({
    page,
  }) => {
    await mockApi(page);
    const { file, calls } = await attachmentApi(page, kind);
    await login(page);
    await page.goto("/#/chat/moto-parts");
    await expect(page.getByLabel("Mesajınız")).toBeEnabled();
    await page.getByLabel("Şəkil və ya video əlavə et").setInputFiles({
      name: file.fileName,
      mimeType: file.contentType,
      buffer:
        kind === "image"
          ? png
          : Buffer.from([0, 0, 0, 24, 102, 116, 121, 112, 109, 112, 52, 50]),
    });
    await expect(page.getByText("Göndərilməyə hazırdır")).toBeVisible();
    await page.getByRole("button", { name: "Göndər", exact: true }).click();
    if (kind === "video") {
      await expect(
        page.getByRole("button", { name: "Videonu aç" }),
      ).toBeVisible();
      expect(calls.some((call) => call.method === "GET")).toBe(false);
      await page.getByRole("button", { name: "Videonu aç" }).click();
      await expect(page.locator(".message video")).toHaveAttribute(
        "src",
        /^blob:/,
      );
      await expect(page.locator(".message video")).toHaveAttribute(
        "controls",
        "",
      );
    } else {
      await expect(page.getByAltText("photo.png")).toHaveAttribute(
        "src",
        /^blob:/,
      );
      await expect
        .poll(() =>
          page
            .getByAltText("photo.png")
            .evaluate((image) => image.naturalWidth),
        )
        .toBeGreaterThan(0);
    }
    expect(calls.find((call) => call.method === "SEND").body).toEqual({
      content: "",
      attachmentId: id,
    });
    expect(
      calls.find((call) => call.method === "GET").headers.authorization,
    ).toMatch(/^Bearer /);
    await expect(page.locator(".chat-attachment-preview")).toHaveCount(0);
    await expect(
      page.getByRole("button", { name: "Göndər", exact: true }),
    ).toBeDisabled();
  });
}

test("Unsupported files are rejected and removing an unsent attachment calls cleanup", async ({
  page,
}) => {
  await mockApi(page);
  const { calls } = await attachmentApi(page);
  await login(page);
  await page.goto("/#/chat/moto-parts");
  const picker = page.getByLabel("Şəkil və ya video əlavə et");
  await expect(picker).toBeEnabled();
  await picker.setInputFiles({
    name: "bad.svg",
    mimeType: "image/svg+xml",
    buffer: Buffer.from("<svg/>"),
  });
  await expect(page.getByRole("alert")).toContainText("JPG, PNG, WEBP");
  expect(calls).toHaveLength(0);
  await picker.setInputFiles({
    name: "large.png",
    mimeType: "image/png",
    buffer: Buffer.alloc(5 * 1024 * 1024 + 1),
  });
  expect(calls).toHaveLength(0);
  await picker.setInputFiles({
    name: "photo.png",
    mimeType: "image/png",
    buffer: png,
  });
  await expect(page.getByText("Göndərilməyə hazırdır")).toBeVisible();
  await page.getByRole("button", { name: "Əlavəni sil" }).click();
  await expect(page.locator(".chat-attachment-preview")).toHaveCount(0);
  expect(
    calls.some((call) => call.method === "DELETE" && call.path.endsWith(id)),
  ).toBe(true);
});

test("Failed upload keeps draft text and does not send a broken attachment", async ({
  page,
}) => {
  await mockApi(page);
  const { calls } = await attachmentApi(page, "image", true);
  await login(page);
  await page.goto("/#/chat/moto-parts");
  await page.getByLabel("Mesajınız").fill("Bu detala baxın.");
  await page
    .getByLabel("Şəkil və ya video əlavə et")
    .setInputFiles({ name: "photo.png", mimeType: "image/png", buffer: png });
  await expect(page.getByRole("alert")).toContainText(
    "Faylı yükləmək mümkün olmadı.",
  );
  await expect(page.getByLabel("Mesajınız")).toHaveValue("Bu detala baxın.");
  await expect(page.locator(".chat-attachment-preview")).toHaveCount(0);
  expect(calls.some((call) => call.method === "SEND")).toBe(false);
});

test("Live media messages send the attachment DTO and duplicate broadcasts render once", async ({
  page,
}) => {
  await mockApi(page);
  const { calls, record } = await attachmentApi(page);
  let sent;
  await page.route("**/hubs/chat/negotiate**", (route) =>
    route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({
        negotiateVersion: 1,
        connectionId: "media",
        connectionToken: "media",
        availableTransports: [
          { transport: "WebSockets", transferFormats: ["Text"] },
        ],
      }),
    }),
  );
  await page.routeWebSocket(/\/hubs\/chat/, (socket) =>
    socket.onMessage((data) => {
      for (const frame of String(data).split("\x1e").filter(Boolean)) {
        const request = JSON.parse(frame);
        if (request.protocol) socket.send("{}\x1e");
        else if (request.type === 1) {
          if (request.target === "SendMediaMessage") {
            sent = request.arguments;
            socket.send(
              JSON.stringify({
                type: 1,
                target: "ReceiveMessage",
                arguments: [record],
              }) + "\x1e",
            );
            socket.send(
              JSON.stringify({
                type: 3,
                invocationId: request.invocationId,
                result: record,
              }) + "\x1e",
            );
          } else
            socket.send(
              JSON.stringify({ type: 3, invocationId: request.invocationId }) +
                "\x1e",
            );
        }
      }
    }),
  );
  await login(page);
  await page.goto("/#/chat/moto-parts");
  await expect(page.locator(".connection-status")).toContainText(
    "Canlı bağlantı",
  );
  await page
    .getByLabel("Şəkil və ya video əlavə et")
    .setInputFiles({ name: "photo.png", mimeType: "image/png", buffer: png });
  await expect(page.getByText("Göndərilməyə hazırdır")).toBeVisible();
  await page.getByRole("button", { name: "Göndər", exact: true }).click();
  await expect(page.getByAltText("photo.png")).toHaveCount(1);
  expect(sent).toEqual([90, { content: "", attachmentId: id }]);
  expect(calls.some((call) => call.method === "SEND")).toBe(false);
});

test("Checkout and media chat fit mobile light and dark themes", async ({
  page,
}, testInfo) => {
  await page.setViewportSize({ width: 320, height: 900 });
  await mockApi(page);
  await attachmentApi(page);
  await login(page);
  await page.goto("/#/products");
  await page.getByRole("button", { name: "Səbətə əlavə et" }).first().click();
  await page.goto("/#/cart");
  await fillAddress(page);
  for (const theme of ["light", "dark"]) {
    await page.evaluate(
      (value) => (document.documentElement.dataset.theme = value),
      theme,
    );
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
    await page.screenshot({
      path: testInfo.outputPath(`checkout-${theme}-320.png`),
      fullPage: true,
    });
  }
  await page.goto("/#/chat/moto-parts");
  await page
    .getByLabel("Şəkil və ya video əlavə et")
    .setInputFiles({ name: "photo.png", mimeType: "image/png", buffer: png });
  await expect(page.getByText("Göndərilməyə hazırdır")).toBeVisible();
  for (const theme of ["light", "dark"]) {
    await page.evaluate(
      (value) => (document.documentElement.dataset.theme = value),
      theme,
    );
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
    await page.screenshot({
      path: testInfo.outputPath(`chat-media-${theme}-320.png`),
      fullPage: true,
    });
  }
});
