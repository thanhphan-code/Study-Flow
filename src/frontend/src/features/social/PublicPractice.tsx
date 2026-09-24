import { useState } from "react";
import { Check, ArrowRight } from "lucide-react";
import { Dialog, ErrorNotice } from "./SocialComponents";
import { useSocialAction, type SocialCard } from "./socialApi";

export function PublicPractice({
  setId,
  cards,
  onClose,
}: {
  setId: string;
  cards: SocialCard[];
  onClose: () => void;
}) {
  const [index, setIndex] = useState(0);
  const [answer, setAnswer] = useState("");
  const [known, setKnown] = useState(0);
  const action = useSocialAction<{
    result: string;
    backText: string;
    explanation?: string;
  }>();
  const card = cards[index];
  return (
    <Dialog title="Học bộ thẻ cộng đồng" onClose={onClose}>
      {card ? (
        <>
          <div className="social-practice-progress">
            <span>
              Thẻ {index + 1} / {cards.length}
            </span>
            <span>{known} câu đúng</span>
          </div>
          <progress max={cards.length} value={index} />
          <div className="social-practice-card">
            <span className="social-muted">Thử nhớ trước khi xem đáp án</span>
            <h3>{card.frontText}</h3>
          </div>
          {action.data ? (
            <div className="social-answer-feedback" role="status">
              <strong>
                {action.data.result === "Correct"
                  ? "Chính xác!"
                  : action.data.result === "Close"
                    ? "Gần đúng rồi!"
                    : "Cùng ghi nhớ lại nhé"}
              </strong>
              <p>{action.data.backText}</p>
              {action.data.explanation && (
                <small>{action.data.explanation}</small>
              )}
              <button
                className="social-button primary social-wide"
                onClick={() => {
                  setIndex((n) => n + 1);
                  setAnswer("");
                  action.reset();
                }}
              >
                {index + 1 === cards.length ? "Hoàn thành" : "Thẻ tiếp theo"}
                <ArrowRight size={17} />
              </button>
            </div>
          ) : (
            <form
              className="social-form"
              onSubmit={(e) => {
                e.preventDefault();
                void action
                  .mutateAsync({
                    path: `/sets/${setId}/answer`,
                    body: { flashcardId: card.id, answer },
                  })
                  .then((result) => {
                    if (result.result === "Correct") setKnown((n) => n + 1);
                  })
                  .catch(() => {});
              }}
            >
              <label>
                Câu trả lời của bạn
                <textarea
                  value={answer}
                  onChange={(e) => setAnswer(e.target.value)}
                  maxLength={5000}
                  required
                  rows={2}
                  autoFocus
                />
              </label>
              <ErrorNotice error={action.error} />
              <button
                className="social-button primary"
                disabled={action.isPending || !answer.trim()}
              >
                {action.isPending ? "Đang kiểm tra…" : "Kiểm tra đáp án"}
              </button>
            </form>
          )}
        </>
      ) : (
        <div className="social-empty">
          <Check size={36} />
          <h3>Hoàn thành lượt học!</h3>
          <p>
            Bạn trả lời đúng {known}/{cards.length} thẻ.
          </p>
          <p>
            Muốn có lịch ôn riêng? Tạo bản sao bộ học rồi tiếp tục với
            SmartLearn.
          </p>
          <button className="social-button primary" onClick={onClose}>
            Về bộ học
          </button>
        </div>
      )}
    </Dialog>
  );
}
