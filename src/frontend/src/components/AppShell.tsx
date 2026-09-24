import { lazy, Suspense, type PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useLanguage } from "@/i18n/LanguageProvider";
import {
  Swords,
  BookOpen,
  ChartNoAxesColumnIncreasing,
  Compass,
  House,
  ShieldCheck,
} from "lucide-react";
import { useAuthStore } from "@/features/auth/store/authStore";
const SocialHeader = lazy(() =>
  import("@/features/social/SocialHeader").then((m) => ({
    default: m.SocialHeader,
  })),
);

const icons = {
  home: House,
  subjects: BookOpen,
  explore: Compass,
  progress: ChartNoAxesColumnIncreasing,
  battle: Swords,
};

export function LanguageSwitch() {
  const { language, setLanguage, t } = useLanguage();
  return (
    <div className="language-switch" role="group" aria-label={t("language")}>
      <button
        type="button"
        aria-pressed={language === "en"}
        className={language === "en" ? "active" : ""}
        onClick={() => setLanguage("en")}
      >
        EN
      </button>
      <button
        type="button"
        aria-pressed={language === "vi"}
        className={language === "vi" ? "active" : ""}
        onClick={() => setLanguage("vi")}
      >
        VI
      </button>
    </div>
  );
}

export function AppShell({ children }: PropsWithChildren) {
  const { t, language } = useLanguage();
  const user = useAuthStore((state) => state.user);
  const location = useLocation();
  const focused =
    location.pathname.startsWith("/battles/") ||
    location.pathname.startsWith("/study/") ||
    location.pathname.startsWith("/study-together") ||
    location.pathname.startsWith("/quizzes/") ||
    location.pathname === "/reviews/due" ||
    location.pathname === "/login" ||
    location.pathname === "/register";
  const links = [
    ["/home", "home"],
    ["/explore", "explore"],
    ["/subjects", "subjects"],
    ["/join", "battle"],
    ["/progress", "progress"],
  ] as const;
  const navigation = (className: string) => (
    <nav
      className={className}
      aria-label={language === "vi" ? "Điều hướng chính" : "Primary navigation"}
    >
      {links.map(([to, key]) => {
        const Icon = icons[key];
        return (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) => (isActive ? "active" : "")}
          >
            <Icon aria-hidden size={18} />
            <small>
              {key === "battle"
                ? "Battle"
                : key === "explore"
                  ? language === "vi"
                    ? "Khám phá"
                    : "Explore"
                  : t(key)}
            </small>
          </NavLink>
        );
      })}
    </nav>
  );

  return (
    <div className={focused ? "" : "app-shell"}>
      <a href="#main-content" className="skip-link">
        {language === "vi" ? "Đi đến nội dung chính" : "Skip to main content"}
      </a>
      {!focused && (
        <header className="app-topbar">
          <NavLink
            to="/home"
            className="brand-mark"
            aria-label="StudyFlow home"
          >
            Study<span>Flow</span>
          </NavLink>
          {navigation("desktop-nav")}
          <div className="app-account-tools">
            {user?.role === "Admin" && (
              <NavLink
                className="admin-entry"
                to="/admin"
                aria-label="Mở bảng quản trị"
              >
                <ShieldCheck size={17} aria-hidden />
                <span>Admin</span>
              </NavLink>
            )}
            <Suspense fallback={<span style={{ width: 120, height: 40 }} />}>
              <SocialHeader />
            </Suspense>
            <LanguageSwitch />
          </div>
        </header>
      )}
      <div
        key={location.pathname}
        id="main-content"
        className="route-view"
        tabIndex={-1}
      >
        {children}
      </div>
      {!focused && navigation("bottom-nav")}
    </div>
  );
}
