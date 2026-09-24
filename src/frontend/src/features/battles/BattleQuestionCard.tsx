import { useState, type FormEvent } from "react";
import { ArrowDown, ArrowUp, Check, Send } from "lucide-react";
import type { BattleQuestion } from "./battleApi";

export function BattleQuestionCard({
  question: q,
  disabled,
  pending,
  onSubmit,
}: {
  question: BattleQuestion;
  disabled: boolean;
  pending: boolean;
  onSubmit: (answer: string) => void;
}) {
  const [selected, setSelected] = useState("");
  const [text, setText] = useState("");
  const [activeLeft, setActiveLeft] = useState(0);
  const [matches, setMatches] = useState<string[]>(q.leftItems.map(() => ""));
  const [order, setOrder] = useState(q.options.map((o) => o.id));
  const choice = ["MultipleChoice", "TrueFalse"].includes(q.questionType);
  const answer = choice
    ? selected
    : q.questionType === "Matching"
      ? JSON.stringify(matches)
      : q.questionType === "Ordering"
        ? JSON.stringify(order)
        : text.trim();
  const valid =
    q.questionType === "Matching" ? matches.every(Boolean) : !!answer;
  function submit(event: FormEvent) {
    event.preventDefault();
    if (valid && !disabled && !pending) onSubmit(answer);
  }
  function pair(id: string) {
    setMatches((current) =>
      current.map((value, index) =>
        index === activeLeft ? id : value === id ? "" : value,
      ),
    );
    setActiveLeft((current) => Math.min(current + 1, q.leftItems.length - 1));
  }
  function move(index: number, delta: number) {
    setOrder((current) => {
      const next = [...current];
      [next[index], next[index + delta]] = [next[index + delta], next[index]];
      return next;
    });
  }
  return (
    <form className="battle-answer-form" onSubmit={submit}>
      <fieldset disabled={disabled || pending}>
        <legend className="sr-only">Trả lời câu hỏi</legend>
        {choice ? (
          <div
            className={`battle-answers ${q.questionType === "TrueFalse" ? "battle-binary" : ""}`}
          >
            {q.options.map((o, index) => (
              <label
                key={o.id}
                className={`battle-answer battle-answer-${index} ${selected === o.id ? "is-selected" : ""}`}
              >
                <input
                  type="radio"
                  name={`answer-${q.id}`}
                  value={o.id}
                  checked={selected === o.id}
                  onChange={() => setSelected(o.id)}
                />
                {q.questionType !== "TrueFalse" && <span className="battle-answer-letter" aria-hidden="true">{String.fromCharCode(65 + index)}</span>}
                <span>{o.text}</span>
                {selected === o.id && (
                  <Check size={20} className="battle-answer-check" />
                )}
              </label>
            ))}
          </div>
        ) : q.questionType === "Matching" ? (
          <>
            <p className="battle-muted">
              Chọn một ý bên trái, rồi chọn ý tương ứng bên phải.
            </p>
            <div className="battle-matching">
              <div>
                {q.leftItems.map((o, i) => (
                  <button
                    type="button"
                    key={o.id}
                    aria-pressed={activeLeft === i}
                    className={`battle-match ${activeLeft === i ? "is-selected" : ""} ${matches[i] ? "is-paired" : ""}`}
                    onClick={() => setActiveLeft(i)}
                  >
                    <span>{i + 1}</span>
                    {o.text}
                    {matches[i] && <Check size={16} />}
                  </button>
                ))}
              </div>
              <div>
                {q.options.map((o) => (
                  <button
                    type="button"
                    key={o.id}
                    className={`battle-match ${matches.includes(o.id) ? "is-paired" : ""}`}
                    onClick={() => pair(o.id)}
                  >
                    <span>
                      {matches.includes(o.id) ? matches.indexOf(o.id) + 1 : "?"}
                    </span>
                    {o.text}
                  </button>
                ))}
              </div>
            </div>
          </>
        ) : q.questionType === "Ordering" ? (
          <>
            <p className="battle-muted">
              Dùng mũi tên để đưa các bước về đúng thứ tự.
            </p>
            <ol className="battle-order">
              {order.map((id, i) => (
                <li key={id}>
                  <span className="battle-order-number">{i + 1}</span>
                  <span>{q.options.find((o) => o.id === id)?.text}</span>
                  <div>
                    <button
                      type="button"
                      disabled={i === 0}
                      aria-label={`Đưa bước ${i + 1} lên`}
                      onClick={() => move(i, -1)}
                    >
                      <ArrowUp size={18} />
                    </button>
                    <button
                      type="button"
                      disabled={i === order.length - 1}
                      aria-label={`Đưa bước ${i + 1} xuống`}
                      onClick={() => move(i, 1)}
                    >
                      <ArrowDown size={18} />
                    </button>
                  </div>
                </li>
              ))}
            </ol>
          </>
        ) : (
          <label className="battle-text-answer">
            Đáp án của bạn
            {q.questionType === "ShortAnswer" ? (
              <textarea
                value={text}
                onChange={(e) => setText(e.target.value)}
                maxLength={4000}
                rows={3}
                placeholder="Viết ngắn gọn điều bạn nhớ…"
              />
            ) : (
              <input
                value={text}
                onChange={(e) => setText(e.target.value)}
                maxLength={4000}
                placeholder="Nhập đáp án…"
                autoComplete="off"
              />
            )}
          </label>
        )}
      </fieldset>
      <div className="battle-submit-row">
        <span className="battle-help">
          {disabled
            ? "Đáp án được chấm và lưu tự động."
            : "Chọn kỹ nhé. Mỗi câu chỉ chốt một lần."}
        </span>
        <button
          className="battle-primary"
          disabled={disabled || pending || !valid}
        >
          {pending ? "Đang chốt…" : "Chốt đáp án"}
          <Send size={17} />
        </button>
      </div>
    </form>
  );
}
