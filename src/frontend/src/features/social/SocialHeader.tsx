import { useEffect } from "react";
import { NavLink } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { Bell, MessageCircle, CircleUserRound } from "lucide-react";
import { apiBaseUrl } from "@/api/httpClient";
import { useAuthStore } from "@/features/auth/store/authStore";
import { socialGet, type Page, type Notice } from "./socialApi";
import "./social.css";

export function SocialHeader() {
  const cache = useQueryClient();
  const userId = useAuthStore((s) => s.user?.id);
  const notices = useQuery({
    queryKey: ["social", "unread"],
    queryFn: () => socialGet<Page<Notice>>("/notifications"),
    refetchInterval: 30000,
    enabled: !!userId,
    retry: false,
  });
  useEffect(() => {
    if (!userId) return;
    let disposed = false;
    let retry: ReturnType<typeof setTimeout> | undefined;
    let refreshTimer: ReturnType<typeof setTimeout> | undefined;
    let starting = false;
    const hub = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/social-hub`, {
          accessTokenFactory: async () => {
            await socialGet("/notifications");
            return useAuthStore.getState().accessToken ?? "";
          },
        })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    const refresh = () => {
      if (disposed || refreshTimer) return;
      refreshTimer = setTimeout(() => {
        refreshTimer = undefined;
        if (!disposed) void cache.invalidateQueries({ queryKey: ["social"] });
      }, 500);
    };
    const start = async () => {
      if (disposed || starting) return;
      starting = true;
      try {
        if (hub.state === HubConnectionState.Disconnected) await hub.start();
        if (disposed) await hub.stop();
      } catch {
        if (!disposed)
          retry = setTimeout(() => {
            void start();
          }, 5000);
      } finally {
        starting = false;
      }
    };
    hub.on("SocialChanged", refresh);
    hub.onreconnected(refresh);
    hub.onclose(() => {
      if (!disposed) {
        clearTimeout(retry);
        retry = setTimeout(() => {
          void start();
        }, 5000);
      }
    });
    void start();
    return () => {
      disposed = true;
      clearTimeout(retry);
      clearTimeout(refreshTimer);
      void hub.stop();
    };
  }, [cache, userId]);
  return (
    <nav className="social-header-actions" aria-label="Tài khoản">
      <NavLink to="/messages" aria-label="Tin nhắn">
        <MessageCircle size={20} />
      </NavLink>
      <NavLink
        to="/notifications"
        aria-label={`Thông báo${notices.data?.unread ? `, ${notices.data.unread} chưa đọc` : ""}`}
      >
        <Bell size={20} />
        {!!notices.data?.unread && (
          <span>{notices.data.unread > 99 ? "99+" : notices.data.unread}</span>
        )}
      </NavLink>
      <NavLink to="/people/me" aria-label="Hồ sơ của tôi">
        <CircleUserRound size={22} />
      </NavLink>
    </nav>
  );
}
