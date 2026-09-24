import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent,
} from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { MailCheck, ArrowLeft } from "lucide-react";
import { ApiRequestError, type AuthPayload } from "@/api/httpClient";
import { authApi } from "@/features/auth/api/authApi";
import { useAuthStore } from "@/features/auth/store/authStore";
import { LanguageSwitch } from "@/components/AppShell";
import { useLanguage } from "@/i18n/LanguageProvider";

export function AuthPage({ mode }: { mode: "login" | "register" }) {
  const [error, setError] = useState(""),
    [pending, setPending] = useState(false);
  const [verificationEmail, setVerificationEmail] = useState(""),
    [resendAt, setResendAt] = useState(0);
  const setSession = useAuthStore((state) => state.setSession),
    navigate = useNavigate(),
    location = useLocation();
  const { language } = useLanguage();
  const vi = language === "vi",
    registering = mode === "register";
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setPending(true);
    const data = new FormData(event.currentTarget),
      credentials = {
        email: String(data.get("email")).trim(),
        password: String(data.get("password")),
      };
    try {
      if (registering) {
        const result = await authApi.register({
          ...credentials,
          displayName: String(data.get("displayName")),
          timeZoneId: Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC",
        });
        if ("accessToken" in result) {
          setSession(result);
          navigate(safeReturnPath((location.state as { from?: string } | null)?.from), { replace: true });
        } else {
          setVerificationEmail(result.email);
          setResendAt(Date.now() + result.resendAfterSeconds * 1000);
        }
      } else {
        const result = await authApi.login(credentials);
        setSession(result);
        navigate(
          safeReturnPath((location.state as { from?: string } | null)?.from),
          { replace: true },
        );
      }
    } catch (reason) {
      if (
        !registering &&
        reason instanceof ApiRequestError &&
        reason.code === "EMAIL_NOT_VERIFIED"
      ) {
        setVerificationEmail(credentials.email);
        setResendAt(0);
      } else
        setError(
          reason instanceof ApiRequestError
            ? reason.message
            : vi
              ? "Không thể kết nối với StudyFlow."
              : "Unable to connect to StudyFlow.",
        );
    } finally {
      setPending(false);
    }
  }
  if (verificationEmail)
    return (
      <OtpStep
        email={verificationEmail}
        resendAt={resendAt}
        setResendAt={setResendAt}
        onBack={() => {
          setVerificationEmail("");
          setError("");
        }}
        onVerified={(session) => {
          setSession(session);
          navigate(
            safeReturnPath((location.state as { from?: string } | null)?.from),
            { replace: true },
          );
        }}
      />
    );
  return (
    <main className="grid min-h-screen place-items-center px-5 py-10">
      <div className="absolute right-5 top-5">
        <LanguageSwitch />
      </div>
      <div className="sf-card w-full max-w-md rounded-[2rem] p-7 sm:p-9">
        <p className="brand-mark">
          Study<span>Flow</span>
        </p>
        <h1 className="mt-7 text-3xl font-bold tracking-[-.04em] text-[#111943]">
          {registering
            ? vi
              ? "Tạo tài khoản"
              : "Create your account"
            : vi
              ? "Chào mừng trở lại"
              : "Welcome back"}
        </h1>
        <p className="mt-2 text-slate-500">
          {registering
            ? vi
              ? "Đăng ký và xác minh email để bắt đầu học."
              : "Sign up and verify your email to start learning."
            : vi
              ? "Tiếp tục từ nơi bạn đã dừng lại."
              : "Continue where you left off."}
        </p>
        <form className="mt-8 space-y-5" onSubmit={submit}>
          {registering && (
            <Field
              label={vi ? "Tên hiển thị" : "Display name"}
              name="displayName"
              autoComplete="name"
            />
          )}
          <Field label="Email" name="email" type="email" autoComplete="email" />
          <Field
            label={vi ? "Mật khẩu" : "Password"}
            name="password"
            type="password"
            autoComplete={registering ? "new-password" : "current-password"}
            hint={
              registering
                ? vi
                  ? "Ít nhất 8 ký tự, gồm chữ hoa, chữ thường và số."
                  : "At least 8 characters with uppercase, lowercase and a number."
                : undefined
            }
          />
          {error && (
            <p
              role="alert"
              className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700"
            >
              {error}
            </p>
          )}
          <button
            disabled={pending}
            className="sf-primary w-full rounded-xl px-4 py-3 font-bold text-white disabled:opacity-60"
          >
            {pending
              ? vi
                ? "Vui lòng chờ…"
                : "Please wait…"
              : registering
                ? vi
                  ? "Gửi mã xác minh"
                  : "Send verification code"
                : vi
                  ? "Đăng nhập"
                  : "Sign in"}
          </button>
        </form>
        <p className="mt-6 text-center text-sm text-slate-500">
          {registering
            ? vi
              ? "Đã có tài khoản?"
              : "Already have an account?"
            : vi
              ? "Chưa có tài khoản?"
              : "New to StudyFlow?"}{" "}
          <Link
            className="font-bold text-blue-600"
            to={registering ? "/login" : "/register"}
            state={location.state}
          >
            {registering
              ? vi
                ? "Đăng nhập"
                : "Sign in"
              : vi
                ? "Tạo tài khoản"
                : "Create account"}
          </Link>
        </p>
      </div>
    </main>
  );
}

