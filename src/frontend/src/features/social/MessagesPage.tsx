import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Send,
  MessageCircle,
  Swords,
  BookOpen,
  Bell,
  CheckCheck,
} from "lucide-react";
import {
  Avatar,
  Empty,
  ErrorNotice,
  More,
  SocialLayout,
} from "./SocialComponents";
import {
  socialSend,
  useSocialAction,
  useSocialList,
  type Conversation,
  type Message,
  type Notice,
} from "./socialApi";

export function MessagesPage() {
  const { id = "" } = useParams();
  const conversations = useSocialList<Conversation>(
    "/conversations",
    true,
    15000,
  );
  const messages = useSocialList<Message>(
    `/conversations/${id}/messages`,
    !!id,
    5000,
  );
  const action = useSocialAction();
  const [text, setText] = useState("");
  const bottom = useRef<HTMLDivElement>(null);
  const items =
    messages.data?.pages
      .flatMap((p) => p.items)
      .slice()
      .reverse() ?? [];
  const latestId = messages.data?.pages[0].items[0]?.id;
  const latestOwn = messages.data?.pages[0].items[0]?.isOwn;
  const person = conversations.data?.pages
    .flatMap((p) => p.items)
    .find((c) => c.id === id)?.person;
  useEffect(() => {
    if (!id || !latestId || document.visibilityState !== "visible") return;
    if (!latestOwn)
      void socialSend(`/conversations/${id}/read`).catch(() => {});
    const messageList = bottom.current?.parentElement;
    if (messageList) messageList.scrollTop = messageList.scrollHeight;
  }, [id, latestId, latestOwn]);
  return (
    <SocialLayout>
      <header className="social-heading compact">
        <h1>Tin nhắn</h1>
        <p>Một câu hỏi nhỏ. Một bước tiến mới.</p>
      </header>
      <div className={`social-inbox ${id ? "has-conversation" : ""}`}>
        <aside className="social-conversations">
          <h2>Bạn bè của bạn</h2>
          <ErrorNotice error={conversations.error} />
          {conversations.data?.pages
            .flatMap((p) => p.items)
            .map((c) => (
              <Link
                key={c.id}
                to={`/messages/${c.id}`}
                className={`social-conversation ${c.id === id ? "active" : ""}`}
              >
                <Avatar person={c.person} />
                <div>
                  <strong>{c.person.displayName}</strong>
                  <small>@{c.person.username}</small>
                </div>
                {c.unread > 0 && (
                  <span className="social-unread">{c.unread}</span>
                )}
              </Link>
            ))}
          {conversations.data?.pages[0].items.length === 0 && (
            <Empty title="Bắt đầu trò chuyện">
              <Link to="/community/friends" className="social-text-link">
                Chọn một người bạn
              </Link>
            </Empty>
          )}
          <More
            more={conversations.hasNextPage}
            pending={conversations.isFetchingNextPage}
            load={() => {
              void conversations.fetchNextPage();
            }}
          />
        </aside>
        <section className="social-thread">
          {id ? (
            <>
              <header>
                <Link
                  to="/messages"
                  className="social-icon social-inbox-back"
                  aria-label="Về danh sách tin nhắn"
                >
                  <ArrowLeft size={20} />
                </Link>
                {person && <Avatar person={person} />}
                <div>
                  <strong>{person?.displayName ?? "Cuộc trò chuyện"}</strong>
                  <small>Chỉ bạn bè có thể nhắn tin</small>
                </div>
              </header>
              <div className="social-messages" role="log" aria-label="Tin nhắn">
                <ErrorNotice error={messages.error} />
                {messages.isPending && <p role="status">Đang tải tin nhắn…</p>}
                <More
                  more={messages.hasNextPage}
                  pending={messages.isFetchingNextPage}
                  load={() => {
                    void messages.fetchNextPage();
                  }}
                />
                {items.map((m) => (
                  <article
                    key={m.id}
                    className={`social-message ${m.isOwn ? "own" : ""}`}
                  >
                    {m.kind !== "Text" &&
                      (m.shared ? (
                        <Link
                          to={m.shared.url}
                          className="social-shared-message"
                        >
                          {m.kind === "BattleInvite" ? (
                            <Swords size={22} />
                          ) : (
                            <BookOpen size={22} />
                          )}
                          <div>
                            <small>
                              {m.kind === "BattleInvite"
                                ? "Lời mời Live Battle"
                                : "Bộ học được chia sẻ"}
                            </small>
                            <strong>{m.shared.title}</strong>
                            <span>
                              {m.kind === "BattleInvite"
                                ? "Vào phòng →"
                                : "Mở bộ học →"}
                            </span>
                          </div>
                        </Link>
                      ) : (
                        <p className="social-muted">
                          Nội dung không còn khả dụng hoặc đã giới hạn quyền
                          xem.
                        </p>
                      ))}
                    {m.content && <p>{m.content}</p>}
                    <small>
                      {new Date(m.createdAt).toLocaleTimeString("vi-VN", {
                        hour: "2-digit",
                        minute: "2-digit",
                      })}
                      {m.isOwn && (m.isRead ? " · Đã xem" : " · Đã gửi")}
                    </small>
                  </article>
                ))}
                {messages.data?.pages[0].items.length === 0 && (
                  <Empty title="Nói lời chào nhé">
                    Hỏi bài, trao đổi cách học hoặc mời nhau vào Battle.
                  </Empty>
                )}
                <div ref={bottom} />
              </div>
              <form
                className="social-compose"
                onSubmit={(e) => {
                  e.preventDefault();
                  void action
                    .mutateAsync({
                      path: `/conversations/${id}/messages`,
                      body: { content: text },
                    })
                    .then(() => setText(""))
                    .catch(() => {});
                }}
              >
                <textarea
                  aria-label="Tin nhắn"
                  value={text}
                  onChange={(e) => setText(e.target.value)}
                  placeholder="Viết tin nhắn…"
                  maxLength={4000}
                  rows={2}
                  disabled={messages.isError}
                />
                <button
                  className="social-button primary"
                  disabled={
                    action.isPending || !text.trim() || messages.isError
                  }
                  aria-label="Gửi tin nhắn"
                >
                  <Send size={19} />
                </button>
                <ErrorNotice error={action.error} />
              </form>
            </>
          ) : (
            <Empty title="Học cùng nhau từ một cuộc trò chuyện">
              <MessageCircle size={28} />
              <br />
              Chọn bạn bè để trao đổi kiến thức.
            </Empty>
          )}
        </section>
      </div>
    </SocialLayout>
  );
}

