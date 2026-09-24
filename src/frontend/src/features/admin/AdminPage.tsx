import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  Activity,
  ArrowLeft,
  BookOpenCheck,
  FileWarning,
  History,
  KeyRound,
  Search,
  ShieldCheck,
  UserCheck,
  UserRoundCog,
  UsersRound,
  X,
} from "lucide-react";
import { ApiRequestError } from "@/api/httpClient";
import { useAuthStore } from "@/features/auth/store/authStore";
import {
  adminApi,
  type AdminOverview,
  type AdminUser,
  type AuditItem,
  type Paged,
  type UserDetails,
  type UserRole,
} from "./adminApi";
import "./admin.css";

const date = (value: string | null) =>
  value
    ? new Intl.DateTimeFormat("vi-VN", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(value))
    : "Chưa hoạt động";
const active = (value: string | null) =>
  Boolean(value && Date.now() - new Date(value).getTime() <= 15 * 60_000);
const errorMessage = (error: unknown) =>
  error instanceof ApiRequestError
    ? error.message
    : "Không thể tải dữ liệu. Vui lòng thử lại.";
const actionLabels: Record<string, string> = {
  USER_SUSPENDED: "Tạm ngưng tài khoản",
  USER_ACTIVATED: "Mở lại tài khoản",
  USER_ROLE_CHANGED: "Thay đổi vai trò",
  USER_SESSIONS_REVOKED: "Thu hồi phiên",
};

