// Explicit browser-test fixtures. Runtime application code always uses the API.
export async function mockApi(
  page,
  role = "User",
  { empty = false, fail = false } = {},
) {
  const requests = [];
  const token = `test.${Buffer.from(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600, nameid: "1", unique_name: "Vüqar Məmmədov", role, email: "test@example.invalid" })).toString("base64url")}.test`;
  const auth = {
    accessToken: token,
    refreshToken: "browser-test-refresh",
    accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
    refreshTokenExpiresAt: new Date(Date.now() + 86400000).toISOString(),
  };
  const adminUsers = [
    {
      id: 1,
      fullName: "Vüqar Məmmədov",
      email: "test@example.invalid",
      role: "Admin",
      emailConfirmed: true,
      isBlocked: false,
      createdAt: new Date().toISOString(),
    },
    {
      id: 2,
      fullName: "Aysel Əliyeva",
      email: "user@example.invalid",
      role: "User",
      emailConfirmed: true,
      isBlocked: false,
      createdAt: new Date().toISOString(),
    },
  ];
  const vehicles = [
    {
      id: 1,
      userId: 1,
      brand: "Toyota",
      model: "Corolla",
      slug: "toyota-corolla-test",
      year: 2022,
      engine: "1.8 Hybrid",
      vehicleType: "Car",
      fuelType: "Hybrid",
      vin: "TESTVIN123",
      plateNumber: "10-AB-123",
      currentKm: 24500,
    },
    {
      id: 2,
      userId: 1,
      brand: "CFMOTO",
      model: "450 SR",
      slug: "cfmoto-450-sr-test",
      year: 2024,
      engine: "450 cc",
      vehicleType: "Motorcycle",
      fuelType: "Petrol",
      vin: "TESTVIN456",
      plateNumber: "99-CD-456",
      currentKm: 4200,
    },
  ];
  const mechanic = {
    id: 10,
    userId: role === "Mechanic" ? 1 : 20,
    businessName: "Baku Auto & Moto Servis",
    fullName: "Əli Məmmədov",
    slug: "baku-servis",
    description: "Avtomobil və motosiklet üçün texniki qulluq.",
    address: "Bakı, Nərimanov",
    phoneNumber: "+994501234567",
    hourlyRate: 30,
    rating: 4.8,
    isActive: true,
  };
  const store = {
    id: 20,
    userId: role === "Store" ? 1 : 30,
    storeName: "Garage Parts",
    slug: "garage-parts",
    description: "Avtomobil və motosiklet üçün ehtiyat hissələri.",
    address: "Bakı, Yasamal",
    phoneNumber: "+994551234567",
    rating: 4.7,
    isActive: true,
  };
  const products = [
    {
      id: 30,
      storeId: 20,
      name: "Mühərrik yağı 5W-40",
      slug: "engine-oil",
      brand: "Motul",
      description: "Sintetik mühərrik yağı.",
      vehicleCompatibility: "Uyğun mühərriklər üçün",
      price: 45,
      stock: 2,
      isActive: true,
    },
    {
      id: 31,
      storeId: 21,
      name: "Əyləc dəsti",
      slug: "brake-kit",
      brand: "Brembo",
      price: 75,
      stock: 4,
      isActive: true,
    },
  ];
  const now = new Date().toISOString();
  const fuel = [
    {
      id: 40,
      vehicleId: 1,
      liters: 30,
      pricePerLiter: 1.1,
      totalPrice: 33,
      kilometer: 24500,
      fuelDate: now,
    },
  ];
  const reminder = {
    id: 50,
    vehicleId: 1,
    title: "Yağ dəyişimi",
    reminderDate: now,
    reminderKm: 25000,
    isCompleted: false,
  };
  const appointment = {
    id: 60,
    userId: 1,
    mechanicId: 10,
    vehicleId: 1,
    vehicleName: "Toyota Corolla",
    mechanicName: "Əli Məmmədov",
    customerName: "Vüqar Məmmədov",
    appointmentDate: now,
    description: "Əyləc sisteminin yoxlanması",
    status: "Pending",
    createdAt: now,
  };
  const order = {
    id: 70,
    userId: 1,
    storeId: 20,
    totalAmount: 45,
    status: "Pending",
    createdAt: now,
    items: [
      {
        id: 1,
        productId: 30,
        productName: products[0].name,
        quantity: 1,
        unitPrice: 45,
        totalPrice: 45,
      },
    ],
  };
  const notifications = [
    {
      id: 80,
      title: "Görüşünüz yaradıldı",
      message: "Ustanın cavabını gözləyin.",
      link: "/appointments/60",
      createdAt: now,
      type: "Appointment",
      isRead: false,
    },
  ];
  const conversations = [
    {
      id: 90,
      userId: 1,
      storeId: 20,
      mechanicId: null,
      createdAt: now,
      userFullName: "Vüqar Məmmədov",
      storeName: "Moto Parts",
      participantName: role === "User" ? "Moto Parts" : "Vüqar Məmmədov",
      slug: role === "User" ? "moto-parts" : "vuqar-memmedov",
    },
  ];
  const messages = [
    {
      id: 100,
      conversationId: 90,
      senderUserId: 30,
      content: "Salam. Sizə necə kömək edə bilərik?",
      sentAt: now,
      status: "Sent",
    },
  ];
  await page.route("**/hubs/**", (route) =>
    route.fulfill({ status: 404, body: "" }),
  );
  await page.route(/^https?:\/\/[^/]+\/api\//, async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname.replace("/api", "");
    const method = request.method();
    const body = request.headers()["content-type"]?.includes("application/json")
      ? request.postDataJSON()
      : null;
    requests.push({
      path,
      method,
      body,
      query: Object.fromEntries(url.searchParams),
      authorization: request.headers().authorization,
    });
    const respond = (data, status = 200) =>
      route.fulfill({
        status,
        contentType: "application/json",
        body: JSON.stringify(data),
      });
    if (
      path === "/auth/login" ||
      path === "/auth/verify-email" ||
      path === "/auth/refresh-token"
    )
      return respond(auth);
    if (path === "/auth/register")
      return respond({
        message: "Təsdiqləmə kodu göndərildi.",
        email: body.email,
      });
    if (path === "/auth/resend-verification-code")
      return respond({ message: "Yeni kod göndərildi." });
    if (path === "/auth/logout") return route.fulfill({ status: 204 });
    if (fail && method === "GET" && path === "/Vehicle")
      return respond({ message: "Test üçün server xətası." }, 500);
    if (method !== "GET") {
      if (method === "DELETE" && /^\/admin\/users\/\d+$/.test(path)) {
        const index = adminUsers.findIndex(
          (user) => user.id === Number(path.split("/").pop()),
        );
        if (index >= 0) adminUsers.splice(index, 1);
        return route.fulfill({ status: 204 });
      }
      if (method === "DELETE" && path === "/notifications") {
        notifications.splice(0);
        return route.fulfill({ status: 204 });
      }
      if (method === "DELETE" && /^\/notifications\/\d+$/.test(path)) {
        const index = notifications.findIndex(
          (item) => item.id === Number(path.split("/").pop()),
        );
        if (index >= 0) notifications.splice(index, 1);
        return route.fulfill({ status: 204 });
      }
      if (path === "/Vehicle" && method === "POST") {
        const item = { ...body, id: 3, userId: 1, slug: "new-vehicle-test" };
        vehicles.push(item);
        return respond(item, 201);
      }
      if (path === "/orders" && method === "POST")
        return respond({ ...order, id: 71 }, 201);
      if (path === "/appointments" && method === "POST")
        return respond({ ...appointment, ...body, id: 61 }, 201);
      if (path === "/chat/conversations/90/messages") {
        const item = {
          id: messages.length + 101,
          conversationId: 90,
          senderUserId: 1,
          content: body.content,
          sentAt: now,
          status: "Sent",
        };
        messages.push(item);
        return respond(item, 201);
      }
      if (/^\/admin\/users\/\d+\/block$/.test(path))
        return route.fulfill({ status: 204 });
      if (path === "/images/upload")
        return respond({
          url: "https://example.invalid/test.png",
          publicId: "test-owned-image",
        });
      return route.fulfill({ status: 204 });
    }
    if (path === "/dashboard")
      return respond({
        totalVehicles: empty ? 0 : 2,
        thisMonthExpenses: empty ? 0 : 180,
        activeRemindersCount: empty ? 0 : 1,
        pendingAppointmentsCount: empty ? 0 : 1,
        upcomingReminders: empty ? [] : [reminder],
        recentFuelRecords: empty ? [] : fuel,
        upcomingAppointments: empty ? [] : [appointment],
      });
    if (path === "/admin/dashboard")
      return respond({
        totalUsers: 42,
        pendingBusinessApplications: 2,
        blockedUsers: 1,
        totalMechanics: 8,
        totalStores: 6,
        totalProducts: 125,
        totalOrders: 30,
        pendingOrders: 4,
        pendingAppointments: 3,
      });
    if (path === "/admin/users") return respond(adminUsers);
    if (path === "/admin/business-applications") return respond([]);
    if (path === "/profile")
      return respond({
        id: 1,
        fullName: "Vüqar Məmmədov",
        email: "test@example.invalid",
        role,
        phoneNumber: "+994501234567",
        emailConfirmed: true,
        createdAt: now,
      });
    if (path === "/Vehicle") return respond(empty ? [] : vehicles);
    if (path.startsWith("/Vehicle/by-slug/")) {
      const vehicle = vehicles.find(
        (item) => item.slug === path.split("/").pop(),
      );
      return vehicle
        ? respond(vehicle)
        : respond({ message: "Nəqliyyat vasitəsi tapılmadı." }, 404);
    }
    if (path.startsWith("/Vehicle/"))
      return respond(
        vehicles.find((item) => item.id === Number(path.split("/").pop())),
      );
    if (path === "/mechanics") return respond(empty ? [] : [mechanic]);
    if (path.startsWith("/mechanics/")) return respond(mechanic);
    if (path === "/stores") return respond(empty ? [] : [store]);
    if (path.startsWith("/stores/")) return respond(store);
    if (path.startsWith("/products/store/"))
      return respond(
        empty ? [] : products.filter((item) => item.storeId === 20),
      );
    if (path === "/products") return respond(empty ? [] : products);
    if (path.startsWith("/products/"))
      return respond(
        products.find((item) => item.slug === path.split("/").pop()),
      );
    if (path === "/fuel-records/stats")
      return respond({
        vehicleId: 1,
        recordCount: 2,
        distanceKm: 500,
        totalLiters: 30,
        totalFuelCost: 33,
        averageConsumptionPer100Km: 6,
      });
    if (path.startsWith("/fuel-records/vehicle/"))
      return respond(empty ? [] : fuel);
    if (path === "/expenses")
      return respond(
        empty
          ? []
          : [
              {
                id: 41,
                expenseType: "Service",
                description: "Yağ dəyişimi",
                amount: 80,
                expenseDate: now,
              },
            ],
      );
    if (path.startsWith("/service-records/vehicle/"))
      return respond(
        empty
          ? []
          : [
              {
                id: 42,
                vehicleId: 1,
                serviceName: "Texniki qulluq",
                kilometer: 24500,
                cost: 80,
                serviceDate: now,
              },
            ],
      );
    if (path.startsWith("/reminders/vehicle/"))
      return respond(empty ? [] : [reminder]);
    if (path === "/appointments/availability") {
      const day = url.searchParams.get("date");
      return respond({
        timeZone: "Asia/Baku",
        durationMinutes: 60,
        slots: empty
          ? []
          : [
              new Date(`${day}T10:00:00+04:00`).toISOString(),
              new Date(`${day}T11:00:00+04:00`).toISOString(),
            ],
      });
    }
    if (path === "/appointments/working-hours")
      return respond(
        Array.from({ length: 7 }, (_, dayOfWeek) => ({
          dayOfWeek,
          isOpen: dayOfWeek !== 0,
          start: "09:00:00",
          end: "18:00:00",
        })),
      );
    if (path === "/appointments/calendar")
      return respond(empty ? [] : [appointment]);
    if (/^\/appointments\/\d+\/report$/.test(path)) return respond(null);
    if (path === "/business/statistics")
      return respond({
        role,
        completedCount: 2,
        cancelledCount: 1,
        pendingCount: 1,
        amount: 120,
        averageRating: 4.5,
        reviewCount: 2,
        lowStockCount: 1,
        outOfStockCount: 1,
        bestSellers: empty
          ? []
          : [{ productId: 30, name: "Motor oil", quantity: 2, amount: 120 }],
      });
    if (path === "/admin/audit-logs")
      return respond({ total: 0, page: 1, pageSize: 25, items: [] });
    if (path.startsWith("/appointments/"))
      return respond(empty ? [] : [appointment]);
    if (path.startsWith("/orders/")) return respond(empty ? [] : [order]);
    if (path.startsWith("/reviews/"))
      return respond(
        empty
          ? []
          : [
              {
                id: 43,
                userId: 2,
                mechanicId: 10,
                userFullName: "Vüsal Mövsümlü",
                rating: 5,
                comment: "Səliqəli və diqqətli servis.",
                createdAt: now,
              },
            ],
      );
    if (path === "/notifications/unread-count")
      return respond({
        unreadCount: empty
          ? 0
          : notifications.filter((item) => !item.isRead).length,
      });
    if (path === "/notifications") return respond(empty ? [] : notifications);
    if (path === "/chat/conversations")
      return respond(empty ? [] : conversations);
    if (path.endsWith("/messages")) return respond(messages);
    return respond({ message: `Unhandled test route: ${path}` }, 404);
  });
  return { requests, auth };
}
export async function login(page) {
  await page.goto("/#/login");
  await page.getByLabel("Email ünvanı").fill("test@example.invalid");
  await page.getByLabel("Şifrə", { exact: true }).fill("testpass1");
  await page.getByRole("button", { name: "Daxil ol", exact: true }).click();
  await page
    .getByRole("heading", { name: /Yenidən xoş gördük, Vüqar/ })
    .waitFor();
}
