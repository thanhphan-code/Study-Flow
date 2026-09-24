import { useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  ArrowRight,
  Swords,
  Users,
  Timer,
  Sparkles,
} from "lucide-react";
import { studySetsApi } from "@/features/studySets/api/studySetsApi";
import { battleApi, questionLabels, type BattleSettings } from "./battleApi";
import "./battle.css";
import { socialSend } from '@/features/social/socialApi';

export function CreateBattlePage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const invite = new URLSearchParams(location.search).get('invite');
  const [createdRoom, setCreatedRoom] = useState<string | null>(null);
  const set = useQuery({
    queryKey: ["study-set", id],
    queryFn: () => studySetsApi.get(id),
  });
  const [settings, setSettings] = useState<BattleSettings>({
    studySetId: id,
    name: "",
    questionCount: 10,
    maxPlayers: 10,
    timeLimitSeconds: 20,
    questionTypes: ["MultipleChoice", "FillBlank", "ShortAnswer"],
    difficulty: "Balanced",
    mode: "Classic",
    leaderboardMode: "BetweenRounds",
  });
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  function update<K extends keyof BattleSettings>(
    key: K,
    value: BattleSettings[K],
  ) {
    setSettings((s) => ({ ...s, [key]: value }));
  }
  async function submit(e: FormEvent) {
    e.preventDefault();
    setPending(true);
    setError("");
    try {
      const room = await battleApi.create({
        ...settings,
        name: settings.name.trim() || set.data?.title || "Cùng nhau ôn bài",
      });
      setCreatedRoom(room.id);
      if (invite) {
        const conversation = await socialSend<{ id: string }>(`/conversations/direct/${invite}`);
        await socialSend(`/conversations/${conversation.id}/messages`, 'POST', { kind: 'BattleInvite', sharedId: room.id });
      }
      navigate(`/battles/${room.id}`);
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "Không thể tạo phòng. Hãy thử lại.",
      );
    } finally {
      setPending(false);
    }
  }
  return (
    <div className="battle-page">
      {createdRoom && error && <p role="alert">Phòng đã tạo nhưng chưa gửi được lời mời. <Link to={`/battles/${createdRoom}`}>Mở phòng để mời lại bạn bè</Link></p>}
      <Link to={`/study-sets/${id}`} className="battle-back">
        <ArrowLeft size={17} /> Về bộ học
      </Link>
      <header className="battle-heading">
        <span className="battle-label">
          <Swords size={17} /> STUDYFLOW LIVE
        </span>
        <h1>
          Ôn bài. So tài.
          <br />
          <span>Cùng tiến bộ.</span>
        </h1>
        <p>Mời bạn vào phòng, chốt đáp án và khám phá điều mình đã nhớ.</p>
      </header>
      <div className="battle-create-grid">
        <form className="battle-panel battle-form" onSubmit={submit}>
          <h2>Thiết lập trận đấu</h2>
          <p className="battle-muted">
            {set.isPending
              ? "Đang tải bộ học…"
              : (set.data?.title ?? "Không tải được bộ học")}
          </p>
          <label>
            Tên phòng
            <input
              maxLength={120}
              value={settings.name}
              placeholder={set.data?.title ?? "Cùng nhau ôn bài"}
              onChange={(e) => update("name", e.target.value)}
            />
          </label>
          <div className="battle-form-row">
            <label>
              Số câu hỏi
              <select
                value={settings.questionCount}
                onChange={(e) =>
                  update("questionCount", Number(e.target.value))
                }
              >
                {[10, 15, 20, 30].map((n) => (
                  <option key={n} value={n}>
                    {n} câu
                  </option>
                ))}
              </select>
            </label>
            <label>
              Số người tối đa
              <select
                value={settings.maxPlayers}
                onChange={(e) => update("maxPlayers", Number(e.target.value))}
              >
                {[10, 20, 30, 50].map((n) => (
                  <option key={n} value={n}>
                    {n} người
                  </option>
                ))}
              </select>
            </label>
          </div>
          <div className="battle-form-row">
            <label>
              Thời gian mỗi câu
              <select
                value={settings.timeLimitSeconds ?? "none"}
                onChange={(e) =>
                  update(
                    "timeLimitSeconds",
                    e.target.value === "none" ? null : Number(e.target.value),
                  )
                }
              >
                {[10, 15, 20, 30].map((n) => (
                  <option key={n} value={n}>
                    {n} giây
                  </option>
                ))}
                <option value="none">Không giới hạn</option>
              </select>
            </label>
            <label>
              Độ khó
              <select
                value={settings.difficulty}
                onChange={(e) => update("difficulty", e.target.value)}
              >
                <option value="Easy">Dễ</option>
                <option value="Balanced">Cân bằng</option>
                <option value="Hard">Khó</option>
              </select>
            </label>
          </div>
          <fieldset>
            <legend>Nhịp chơi</legend>
            <div className="battle-mode-grid">
              {[
                ["Classic", "Classic", "Tập trung ghi nhớ"],
                ["Mixed", "Mixed", "Thêm ghép cặp, sắp xếp"],
              ].map(([value, title, subtitle]) => (
                <button
                  type="button"
                  key={value}
                  className={`battle-mode ${settings.mode === value ? "is-selected" : ""}`}
                  aria-pressed={settings.mode === value}
                  onClick={() =>
                    setSettings((s) => ({
                      ...s,
                      mode: value,
                      questionTypes:
                        value === "Classic"
                          ? s.questionTypes.filter(
                              (t) => !["Matching", "Ordering"].includes(t),
                            )
                          : s.questionTypes,
                    }))
                  }
                >
                  <strong>{title}</strong>
                  <span>{subtitle}</span>
                </button>
              ))}
            </div>
          </fieldset>
          <fieldset>
            <legend>Dạng câu hỏi</legend>
            <div className="battle-types">
              {Object.entries(questionLabels).map(([type, label]) => (
                <label
                  key={type}
                  className={
                    settings.mode === "Classic" &&
                    ["Matching", "Ordering"].includes(type)
                      ? "is-disabled"
                      : ""
                  }
                >
                  <input
                    type="checkbox"
                    checked={settings.questionTypes.includes(type)}
                    disabled={
                      settings.mode === "Classic" &&
                      ["Matching", "Ordering"].includes(type)
                    }
                    onChange={(e) =>
                      update(
                        "questionTypes",
                        e.target.checked
                          ? [...settings.questionTypes, type]
                          : settings.questionTypes.filter((t) => t !== type),
                      )
                    }
                  />
                  {label}
                </label>
              ))}
            </div>
          </fieldset>
          <p className="battle-help">
            Ghép cặp cần 4 ý nghĩa khác nhau. Sắp xếp cần thẻ có đáp án đánh số
            1., 2., 3. Câu dài có ít nhất 30 giây; Đúng/Sai tối đa 25%. Độ khó
            ưu tiên mức nhận biết hoặc tự nhớ trong các dạng đã chọn.
          </p>
          <label>
            Bảng xếp hạng
            <select
              value={settings.leaderboardMode}
              onChange={(e) => update("leaderboardMode", e.target.value)}
            >
              <option value="BetweenRounds">Giữa các câu</option>
              <option value="Always">Luôn hiển thị</option>
              <option value="EndOnly">Chỉ khi kết thúc</option>
            </select>
          </label>
          {(error || set.isError) && (
            <p className="battle-error" role="alert">
              {error || "Không tải được bộ học. Hãy tải lại trang."}
            </p>
          )}
          <button
            className="battle-primary"
            disabled={
              pending || !!createdRoom || !set.data || settings.questionTypes.length === 0
            }
          >
            {pending ? "Đang chuẩn bị câu hỏi…" : invite ? "Tạo phòng & gửi lời mời" : "Tạo phòng & mời bạn"}
            <ArrowRight size={18} />
          </button>
        </form>
        <aside className="battle-create-aside">
          <div className="battle-ticket">
            <Swords size={48} strokeWidth={1.5} />
            <h2>
              Một trận vui.
              <br />
              Nhớ bài lâu hơn.
            </h2>
            <div className="battle-ticket-row">
              <Sparkles size={19} />
              <span>{settings.questionCount} cơ hội bứt phá</span>
            </div>
            <div className="battle-ticket-row">
              <Users size={19} />
              <span>Tối đa {settings.maxPlayers} người, tính cả bạn</span>
            </div>
            <div className="battle-ticket-row">
              <Timer size={19} />
              <span>
                {settings.timeLimitSeconds
                  ? `Từ ${settings.timeLimitSeconds} giây mỗi câu`
                  : "Bình tĩnh, không đếm ngược"}
              </span>
            </div>
            <p>
              Đúng trước, nhanh sau.
              <br />
              Tốc độ chỉ thưởng tối đa 20 điểm.
            </p>
          </div>
          <div className="battle-how">
            <h3>Chơi rất đơn giản</h3>
            <ol>
              <li>
                <span>1</span>Tạo phòng, chia sẻ mã hoặc QR.
              </li>
              <li>
                <span>2</span>Mọi người đăng nhập, cùng trả lời.
              </li>
              <li>
                <span>3</span>Xem thứ hạng và ôn lại câu chưa chắc.
              </li>
            </ol>
          </div>
        </aside>
      </div>
    </div>
  );
}
