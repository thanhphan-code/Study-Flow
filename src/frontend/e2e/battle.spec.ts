import {
  test,
  expect,
  type APIRequestContext,
  type Page,
} from "@playwright/test";

const password = "BattleTest123!";
const cards = [
  ["Định luật II Newton", "F = ma"],
  ["Đơn vị của lực", "Newton"],
  ["Đơn vị của khối lượng", "Kilogram"],
  ["Gia tốc trọng trường gần mặt đất", "9,8 m/s²"],
  ["Lực hút của Trái Đất", "Trọng lực"],
  ["Lực cản chuyển động giữa hai bề mặt", "Lực ma sát"],
  ["Đơn vị của công", "Joule"],
  ["Đơn vị của công suất", "Watt"],
  ["Công thức vận tốc", "v = s/t"],
  ["Công thức động lượng", "p = mv"],
];

async function seed(request: APIRequestContext) {
  const email = `battle-e2e-${Date.now()}@studyflow.test`;
  const registration = await request.post("/api/auth/register", {
    data: {
      email,
      password,
      displayName: "Minh Anh · QA",
      timeZoneId: "Asia/Ho_Chi_Minh",
    },
  });
  expect(registration.ok()).toBeTruthy();
  const auth = await registration.json();
  const headers = { Authorization: `Bearer ${auth.accessToken}` };
  const subject = await (
    await request.post("/api/subjects", {
      headers,
      data: {
        name: "Battle E2E · Vật lý",
        description: "Dữ liệu kiểm thử tự động",
      },
    })
  ).json();
  const set = await (
    await request.post(`/api/subjects/${subject.id}/study-sets`, {
      headers,
      data: {
        title: "Vật lý · Chuyển động & lực",
        description: "Bộ học kiểm thử Live Battle",
      },
    })
  ).json();
  const response = await request.post(
    `/api/study-sets/${set.id}/flashcards/bulk`,
    {
      headers,
      data: {
        cards: cards.map(([frontText, backText]) => ({
          frontText,
          backText,
          explanation: `Kiến thức cần nhớ: ${backText}.`,
        })),
      },
    },
  );
  expect(response.ok()).toBeTruthy();
  return { email, headers, setId: set.id };
}

async function noOverflow(page: Page) {
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBeTruthy();
}