const noticeText: Record<string, string> = {
  StudySetPublished: "đã chia sẻ bộ học mới",
  Follow: "đã theo dõi bạn",
  FriendRequest: "đã gửi lời mời kết bạn",
  FriendAccepted: "đã chấp nhận lời mời kết bạn",
  Like: "đã thích bộ học của bạn",
  Save: "đã lưu bộ học của bạn",
  Comment: "đã bình luận về bộ học",
  Reply: "đã trả lời bình luận của bạn",
  Message: "đã gửi tin nhắn",
  BattleInvite: "đã mời bạn vào Battle",
};
export function NotificationsPage() {
  const list = useSocialList<Notice>("/notifications", true, 15000);
  const action = useSocialAction();
  return (
    <SocialLayout>
      <header className="social-section-heading">
        <div>
          <h1>Thông báo</h1>
          <p className="social-muted">
            Những kết nối mới quanh việc học của bạn.
          </p>
        </div>
        <button
          className="social-button"
          disabled={action.isPending}
          onClick={() => action.mutate({ path: "/notifications/read-all" })}
        >
          <CheckCheck size={18} />
          Đọc tất cả
        </button>
      </header>
      <ErrorNotice error={list.error || action.error} />
      {list.isPending && <p role="status">Đang tải thông báo…</p>}
      <div className="social-panel">
        {list.data?.pages
          .flatMap((p) => p.items)
          .map((n) => (
            <Link
              className={`social-notice ${n.isRead ? "" : "unread"}`}
              key={n.id}
              to={
                ["Message", "BattleInvite"].includes(n.kind)
                  ? `/messages/${n.entityId}`
                  : ["Follow", "FriendAccepted", "FriendRequest"].includes(
                        n.kind,
                      )
                    ? `/people/${n.actor.username}`
                    : `/community/sets/${n.entityId}`
              }
              onClick={() =>
                action.mutate({ path: `/notifications/${n.id}/read` })
              }
            >
              <Avatar person={n.actor} />
              <div>
                <p>
                  <strong>{n.actor.displayName}</strong>{" "}
                  {noticeText[n.kind] ?? "đã tương tác với bạn"}.
                </p>
                <small>{new Date(n.createdAt).toLocaleString("vi-VN")}</small>
              </div>
              {!n.isRead && <Bell size={17} aria-label="Chưa đọc" />}
            </Link>
          ))}
        {list.data?.pages[0].items.length === 0 && (
          <Empty title="Bạn đã xem hết rồi">
            Thông báo mới sẽ xuất hiện tại đây.
          </Empty>
        )}
      </div>
      <More
        more={list.hasNextPage}
        pending={list.isFetchingNextPage}
        load={() => {
          void list.fetchNextPage();
        }}
      />
    </SocialLayout>
  );
}
