import { useState } from "react";
import {
  SocialLayout,
  ErrorNotice,
  Empty,
  More,
  Dialog,
} from "./SocialComponents";
import { useSocialList, useSocialAction } from "./socialApi";

type Report = {
  id: string;
  targetId: string;
  targetType: string;
  reason: string;
  details: string;
  status: string;
  createdAt: string;
  summary: string;
};
export function ModerationPage() {
  const reports = useSocialList<Report>("/moderation/reports");
  const action = useSocialAction();
  const [confirm, setConfirm] = useState<{
    report: Report;
    action: string;
  } | null>(null);
  return (
    <SocialLayout>
      <header className="social-heading">
        <h1>Báo cáo cộng đồng</h1>
        <p>Xem xét nội dung được báo cáo và xử lý vi phạm.</p>
      </header>
      <ErrorNotice error={reports.error || action.error} />
      {reports.isPending && <p role="status">Đang tải báo cáo…</p>}
      {reports.data?.pages
        .flatMap((p) => p.items)
        .map((r) => (
          <article key={r.id} className="social-panel">
            <div className="social-section-heading">
              <h2>
                {r.targetType} · {r.reason}
              </h2>
              <span>{r.status}</span>
            </div>
            <p>{r.summary}</p>
            <p className="social-muted">
              {r.details || "Không có thông tin bổ sung."}
            </p>
            <small>
              {new Date(r.createdAt).toLocaleString("vi-VN")} · {r.targetId}
            </small>
            <div className="social-actions" style={{ margin: "18px 0" }}>
              {r.status === "Pending" && (
                <>
                  <button
                    className="social-button"
                    disabled={action.isPending}
                    onClick={() =>
                      action.mutate({
                        path: `/moderation/reports/${r.id}`,
                        body: { action: "Dismiss" },
                      })
                    }
                  >
                    Bỏ qua báo cáo
                  </button>
                  <button
                    className="social-button danger"
                    onClick={() =>
                      setConfirm({
                        report: r,
                        action: r.targetType === "User" ? "Suspend" : "Remove",
                      })
                    }
                  >
                    {r.targetType === "User"
                      ? "Tạm ngưng tài khoản"
                      : "Ẩn nội dung"}
                  </button>
                </>
              )}
              {r.targetType === "User" && r.status === "Resolved" && (
                <button
                  className="social-button"
                  onClick={() => setConfirm({ report: r, action: "Restore" })}
                >
                  Khôi phục tài khoản
                </button>
              )}
            </div>
          </article>
        ))}
      {reports.data?.pages[0].items.length === 0 && (
        <Empty title="Không có báo cáo">
          Các báo cáo mới sẽ xuất hiện tại đây.
        </Empty>
      )}
      <More
        more={reports.hasNextPage}
        pending={reports.isFetchingNextPage}
        load={() => {
          void reports.fetchNextPage();
        }}
      />
      {confirm && (
        <Dialog title="Xác nhận xử lý báo cáo" onClose={() => setConfirm(null)}>
          <p>
            Thao tác sẽ thay đổi quyền truy cập nội dung hoặc tài khoản được báo
            cáo.
          </p>
          <ErrorNotice error={action.error} />
          <button
            className="social-button danger"
            disabled={action.isPending}
            onClick={() => {
              void action
                .mutateAsync({
                  path: `/moderation/reports/${confirm.report.id}`,
                  body: { action: confirm.action },
                })
                .then(() => setConfirm(null))
                .catch(() => {});
            }}
          >
            Xác nhận
          </button>
        </Dialog>
      )}
    </SocialLayout>
  );
}
