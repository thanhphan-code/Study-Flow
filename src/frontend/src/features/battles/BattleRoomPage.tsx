import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { QRCodeSVG } from "qrcode.react";
import {
  ArrowLeft,
  ArrowRight,
  Check,
  Copy,
  Crown,
  Flame,
  LockKeyhole,
  LogOut,
  Play,
  RotateCcw,
  Swords,
  Timer,
  Trophy,
  Users,
  X,
} from "lucide-react";
import { useAuthStore } from "@/features/auth/store/authStore";
import {
  battleApi,
  questionLabels,
  type BattleRoom,
  type Source,
} from "./battleApi";
import { useBattleRoom } from "./useBattleRoom";
import { BattleQuestionCard } from "./BattleQuestionCard";
import "./battle.css";
import { ShareDialog } from '@/features/social/SocialComponents';

function SourceDetails({ source }: { source: Source | null }) {
  return (
    source && (
      <details className="battle-source">
        <summary>
          Xem nguồn · {source.name}
          {source.page ? ` · trang ${source.page}` : ""}
        </summary>
        <blockquote>{source.snippet}</blockquote>
        {source.section && <p>{source.section}</p>}
      </details>
    )
  );
}

function Players({
  room,
  userId,
  kick,
  pending,
}: {
  room: BattleRoom;
  userId: string;
  kick?: (id: string) => void;
  pending: boolean;
}) {
  return (
    <ol className="battle-players">
      {room.players.map((p, index) => (
        <li key={p.userId} className={p.userId === userId ? "is-me" : ""}>
          <span className={`battle-avatar avatar-${index % 4}`}>
            {p.rank ?? p.name.slice(0, 1).toUpperCase()}
          </span>
          <span className="battle-player-name">
            <strong>
              {p.name}
              {p.userId === userId && <small> Bạn</small>}
            </strong>
            <span>
              {p.isHost
                ? "Chủ phòng"
                : p.accuracy === null
                  ? "Sẵn sàng"
                  : `${p.accuracy}% chính xác`}
            </span>
          </span>
          {p.isHost && <Crown size={17} aria-label="Chủ phòng" />}
          {p.score !== null && (
            <strong className="battle-player-score">
              {p.score.toLocaleString("vi-VN")}
              <small>điểm</small>
            </strong>
          )}
          {kick && !p.isHost && (
            <button
              disabled={pending}
              className="battle-icon-button"
              aria-label={`Mời ${p.name} ra khỏi phòng`}
              onClick={() => kick(p.userId)}
            >
              <X size={16} />
            </button>
          )}
        </li>
      ))}
    </ol>
  );
}

