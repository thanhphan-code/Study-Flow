import { useEffect, useState } from "react";
import { PublicPractice } from "./PublicPractice";
import { FlashcardImage } from "@/features/flashcards/components/FlashcardImage";
import { apiBaseUrl } from "@/api/httpClient";
import { useQuery } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  Heart,
  Bookmark,
  Copy,
  Share2,
  Play,
  LockKeyhole,
  Settings2,
  Flag,
  ArrowLeft,
  ArrowRight,
} from "lucide-react";
import {
  SocialLayout,
  Avatar,
  Dialog,
  ShareDialog,
  ReportDialog,
  ErrorNotice,
  More,
  Empty,
} from "./SocialComponents";
import {
  socialGet,
  useSocialAction,
  useSocialList,
  visibilityLabels,
  type SetDetail,
  type Comment,
} from "./socialApi";

export function PublicationForm({
  item,
  onClose,
}: {
  item: SetDetail;
  onClose: () => void;
}) {
  const action = useSocialAction();
  const [visibility, setVisibility] = useState(item.visibility);
  const help: Record<string, string> = {
    Private: "Chỉ bạn xem được. Bộ học không xuất hiện trong tìm kiếm.",
    FriendsOnly: "Bạn và những người đã kết bạn có thể xem và học.",
    Public: "Mọi người đăng nhập StudyFlow có thể tìm, xem và học.",
    Unlisted: "Chỉ người có liên kết xem được. Không xuất hiện ở Khám phá.",
  };
  return (
    <Dialog title="Quyền chia sẻ bộ học" onClose={onClose}>
      <form
        className="social-form"
        onSubmit={(e) => {
          e.preventDefault();
          const f = new FormData(e.currentTarget);
          void action
            .mutateAsync({
              path: `/sets/${item.id}/publication`,
              method: "PUT",
              body: {
                visibility,
                allowComments: f.has("comments"),
                allowRemix: f.has("remix"),
                tags: f.get("tags"),
              },
            })
            .then(onClose)
            .catch(() => {});
        }}
      >
        <label>
          Ai được xem?
          <select
            value={visibility}
            onChange={(e) => setVisibility(e.target.value)}
          >
            {Object.entries(visibilityLabels).map(([value, label]) => (
              <option value={value} key={value}>
                {label}
              </option>
            ))}
          </select>
        </label>
        <p className="social-setting-help">{help[visibility]}</p>
        <label>
          Chủ đề, phân cách bằng dấu phẩy
          <input
            name="tags"
            defaultValue={item.tags}
            maxLength={200}
            placeholder="Ngoại ngữ, IELTS, Từ vựng"
          />
        </label>
        <label className="social-check">
          <input
            name="comments"
            type="checkbox"
            defaultChecked={item.allowComments}
          />
          Cho phép bình luận
        </label>
        <label className="social-check">
          <input
            name="remix"
            type="checkbox"
            defaultChecked={item.allowRemix}
          />
          Cho phép tạo bản sao riêng để chỉnh sửa
        </label>
        <div className="social-setting-help">
          <LockKeyhole size={18} />
          <span>
            Tài liệu nguồn và tiến độ học không được chia sẻ. Người khác chỉ
            thấy nội dung thẻ bạn xuất bản.
          </span>
        </div>
        <ErrorNotice error={action.error} />
        <button className="social-button primary" disabled={action.isPending}>
          {action.isPending ? "Đang lưu…" : "Lưu quyền chia sẻ"}
        </button>
      </form>
    </Dialog>
  );
}

