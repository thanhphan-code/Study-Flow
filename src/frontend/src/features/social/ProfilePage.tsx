import { useState } from "react";
import { ChallengeDialog } from "./ChallengeDialog";
import { useQuery } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  MessageCircle,
  UserPlus,
  Settings2,
  Flag,
  ShieldBan,
  FileText,
  RotateCcw,
  Swords,
} from "lucide-react";
import {
  Avatar,
  SocialLayout,
  ErrorNotice,
  Empty,
  More,
  SetCard,
  Dialog,
  ReportDialog,
} from "./SocialComponents";
import {
  socialGet,
  useSocialAction,
  useSocialList,
  type Profile,
  type Person,
  type SocialSet,
} from "./socialApi";

export function ProfilePage() {
  const { username = "me" } = useParams();
  const profile = useQuery({
    queryKey: ["social", "profile", username],
    queryFn: () => socialGet<Profile>(`/profiles/${username}`),
    retry: false,
  });
  const p = profile.data;
  const sets = useSocialList<SocialSet>(
    `/sets?authorId=${p?.userId ?? ""}&sort=newest`,
    !!p && !p.isRestricted,
  );
  const action = useSocialAction<{ id: string; username: string }>();
  const navigate = useNavigate();
  const [edit, setEdit] = useState(false);
  const [challenge, setChallenge] = useState(false);
  const [report, setReport] = useState(false);
  const [confirmBlock, setConfirmBlock] = useState(false);
  const relationship = (kind: string) =>
    action.mutate({ path: `/people/${p!.userId}/${kind}` });
  return (
    <SocialLayout>
      <ErrorNotice error={profile.error || action.error} />
      {profile.isPending && <p role="status">Đang tải hồ sơ…</p>}
      {p && (
        <>
          <section className="social-profile">
            <div className="social-profile-cover">
              <span>Cùng học. Cùng tiến bộ.</span>
            </div>
            <div className="social-profile-body">
              <Avatar person={p} large />
              <div className="social-profile-name">
                <h1>{p.displayName}</h1>
                <span>@{p.username}</span>
              </div>
              {p.bio && <p className="social-bio">{p.bio}</p>}
              <div className="social-stats">
                <span>
                  <strong>{p.publicSets}</strong> bộ học công khai
                </span>
                <span>
                  <strong>{p.followers}</strong> người theo dõi
                </span>
                <span>
                  <strong>{p.following}</strong> đang theo dõi
                </span>
                <span>
                  <strong>{p.friends}</strong> bạn bè
                </span>
              </div>
              <div className="social-actions">
                {p.isOwn ? (
                  <>
                    <button
                      className="social-button primary"
                      onClick={() => setEdit(true)}
                    >
                      <Settings2 size={17} />
                      Chỉnh sửa hồ sơ
                    </button>
                    <Link className="social-button" to="/community/friends">
                      Quản lý bạn bè
                    </Link>
                  </>
                ) : (
                  <>
                    <button
                      className="social-button primary"
                      disabled={action.isPending}
                      onClick={() =>
                        relationship(p.isFollowing ? "unfollow" : "follow")
                      }
                    >
                      {p.isFollowing ? "Đang theo dõi" : "Theo dõi"}
                    </button>
                    {p.incomingRequest ? (
                      <button
                        className="social-button"
                        disabled={action.isPending}
                        onClick={() => relationship("accept")}
                      >
                        Chấp nhận kết bạn
                      </button>
                    ) : (
                      <button
                        className="social-button"
                        disabled={action.isPending}
                        onClick={() =>
                          relationship(
                            p.isFriend
                              ? "unfriend"
                              : p.outgoingRequest
                                ? "cancel"
                                : "request",
                          )
                        }
                      >
                        <UserPlus size={17} />
                        {p.isFriend
                          ? "Hủy kết bạn"
                          : p.outgoingRequest
                            ? "Hủy lời mời"
                            : "Kết bạn"}
                      </button>
                    )}
                    {p.isFriend && (
                      <>
                        <button
                          className="social-button"
                          disabled={action.isPending}
                          onClick={() => {
                            void action
                              .mutateAsync({
                                path: `/conversations/direct/${p.userId}`,
                              })
                              .then((c) => navigate(`/messages/${c.id}`))
                              .catch(() => {});
                          }}
                        >
                          <MessageCircle size={17} />
                          Nhắn tin
                        </button>
                        <button
                          className="social-button"
                          onClick={() => setChallenge(true)}
                        >
                          <Swords size={17} />
                          Thách đấu
                        </button>
                      </>
                    )}
                    <button
                      className="social-icon"
                      aria-label="Báo cáo người dùng"
                      onClick={() => setReport(true)}
                    >
                      <Flag size={18} />
                    </button>
                    <button
                      className="social-icon"
                      aria-label="Chặn người dùng"
                      onClick={() => setConfirmBlock(true)}
                    >
                      <ShieldBan size={18} />
                    </button>
                  </>
                )}
              </div>
            </div>
          </section>
          {p.isOwn && (
            <div className="social-quick-links">
              <Link to="/documents">
                <FileText size={18} />
                Tài liệu của tôi
              </Link>
              <Link to="/reviews/due">
                <RotateCcw size={18} />
                Ôn tập đến hạn
              </Link>
              <Link to="/community/friends?tab=blocked">
                <ShieldBan size={18} />
                Tài khoản đã chặn
              </Link>
            </div>
          )}
          <h2 className="social-section-title">Bộ học đã chia sẻ</h2>
          {p.isRestricted ? (
            <Empty title="Hồ sơ riêng tư">
              Người học này giới hạn thông tin hồ sơ.
            </Empty>
          ) : (
            <>
              <ErrorNotice error={sets.error} />
              <div className="social-set-grid">
                {sets.data?.pages
                  .flatMap((x) => x.items)
                  .map((s) => (
                    <SetCard item={s} key={s.id} />
                  ))}
              </div>
              {sets.data?.pages[0].items.length === 0 && (
                <Empty title="Chưa có bộ học đã chia sẻ">
                  {p.isOwn
                    ? "Mở một bộ học và chọn Chia sẻ để xuất bản."
                    : "Hãy theo dõi để dễ tìm lại tác giả này."}
                </Empty>
              )}
              <More
                more={sets.hasNextPage}
                pending={sets.isFetchingNextPage}
                load={() => {
                  void sets.fetchNextPage();
                }}
              />
            </>
          )}
        </>
      )}
      {challenge && p && (
        <ChallengeDialog person={p} onClose={() => setChallenge(false)} />
      )}
      {edit && p && (
        <Dialog title="Chỉnh sửa hồ sơ" onClose={() => setEdit(false)}>
          <form
            className="social-form"
            onSubmit={(e) => {
              e.preventDefault();
              const f = new FormData(e.currentTarget);
              void action
                .mutateAsync({
                  path: "/profile",
                  method: "PUT",
                  body: {
                    username: f.get("username"),
                    displayName: f.get("displayName"),
                    bio: f.get("bio"),
                    visibility: f.get("visibility"),
                    avatarUrl: f.get("avatarUrl"),
                  },
                })
                .then((result) => {
                  setEdit(false);
                  navigate(`/people/${result.username}`, { replace: true });
                })
                .catch(() => {});
            }}
          >
            <label>
              Tên hiển thị
              <input
                name="displayName"
                defaultValue={p.displayName}
                required
                maxLength={100}
              />
            </label>
            <label>
              Tên người dùng
              <input
                name="username"
                defaultValue={p.username.startsWith("u_") ? "" : p.username}
                placeholder="vd: minhanh_hoc"
                required
                minLength={3}
                maxLength={30}
                pattern="[A-Za-z0-9_]+"
                autoComplete="username"
              />
            </label>
            <label>
              Giới thiệu
              <textarea
                name="bio"
                defaultValue={p.bio}
                maxLength={500}
                rows={3}
              />
            </label>
            <label>
              Liên kết ảnh đại diện (HTTPS)
              <input
                name="avatarUrl"
                type="url"
                defaultValue={p.avatarUrl ?? ""}
                maxLength={1000}
              />
            </label>
            <label>
              Ai xem thông tin hồ sơ?
              <select name="visibility" defaultValue={p.visibility}>
                <option value="Public">Cộng đồng</option>
                <option value="FriendsOnly">Bạn bè</option>
                <option value="Private">Chỉ mình tôi</option>
              </select>
            </label>
            <p className="social-muted">
              Quyền xem từng bộ học được cài đặt riêng. Tiến độ học luôn riêng
              tư.
            </p>
            <ErrorNotice error={action.error} />
            <button
              className="social-button primary"
              disabled={action.isPending}
            >
              {action.isPending ? "Đang lưu…" : "Lưu thay đổi"}
            </button>
          </form>
        </Dialog>
      )}
      {report && p && (
        <ReportDialog
          targetType="User"
          targetId={p.userId}
          onClose={() => setReport(false)}
        />
      )}
      {confirmBlock && p && (
        <Dialog
          title={`Chặn ${p.displayName}?`}
          onClose={() => setConfirmBlock(false)}
        >
          <p>
            Hai người sẽ không thể theo dõi, kết bạn, nhắn tin hoặc tương tác
            với nhau. Quan hệ bạn bè hiện tại sẽ được gỡ.
          </p>
          <button
            className="social-button danger"
            disabled={action.isPending}
            onClick={() => {
              void action
                .mutateAsync({ path: `/people/${p.userId}/block` })
                .then(() => navigate("/community/friends?tab=blocked"))
                .catch(() => {});
            }}
          >
            Chặn người dùng
          </button>
          <ErrorNotice error={action.error} />
        </Dialog>
      )}
    </SocialLayout>
  );
}

