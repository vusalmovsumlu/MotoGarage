import { test, expect } from "@playwright/test";
import { mockApi, login } from "./fixtures";

const photo = (index) => ({
  name: `workplace-${index}.png`,
  mimeType: "image/png",
  buffer: Buffer.from(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=",
    "base64",
  ),
});
const application = {
  id: 12,
  fullName: "Test Usta",
  email: "usta@example.invalid",
  role: "Mechanic",
  status: "Pending",
  createdAt: new Date().toISOString(),
  photos: [1, 2, 3].map((id) => `/icons/garage.png?photo=${id}`),
};
const respond = (route, data) =>
  route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(data),
  });

for (const role of ["Mechanic", "Store"]) {
  test(`${role} registration requires three photos and sends multipart to the backend`, async ({
    page,
  }) => {
    await mockApi(page);
    let request;
    await page.route("**/api/auth/register-business", async (route) => {
      request = route.request();
      await respond(route, {
        email: "business@example.invalid",
        message: "Kod göndərildi.",
      });
    });
    await page.goto("/#/register");
    await page.getByLabel("Ad", { exact: true }).fill("Test");
    await page.getByLabel("Soyad", { exact: true }).fill("Business");
    await page.getByLabel("Hesab növü").selectOption(role);
    await expect(
      page.getByText(
        role === "Mechanic"
          ? /İşlədiyiniz qarajın/
          : /Mağazada olan real detalların/,
      ),
    ).toBeVisible();
    await page.getByLabel("Email ünvanı").fill("business@example.invalid");
    await page.getByLabel("Şifrə", { exact: true }).fill("testpass1");
    await page.getByLabel("Şəkil 1", { exact: true }).setInputFiles(photo(1));
    await page.getByLabel("Şəkil 2", { exact: true }).setInputFiles(photo(2));
    await page.getByRole("button", { name: "Hesab yarat" }).click();
    expect(request).toBeUndefined();
    await expect(page.getByLabel("Şəkil 3", { exact: true })).toHaveJSProperty(
      "required",
      true,
    );
    await page.getByLabel("Şəkil 3", { exact: true }).setInputFiles(photo(3));
    await expect(page.getByAltText("Seçilmiş şəkil 3")).toBeVisible();
    await page.getByRole("button", { name: "Hesab yarat" }).click();
    await expect(
      page.getByRole("heading", { name: "Emailinizi təsdiqləyin." }),
    ).toBeVisible();
    expect(request.headers()["content-type"]).toContain(
      "multipart/form-data; boundary=",
    );
    expect(request.headers().authorization).toBeUndefined();
    const body = request.postDataBuffer().toString();
    expect(body.match(/name="photos"/g)).toHaveLength(3);
    expect(body).toContain(`\r\n${role}\r\n`);
    expect(body).toContain("business@example.invalid");
  });
}

test("Business registration validates images and fits mobile light and dark themes", async ({
  page,
}) => {
  await mockApi(page);
  await page.setViewportSize({ width: 320, height: 850 });
  await page.goto("/#/register");
  await page.getByLabel("Hesab növü").selectOption("Store");
  await page.getByLabel("Şəkil 1", { exact: true }).setInputFiles({
    name: "fake.txt",
    mimeType: "text/plain",
    buffer: Buffer.from("invalid"),
  });
  await expect(page.getByRole("alert")).toContainText("JPG, PNG və ya WEBP");
  await page.getByLabel("Şəkil 1", { exact: true }).setInputFiles(photo(1));
  await expect(page.getByAltText("Seçilmiş şəkil 1")).toBeVisible();
  for (let theme = 0; theme < 2; theme++) {
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
    await page.getByRole("button", { name: "Temanı dəyiş" }).click();
  }
  await page.getByRole("button", { name: "Şəkil 1 sil" }).click();
  await expect(page.getByAltText("Seçilmiş şəkil 1")).toHaveCount(0);
});

