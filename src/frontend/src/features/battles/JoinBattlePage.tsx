import { useState, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, Swords, Users } from "lucide-react";
import { battleApi } from "./battleApi";
import "./battle.css";

export function JoinBattlePage() {
  const { code = "" } = useParams();
  const [input, setInput] = useState(code);
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const navigate = useNavigate();
  const room = useQuery({
    queryKey: ["battle-code", code],
    queryFn: () => battleApi.find(code),
    enabled: code.length === 6,
    retry: false,
  });
  async function join(e: FormEvent) {
    e.preventDefault();
    setPending(true);
    setError("");
    try {
      const found = await battleApi.find(input.trim().toUpperCase());
      await battleApi.act(found.id, "join");
      navigate(`/battles/${found.id}`);
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "Không thể vào phòng.",
      );
    } finally {
      setPending(false);
    }
  }
  return (
    <div className="battle-page battle-join">
      <div className="battle-emblem">
        <Swords size={40} />
      </div>
      <span className="battle-label">STUDYFLOW LIVE</span>
      <h1>Sẵn sàng so tài?</h1>
      <p className="battle-muted">
        Nhập mã từ bạn bè hoặc quét QR của chủ phòng.
      </p>
      <form className="battle-panel battle-form" onSubmit={join}>
        <label htmlFor="join-code">Mã phòng</label>
        <input
          id="join-code"
          className="battle-code-input"
          autoComplete="off"
          autoCapitalize="characters"
          spellCheck={false}
          maxLength={6}
          minLength={6}
          pattern="[A-Za-z2-9]{6}"
          required
          value={input}
          onChange={(e) => {
            setInput(e.target.value.toUpperCase().replace(/[^A-Z2-9]/g, ""));
            setError("");
          }}
          placeholder="A7K9Q2"
        />
        {room.data && input === code && (
          <div className="battle-join-info">
            <strong>{room.data.name}</strong>
            <span>
              <Users size={16} />
              {room.data.playerCount} / {room.data.maxPlayers} người ·{" "}
              {room.data.questionCount} câu
            </span>
          </div>
        )}
        {error && (
          <p role="alert" className="battle-error">
            {error}
          </p>
        )}
        <button
          className="battle-primary"
          disabled={pending || input.length !== 6}
        >
          {pending ? "Đang vào phòng…" : "Vào phòng"}
          <ArrowRight size={18} />
        </button>
      </form>
      <p className="battle-help">
        Bạn đã đăng nhập. Kết quả trận đấu sẽ được lưu vào tiến trình học của
        bạn.
      </p>
    </div>
  );
}
