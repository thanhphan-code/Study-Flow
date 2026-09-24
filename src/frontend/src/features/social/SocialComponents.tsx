import { useEffect, useRef, useState, type PropsWithChildren } from "react";
import { Link, NavLink } from "react-router-dom";
import {
  BookOpen,
  Bookmark,
  Compass,
  Heart,
  MessageCircle,
  Users,
  X,
  ShieldCheck,
  ArrowRight,
} from "lucide-react";
import {
  useSocialAction,
  useSocialList,
  type Person,
  type SocialSet,
} from "./socialApi";
import "./social.css";

export function Avatar({
  person,
  large = false,
}: {
  person: Person;
  large?: boolean;
}) {
  const [failed, setFailed] = useState(false);
  return (
    <span className={`social-avatar ${large ? "large" : ""}`} aria-hidden>
      {person.avatarUrl && !failed ? (
        <img
          src={person.avatarUrl}
          alt=""
          referrerPolicy="no-referrer"
          onError={() => setFailed(true)}
        />
      ) : (
        person.displayName.trim().slice(0, 2).toUpperCase()
      )}
    </span>
  );
}
export function SocialLayout({ children }: PropsWithChildren) {
  return (
    <main className="social-layout">
      <aside className="social-sidebar">
        <span className="social-sidebar-title">Cùng nhau học tốt hơn</span>
        <nav aria-label="Cộng đồng">
          <NavLink to="/explore">
            <Compass size={19} />
            Khám phá
          </NavLink>
          <NavLink to="/community/saved">
            <Bookmark size={19} />
            Bộ học đã lưu
          </NavLink>
          <NavLink to="/community/friends">
            <Users size={19} />
            Bạn bè
          </NavLink>
          <NavLink to="/messages">
            <MessageCircle size={19} />
            Tin nhắn
          </NavLink>
        </nav>
        <div className="social-privacy-note">
          <ShieldCheck size={24} />
          <strong>Việc học là của bạn.</strong>
          <p>
            Tiến độ và tài liệu nguồn luôn riêng tư. Bạn chọn nội dung muốn chia
            sẻ.
          </p>
        </div>
      </aside>
      <div className="social-main">{children}</div>
    </main>
  );
}
export function ErrorNotice({ error }: { error: unknown }) {
  return error ? (
    <p role="alert" className="social-error">
      {error instanceof Error
        ? error.message
        : "Không thể thực hiện. Vui lòng thử lại."}
    </p>
  ) : null;
}
export function Empty({
  title,
  children,
}: PropsWithChildren<{ title: string }>) {
  return (
    <div className="social-empty">
      <BookOpen size={32} />
      <h3>{title}</h3>
      <p>{children}</p>
    </div>
  );
}
export function More({
  more,
  pending,
  load,
}: {
  more: boolean;
  pending: boolean;
  load: () => void;
}) {
  return more ? (
    <button
      className="social-button social-load"
      disabled={pending}
      onClick={load}
    >
      {pending ? "Đang tải…" : "Xem thêm"}
    </button>
  ) : null;
}
export function Dialog({
  title,
  onClose,
  children,
}: PropsWithChildren<{ title: string; onClose: () => void }>) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    ref.current?.showModal();
    const node = ref.current;
    return () => node?.close();
  }, []);
  return (
    <dialog
      ref={ref}
      className="social-dialog"
      aria-label={title}
      onCancel={onClose}
    >
      <header>
        <h2>{title}</h2>
        <button aria-label="Đóng" className="social-icon" onClick={onClose}>
          <X size={20} />
        </button>
      </header>
      {children}
    </dialog>
  );
}
export function SetCard({ item }: { item: SocialSet }) {
  const action = useSocialAction();
  return (
    <article className="social-set-card">
      <div className="social-card-top">
        <span className="social-subject">
          {item.tags.split(",")[0] || "Bộ học cộng đồng"}
        </span>
        <button
          aria-label={item.isSaved ? "Bỏ lưu" : "Lưu bộ học"}
          aria-pressed={item.isSaved}
          className={`social-icon ${item.isSaved ? "selected" : ""}`}
          disabled={action.isPending}
          onClick={() =>
            action.mutate({
              path: `/reactions/${item.id}/Save`,
              method: item.isSaved ? "DELETE" : "PUT",
            })
          }
        >
          <Bookmark size={20} fill={item.isSaved ? "currentColor" : "none"} />
        </button>
      </div>
      <Link to={`/community/sets/${item.id}`} className="social-set-title">
        <h3>{item.title}</h3>
        <p>{item.description || "Mở bộ học và khám phá kiến thức mới."}</p>
      </Link>
      <span className="social-meta">
        <BookOpen size={15} />
        {item.cardCount} thẻ <Heart size={15} />
        {item.likes} <Bookmark size={15} />
        {item.saves}
      </span>
      <footer>
        <Link to={`/people/${item.author.username}`} className="social-person">
          <Avatar person={item.author} />
          <span>{item.author.displayName}</span>
        </Link>
        <Link
          className="social-study-link"
          to={`/community/sets/${item.id}?study=1`}
          aria-label={`Học ${item.title}`}
        >
          <ArrowRight size={20} />
        </Link>
      </footer>
      <ErrorNotice error={action.error} />
    </article>
  );
}
export function ShareDialog({
  kind,
  id,
  onClose,
}: {
  kind: string;
  id: string;
  onClose: () => void;
}) {
  const friends = useSocialList<Person>("/people?kind=friends");
  const action = useSocialAction<{ id: string }>();
  const send = useSocialAction();
  const [done, setDone] = useState<string[]>([]);
  const [copy, setCopy] = useState(false);
  async function share(person: Person) {
    const conversation = await action.mutateAsync({
      path: `/conversations/direct/${person.userId}`,
    });
    await send.mutateAsync({
      path: `/conversations/${conversation.id}/messages`,
      body: { kind, sharedId: id },
    });
    setDone((x) => [...x, person.userId]);
  }
  return (
    <Dialog title="Chia sẻ với bạn bè" onClose={onClose}>
      <p className="social-muted">
        Người nhận phải có quyền xem nội dung. Đổi bộ học riêng tư sang “Bạn bè”
        trước khi gửi.
      </p>
      {kind === "StudySetShare" && (
        <button
          className="social-button"
          onClick={() => {
            void navigator.clipboard
              .writeText(`${location.origin}/community/sets/${id}`)
              .then(() => setCopy(true))
              .catch(() => setCopy(false));
          }}
        >
          {copy ? "Đã sao chép liên kết" : "Sao chép liên kết"}
        </button>
      )}
      <ErrorNotice error={action.error || send.error || friends.error} />
      {friends.isPending && <p role="status">Đang tải bạn bè…</p>}
      {friends.data?.pages
        .flatMap((p) => p.items)
        .map((p) => (
          <div className="social-person-row" key={p.userId}>
            <Avatar person={p} />
            <span>{p.displayName}</span>
            <button
              className="social-button"
              disabled={
                action.isPending || send.isPending || done.includes(p.userId)
              }
              onClick={() => {
                void share(p).catch(() => {});
              }}
            >
              {done.includes(p.userId) ? "Đã gửi" : "Gửi"}
            </button>
          </div>
        ))}
      {friends.data?.pages[0].items.length === 0 && (
        <Empty title="Chưa có bạn bè">
          Kết bạn để chia sẻ bộ học và mời Battle.
        </Empty>
      )}
      <More
        more={friends.hasNextPage}
        pending={friends.isFetchingNextPage}
        load={() => {
          void friends.fetchNextPage();
        }}
      />
    </Dialog>
  );
}
export function ReportDialog({
  targetType,
  targetId,
  onClose,
}: {
  targetType: string;
  targetId: string;
  onClose: () => void;
}) {
  const action = useSocialAction();
  return (
    <Dialog title="Báo cáo nội dung" onClose={onClose}>
      {action.isSuccess ? (
        <p role="status">
          Đã gửi báo cáo. Cảm ơn bạn đã giúp cộng đồng an toàn hơn.
        </p>
      ) : (
        <form
          className="social-form"
          onSubmit={(e) => {
            e.preventDefault();
            const f = new FormData(e.currentTarget);
            action.mutate({
              path: "/reports",
              body: {
                targetType,
                targetId,
                reason: f.get("reason"),
                details: f.get("details"),
              },
            });
          }}
        >
          <label>
            Lý do
            <select name="reason">
              <option value="Spam">Spam</option>
              <option value="Harassment">Quấy rối</option>
              <option value="InappropriateContent">
                Nội dung không phù hợp
              </option>
              <option value="Copyright">Vi phạm bản quyền</option>
              <option value="Impersonation">Mạo danh</option>
              <option value="Other">Lý do khác</option>
            </select>
          </label>
          <label>
            Thông tin bổ sung
            <textarea name="details" maxLength={2000} rows={3} />
          </label>
          <ErrorNotice error={action.error} />
          <button className="social-button primary" disabled={action.isPending}>
            Gửi báo cáo
          </button>
        </form>
      )}
    </Dialog>
  );
}
