import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures.js";

async function liveHub(page) {
  const sockets = new Set();
  const calls = [];
  await page.route("**/hubs/chat/negotiate**", (route) =>
    route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({
        negotiateVersion: 1,
        connectionId: "live-test",
        connectionToken: "live-test",
        availableTransports: [
          { transport: "WebSockets", transferFormats: ["Text", "Binary"] },
        ],
      }),
    }),
  );
  await page.routeWebSocket(/\/hubs\/chat/, (socket) => {
    sockets.add(socket);
    socket.onClose(() => sockets.delete(socket));
    socket.onMessage((data) => {
      for (const frame of String(data).split("\x1e").filter(Boolean)) {
        const message = JSON.parse(frame);
        if (message.protocol) socket.send("{}\x1e");
        else if (message.type === 1) {
          calls.push(message);
          socket.send(
            JSON.stringify({ type: 3, invocationId: message.invocationId }) +
              "\x1e",
          );
        }
      }
    });
  });
  return {
    sockets,
    calls,
    notify() {
      for (const socket of sockets)
        socket.send(
          JSON.stringify({
            type: 1,
            target: "NotificationsChanged",
            arguments: [],
          }) + "\x1e",
        );
    },
  };
}

for (const role of ["Mechanic", "Store"]) {
  test(`${role} chat uses customer name and canonical name URL, including old notification links`, async ({
    page,
  }) => {
    const { requests } = await mockApi(page, role);
    await login(page);
    await page.goto("/#/chat/90");
    await expect(page).toHaveURL(/#\/chat\/vuqar-memmedov$/);
    await expect(page.locator(".chat-header h2")).toHaveText("Vüqar Məmmədov");
    await expect(page.locator(".conversation-item strong")).toHaveText(
      "Vüqar Məmmədov",
    );
    await expect(page.getByText(/Müştəri #/)).toHaveCount(0);
    await expect(
      page.getByText("Salam. Sizə necə kömək edə bilərik?"),
    ).toBeVisible();
    await page.getByLabel("Mesajınız").fill("Salam!");
    await page.getByRole("button", { name: "Göndər", exact: true }).click();
    await expect(page.getByText("Salam!", { exact: true })).toHaveCount(1);
    expect(
      requests.some((request) =>
        ["/mechanics", "/stores"].includes(request.path),
      ),
    ).toBe(false);
    await page.reload();
    await expect(page.locator(".chat-header h2")).toHaveText("Vüqar Məmmədov");
  });

  test(`${role} receives live notifications outside chat and updates the open list without polling`, async ({
    page,
  }) => {
    await mockApi(page, role);
    const hub = await liveHub(page);
    let due = false;
    await page.route("**/api/notifications/unread-count", (route) =>
      route.fulfill({
        contentType: "application/json",
        body: JSON.stringify({ unreadCount: due ? 1 : 0 }),
      }),
    );
    await page.route(
      (url) => url.pathname === "/api/notifications",
      (route) =>
        route.fulfill({
          contentType: "application/json",
          body: JSON.stringify(
            due
              ? [
                  {
                    id: 500,
                    title: "Yeni biznes bildirişi",
                    message: "Əməliyyat tamamlandı.",
                    link: role === "Store" ? "/orders" : "/appointments",
                    type: "General",
                    isRead: false,
                    createdAt: new Date().toISOString(),
                  },
                ]
              : [],
          ),
        }),
    );
    await login(page);
    await expect.poll(() => hub.sockets.size).toBe(1);
    await page.goto("/#/notifications");
    await expect(
      page.getByRole("heading", { name: "Hər şey qaydasındadır" }),
    ).toBeVisible();
    due = true;
    hub.notify();
    await expect(
      page.getByRole("link", { name: "Bildirişlər, 1 oxunmamış" }),
    ).toBeVisible({ timeout: 4000 });
    await expect(
      page.getByText("Yeni biznes bildirişi", { exact: true }),
    ).toBeVisible({ timeout: 4000 });
    await page.goto("/#/business");
    await expect.poll(() => hub.sockets.size).toBe(1);
    due = false;
    hub.notify();
    await expect(
      page.getByRole("link", { name: "Bildirişlər, 0 oxunmamış" }),
    ).toBeVisible({ timeout: 4000 });
  });
}

test("Same-name customers resolve only the selected conversation and unknown slugs reveal no messages", async ({
  page,
}) => {
  const { requests } = await mockApi(page, "Store");
  await page.route("**/api/chat/conversations", (route) =>
    route.fulfill({
      contentType: "application/json",
      body: JSON.stringify([
        {
          id: 90,
          userId: 4,
          participantName: "Vüsal Mövsümlü",
          slug: "vusal-movsumlu-aaaa",
          createdAt: new Date().toISOString(),
        },
        {
          id: 91,
          userId: 5,
          participantName: "Vüsal Mövsümlü",
          slug: "vusal-movsumlu-bbbb",
          createdAt: new Date().toISOString(),
        },
      ]),
    }),
  );
  await login(page);
  await page.goto("/#/chat/vusal-movsumlu-bbbb");
  await expect(page.locator(".chat-header h2")).toHaveText("Vüsal Mövsümlü");
  expect(
    requests.some(
      (request) => request.path === "/chat/conversations/91/messages",
    ),
  ).toBe(true);
  expect(
    requests.some(
      (request) => request.path === "/chat/conversations/90/messages",
    ),
  ).toBe(false);
  await page.goto("/#/chat/unknown-person");
  await expect(
    page.getByRole("heading", { name: "Bir söhbət seçin" }),
  ).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Mesajınız" })).toHaveCount(0);
});

test("A live update keeps the active chat joined and logout closes live connections", async ({
  page,
}) => {
  await mockApi(page, "Mechanic");
  const hub = await liveHub(page);
  await login(page);
  await page.goto("/#/chat/vuqar-memmedov");
  await expect(page.locator(".connection-status")).toContainText(
    "Canlı bağlantı",
  );
  await expect
    .poll(
      () =>
        hub.calls.filter((call) => call.target === "JoinConversation").length,
    )
    .toBe(1);
  hub.notify();
  await expect(page.locator(".chat-header h2")).toHaveText("Vüqar Məmmədov");
  await expect(
    page.getByText("Salam. Sizə necə kömək edə bilərik?"),
  ).toBeVisible();
  expect(
    hub.calls.filter((call) => call.target === "LeaveConversation"),
  ).toHaveLength(0);
  await page.evaluate(async () => {
    const { setSession } = await import("/src/shared/api/client.js");
    setSession(null);
  });
  await expect.poll(() => hub.sockets.size).toBe(0);
});