function Comments({ item }: { item: SetDetail }) {
  const list = useSocialList<Comment>(`/sets/${item.id}/comments`);
  const action = useSocialAction();
  const [content, setContent] = useState("");
  const [reply, setReply] = useState<Comment | null>(null);
  const [editing, setEditing] = useState<Comment | null>(null);
  const [report, setReport] = useState<string | null>(null);
  return (
    <section className="social-comments">
      <h2>Trao đổi về bộ học</h2>
      <p className="social-muted">
        Đặt câu hỏi, góp ý và cùng hiểu bài sâu hơn.
      </p>
      {item.allowComments ? (
        <form
          className="social-form"
          onSubmit={(e) => {
            e.preventDefault();
            void action
              .mutateAsync({
                path: `/sets/${item.id}/comments`,
                body: { content, parentCommentId: reply?.id },
              })
              .then(() => {
                setContent("");
                setReply(null);
              })
              .catch(() => {});
          }}
        >
          {reply && (
            <div className="social-reply-note">
              Trả lời {reply.author.displayName}
              <button type="button" onClick={() => setReply(null)}>
                Hủy
              </button>
            </div>
          )}
          <textarea
            aria-label="Viết bình luận"
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="Bạn có câu hỏi hay cách ghi nhớ nào hay?"
            maxLength={2000}
            rows={3}
            required
          />
          <button
            className="social-button primary"
            disabled={action.isPending || !content.trim()}
          >
            Gửi bình luận
          </button>
        </form>
      ) : (
        <p className="social-setting-help">Tác giả đã tắt bình luận.</p>
      )}
      <ErrorNotice error={list.error || action.error} />
      {list.data?.pages
        .flatMap((p) => p.items)
        .map((c) => (
          <article
            className={`social-comment ${c.parentCommentId ? "reply" : ""}`}
            key={c.id}
          >
            <Avatar person={c.author} />
            <div>
              <Link to={`/people/${c.author.username}`}>
                <strong>{c.author.displayName}</strong>
              </Link>
              <small>
                {new Date(c.createdAt).toLocaleDateString("vi-VN")}
                {c.parentCommentId ? " · Phản hồi" : ""}
              </small>
              <p>{c.content}</p>
              {!c.isDeleted && (
                <div className="social-comment-actions">
                  {item.allowComments && !c.parentCommentId && (
                    <button
                      onClick={() => {
                        setReply(c);
                        document
                          .querySelector<HTMLTextAreaElement>(
                            '[aria-label="Viết bình luận"]',
                          )
                          ?.focus();
                      }}
                    >
                      Trả lời
                    </button>
                  )}
                  {c.isOwn ? (
                    <>
                      <button onClick={() => setEditing(c)}>Sửa</button>
                      <button
                        disabled={action.isPending}
                        onClick={() =>
                          action.mutate({
                            path: `/comments/${c.id}`,
                            method: "DELETE",
                          })
                        }
                      >
                        Xóa
                      </button>
                    </>
                  ) : (
                    <button onClick={() => setReport(c.id)}>Báo cáo</button>
                  )}
                </div>
              )}
            </div>
          </article>
        ))}
      {list.data?.pages[0].items.length === 0 && (
        <p className="social-muted">
          Chưa có bình luận. Bắt đầu cuộc trao đổi đầu tiên.
        </p>
      )}
      <More
        more={list.hasNextPage}
        pending={list.isFetchingNextPage}
        load={() => {
          void list.fetchNextPage();
        }}
      />
      {editing && (
        <Dialog title="Sửa bình luận" onClose={() => setEditing(null)}>
          <form
            className="social-form"
            onSubmit={(e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              void action
                .mutateAsync({
                  path: `/comments/${editing.id}`,
                  method: "PUT",
                  body: { content: f.get("content") },
                })
                .then(() => setEditing(null))
                .catch(() => {});
            }}
          >
            <textarea
              name="content"
              aria-label="Nội dung bình luận"
              defaultValue={editing.content}
              maxLength={2000}
              required
            />
            <ErrorNotice error={action.error} />
            <button
              className="social-button primary"
              disabled={action.isPending}
            >
              Lưu
            </button>
          </form>
        </Dialog>
      )}
      {report && (
        <ReportDialog
          targetType="Comment"
          targetId={report}
          onClose={() => setReport(null)}
        />
      )}
    </section>
  );
}