export function FriendsPage() {
  const [tab, setTab] = useState(
    new URLSearchParams(location.search).get("tab") || "friends",
  );
  const people = useSocialList<Person>(`/people?kind=${tab}`);
  const action = useSocialAction<{ id: string }>();
  const navigate = useNavigate();
  return (
    <SocialLayout>
      <header className="social-heading">
        <h1>Bạn đồng hành</h1>
        <p>Kết nối với những người cùng bạn học tốt hơn mỗi ngày.</p>
      </header>
      <div className="social-tabs">
        {[
          ["friends", "Bạn bè"],
          ["requests", "Lời mời"],
          ["following", "Đang theo dõi"],
          ["followers", "Người theo dõi"],
          ["blocked", "Đã chặn"],
        ].map(([key, label]) => (
          <button
            className={tab === key ? "active" : ""}
            aria-pressed={tab === key}
            key={key}
            onClick={() => setTab(key)}
          >
            {label}
          </button>
        ))}
      </div>
      <ErrorNotice error={people.error || action.error} />
      {people.isPending && <p role="status">Đang tải…</p>}
      <div className="social-panel">
        {people.data?.pages
          .flatMap((p) => p.items)
          .map((p) => (
            <div className="social-person-row" key={p.userId}>
              <Avatar person={p} />
              <div className="social-person-info">
                {tab === "blocked" ? (
                  <strong>{p.displayName}</strong>
                ) : (
                  <Link to={`/people/${p.username}`}>
                    <strong>{p.displayName}</strong>
                  </Link>
                )}
                <small>@{p.username}</small>
              </div>
              {tab === "requests" ? (
                <div className="social-actions">
                  <button
                    className="social-button primary"
                    disabled={action.isPending}
                    onClick={() =>
                      action.mutate({ path: `/people/${p.userId}/accept` })
                    }
                  >
                    Chấp nhận
                  </button>
                  <button
                    className="social-button"
                    disabled={action.isPending}
                    onClick={() =>
                      action.mutate({ path: `/people/${p.userId}/decline` })
                    }
                  >
                    Từ chối
                  </button>
                </div>
              ) : tab === "blocked" ? (
                <button
                  className="social-button"
                  disabled={action.isPending}
                  onClick={() =>
                    action.mutate({ path: `/people/${p.userId}/unblock` })
                  }
                >
                  Bỏ chặn
                </button>
              ) : tab === "friends" ? (
                <button
                  aria-label={`Nhắn tin ${p.displayName}`}
                  className="social-icon"
                  disabled={action.isPending}
                  onClick={() => {
                    void action
                      .mutateAsync({
                        path: `/conversations/direct/${p.userId}`,
                      })
                      .then((c) => navigate(`/messages/${c.id}`))
                      .catch(() => {});
                  }}
                >
                  <MessageCircle size={20} />
                </button>
              ) : null}
            </div>
          ))}
        {people.data?.pages[0].items.length === 0 && (
          <Empty title="Danh sách đang trống">
            <Link to="/explore" className="social-text-link">
              Khám phá người học và bộ học
            </Link>
          </Empty>
        )}
      </div>
      <More
        more={people.hasNextPage}
        pending={people.isFetchingNextPage}
        load={() => {
          void people.fetchNextPage();
        }}
      />
    </SocialLayout>
  );
}
