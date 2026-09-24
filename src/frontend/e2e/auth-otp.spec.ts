import { test, expect } from "@playwright/test";

test("registration verifies six-digit email OTP with paste on mobile", async ({
  page,
}, testInfo) => {
  test.setTimeout(30000);
  await page.addInitScript(() => {
    localStorage.setItem("studyflow-language", "vi");
  });
  await page.route("**/api/auth/register", (route) =>
    route.fulfill({
      status: 202,
      contentType: "application/json",
      body: JSON.stringify({
        email: "student@gmail.com",
        expiresAt: new Date(Date.now() + 600000).toISOString(),
        resendAfterSeconds: 60,
      }),
    }),
  );
  await page.route("**/api/auth/verify-email", async (route) => {
    const body = JSON.parse(route.request().postData() ?? "{}");
    if (body.code !== "123456")
      return route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({
          code: "INVALID_OTP",
          message: "Mã OTP không đúng.",
        }),
      });
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        user: {
          id: crypto.randomUUID(),
          email: body.email,
          displayName: "OTP Student",
          avatarUrl: null,
          timeZoneId: "Asia/Ho_Chi_Minh",
          isEmailVerified: true,
        },
        accessToken: "test-token",
        accessTokenExpiresAt: new Date(Date.now() + 900000).toISOString(),
      }),
    });
  });
  await page.setViewportSize({ width: 320, height: 740 });
  await page.goto("/register");
  await page.getByLabel("Tên hiển thị").fill("OTP Student");
  await page.getByLabel("Email").fill("student@gmail.com");
  await page.getByLabel("Mật khẩu").fill("Password1");
  await page.getByRole("button", { name: "Gửi mã xác minh" }).click();
  await expect(
    page.getByRole("heading", { name: "Kiểm tra email" }),
  ).toBeVisible();
  await expect(page.getByText(/st\*+@gmail\.com/)).toBeVisible();
  for (let index = 0; index < 6; index++)
    await page.getByLabel(`Chữ số OTP ${index + 1}`).fill("0");
  await page.getByRole("button", { name: "Xác minh và tiếp tục" }).click();
  await expect(page.getByRole("alert")).toContainText("Mã OTP không đúng");
  await page.getByLabel("Chữ số OTP 1").evaluate((element) => {
    const data = new DataTransfer();
    data.setData("text", "123456");
    element.dispatchEvent(
      new ClipboardEvent("paste", { bubbles: true, clipboardData: data }),
    );
  });
  for (const [index, digit] of [..."123456"].entries())
    await expect(page.getByLabel(`Chữ số OTP ${index + 1}`)).toHaveValue(digit);
  await page.screenshot({
    path: testInfo.outputPath("email-otp-mobile.png"),
    fullPage: true,
  });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBeTruthy();
  await page.getByRole("button", { name: "Xác minh và tiếp tục" }).click();
  await expect(page).toHaveURL(/\/home$/);
});
