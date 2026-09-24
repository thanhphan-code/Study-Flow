import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { subjectsApi } from "@/features/subjects/api/subjectsApi";
import { studySetsApi } from "@/features/studySets/api/studySetsApi";
import { Dialog, Empty, ErrorNotice } from "./SocialComponents";
import type { Person } from "./socialApi";

export function ChallengeDialog({
  person,
  onClose,
}: {
  person: Person;
  onClose: () => void;
}) {
  const [subjectId, setSubjectId] = useState("");
  const subjects = useQuery({
    queryKey: ["subjects"],
    queryFn: subjectsApi.list,
  });
  const selected = subjectId || subjects.data?.[0]?.id || "";
  const sets = useQuery({
    queryKey: ["study-sets", "subject", selected],
    queryFn: () => studySetsApi.list(selected),
    enabled: !!selected,
  });
  return (
    <Dialog title={`Thách đấu ${person.displayName}`} onClose={onClose}>
      <p className="social-muted">
        Chọn bộ học của bạn, thiết lập trận đấu rồi gửi lời mời.
      </p>
      <div className="social-form">
        <label>
          Môn học
          <select
            value={selected}
            onChange={(e) => setSubjectId(e.target.value)}
          >
            {subjects.data?.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </label>
      </div>
      <ErrorNotice error={subjects.error || sets.error} />
      {(subjects.isPending || (!!selected && sets.isPending)) && (
        <p role="status">Đang tải bộ học…</p>
      )}
      {sets.data?.map((s) => (
        <Link
          key={s.id}
          to={`/study-sets/${s.id}/battle?invite=${person.userId}`}
          className="social-person-row"
        >
          <strong>{s.title}</strong>
          <span className="social-text-link">Chọn →</span>
        </Link>
      ))}
      {(subjects.data?.length === 0 || sets.data?.length === 0) && (
        <Empty title="Chưa có bộ học">
          <Link className="social-text-link" to="/subjects">
            Tạo bộ học trước khi thách đấu
          </Link>
        </Empty>
      )}
    </Dialog>
  );
}