export function CommunitySetPage() {
  const { id = "" } = useParams();
  const query = useQuery({
    queryKey: ["social", "set", id],
    queryFn: () => socialGet<SetDetail>(`/sets/${id}`),
    retry: false,
  });
  const item = query.data;
  const action = useSocialAction<{ studySetId: string }>();
  const navigate = useNavigate();
  const [settings, setSettings] = useState(false);
  const [sharing, setSharing] = useState<string | null>(null);
  const [report, setReport] = useState(false);
  const [practice, setPractice] = useState(
    new URLSearchParams(location.search).has("study"),
  );
  const [count, setCount] = useState(
    location.hash.startsWith("#card-") ? 500 : 20,
  );
  useEffect(() => {
    if (item && location.hash.startsWith("#card-"))
      document
        .getElementById(location.hash.slice(1))
        ?.scrollIntoView({ block: "center" });
  }, [item]);
  return (
    <SocialLayout>
      <Link className="social-back" to="/explore">
        <ArrowLeft size={17} />
        Khám phá bộ học
      </Link>
      <ErrorNotice error={query.error} />
      {query.isPending && <p role="status">Đang tải bộ học…</p>}
      {query.isError && (
        <Link className="social-button" to="/explore">
          Tìm bộ học khác
        </Link>
      )}
      {item && (
        <>
          <section className="social-set-heading">
            <div className="social-card-top">
              <span className="social-subject">
                {visibilityLabels[item.visibility]}
              </span>
              {item.isOwn ? (
                <button
                  className="social-button"
                  onClick={() => setSettings(true)}
                >
                  <Settings2 size={17} />
                  Quyền chia sẻ
                </button>
              ) : (
                <button
                  aria-label="Báo cáo bộ học"
                  className="social-icon"
                  onClick={() => setReport(true)}
                >
                  <Flag size={18} />
                </button>
              )}
            </div>
            <h1>{item.title}</h1>
            <p>{item.description}</p>
            <div className="social-person">
              <Avatar person={item.author} />
              <Link to={`/people/${item.author.username || "me"}`}>
                {item.author.displayName}
              </Link>
              <span className="social-muted">· {item.cards.length} thẻ</span>
            </div>
            {item.attribution && (
              <p className="social-attribution">
                Bản sao từ{" "}
                {item.attribution.studySetId ? (
                  <Link to={`/community/sets/${item.attribution.studySetId}`}>
                    {item.attribution.title}
                  </Link>
                ) : (
                  item.attribution.title
                )}{" "}
                · {item.attribution.author}
              </p>
            )}
            <div className="social-actions social-set-actions">
              <button
                className="social-button primary"
                disabled={!item.cards.length}
                onClick={() => setPractice(true)}
              >
                <Play size={18} />
                Học ngay
              </button>
              <button
                className={`social-button ${item.isLiked ? "selected" : ""}`}
                aria-pressed={item.isLiked}
                disabled={action.isPending}
                onClick={() =>
                  action.mutate({
                    path: `/reactions/${id}/Like`,
                    method: item.isLiked ? "DELETE" : "PUT",
                  })
                }
              >
                <Heart
                  size={17}
                  fill={item.isLiked ? "currentColor" : "none"}
                />
                {item.likes}
              </button>
              <button
                className={`social-button ${item.isSaved ? "selected" : ""}`}
                aria-pressed={item.isSaved}
                disabled={action.isPending}
                onClick={() =>
                  action.mutate({
                    path: `/reactions/${id}/Save`,
                    method: item.isSaved ? "DELETE" : "PUT",
                  })
                }
              >
                <Bookmark size={17} />
                {item.isSaved ? "Đã lưu" : "Lưu"}
              </button>
              <button className="social-button" onClick={() => setSharing(id)}>
                <Share2 size={17} />
                Chia sẻ
              </button>
              {item.allowRemix && (
                <button
                  className="social-button"
                  disabled={action.isPending}
                  onClick={() => {
                    void action
                      .mutateAsync({ path: `/sets/${id}/remix` })
                      .then((r) => navigate(`/study-sets/${r.studySetId}`))
                      .catch(() => {});
                  }}
                >
                  <Copy size={17} />
                  Tạo bản sao
                </button>
              )}
              {item.isOwn && (
                <Link className="social-button" to={`/study-sets/${id}`}>
                  Chỉnh sửa nội dung
                  <ArrowRight size={17} />
                </Link>
              )}
            </div>
            <ErrorNotice error={action.error} />
          </section>
          <div className="social-section-heading">
            <h2>Nội dung bộ học</h2>
            <span>{item.cards.length} thẻ</span>
          </div>
          <div className="social-flashcards">
            {item.cards.slice(0, count).map((card, index) => (
              <article
                id={`card-${card.id}`}
                key={card.id}
                className="social-flashcard"
              >
                <span className="social-card-number">
                  {String(index + 1).padStart(2, "0")}
                </span>
                <div>
                  <h3>{card.frontText}</h3>
                  {card.hasImage && (
                    <FlashcardImage
                      imageUrl={`${apiBaseUrl}/social/sets/${id}/cards/${card.id}/image`}
                      alt="Ảnh minh họa thẻ"
                      className="social-card-image"
                    />
                  )}
                  {card.readingText && (
                    <small>
                      {card.readingText} {card.romanization}
                    </small>
                  )}
                  <p>{card.backText}</p>
                  {card.explanation && (
                    <details>
                      <summary>Giải thích</summary>
                      <p>{card.explanation}</p>
                    </details>
                  )}
                  {card.hasPrivateSource && (
                    <span className="social-private-source">
                      <LockKeyhole size={13} />
                      Nguồn được lưu riêng tư
                    </span>
                  )}
                </div>
                <div className="social-card-tools">
                  <button
                    className={`social-icon ${card.isLiked ? "selected" : ""}`}
                    aria-label={`Thích thẻ ${index + 1}`}
                    aria-pressed={card.isLiked}
                    disabled={action.isPending}
                    onClick={() =>
                      action.mutate({
                        path: `/reactions/${card.id}/CardLike`,
                        method: card.isLiked ? "DELETE" : "PUT",
                      })
                    }
                  >
                    <Heart size={17} />
                    <small>{card.likes}</small>
                  </button>
                  <button
                    className="social-icon"
                    aria-label={`Chia sẻ thẻ ${index + 1}`}
                    onClick={() => setSharing(card.id)}
                  >
                    <Share2 size={17} />
                  </button>
                </div>
              </article>
            ))}
          </div>
          {item.cards.length === 0 && (
            <Empty title="Bộ học chưa có thẻ">
              Tác giả chưa thêm nội dung.
            </Empty>
          )}
          <More
            more={count < item.cards.length}
            pending={false}
            load={() => setCount((n) => n + 20)}
          />
          <Comments item={item} />
          {settings && (
            <PublicationForm item={item} onClose={() => setSettings(false)} />
          )}
          {sharing && (
            <ShareDialog
              kind={sharing === id ? "StudySetShare" : "FlashcardShare"}
              id={sharing}
              onClose={() => setSharing(null)}
            />
          )}
          {report && (
            <ReportDialog
              targetType="StudySet"
              targetId={id}
              onClose={() => setReport(false)}
            />
          )}
          {practice && item.cards.length > 0 && (
            <PublicPractice
              setId={id}
              cards={item.cards}
              onClose={() => setPractice(false)}
            />
          )}
        </>
      )}
    </SocialLayout>
  );
}
