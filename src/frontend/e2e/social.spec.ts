import {
  test,
  expect,
  type APIRequestContext,
  type Page,
} from "@playwright/test";

const password = "SocialTest123!";
async function account(request: APIRequestContext, name: string) {
  const suffix = `${Date.now()}${Math.floor(Math.random() * 10000)}`;
  const email = `social-${suffix}@studyflow.test`;
  const r = await request.post("/api/auth/register", {
    data: {
      email,
      password,
      displayName: name,
      timeZoneId: "Asia/Ho_Chi_Minh",
    },
  });
  expect(r.ok()).toBeTruthy();
  const auth = await r.json();
  const headers = { Authorization: `Bearer ${auth.accessToken}` };
  const username = `learner_${suffix}`;
  const profile = await request.put("/api/social/profile", {
    headers,
    data: {
      username,
      displayName: name,
      bio: "Học một chút mỗi ngày. Chia sẻ để cùng tiến bộ.",
      visibility: "Public",
    },
  });
  expect(profile.ok()).toBeTruthy();
  return { email, username, id: auth.user.id as string, headers };
}
async function login(page: Page, email: string, path = "/explore") {
  await page.goto(path);
  await page.locator("input[name=email]").fill(email);
  await page.locator("input[name=password]").fill(password);
  await page.locator("form button").last().click();
  await expect(page).not.toHaveURL(/login/);
}
async function noOverflow(page: Page) {
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBeTruthy();
}

