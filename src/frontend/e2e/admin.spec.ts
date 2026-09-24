import { expect, test } from "@playwright/test";

const adminUser = {
  id: "admin-1",
  email: "admin@studyflow.test",
  displayName: "Thanh Phan",
  avatarUrl: null,
  timeZoneId: "Asia/Ho_Chi_Minh",
  isEmailVerified: true,
  role: "Admin",
  isSuspended: false,
};
const learner = {
  id: "user-1",
  email: "learner@studyflow.test",
  displayName: "Minh Anh",
  username: "minhanh",
  role: "User",
  isEmailVerified: true,
  isSuspended: false,
  suspensionReason: null,
  lastActiveAt: new Date().toISOString(),
  createdAt: "2026-09-21T08:00:00Z",
};

test("admin control center is usable at 320px and opens user controls", async ({
  page,
}, testInfo) => {
  await page.route("**/api/auth/refresh", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        user: adminUser,
        accessToken: "admin-token",
        accessTokenExpiresAt: new Date(Date.now() + 900000).toISOString(),
      }),
    }),
  );
  await page.route("**/api/admin/**", (route) => {
    const url = new URL(route.request().url());
    if (url.pathname === "/api/admin/overview")
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          totalUsers: 148,
          activeUsers: 23,
          suspendedUsers: 4,
          unverifiedUsers: 7,
          adminUsers: 2,
          newUsersToday: 9,
          pendingReports: 3,
          failedDocuments: 2,
          studySessionsToday: 61,
        }),
      });
    if (url.pathname === "/api/admin/users/user-1")
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          user: learner,
          subjects: 4,
          studySets: 12,
          studySessions: 38,
          documents: 7,
          battleParticipations: 16,
          recentAudit: [],
        }),
      });
    if (url.pathname === "/api/admin/users")
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [learner],
          page: 1,
          pageSize: 20,
          totalItems: 1,
          totalPages: 1,
        }),
      });
    if (url.pathname === "/api/admin/audit")
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [],
          page: 1,
          pageSize: 30,
          totalItems: 0,
          totalPages: 0,
        }),
      });
    return route.fulfill({ status: 204 });
  });
  await page.setViewportSize({ width: 320, height: 740 });
  await page.goto("/admin");
  await expect(
    page.getByRole("heading", { name: /Kiểm soát người dùng/ }),
  ).toBeVisible();
  await expect(
    page
      .getByRole("region", { name: "Tổng quan người dùng" })
      .getByText("Đang hoạt động"),
  ).toBeVisible();
  await page.getByRole("button", { name: "Quản lý" }).click();
  await expect(page.getByRole("heading", { name: "Minh Anh" })).toBeVisible();
  await expect(page.getByText("Thu hồi tất cả phiên đăng nhập")).toBeVisible();
  await page.screenshot({
    path: testInfo.outputPath("admin-mobile.png"),
    fullPage: true,
  });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBeTruthy();
});