test("two accounts: create, QR, register return, reconnect, score, results and review at 320px", async ({
  page,
  request,
  browser,
}, testInfo) => {
  const seeded = await seed(request);
  // Context's cookie is separate from the seed API context; sign in through the real form.
  await page.goto(`/study-sets/${seeded.setId}/battle`);
  await expect(page).toHaveURL(/login/);
  await page.locator("input[name=email]").fill(seeded.email);
  await page.locator("input[name=password]").fill(password);
  await page.locator("form button[type=submit], form button").last().click();
  await expect(
    page.getByRole("heading", { name: "Thiết lập trận đấu" }),
  ).toBeVisible();
  await noOverflow(page);
  await page.screenshot({
    path: testInfo.outputPath("01-create-desktop.png"),
    fullPage: true,
  });
  await page.setViewportSize({ width: 320, height: 740 });
  await noOverflow(page);
  await page.screenshot({
    path: testInfo.outputPath("02-create-mobile.png"),
    fullPage: true,
  });
  await page.setViewportSize({ width: 1365, height: 900 });
  await page.getByLabel("Thời gian mỗi câu").selectOption("none");
  await page.getByLabel("Trắc nghiệm", { exact: true }).uncheck();
  await page.getByLabel("Trả lời ngắn", { exact: true }).uncheck();
  await page.getByRole("button", { name: "Tạo phòng & mời bạn" }).click();
  await expect(page).toHaveURL(/battles\//);
  await expect(page.getByText("Trực tiếp", { exact: true })).toBeVisible({
    timeout: 15000,
  });
  const id = page.url().split("/").at(-1)!;
  const code = await page.locator(".battle-code").innerText();
  await expect(page.locator(".battle-qr svg")).toBeVisible();
  await page.screenshot({
    path: testInfo.outputPath("03-lobby-desktop.png"),
    fullPage: true,
  });
  const playerContext = await browser.newContext({
    baseURL: "http://127.0.0.1:5173",
    viewport: { width: 320, height: 740 },
  });
  const player = await playerContext.newPage();
  await player.goto(`/join/${code}`);
  await expect(player).toHaveURL(/login/);
  await player.locator('a[href="/register"]').click();
  await player.locator("input[name=displayName]").fill("Thảo · QA");
  await player
    .locator("input[name=email]")
    .fill(`battle-player-${Date.now()}@studyflow.test`);
  await player.locator("input[name=password]").fill(password);
  await player.locator("form button").last().click();
  await expect(player).toHaveURL(new RegExp(`/join/${code}`));
  await player.getByRole("button", { name: "Vào phòng", exact: true }).click();
  await expect(player).toHaveURL(/battles\//);
  await expect(page.getByText("Thảo · QA")).toBeVisible();
  await noOverflow(player);
  await player.screenshot({
    path: testInfo.outputPath("04-lobby-mobile.png"),
    fullPage: true,
  });
  await player.reload();
  await expect(player.getByText("Trực tiếp", { exact: true })).toBeVisible({
    timeout: 15000,
  });
  await expect(page.locator(".battle-players li")).toHaveCount(2);
  await page.getByRole("button", { name: "Khóa phòng", exact: true }).click();
  await expect(player.getByText("PHÒNG ĐÃ KHÓA")).toBeVisible();
  await page.getByRole("button", { name: "Mở khóa", exact: true }).click();
  await page.getByRole("button", { name: "Bắt đầu trận" }).click();
  for (let i = 0; i < 10; i++) {
    await expect(page.locator(".battle-prompt")).toBeVisible();
    await expect(player.locator(".battle-prompt")).toBeVisible();
    const prompt = await page.locator(".battle-prompt").innerText();
    const expected = cards.find((c) => c[0] === prompt)![1];
    await page.getByLabel("Đáp án của bạn").fill(expected);
    await player
      .getByLabel("Đáp án của bạn")
      .fill(i === 0 ? "Mình chưa nhớ" : expected);
    if (i === 0) {
      await noOverflow(player);
      await page.screenshot({
        path: testInfo.outputPath("05-question-desktop.png"),
        fullPage: true,
      });
      await player.screenshot({
        path: testInfo.outputPath("06-question-mobile.png"),
        fullPage: true,
      });
    }
    await page.getByRole("button", { name: "Chốt đáp án" }).click();
    await player.getByRole("button", { name: "Chốt đáp án" }).click();
    await expect(page.getByText("Chính xác. Giữ nhịp nào!")).toBeVisible();
    if (i === 0)
      await player.screenshot({
        path: testInfo.outputPath("07-feedback-mobile.png"),
        fullPage: true,
      });
    await page
      .getByRole("button", {
        name: i === 9 ? "Xem kết quả" : "Câu tiếp theo",
        exact: true,
      })
      .click();
    if (i < 9) await expect(page.locator(".battle-countdown")).toBeVisible();
  }
  await expect(
    page.getByRole("heading", { name: "Bạn đã dẫn đầu!" }),
  ).toBeVisible();
  await expect(
    player.getByRole("heading", { name: "Thêm một bước tiến." }),
  ).toBeVisible();
  await noOverflow(player);
  await page.screenshot({
    path: testInfo.outputPath("08-results-desktop.png"),
    fullPage: true,
  });
  await player.screenshot({
    path: testInfo.outputPath("09-results-mobile.png"),
    fullPage: true,
  });
  await expect(
    player.getByRole("button", { name: "Cần ôn lại (1)" }),
  ).toBeVisible();
  await player.getByRole("link", { name: "Ôn lại kiến thức" }).click();
  await expect(player).toHaveURL(/study\/.+\/learn/);
  const final = await request.get(`/api/battles/${id}`, {
    headers: seeded.headers,
  });
  expect((await final.json()).status).toBe("Completed");
  await playerContext.close();
});

test("PostgreSQL capacity: parallel requests cannot overbook or duplicate a participant", async ({
  request,
}) => {
  const seeded = await seed(request);
  const response = await request.post("/api/battles", {
    headers: seeded.headers,
    data: {
      studySetId: seeded.setId,
      name: "Capacity QA",
      questionTypes: ["FillBlank"],
      maxPlayers: 10,
      questionCount: 10,
    },
  });
  expect(response.ok()).toBeTruthy();
  const room = await response.json();
  const tokens: string[] = [];
  for (let i = 0; i < 12; i++) {
    const auth = await (
      await request.post("/api/auth/register", {
        data: {
          email: `capacity-${Date.now()}-${i}@studyflow.test`,
          password,
          displayName: `Capacity QA ${i}`,
        },
      })
    ).json();
    tokens.push(auth.accessToken);
  }
  const results = await Promise.all(
    tokens.map((token) =>
      request.post(`/api/battles/${room.id}/join`, {
        headers: { Authorization: `Bearer ${token}` },
      }),
    ),
  );
  expect(results.filter((r) => r.status() === 200)).toHaveLength(9);
  expect(results.filter((r) => r.status() === 409)).toHaveLength(3);
  const token = tokens[results.findIndex((r) => r.status() === 200)];
  await Promise.all(
    Array.from({ length: 4 }, () =>
      request.post(`/api/battles/${room.id}/join`, {
        headers: { Authorization: `Bearer ${token}` },
      }),
    ),
  );
  const state = await (
    await request.get(`/api/battles/${room.id}`, { headers: seeded.headers })
  ).json();
  expect(state.playerCount).toBe(10);
  await request.post(`/api/battles/${room.id}/cancel`, {
    headers: seeded.headers,
  });
});

for (const type of [
  "MultipleChoice",
  "TrueFalse",
  "Matching",
  "Ordering",
  "ShortAnswer",
]) {
  test(`answer interaction: ${type} with keyboard and mobile layout`, async ({
    page,
    request,
  }, testInfo) => {
    const seeded = await seed(request);
    let setId = seeded.setId;
    if (type === "Ordering") {
      const subject = await (
        await request.post("/api/subjects", {
          headers: seeded.headers,
          data: { name: "Quy trình QA" },
        })
      ).json();
      const set = await (
        await request.post(`/api/subjects/${subject.id}/study-sets`, {
          headers: seeded.headers,
          data: { title: "Các bước thực hành" },
        })
      ).json();
      setId = set.id;
      await request.post(`/api/study-sets/${setId}/flashcards/bulk`, {
        headers: seeded.headers,
        data: {
          cards: Array.from({ length: 10 }, (_, i) => ({
            frontText: `Quy trình ${i + 1}`,
            backText:
              "1. Đặt câu hỏi\n2. Đưa ra giả thuyết\n3. Tiến hành thí nghiệm",
            explanation: "Luôn đặt câu hỏi trước khi kiểm chứng.",
          })),
        },
      });
    }
    const response = await request.post("/api/battles", {
      headers: seeded.headers,
      data: {
        studySetId: setId,
        name: `Kiểm thử ${type}`,
        mode: "Mixed",
        questionTypes:
          type === "TrueFalse" ? ["TrueFalse", "FillBlank"] : [type],
        timeLimitSeconds: null,
        questionCount: 10,
        maxPlayers: 10,
      },
    });
    expect(response.ok(), await response.text()).toBeTruthy();
    const room = await response.json();
    await page.goto(`/battles/${room.id}`);
    await page.locator("input[name=email]").fill(seeded.email);
    await page.locator("input[name=password]").fill(password);
    await page.locator("form button").last().click();
    await expect(
      page.getByRole("button", { name: "Bắt đầu trận" }),
    ).toBeVisible();
    await page.setViewportSize({ width: 320, height: 740 });
    await page.getByRole("button", { name: "Bắt đầu trận" }).click();
    await expect(page.locator(".battle-prompt")).toBeVisible();
    if (type === "MultipleChoice") {
      const prompt = await page.locator(".battle-prompt").innerText();
      const answer = cards.find((c) => c[0] === prompt)![1];
      const option = page.getByRole("radio", { name: answer, exact: false });
      await option.focus();
      await page.keyboard.press("Space");
    } else if (type === "TrueFalse") {
      const prompt = await page.locator(".battle-prompt").innerText();
      const [front, value] = prompt.split("\n= ");
      const correct = cards.find((c) => c[0] === front)![1] === value;
      await page
        .getByRole("radio", { name: correct ? "Đúng" : "Sai", exact: true })
        .check();
    } else if (type === "Matching") {
      const leftButtons = page
        .locator(".battle-matching > div")
        .first()
        .getByRole("button");
      for (let i = 0; i < 4; i++) {
        const text = (await leftButtons.nth(i).innerText())
          .replace(/^\d+\s*/, "")
          .trim();
        const front = cards.find((c) => c[0] === text)!;
        await leftButtons.nth(i).click();
        await page
          .locator(".battle-matching > div")
          .last()
          .getByRole("button", { name: front[1], exact: false })
          .click();
      }
    } else if (type === "Ordering") {
      for (const [target, text] of [
        "Đặt câu hỏi",
        "Đưa ra giả thuyết",
        "Tiến hành thí nghiệm",
      ].entries()) {
        let rows = await page.locator(".battle-order li").allTextContents();
        let position = rows.findIndex((row) => row.includes(text));
        while (position > target) {
          await page
            .getByRole("button", {
              name: `Đưa bước ${position + 1} lên`,
              exact: true,
            })
            .click();
          position--;
          rows = await page.locator(".battle-order li").allTextContents();
        }
      }
    } else {
      const prompt = await page.locator(".battle-prompt").innerText();
      await page
        .getByLabel("Đáp án của bạn")
        .fill(cards.find((c) => c[0] === prompt)![1]);
    }
    await noOverflow(page);
    await page.screenshot({
      path: testInfo.outputPath(`${type}-mobile.png`),
      fullPage: true,
    });
    await page.getByRole("button", { name: "Chốt đáp án" }).click();
    await expect(page.getByText("Chính xác. Giữ nhịp nào!")).toBeVisible();
    await request.post(`/api/battles/${room.id}/cancel`, {
      headers: seeded.headers,
    });
  });
}