test("community publication, practice, friends, realtime messages, sharing and privacy at 320px", async ({
  page,
  request,
  browser,
}, testInfo) => {
  const a = await account(request, "Minh Anh · Social QA");
  const b = await account(request, "Khánh Linh · Social QA");
  const subject = await (
    await request.post("/api/subjects", {
      headers: a.headers,
      data: { name: "Ngoại ngữ", description: "Nội dung kiểm thử cộng đồng" },
    })
  ).json();
  const set = await (
    await request.post(`/api/subjects/${subject.id}/study-sets`, {
      headers: a.headers,
      data: {
        title: `Từ vựng mỗi ngày ${Date.now()}`,
        description:
          "Những từ quen thuộc để tự tin giao tiếp. Học và luyện nhớ cùng nhau.",
      },
    })
  ).json();
  const cards = await request.post(
    `/api/study-sets/${set.id}/flashcards/bulk`,
    {
      headers: a.headers,
      data: {
        cards: [
          ["Hello", "Xin chào"],
          ["Thank you", "Cảm ơn"],
          ["Good morning", "Chào buổi sáng"],
        ].map(([frontText, backText]) => ({
          frontText,
          backText,
          explanation: "Từ dùng trong giao tiếp hằng ngày.",
        })),
      },
    },
  );
  expect(cards.ok()).toBeTruthy();
  await login(page, a.email, `/community/sets/${set.id}`);
  await page.getByRole("button", { name: "Quyền chia sẻ" }).click();
  await page.getByLabel("Ai được xem?").selectOption("Public");
  await page
    .getByLabel("Chủ đề, phân cách bằng dấu phẩy")
    .fill("Ngoại ngữ, Giao tiếp");
  await page.getByRole("button", { name: "Lưu quyền chia sẻ" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.goto("/explore");
  await page.getByLabel("Tìm bộ học hoặc người dùng").fill(set.title);
  await page.getByRole("button", { name: "Tìm kiếm", exact: true }).click();
  await expect(page.getByRole("heading", { name: set.title })).toBeVisible();
  expect((await page.locator('.app-topbar').boundingBox())!.height).toBeLessThanOrEqual(84);
  await page.screenshot({
    path: testInfo.outputPath("social-explore-desktop.png"),
    fullPage: true,
  });
  await page.setViewportSize({ width: 320, height: 740 });
  await noOverflow(page);
  await page.screenshot({
    path: testInfo.outputPath("social-explore-mobile.png"),
    fullPage: true,
  });
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
  });
  const friend = await context.newPage();
  await login(friend, b.email, `/community/sets/${set.id}`);
  await friend.getByRole("button", { name: "Lưu", exact: true }).click();
  await expect(
    friend.getByRole("button", { name: "Đã lưu", exact: true }),
  ).toBeVisible();
  await friend.getByRole("button", { name: "Học ngay" }).click();
  await friend.getByLabel("Câu trả lời của bạn").fill("Xin chào");
  await friend.getByRole("button", { name: "Kiểm tra đáp án" }).click();
  await expect(friend.getByText("Chính xác!")).toBeVisible();
  await friend.getByRole("button", { name: "Đóng", exact: true }).click();
  await friend.getByLabel("Viết bình luận").fill("Bộ học dễ nhớ, cảm ơn bạn!");
  await friend.getByRole("button", { name: "Gửi bình luận" }).click();
  await expect(friend.getByText("Bộ học dễ nhớ, cảm ơn bạn!")).toBeVisible();
  await friend.screenshot({
    path: testInfo.outputPath("social-set-mobile.png"),
    fullPage: true,
  });
  await friend.goto(`/people/${a.username}`);
  await friend.getByRole("button", { name: "Kết bạn", exact: true }).click();
  await expect(
    friend.getByRole("button", { name: "Hủy lời mời" }),
  ).toBeVisible();
  await page.goto(`/people/${b.username}`);
  await page.getByRole("button", { name: "Chấp nhận kết bạn" }).click();
  await page.getByRole("button", { name: "Nhắn tin", exact: true }).click();
  await expect(page).toHaveURL(/messages\//);
  const conversation = page.url().split("/").at(-1)!;
  await friend.goto(`/messages/${conversation}`);
  await page
    .getByRole("textbox", { name: "Tin nhắn", exact: true })
    .fill("Cùng ôn bộ từ vựng nhé!");
  await page.getByRole("button", { name: "Gửi tin nhắn" }).click();
  await expect(friend.getByText("Cùng ôn bộ từ vựng nhé!")).toBeVisible({
    timeout: 15000,
  });
  await context.setOffline(true);
  await page
    .getByRole("textbox", { name: "Tin nhắn", exact: true })
    .fill("Tin nhắn khi kết nối gián đoạn");
  await page.getByRole("button", { name: "Gửi tin nhắn" }).click();
  await expect(page.getByText("Tin nhắn khi kết nối gián đoạn")).toBeVisible();
  await context.setOffline(false);
  await expect(friend.getByText("Tin nhắn khi kết nối gián đoạn")).toBeVisible({
    timeout: 20000,
  });
  await noOverflow(page);
  const composer = await page.locator('.social-compose').boundingBox();
  const navigation = await page.locator('.bottom-nav').boundingBox();
  expect(composer!.y + composer!.height).toBeLessThanOrEqual(navigation!.y);
  await page.screenshot({
    path: testInfo.outputPath("social-chat-mobile.png"),
    fullPage: true,
  });
  await page.goto(`/community/sets/${set.id}`);
  await page.getByRole("button", { name: "Chia sẻ", exact: true }).click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Gửi", exact: true })
    .click();
  await expect(
    page.getByRole("button", { name: "Đã gửi", exact: true }),
  ).toBeVisible();
  await expect(
    friend.getByRole("link", { name: new RegExp(set.title) }),
  ).toBeVisible({ timeout: 15000 });
  await page.getByRole("button", { name: "Đóng", exact: true }).click();
  await page.getByRole("button", { name: "Quyền chia sẻ" }).click();
  await page.getByLabel("Ai được xem?").selectOption("Private");
  await page.getByRole("button", { name: "Lưu quyền chia sẻ" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await friend.reload();
  await expect(
    friend.getByText("Nội dung không còn khả dụng hoặc đã giới hạn quyền xem."),
  ).toBeVisible();
  await friend.goto(`/community/sets/${set.id}`);
  await expect(friend.getByRole("alert")).toContainText("không có quyền");
  await friend.goto('/community/saved');
  await expect(friend.getByText('Bộ học không còn khả dụng', { exact: true })).toBeVisible();
  await friend.getByRole('button', { name: 'Bỏ lưu', exact: true }).click();
  await expect(friend.getByText('Bộ học không còn khả dụng', { exact: true })).toHaveCount(0);
  await context.close();
});

test("profile challenge sends a real Battle invitation and friend can join", async ({
  page,
  request,
  browser,
}, testInfo) => {
  const host = await account(request, "Gia Huy · Battle Social QA");
  const guest = await account(request, "Hà My · Battle Social QA");
  expect(
    (
      await request.post(`/api/social/people/${guest.id}/request`, {
        headers: host.headers,
      })
    ).ok(),
  ).toBeTruthy();
  expect(
    (
      await request.post(`/api/social/people/${host.id}/accept`, {
        headers: guest.headers,
      })
    ).ok(),
  ).toBeTruthy();
  const subject = await (
    await request.post("/api/subjects", {
      headers: host.headers,
      data: { name: "Thử thách từ vựng" },
    })
  ).json();
  const set = await (
    await request.post(`/api/subjects/${subject.id}/study-sets`, {
      headers: host.headers,
      data: {
        title: "10 từ vựng cùng bạn",
        description: "Bộ học kiểm thử lời mời",
      },
    })
  ).json();
  const response = await request.post(
    `/api/study-sets/${set.id}/flashcards/bulk`,
    {
      headers: host.headers,
      data: {
        cards: Array.from({ length: 10 }, (_, i) => ({
          frontText: `Question ${i}`,
          backText: `Answer ${i}`,
        })),
      },
    },
  );
  expect(response.ok()).toBeTruthy();
  await login(page, host.email, `/people/${guest.username}`);
  await page.getByRole("button", { name: "Thách đấu" }).click();
  await page
    .getByRole("dialog")
    .getByRole("link", { name: /10 từ vựng/ })
    .click();
  await page.getByRole("button", { name: "Tạo phòng & gửi lời mời" }).click();
  await expect(page).toHaveURL(/battles\//);
  const roomId = page.url().split("/").at(-1)!;
  const context = await browser.newContext({
    viewport: { width: 320, height: 740 },
  });
  const friend = await context.newPage();
  await login(friend, guest.email, "/messages");
  await friend.locator(".social-conversation").first().click();
  await expect(friend.getByText("Lời mời Live Battle")).toBeVisible();
  await noOverflow(friend);
  await friend.screenshot({
    path: testInfo.outputPath("battle-invitation-mobile.png"),
    fullPage: true,
  });
  await friend.getByRole("link", { name: /Vào phòng/ }).click();
  await expect(friend).toHaveURL(/join\//);
  await friend.getByRole("button", { name: /Vào phòng|Tham gia/ }).click();
  await expect(friend).toHaveURL(new RegExp(`battles/${roomId}`));
  await expect(
    page.getByText("Hà My · Battle Social QA", { exact: true }),
  ).toBeVisible({ timeout: 15000 });
  await context.close();
});