export function AdminPage() {
  const currentUser = useAuthStore((state) => state.user)!;
  const [tab, setTab] = useState<"users" | "audit">("users");
  const [overview, setOverview] = useState<AdminOverview | null>(null);
  const [users, setUsers] = useState<Paged<AdminUser> | null>(null);
  const [audits, setAudits] = useState<Paged<AuditItem> | null>(null);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("all");
  const [role, setRole] = useState("all");
  const [page, setPage] = useState(1);
  const [auditPage, setAuditPage] = useState(1);
  const [selected, setSelected] = useState<UserDetails | null>(null);
  const [openingUserId, setOpeningUserId] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadOverview = useCallback(
    async () => setOverview(await adminApi.overview()),
    [],
  );
  const loadUsers = useCallback(
    async () => setUsers(await adminApi.users({ search, status, role, page })),
    [search, status, role, page],
  );
  const loadAudit = useCallback(
    async () => setAudits(await adminApi.audit(auditPage)),
    [auditPage],
  );

  useEffect(() => {
    let activeRequest = true;
    void Promise.resolve().then(async () => {
      if (!activeRequest) return;
      setLoading(true);
      setError("");
      try {
        await Promise.all([
          loadOverview(),
          tab === "users" ? loadUsers() : loadAudit(),
        ]);
      } catch (reason: unknown) {
        if (activeRequest) setError(errorMessage(reason));
      } finally {
        if (activeRequest) setLoading(false);
      }
    });
    return () => {
      activeRequest = false;
    };
  }, [tab, loadOverview, loadUsers, loadAudit]);

  const refreshSelected = async (id: string) =>
    setSelected(await adminApi.user(id));
  const openUser = async (id: string) => {
    setOpeningUserId(id);
    setError("");
    try {
      await refreshSelected(id);
    } catch (reason: unknown) {
      setError(errorMessage(reason));
    } finally {
      setOpeningUserId("");
    }
  };
  const refresh = async () => {
    await Promise.all([loadOverview(), loadUsers(), loadAudit()]);
    if (selected) await refreshSelected(selected.user.id);
  };
  const summary = useMemo(
    () =>
      overview
        ? [
            {
              label: "Đang hoạt động",
              value: overview.activeUsers,
              note: "Trong 15 phút",
              icon: Activity,
              tone: "green",
            },
            {
              label: "Tổng người dùng",
              value: overview.totalUsers,
              note: `+${overview.newUsersToday} hôm nay`,
              icon: UsersRound,
              tone: "blue",
            },
            {
              label: "Tạm ngưng",
              value: overview.suspendedUsers,
              note: "Cần rà soát",
              icon: UserRoundCog,
              tone: "amber",
            },
            {
              label: "Chưa xác minh",
              value: overview.unverifiedUsers,
              note: "Email chưa hoàn tất",
              icon: UserCheck,
              tone: "slate",
            },
          ]
        : [],
    [overview],
  );

  return (
    <div className="admin-root">
      <a className="skip-link" href="#admin-main">
        Đi đến nội dung chính
      </a>
      <header className="admin-topbar">
        <div className="admin-brand">
          <span>SF</span>
          <div>
            <strong>StudyFlow Control</strong>
            <small>Quản trị hệ thống</small>
          </div>
        </div>
        <div className="admin-admin-id">
          <ShieldCheck size={17} aria-hidden />
          <span>{currentUser.displayName}</span>
          <Link to="/home" aria-label="Trở về ứng dụng">
            <ArrowLeft size={18} />
          </Link>
        </div>
      </header>
      <main id="admin-main" className="admin-main">
        <section className="admin-heading">
          <div>
            <p className="admin-kicker">System overview / live</p>
            <h1>
              Kiểm soát người dùng
              <br />
              <span>từ một nơi.</span>
            </h1>
          </div>
          <div className="admin-health">
            <i />
            <span>Hệ thống đang ghi nhận hoạt động</span>
            <small>Dữ liệu làm mới theo thao tác</small>
          </div>
        </section>

        {error && (
          <div className="admin-error" role="alert">
            {error}
            <button onClick={() => location.reload()}>Tải lại</button>
          </div>
        )}
        <section className="admin-summary" aria-label="Tổng quan người dùng">
          {summary.map(({ label, value, note, icon: Icon, tone }) => (
            <article className={`admin-stat ${tone}`} key={label}>
              <Icon aria-hidden />
              <div>
                <strong>{value.toLocaleString("vi-VN")}</strong>
                <span>{label}</span>
                <small>{note}</small>
              </div>
            </article>
          ))}
          {!overview &&
            Array.from({ length: 4 }, (_, index) => (
              <div className="admin-stat admin-skeleton" key={index} />
            ))}
        </section>

        <div className="admin-toolbar">
          <div className="admin-tabs" role="tablist">
            <button
              className={tab === "users" ? "active" : ""}
              onClick={() => setTab("users")}
            >
              <UsersRound size={17} />
              Người dùng
            </button>
            <button
              className={tab === "audit" ? "active" : ""}
              onClick={() => setTab("audit")}
            >
              <History size={17} />
              Nhật ký quản trị
            </button>
          </div>
          {tab === "users" && (
            <form
              className="admin-search"
              onSubmit={(event) => {
                event.preventDefault();
                setPage(1);
                setSearch(searchInput.trim());
              }}
            >
              <Search size={17} aria-hidden />
              <label className="sr-only" htmlFor="admin-search">
                Tìm người dùng
              </label>
              <input
                id="admin-search"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder="Tên hoặc email…"
              />
              <button>Tìm</button>
            </form>
          )}
        </div>

        {tab === "users" ? (
          <section className="admin-panel">
            <div className="admin-filters">
              <label>
                Trạng thái
                <select
                  value={status}
                  onChange={(event) => {
                    setStatus(event.target.value);
                    setPage(1);
                  }}
                >
                  <option value="all">Tất cả</option>
                  <option value="active">Đang hoạt động</option>
                  <option value="offline">Ngoại tuyến</option>
                  <option value="suspended">Tạm ngưng</option>
                  <option value="unverified">Chưa xác minh</option>
                </select>
              </label>
              <label>
                Vai trò
                <select
                  value={role}
                  onChange={(event) => {
                    setRole(event.target.value);
                    setPage(1);
                  }}
                >
                  <option value="all">Tất cả</option>
                  <option value="User">Người dùng</option>
                  <option value="Admin">Quản trị viên</option>
                </select>
              </label>
              <span>{users?.totalItems ?? 0} kết quả</span>
            </div>
            <div className="admin-table-wrap">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>Người dùng</th>
                    <th>Trạng thái</th>
                    <th>Vai trò</th>
                    <th>Hoạt động gần nhất</th>
                    <th>
                      <span className="sr-only">Thao tác</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {users?.items.map((user) => (
                    <tr key={user.id}>
                      <td>
                        <button
                          className="admin-person"
                          onClick={() => void openUser(user.id)}
                        >
                          <span>
                            {user.displayName.slice(0, 2).toUpperCase()}
                          </span>
                          <div>
                            <strong>{user.displayName}</strong>
                            <small>{user.email}</small>
                          </div>
                        </button>
                      </td>
                      <td>
                        <Status user={user} />
                      </td>
                      <td>
                        <span
                          className={`admin-role ${user.role.toLowerCase()}`}
                        >
                          {user.role === "Admin" ? "Admin" : "User"}
                        </span>
                      </td>
                      <td>
                        <strong className="admin-date">
                          {date(user.lastActiveAt)}
                        </strong>
                        <small>
                          {user.createdAt
                            ? `Tham gia ${date(user.createdAt)}`
                            : ""}
                        </small>
                      </td>
                      <td>
                        <button
                          className="admin-open"
                          disabled={openingUserId === user.id}
                          onClick={() => void openUser(user.id)}
                        >
                          {openingUserId === user.id ? "Đang mở…" : "Quản lý"}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {!loading && users?.items.length === 0 && (
                <div className="admin-empty">
                  Không tìm thấy người dùng phù hợp.
                </div>
              )}
              {loading && (
                <div className="admin-loading" role="status">
                  Đang tải dữ liệu…
                </div>
              )}
            </div>
            <div className="admin-mobile-users">
              {users?.items.map((user) => (
                <article key={user.id}>
                  <button
                    className="admin-mobile-person"
                    onClick={() => void openUser(user.id)}
                    aria-label={`Quản lý ${user.displayName}`}
                  >
                    <span>{user.displayName.slice(0, 2).toUpperCase()}</span>
                    <div>
                      <strong>{user.displayName}</strong>
                      <small>{user.email}</small>
                    </div>
                  </button>
                  <div className="admin-mobile-meta">
                    <Status user={user} />
                    <span className={`admin-role ${user.role.toLowerCase()}`}>
                      {user.role}
                    </span>
                  </div>
                  <p>{date(user.lastActiveAt)}</p>
                  <button
                    className="admin-mobile-manage"
                    disabled={openingUserId === user.id}
                    onClick={() => void openUser(user.id)}
                  >
                    {openingUserId === user.id
                      ? "Đang mở…"
                      : "Quản lý tài khoản"}
                  </button>
                </article>
              ))}
              {!loading && users?.items.length === 0 && (
                <div className="admin-empty">
                  Không tìm thấy người dùng phù hợp.
                </div>
              )}
            </div>
            <Pagination
              page={users?.page ?? page}
              total={users?.totalPages ?? 1}
              setPage={setPage}
            />
          </section>
        ) : (
          <AuditPanel
            data={audits}
            loading={loading}
            page={auditPage}
            setPage={setAuditPage}
          />
        )}

        {overview && (
          <section className="admin-integrity">
            <div>
              <BookOpenCheck />
              <span>
                <strong>{overview.studySessionsToday}</strong> phiên học hôm nay
              </span>
            </div>
            <div>
              <FileWarning />
              <span>
                <strong>{overview.failedDocuments}</strong> tài liệu lỗi
              </span>
            </div>
            <Link to="/community/moderation">
              <ShieldCheck />
              <span>
                <strong>{overview.pendingReports}</strong> báo cáo chờ xử lý
              </span>
            </Link>
            <div>
              <KeyRound />
              <span>
                <strong>{overview.adminUsers}</strong> quản trị viên
              </span>
            </div>
          </section>
        )}
      </main>
      {selected && (
        <UserDrawer
          details={selected}
          self={currentUser.id === selected.user.id}
          close={() => setSelected(null)}
          changed={refresh}
        />
      )}
    </div>
  );
}

function Status({ user }: { user: AdminUser }) {
  if (user.isSuspended)
    return (
      <span className="admin-status suspended">
        <i />
        Tạm ngưng
      </span>
    );
  if (active(user.lastActiveAt))
    return (
      <span className="admin-status online">
        <i />
        Đang hoạt động
      </span>
    );
  if (!user.isEmailVerified)
    return (
      <span className="admin-status unverified">
        <i />
        Chưa xác minh
      </span>
    );
  return (
    <span className="admin-status offline">
      <i />
      Ngoại tuyến
    </span>
  );
}

function Pagination({
  page,
  total,
  setPage,
}: {
  page: number;
  total: number;
  setPage: (page: number) => void;
}) {
  if (total <= 1) return null;
  return (
    <nav className="admin-pagination" aria-label="Phân trang">
      <button disabled={page <= 1} onClick={() => setPage(page - 1)}>
        Trước
      </button>
      <span>
        Trang <strong>{page}</strong> / {total}
      </span>
      <button disabled={page >= total} onClick={() => setPage(page + 1)}>
        Sau
      </button>
    </nav>
  );
}

function AuditPanel({
  data,
  loading,
  page,
  setPage,
}: {
  data: Paged<AuditItem> | null;
  loading: boolean;
  page: number;
  setPage: (page: number) => void;
}) {
  return (
    <section className="admin-panel admin-audit">
      <header>
        <div>
          <span>Chuỗi kiểm toán</span>
          <h2>Mọi thay đổi có người chịu trách nhiệm</h2>
        </div>
        <small>{data?.totalItems ?? 0} sự kiện</small>
      </header>
      {data?.items.map((item) => (
        <article key={item.id}>
          <i />
          <div>
            <strong>{actionLabels[item.action] ?? item.action}</strong>
            <p>
              <b>{item.actorDisplayName}</b> tác động lên{" "}
              <b>{item.targetDisplayName}</b>
            </p>
            <blockquote>{item.reason}</blockquote>
          </div>
          <time>{date(item.createdAt)}</time>
        </article>
      ))}
      {!loading && data?.items.length === 0 && (
        <div className="admin-empty">Chưa có thao tác quản trị nào.</div>
      )}
      <Pagination
        page={data?.page ?? page}
        total={data?.totalPages ?? 1}
        setPage={setPage}
      />
    </section>
  );
}

function UserDrawer({
  details,
  self,
  close,
  changed,
}: {
  details: UserDetails;
  self: boolean;
  close: () => void;
  changed: () => Promise<void>;
}) {
  const [reason, setReason] = useState("");
  const [role, setRole] = useState<UserRole>(details.user.role);
  const [pending, setPending] = useState("");
  const [error, setError] = useState("");
  const act = async (name: string, action: () => Promise<unknown>) => {
    if (reason.trim().length < 3) {
      setError("Hãy nhập lý do cụ thể, tối thiểu 3 ký tự.");
      return;
    }
    setPending(name);
    setError("");
    try {
      await action();
      setReason("");
      await changed();
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setPending("");
    }
  };
  const user = details.user;
  return (
    <div
      className="admin-drawer-layer"
      role="presentation"
      onMouseDown={(event) => {
        if (event.currentTarget === event.target) close();
      }}
    >
      <aside
        className="admin-drawer"
        aria-label={`Quản lý ${user.displayName}`}
      >
        <header>
          <div className="admin-drawer-avatar">
            {user.displayName.slice(0, 2).toUpperCase()}
          </div>
          <button onClick={close} aria-label="Đóng">
            <X />
          </button>
          <h2>{user.displayName}</h2>
          <p>{user.email}</p>
          <div>
            <Status user={user} />
            <span className={`admin-role ${user.role.toLowerCase()}`}>
              {user.role}
            </span>
          </div>
        </header>
        <section className="admin-drawer-metrics">
          <div>
            <strong>{details.studySets}</strong>
            <span>Học phần</span>
          </div>
          <div>
            <strong>{details.studySessions}</strong>
            <span>Phiên học</span>
          </div>
          <div>
            <strong>{details.documents}</strong>
            <span>Tài liệu</span>
          </div>
          <div>
            <strong>{details.battleParticipations}</strong>
            <span>Battle</span>
          </div>
        </section>
        <section className="admin-user-facts">
          <p>
            <span>Tên người dùng</span>
            <strong>
              {user.username ? `@${user.username}` : "Chưa thiết lập"}
            </strong>
          </p>
          <p>
            <span>Xác minh email</span>
            <strong>
              {user.isEmailVerified ? "Đã xác minh" : "Chưa xác minh"}
            </strong>
          </p>
          <p>
            <span>Tham gia</span>
            <strong>{date(user.createdAt)}</strong>
          </p>
          {user.suspensionReason && (
            <p>
              <span>Lý do tạm ngưng</span>
              <strong>{user.suspensionReason}</strong>
            </p>
          )}
        </section>
        <section className="admin-actions">
          <h3>Kiểm soát truy cập</h3>
          {self && (
            <p className="admin-self-note">
              Bạn đang xem tài khoản của chính mình. Các thao tác tự khóa và tự
              đổi quyền đã bị vô hiệu.
            </p>
          )}
          <label>
            Lý do bắt buộc
            <textarea
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              maxLength={500}
              placeholder="Ghi rõ căn cứ để lưu vào nhật ký…"
            />
          </label>
          {error && (
            <p className="admin-inline-error" role="alert">
              {error}
            </p>
          )}
          <div className="admin-role-control">
            <label>
              Vai trò
              <select
                value={role}
                disabled={self}
                onChange={(event) => setRole(event.target.value as UserRole)}
              >
                <option value="User">Người dùng</option>
                <option value="Admin">Quản trị viên</option>
              </select>
            </label>
            <button
              disabled={self || pending !== "" || role === user.role}
              onClick={() =>
                void act("role", () => adminApi.role(user.id, role, reason))
              }
            >
              {pending === "role" ? "Đang lưu…" : "Cập nhật quyền"}
            </button>
          </div>
          <button
            className="admin-revoke"
            disabled={pending !== ""}
            onClick={() =>
              void act("revoke", () => adminApi.revoke(user.id, reason))
            }
          >
            <KeyRound size={17} />
            {pending === "revoke"
              ? "Đang thu hồi…"
              : "Thu hồi tất cả phiên đăng nhập"}
          </button>
          <button
            className={user.isSuspended ? "admin-activate" : "admin-suspend"}
            disabled={self || pending !== ""}
            onClick={() =>
              void act("status", () =>
                adminApi.status(user.id, !user.isSuspended, reason),
              )
            }
          >
            {pending === "status"
              ? "Đang cập nhật…"
              : user.isSuspended
                ? "Mở lại tài khoản"
                : "Tạm ngưng tài khoản"}
          </button>
        </section>
        {details.recentAudit.length > 0 && (
          <section className="admin-recent">
            <h3>Thay đổi gần đây</h3>
            {details.recentAudit.slice(0, 5).map((item) => (
              <div key={item.id}>
                <span>{actionLabels[item.action] ?? item.action}</span>
                <time>{date(item.createdAt)}</time>
              </div>
            ))}
          </section>
        )}
      </aside>
    </div>
  );
}