export function BattleRoomPage() {
  const { id = "" } = useParams();
  const userId = useAuthStore((s) => s.user?.id) ?? "";
  const query = useBattleRoom(id);
  const room = query.data;
  const cache = useQueryClient();
  const navigate = useNavigate();
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [copied, setCopied] = useState(false);
  const [inviteFriends, setInviteFriends] = useState(false);
  const [now, setNow] = useState(() => Date.now());
  const [confirm, setConfirm] = useState<string | null>(null);
  const [reviewMode, setReviewMode] = useState<"weak" | "all">("weak");
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 200);
    return () => clearInterval(timer);
  }, []);
  const serverNow = room
    ? Date.parse(room.serverTime) + Math.max(0, now - query.dataUpdatedAt)
    : now;
  async function mutate(operation: () => Promise<BattleRoom>) {
    if (pending) return;
    setPending(true);
    setError("");
    try {
      const updated = await operation();
      cache.setQueryData(["battle", id], updated);
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "Không thể thực hiện. Hãy thử lại.",
      );
      void query.refetch();
    } finally {
      setPending(false);
      setConfirm(null);
    }
  }
  const act = (action: string) => mutate(() => battleApi.act(id, action));
  async function copy() {
    try {
      await navigator.clipboard.writeText(
        `${window.location.origin}/join/${room!.joinCode}`,
      );
      setCopied(true);
    } catch {
      setError(
        "Không sao chép được. Bạn có thể chọn mã phòng và gửi cho bạn bè.",
      );
    }
  }
  if (query.isPending)
    return (
      <div className="battle-page battle-loading" role="status">
        <Swords size={36} />
        <h1>Đang vào đấu trường…</h1>
        <p>Chuẩn bị một chút, mình bắt đầu ngay.</p>
      </div>
    );
  if (query.isError || !room)
    return (
      <div className="battle-page battle-panel">
        <h1>Chưa thể mở phòng</h1>
        <p role="alert">
          {query.error instanceof Error
            ? query.error.message
            : "Hãy kiểm tra lại đường truyền và mã phòng."}
        </p>
        <button className="battle-primary" onClick={() => void query.refetch()}>
          Thử lại
        </button>
        <Link className="battle-back" to="/join">
          Nhập mã khác
        </Link>
      </div>
    );
  const host = room.hostUserId === userId;
  const lobby = ["LobbyOpen", "Locked"].includes(room.status);
  const q = room.question;
  const beforeStart = q ? serverNow < Date.parse(q.startedAt) : false;
  const countdown = q
    ? Math.max(0, Math.ceil((Date.parse(q.startedAt) - serverNow) / 1000))
    : 0;
  const remaining = q?.timeLimitSeconds
    ? Math.max(
        0,
        Math.ceil(
          (Date.parse(q.startedAt) + q.timeLimitSeconds * 1000 - serverNow) /
            1000,
        ),
      )
    : null;
  const over = !!q?.endedAt;
  const feedback = room.myAnswer;
  const me = room.players.find((p) => p.userId === userId);
  const visibleReviews = room.reviews.filter(
    (r) => reviewMode === "all" || r.evaluation !== "Correct",
  );
  return (
    <div className="battle-page">
      <header className="battle-topbar">
        <Link to="/join" className="battle-brand">
          <Swords size={22} />
          StudyFlow <strong>Battle</strong>
        </Link>
        <span className="battle-live">
          <span />
          {query.connection}
        </span>
      </header>
      {error && (
        <p className="battle-error" role="alert">
          {error}
        </p>
      )}
      {!room.isMember ? (
        <section className="battle-panel battle-empty">
          <h1>{room.name}</h1>
          <p>
            {room.playerCount} / {room.maxPlayers} người trong phòng
          </p>
          <button
            className="battle-primary"
            disabled={pending || room.status !== "LobbyOpen"}
            onClick={() => void act("join")}
          >
            Vào phòng
            <ArrowRight size={18} />
          </button>
        </section>
      ) : lobby ? (
        <>
          <div className="battle-lobby-heading">
            <div>
              <span className="battle-label">
                {room.status === "Locked" ? "PHÒNG ĐÃ KHÓA" : "SẢNH CHỜ"}
              </span>
              <h1>{room.name}</h1>
              <p className="battle-muted">
                {room.questionCount} câu · {room.mode} ·{" "}
                {room.timeLimitSeconds
                  ? `Từ ${room.timeLimitSeconds} giây / câu`
                  : "Không giới hạn thời gian"}
              </p>
            </div>
            <span className="battle-lobby-icon">
              <Swords size={50} strokeWidth={1.5} />
            </span>
          </div>
          <div className="battle-lobby-grid">
            <section className="battle-invite">
              <h2>Rủ bạn vào trận</h2>
              <p>Quét QR hoặc nhập mã tại StudyFlow</p>
              <div className="battle-qr">
                <QRCodeSVG
                  value={`${window.location.origin}/join/${room.joinCode}`}
                  size={176}
                  marginSize={2}
                  level="M"
                  title={`Tham gia phòng ${room.joinCode}`}
                />
              </div>
              <span className="battle-code-label">MÃ PHÒNG</span>
              <strong className="battle-code">{room.joinCode}</strong>
              <button className="battle-copy" onClick={() => void copy()}>
                {copied ? <Check size={17} /> : <Copy size={17} />}
                {copied ? "Đã sao chép lời mời" : "Sao chép liên kết mời"}
              </button>
              <p className="battle-invite-note">
                Mọi người cần đăng nhập để lưu kết quả học.
              </p>
              {room.hostUserId === userId && <button className="battle-copy" onClick={() => setInviteFriends(true)}><Users size={17}/>Mời bạn bè</button>}
              {inviteFriends && <ShareDialog kind="BattleInvite" id={room.id} onClose={() => setInviteFriends(false)} />}
            </section>
            <section className="battle-panel battle-lobby-players">
              <div className="battle-section-title">
                <h2>
                  <Users size={20} />
                  Đồng đội so tài
                </h2>
                <strong>
                  {room.playerCount}
                  <span> / {room.maxPlayers}</span>
                </strong>
              </div>
              <progress
                className="battle-capacity"
                value={room.playerCount}
                max={room.maxPlayers}
                aria-label="Số người trong phòng"
              />
              <Players
                room={room}
                userId={userId}
                pending={pending}
                kick={
                  host
                    ? (player) => setConfirm(`participants/${player}/kick`)
                    : undefined
                }
              />
              {room.playerCount === 1 && (
                <div className="battle-waiting">
                  <Users size={30} />
                  <p>Ghế đã sẵn sàng, còn thiếu bạn bè.</p>
                  <span>Chia sẻ mã phòng để mọi người cùng vào nhé.</span>
                </div>
              )}
              <div className="battle-lobby-actions">
                {host ? (
                  <>
                    <button
                      className="battle-secondary"
                      disabled={pending}
                      onClick={() =>
                        void act(room.status === "Locked" ? "unlock" : "lock")
                      }
                    >
                      <LockKeyhole size={17} />
                      {room.status === "Locked" ? "Mở khóa" : "Khóa phòng"}
                    </button>
                    <button
                      className="battle-primary"
                      disabled={pending}
                      onClick={() => void act("start")}
                    >
                      <Play size={18} fill="currentColor" />
                      Bắt đầu trận
                    </button>
                  </>
                ) : (
                  <p className="battle-wait-host" role="status">
                    Bạn đã vào phòng. Đợi chủ phòng bắt đầu nhé!
                  </p>
                )}
              </div>
            </section>
          </div>
          <div className="battle-bottom-note">
            <p>
              <Flame size={17} />
              Đúng liên tiếp để tăng combo. Trả lời đúng quan trọng hơn trả lời
              nhanh.
            </p>
            <button
              className="battle-text-button"
              disabled={pending}
              onClick={() =>
                host
                  ? setConfirm("cancel")
                  : void battleApi
                      .act(id, "leave")
                      .then(() => navigate("/join"))
                      .catch((e) => setError(String(e.message)))
              }
            >
              <LogOut size={16} />
              {host ? "Hủy phòng" : "Rời phòng"}
            </button>
          </div>
        </>
      ) : room.status === "InProgress" && q ? (
        <>
          <div className="battle-game-heading">
            <span>
              Câu <strong>{q.number}</strong> / {room.questionCount}
            </span>
            <h1>{room.name}</h1>
            <div className="battle-score">
              <Flame size={19} />
              <strong>{room.myCombo} combo</strong>
              <span>{room.myScore.toLocaleString("vi-VN")} điểm</span>
            </div>
          </div>
          <progress
            className="battle-round-progress"
            value={q.number}
            max={room.questionCount}
            aria-label="Tiến độ trận đấu"
          />
          <div className="battle-game-grid">
            <section className="battle-panel battle-question" key={q.id}>
              <div className="battle-question-meta">
                <span>
                  {questionLabels[q.questionType]} ·{" "}
                  {q.difficulty === "Hard"
                    ? "Khó"
                    : q.difficulty === "Easy"
                      ? "Dễ"
                      : "Vừa"}
                </span>
                <span
                  className={`battle-timer ${remaining !== null && remaining <= 5 && !over ? "is-urgent" : ""}`}
                >
                  <Timer size={19} />
                  {over
                    ? "Đã hết lượt"
                    : beforeStart
                      ? "Chuẩn bị"
                      : remaining === null
                        ? "Không giới hạn"
                        : `${remaining}s`}
                </span>
              </div>
              {beforeStart ? (
                <div className="battle-countdown" role="status">
                  <span>Sẵn sàng nhé</span>
                  <strong key={countdown}>{countdown}</strong>
                  <p>Tập trung. Bạn làm được!</p>
                </div>
              ) : (
                <>
                  <h2 className="battle-prompt">{q.prompt}</h2>
                  <BattleQuestionCard
                    question={q}
                    disabled={!!feedback || over || remaining === 0}
                    pending={pending}
                    onSubmit={(answer) =>
                      void mutate(() => battleApi.answer(id, q.id, answer))
                    }
                  />
                  {feedback && (
                    <div
                      className={`battle-feedback ${feedback.evaluation === "Correct" ? "is-correct" : feedback.evaluation === "Close" ? "is-close" : "is-wrong"}`}
                      role="status"
                    >
                      <div>
                        <strong>
                          {feedback.evaluation === "Correct"
                            ? "Chính xác. Giữ nhịp nào!"
                            : feedback.evaluation === "Close"
                              ? "Gần đúng rồi!"
                              : "Một cơ hội để nhớ lâu hơn."}
                        </strong>
                        <span className="battle-points">
                          +{feedback.scoreEarned}
                        </span>
                      </div>
                      <p className="battle-correct-answer">
                        {feedback.correctAnswer}
                      </p>
                      {feedback.explanation && <p>{feedback.explanation}</p>}
                      <SourceDetails source={feedback.source} />
                    </div>
                  )}
                  <div className="battle-round-status">
                    <span role="status">
                      {room.answeredCount} / {room.playerCount} người đã trả lời
                      {over
                        ? " · Câu tiếp theo tự động sau 8 giây"
                        : feedback
                          ? " · Chờ các bạn một chút nhé"
                          : ""}
                    </span>
                    {host &&
                      (over ? (
                        <button
                          className="battle-secondary"
                          disabled={pending}
                          onClick={() => void act("next")}
                        >
                          {q.number === room.questionCount
                            ? "Xem kết quả"
                            : "Câu tiếp theo"}
                          <ArrowRight size={17} />
                        </button>
                      ) : (
                        <button
                          className="battle-text-button"
                          disabled={pending}
                          onClick={() => setConfirm("end-question")}
                        >
                          Kết thúc câu
                        </button>
                      ))}
                  </div>
                </>
              )}
            </section>
            <aside className="battle-panel battle-ranks">
              <h2>
                <Trophy size={20} />
                Bảng xếp hạng
              </h2>
              {room.players.every((p) => p.rank === null) ? (
                <div className="battle-ranking-hidden">
                  <LockKeyhole size={32} />
                  <p>
                    {room.leaderboardMode === "EndOnly"
                      ? "Thứ hạng được mở khi trận kết thúc."
                      : "Thứ hạng sẽ hiện giữa các câu."}
                  </p>
                  <span>Tập trung vào đáp án của bạn.</span>
                </div>
              ) : (
                <Players room={room} userId={userId} pending={pending} />
              )}
              <p className="battle-help">
                Đúng +100 · Độ khó +0–30
                <br />
                Combo +10–30 · Tốc độ tối đa +20
              </p>
            </aside>
          </div>
          {host && (
            <button
              className="battle-text-button battle-cancel"
              disabled={pending}
              onClick={() => setConfirm("cancel")}
            >
              Hủy trận
            </button>
          )}
        </>
      ) : room.status === "Completed" ? (
        <>
          <section className="battle-results-heading">
            <div className="battle-trophy">
              <Trophy size={42} />
            </div>
            <span className="battle-label">HOÀN THÀNH TRẬN ĐẤU</span>
            <h1>
              {me?.rank === 1 ? "Bạn đã dẫn đầu!" : "Thêm một bước tiến."}
            </h1>
            <p>{room.name} · Cảm ơn bạn đã cùng nhau học tốt hơn.</p>
            <div className="battle-result-stats">
              <div>
                <span>Xếp hạng</span>
                <strong>
                  #{me?.rank}
                  <small> / {room.playerCount}</small>
                </strong>
              </div>
              <div>
                <span>Tổng điểm</span>
                <strong>{room.myScore.toLocaleString("vi-VN")}</strong>
              </div>
              <div>
                <span>Chính xác</span>
                <strong>
                  {me?.accuracy ?? 0}%
                  <small>
                    {me?.correctCount ?? 0}/{room.questionCount} câu
                  </small>
                </strong>
              </div>
              <div>
                <span>Trung bình</span>
                <strong>
                  {me?.averageResponseSeconds ?? 0}
                  <small> giây</small>
                </strong>
              </div>
            </div>
          </section>
          <div className="battle-game-grid">
            <section className="battle-panel battle-review">
              <div className="battle-section-title">
                <h2>Điều mang theo sau trận</h2>
                <span>
                  {
                    room.reviews.filter((r) => r.evaluation === "Correct")
                      .length
                  }{" "}
                  câu vững
                </span>
              </div>
              <p className="battle-muted">
                Lượt trả lời đã được lưu vào tiến trình học và lịch ôn của bạn.
              </p>
              {host && (
                <section
                  className="battle-host-insights"
                  aria-label="Kết quả cả phòng"
                >
                  <h3>Cả phòng cần chú ý</h3>
                  <p className="battle-help">
                    Các câu có tỷ lệ trả lời đúng thấp nhất.
                  </p>
                  <ol>
                    {[...room.reviews]
                      .sort((a, b) => a.roomAccuracy - b.roomAccuracy)
                      .slice(0, 3)
                      .map((review) => (
                        <li key={review.questionId}>
                          <span>{review.prompt}</span>
                          <strong>{review.roomAccuracy}% đúng</strong>
                        </li>
                      ))}
                  </ol>
                </section>
              )}
              <div className="battle-review-tabs">
                <button
                  aria-pressed={reviewMode === "weak"}
                  onClick={() => setReviewMode("weak")}
                >
                  Cần ôn lại (
                  {
                    room.reviews.filter((r) => r.evaluation !== "Correct")
                      .length
                  }
                  )
                </button>
                <button
                  aria-pressed={reviewMode === "all"}
                  onClick={() => setReviewMode("all")}
                >
                  Tất cả & nguồn
                </button>
              </div>
              {visibleReviews.length === 0 ? (
                <div className="battle-empty">
                  <Check size={36} />
                  <h3>Vững cả bộ câu hỏi!</h3>
                  <p>Hãy giữ nhịp ôn tập để kiến thức ở lại lâu hơn.</p>
                </div>
              ) : (
                visibleReviews.map((r, i) => (
                  <details key={r.questionId} className="battle-review-item">
                    <summary>
                      <span>{i + 1}</span>
                      <strong>{r.prompt}</strong>
                      <span>
                        {r.evaluation === "Correct"
                          ? "Đã vững"
                          : r.evaluation === "Close"
                            ? "Gần đúng"
                            : "Cần ôn"}
                      </span>
                    </summary>
                    <div>
                      <p className="battle-help">
                        Thử tự nhớ lại trước khi đọc đáp án.
                      </p>
                      <p className="battle-correct-answer">{r.correctAnswer}</p>
                      {r.explanation && <p>{r.explanation}</p>}
                      <SourceDetails source={r.source} />
                      {host && (
                        <p className="battle-help">
                          Cả phòng: {r.roomAccuracy}% trả lời đúng.
                        </p>
                      )}
                    </div>
                  </details>
                ))
              )}
            </section>
            <aside className="battle-panel battle-ranks">
              <h2>
                <Trophy size={20} />
                Bảng vàng
              </h2>
              <Players room={room} userId={userId} pending={pending} />
            </aside>
          </div>
          <div className="battle-result-actions">
            {room.reviewStudySetId && (
              <Link
                className="battle-primary"
                to={`/study/${room.reviewStudySetId}/learn`}
              >
                Ôn lại kiến thức
                <ArrowRight size={17} />
              </Link>
            )}
            <Link className="battle-secondary" to="/home">
              <ArrowLeft size={17} />
              Về trang chủ
            </Link>
            {host ? (
              <Link
                className="battle-primary"
                to={`/study-sets/${room.studySetId}/battle`}
              >
                <RotateCcw size={17} />
                Tạo trận mới
              </Link>
            ) : (
              <Link className="battle-primary" to="/join">
                Tham gia trận khác
                <ArrowRight size={17} />
              </Link>
            )}
          </div>
        </>
      ) : (
        <section className="battle-panel battle-empty">
          <Swords size={36} />
          <h1>
            {room.status === "Expired"
              ? "Phòng đã hết hạn"
              : "Trận đấu đã được hủy"}
          </h1>
          <p>Các lượt học đã hoàn thành vẫn được giữ lại.</p>
          <Link className="battle-primary" to="/join">
            Tìm trận khác
            <ArrowRight size={18} />
          </Link>
        </section>
      )}
      {confirm && (
        <ConfirmDialog onClose={() => setConfirm(null)}>
          <h2 id="battle-confirm-title">
            {confirm === "cancel"
              ? "Hủy trận đấu này?"
              : confirm === "end-question"
                ? "Kết thúc câu hỏi?"
                : "Mời người chơi ra khỏi phòng?"}
          </h2>
          <p>
            {confirm === "cancel"
              ? "Mọi người sẽ dừng chơi. Các lượt học đã ghi nhận được giữ lại."
              : confirm === "end-question"
                ? "Người chưa trả lời sẽ được tính là bỏ lỡ câu này."
                : "Người chơi này sẽ không thể vào lại phòng bằng mã hiện tại."}
          </p>
          <div>
            <button
              className="battle-secondary"
              autoFocus
              onClick={() => setConfirm(null)}
            >
              Quay lại
            </button>
            <button
              className="battle-primary"
              disabled={pending}
              onClick={() => void act(confirm)}
            >
              Xác nhận
            </button>
          </div>
        </ConfirmDialog>
      )}
    </div>
  );
}

function ConfirmDialog({
  children,
  onClose,
}: {
  children: React.ReactNode;
  onClose: () => void;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const element = dialog.current!;
    element.showModal();
    return () => element.close();
  }, []);
  return (
    <dialog
      ref={dialog}
      className="battle-panel battle-dialog"
      aria-labelledby="battle-confirm-title"
      onCancel={onClose}
      onClose={onClose}
    >
      {children}
    </dialog>
  );
}