function OtpStep({
  email,
  resendAt,
  setResendAt,
  onBack,
  onVerified,
}: {
  email: string;
  resendAt: number;
  setResendAt: (value: number) => void;
  onBack: () => void;
  onVerified: (session: AuthPayload) => void;
}) {
  const [digits, setDigits] = useState(["", "", "", "", "", ""]),
    [error, setError] = useState(""),
    [pending, setPending] = useState(false),
    [now, setNow] = useState(() => Date.now());
  const inputs = useRef<Array<HTMLInputElement | null>>([]);
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    inputs.current[0]?.focus();
    return () => clearInterval(timer);
  }, []);
  const remaining = Math.max(0, Math.ceil((resendAt - now) / 1000));
  function apply(value: string, start = 0) {
    const incoming = value
      .replace(/\D/g, "")
      .slice(0, 6 - start)
      .split("");
    setDigits((current) => {
      const next = [...current];
      incoming.forEach((digit, index) => {
        next[start + index] = digit;
      });
      return next;
    });
    inputs.current[Math.min(5, start + incoming.length)]?.focus();
  }
  function keyDown(event: KeyboardEvent<HTMLInputElement>, index: number) {
    if (event.key === "Backspace" && !digits[index] && index > 0)
      inputs.current[index - 1]?.focus();
    if (event.key === "ArrowLeft" && index > 0)
      inputs.current[index - 1]?.focus();
    if (event.key === "ArrowRight" && index < 5)
      inputs.current[index + 1]?.focus();
  }
  async function verify(event: FormEvent) {
    event.preventDefault();
    const code = digits.join("");
    if (code.length !== 6) {
      setError("Vui lòng nhập đủ 6 chữ số.");
      return;
    }
    setPending(true);
    setError("");
    try {
      onVerified(await authApi.verifyEmail(email, code));
    } catch (reason) {
      setError(
        reason instanceof ApiRequestError
          ? reason.message
          : "Không thể xác minh mã OTP.",
      );
    } finally {
      setPending(false);
    }
  }
  async function resend() {
    setPending(true);
    setError("");
    try {
      const result = await authApi.resendEmailOtp(email);
      setResendAt(Date.now() + result.resendAfterSeconds * 1000);
      setDigits(["", "", "", "", "", ""]);
      inputs.current[0]?.focus();
    } catch (reason) {
      setError(
        reason instanceof ApiRequestError
          ? reason.message
          : "Không thể gửi lại mã OTP.",
      );
    } finally {
      setPending(false);
    }
  }
  return (
    <main className="grid min-h-screen place-items-center px-5 py-10">
      <div className="sf-card w-full max-w-md rounded-[2rem] p-7 sm:p-9">
        <button
          className="mb-6 inline-flex min-h-11 items-center gap-2 text-sm font-semibold text-slate-500"
          onClick={onBack}
        >
          <ArrowLeft size={17} />
          Quay lại
        </button>
        <div className="grid h-14 w-14 place-items-center rounded-2xl bg-blue-50 text-blue-600">
          <MailCheck size={28} />
        </div>
        <h1 className="mt-6 text-3xl font-bold tracking-[-.04em] text-[#111943]">
          Kiểm tra email
        </h1>
        <p className="mt-3 leading-7 text-slate-500">
          Nhập mã gồm 6 chữ số vừa gửi đến{" "}
          <strong className="text-slate-700">{maskEmail(email)}</strong>. Mã có
          hiệu lực trong 10 phút.
        </p>
        <form className="mt-7" onSubmit={verify}>
          <div
            className="grid grid-cols-6 gap-2"
            onPaste={(event) => {
              event.preventDefault();
              apply(event.clipboardData.getData("text"));
            }}
          >
            {digits.map((digit, index) => (
              <input
                key={index}
                ref={(node) => {
                  inputs.current[index] = node;
                }}
                aria-label={`Chữ số OTP ${index + 1}`}
                className="h-14 min-w-0 rounded-xl border border-slate-200 bg-white text-center text-xl font-bold text-[#111943] outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
                inputMode="numeric"
                autoComplete={index === 0 ? "one-time-code" : "off"}
                maxLength={1}
                value={digit}
                onChange={(event) => {
                  const value = event.target.value.replace(/\D/g, "");
                  if (value) apply(value.slice(-1), index);
                  else
                    setDigits((current) =>
                      current.map((item, i) => (i === index ? "" : item)),
                    );
                }}
                onKeyDown={(event) => keyDown(event, index)}
              />
            ))}
          </div>
          {error && (
            <p
              role="alert"
              className="mt-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700"
            >
              {error}
            </p>
          )}
          <button
            disabled={pending || digits.join("").length !== 6}
            className="sf-primary mt-6 w-full rounded-xl px-4 py-3 font-bold text-white disabled:opacity-60"
          >
            {pending ? "Đang xác minh…" : "Xác minh và tiếp tục"}
          </button>
        </form>
        <div className="mt-5 text-center text-sm text-slate-500">
          Chưa nhận được mã?{" "}
          <button
            disabled={pending || remaining > 0}
            onClick={() => void resend()}
            className="min-h-11 font-bold text-blue-600 disabled:text-slate-400"
          >
            {remaining > 0 ? `Gửi lại sau ${remaining}s` : "Gửi lại mã"}
          </button>
        </div>
        <p className="mt-3 text-center text-xs leading-5 text-slate-400">
          Kiểm tra cả thư mục Spam hoặc Quảng cáo nếu chưa thấy email.
        </p>
      </div>
    </main>
  );
}

function Field({
  label,
  name,
  type = "text",
  autoComplete,
  hint,
}: {
  label: string;
  name: string;
  type?: string;
  autoComplete: string;
  hint?: string;
}) {
  return (
    <label className="block text-sm font-semibold text-slate-700">
      {label}
      <input
        required
        name={name}
        type={type}
        autoComplete={autoComplete}
        className="mt-2 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-3 outline-none transition focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
      />
      {hint && (
        <span className="mt-2 block text-xs font-normal text-slate-500">
          {hint}
        </span>
      )}
    </label>
  );
}
function maskEmail(email: string) {
  const [name, domain] = email.split("@");
  if (!domain) return email;
  return `${name.slice(0, 2)}${"*".repeat(Math.max(2, Math.min(6, name.length - 2)))}@${domain}`;
}
function safeReturnPath(path?: string) {
  return path &&
    path.startsWith("/") &&
    !path.startsWith("//") &&
    !path.includes("\\")
    ? path
    : "/home";
}
