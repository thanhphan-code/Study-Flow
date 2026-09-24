import { useState } from "react";
import { Link } from "react-router-dom";
import { Search, ArrowUpRight, BookOpen, Users } from "lucide-react";
import {
  SocialLayout,
  Empty,
  ErrorNotice,
  More,
  SetCard,
  Avatar,
} from "./SocialComponents";
import {
  useSocialList,
  useSocialAction,
  type SocialSet,
  type Person,
} from "./socialApi";

export function ExplorePage({ saved = false }: { saved?: boolean }) {
  const [tab, setTab] = useState("discover");
  const [sort, setSort] = useState("trending");
  const [search, setSearch] = useState("");
  const [draft, setDraft] = useState("");
  const [type, setType] = useState("sets");
  const unavailable = useSocialList<{ id: string }>(
    "/saved/unavailable",
    saved,
  );
  const remove = useSocialAction();
  const sets = useSocialList<SocialSet>(
    `/sets?tab=${saved ? "saved" : tab}&sort=${sort}&q=${encodeURIComponent(search)}`,
    type === "sets",
  );
  const people = useSocialList<Person>(
    `/people?q=${encodeURIComponent(search)}`,
    type === "people",
  );
  const active = type === "sets" ? sets : people;
  const cards = sets.data?.pages.flatMap((p) => p.items) ?? [];
  return (
    <SocialLayout>
      <header className="social-heading">
        <span className="social-eyebrow">STUDYFLOW COMMUNITY</span>
        <h1>
          {saved ? (
            "Góc học đã lưu"
          ) : (
            <>
              Kiến thức hay.
              <br />
              <span>Học cùng nhau.</span>
            </>
          )}
        </h1>
        <p>
          {saved
            ? "Những bộ học bạn muốn quay lại. Nội dung cập nhật từ tác giả."
            : "Khám phá bộ học, tìm bạn đồng hành và chia sẻ điều bạn biết."}
        </p>
      </header>
      {!saved && (
        <form
          className="social-search"
          role="search"
          onSubmit={(e) => {
            e.preventDefault();
            setSearch(draft);
          }}
        >
          <Search size={21} />
          <input
            aria-label="Tìm bộ học hoặc người dùng"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            placeholder="Bạn muốn học gì hôm nay?"
            maxLength={100}
          />
          <button className="social-button primary">Tìm kiếm</button>
        </form>
      )}
      <div className="social-toolbar">
        <div className="social-tabs" aria-label="Nguồn nội dung">
          {!saved &&
            [
              ["discover", "Dành cho bạn"],
              ["following", "Đang theo dõi"],
              ["friends", "Bạn bè"],
            ].map(([key, label]) => (
              <button
                key={key}
                aria-pressed={tab === key}
                className={tab === key ? "active" : ""}
                onClick={() => {
                  setTab(key);
                  setType("sets");
                }}
              >
                {label}
              </button>
            ))}
        </div>
        <select
          aria-label="Sắp xếp bộ học"
          value={sort}
          onChange={(e) => setSort(e.target.value)}
        >
          <option value="trending">Đang được quan tâm</option>
          <option value="newest">Mới nhất</option>
          <option value="saved">Được lưu nhiều</option>
          <option value="liked">Được thích nhiều</option>
          <option value="updated">Vừa cập nhật</option>
        </select>
      </div>
      {!saved && (
        <div className="social-filters">
          <button
            className={type === "sets" ? "selected" : ""}
            onClick={() => setType("sets")}
          >
            <BookOpen size={16} />
            Bộ học
          </button>
          <button
            className={type === "people" ? "selected" : ""}
            onClick={() => setType("people")}
          >
            <Users size={16} />
            Người học
          </button>
          {type === "sets" &&
            [
              "Tất cả",
              "Ngoại ngữ",
              "Vật lý",
              "Sinh học",
              "Lập trình",
              "Y khoa",
              "Ôn thi",
            ].map((topic) => (
              <button
                key={topic}
                className={
                  search === topic || (topic === "Tất cả" && !search)
                    ? "selected"
                    : ""
                }
                onClick={() => {
                  setSearch(topic === "Tất cả" ? "" : topic);
                  setDraft(topic === "Tất cả" ? "" : topic);
                }}
              >
                {topic}
              </button>
            ))}
        </div>
      )}
      <ErrorNotice error={active.error} />
      {active.isPending && (
        <div className="social-skeleton" role="status">
          Đang tìm nội dung cho bạn…
        </div>
      )}
      {type === "sets" ? (
        <>
          <div className="social-set-grid">
            {cards.map((s) => (
              <SetCard key={s.id} item={s} />
            ))}
          </div>
          {!sets.isPending && !sets.isError && cards.length === 0 && (
            <Empty
              title={
                saved
                  ? "Chưa có bộ học đã lưu có thể xem"
                  : "Chưa có bộ học phù hợp"
              }
            >
              {saved
                ? "Bộ học chuyển sang riêng tư sẽ không xuất hiện. Khám phá để lưu thêm nội dung."
                : "Thử chủ đề khác hoặc chia sẻ bộ học đầu tiên của bạn."}
              <br />
              <Link
                to={saved ? "/explore" : "/subjects"}
                className="social-text-link"
              >
                {saved ? "Khám phá bộ học" : "Mở bộ học của tôi"}{" "}
                <ArrowUpRight size={15} />
              </Link>
            </Empty>
          )}
        </>
      ) : (
        <div className="social-people-grid">
          {people.data?.pages
            .flatMap((p) => p.items)
            .map((p) => (
              <Link
                className="social-person-card"
                key={p.userId}
                to={`/people/${p.username}`}
              >
                <Avatar person={p} large />
                <h3>{p.displayName}</h3>
                <span>@{p.username}</span>
                <p>{p.bio || "Cùng kết nối và học tập."}</p>
              </Link>
            ))}
          {people.data?.pages[0].items.length === 0 && (
            <Empty title="Chưa tìm thấy người học">
              Thử tìm bằng tên người dùng.
            </Empty>
          )}
        </div>
      )}
      <More
        more={active.hasNextPage}
        pending={active.isFetchingNextPage}
        load={() => {
          void active.fetchNextPage();
        }}
      />
      {saved && (
        <>
          <ErrorNotice error={unavailable.error || remove.error} />
          {unavailable.data?.pages
            .flatMap((p) => p.items)
            .map((item) => (
              <div className="social-person-row" key={item.id}>
                <div className="social-person-info">
                  <strong>Bộ học không còn khả dụng</strong>
                  <small>
                    Tác giả đã giới hạn quyền xem hoặc xóa nội dung.
                  </small>
                </div>
                <button
                  className="social-button"
                  disabled={remove.isPending}
                  onClick={() =>
                    remove.mutate({
                      path: `/reactions/${item.id}/Save`,
                      method: "DELETE",
                    })
                  }
                >
                  Bỏ lưu
                </button>
              </div>
            ))}
          <More
            more={unavailable.hasNextPage}
            pending={unavailable.isFetchingNextPage}
            load={() => {
              void unavailable.fetchNextPage();
            }}
          />
        </>
      )}
    </SocialLayout>
  );
}