test("Admin reviews three photos, requires a decline comment and submits its trimmed value", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  let decision;
  await page.route("**/api/admin/business-applications**", async (route) => {
    if (route.request().method() === "GET")
      return respond(route, decision ? [] : [application]);
    decision = route.request().postDataJSON();
    return route.fulfill({ status: 204 });
  });
  await login(page);
  await page.goto("/#/admin/applications");
  await expect(
    page.getByRole("heading", { name: "Biznes müraciətləri" }),
  ).toBeVisible();
  await expect(page.getByAltText(/Müraciət şəkli [123]/)).toHaveCount(3);
  await page.getByRole("button", { name: "Şəkil 2 böyüt" }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await page.getByRole("button", { name: "Rədd et", exact: true }).click();
  const modal = page.getByRole("dialog");
  await page.getByLabel("Rədd etmə səbəbi").fill("   ");
  await modal.getByRole("button", { name: "Rədd et", exact: true }).click();
  await expect(modal.getByRole("alert")).toHaveText(
    "Rədd etmə səbəbini yazın.",
  );
  expect(decision).toBeUndefined();
  await page
    .getByLabel("Rədd etmə səbəbi")
    .fill("  Qarajın şəkillərini təqdim edin.  ");
  await modal.getByRole("button", { name: "Rədd et", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Müraciət yoxdur" }),
  ).toBeVisible();
  expect(decision).toEqual({ comment: "Qarajın şəkillərini təqdim edin." });
});

test("Admin accepts an application and status filters reach the API", async ({
  page,
}) => {
  await mockApi(page, "Admin");
  let accepted = false;
  const filters = [];
  await page.route("**/api/admin/business-applications**", async (route) => {
    if (route.request().method() === "GET") {
      filters.push(new URL(route.request().url()).searchParams.get("status"));
      return respond(route, accepted ? [] : [application]);
    }
    expect(route.request().url()).toMatch(/\/12\/accept$/);
    accepted = true;
    return route.fulfill({ status: 204 });
  });
  await login(page);
  await page.goto("/#/admin/applications");
  await page.getByRole("button", { name: "Təsdiqlə", exact: true }).click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Təsdiqlə", exact: true })
    .click();
  await expect(
    page.getByRole("heading", { name: "Müraciət yoxdur" }),
  ).toBeVisible();
  await page.getByLabel("Müraciət statusu").selectOption("Rejected");
  await expect.poll(() => filters.at(-1)).toBe("Rejected");
  expect(accepted).toBe(true);
});

for (const status of ["Pending", "Rejected"]) {
  test(`${status} business account shows decision gate, then opens workspace after approval`, async ({
    page,
  }) => {
    const { auth } = await mockApi(page, "Mechanic");
    const claims = JSON.parse(
      Buffer.from(auth.accessToken.split(".")[1], "base64url").toString(),
    );
    claims.businessApprovalStatus = status;
    auth.accessToken = `test.${Buffer.from(JSON.stringify(claims)).toString("base64url")}.test`;
    let currentStatus = status;
    await page.route("**/api/business-applications/my", (route) =>
      respond(route, {
        ...application,
        status: currentStatus,
        decisionComment:
          status === "Rejected" ? "Qarajın real şəkillərini göndərin." : null,
      }),
    );
    await page.goto("/#/login");
    await page.getByLabel("Email ünvanı").fill("test@example.invalid");
    await page.getByLabel("Şifrə", { exact: true }).fill("testpass1");
    await page.getByRole("button", { name: "Daxil ol", exact: true }).click();
    await expect(
      page.getByRole("heading", {
        name:
          status === "Pending"
            ? "Admin təsdiqi gözlənilir"
            : "Müraciətiniz rədd edildi",
      }),
    ).toBeVisible();
    if (status === "Rejected")
      await expect(
        page.getByText("Qarajın real şəkillərini göndərin."),
      ).toBeVisible();
    await expect(page.locator(".workspace")).toHaveCount(0);
    await page.goto("/#/business");
    await expect(page.locator(".business-approval-screen")).toBeVisible();
    currentStatus = "Approved";
    await page.getByRole("button", { name: "Statusu yenilə" }).click();
    await expect(
      page.getByRole("heading", { name: "Usta profilim" }),
    ).toBeVisible();
    await expect(page.locator(".business-approval-screen")).toHaveCount(0);
  });
}

test("Ordinary users cannot open admin business applications", async ({
  page,
}) => {
  const { requests } = await mockApi(page);
  await login(page);
  await page.goto("/#/admin/applications");
  await expect(
    page.getByRole("heading", { name: "Səhifə əlçatan deyil" }),
  ).toBeVisible();
  expect(
    requests.some((request) =>
      request.path.includes("/admin/business-applications"),
    ),
  ).toBe(false);
});
