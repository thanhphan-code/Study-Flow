import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useAuthStore } from "@/features/auth/store/authStore";
import { apiRequest } from "@/api/httpClient";
import { battleApi } from "./battleApi";

export function useBattleRoom(id: string) {
  const cache = useQueryClient();
  const [connection, setConnection] = useState("Đang kết nối");
  const room = useQuery({
    queryKey: ["battle", id],
    queryFn: () => battleApi.get(id),
    refetchInterval: (query) =>
      ["Completed", "Cancelled", "Expired"].includes(
        query.state.data?.status ?? "",
      )
        ? false
        : 3000,
    retry: false,
  });
  const member = !room.isError && (room.data?.isMember ?? false);
  useEffect(() => {
    if (!member) return;
    let disposed = false;
    let retry: ReturnType<typeof setTimeout> | undefined;
    let refreshTimer: ReturnType<typeof setTimeout> | undefined;
    let starting = false;
    const base = (import.meta.env.VITE_API_BASE_URL ?? "/api").replace(
      /\/$/,
      "",
    );
    const hub = new HubConnectionBuilder()
      .withUrl(`${base}/battle-hub`, {
        accessTokenFactory: async () => {
          // Use the shared HTTP refresh flow before negotiating a fresh realtime connection.
          await apiRequest(`/battles/${id}`);
          return useAuthStore.getState().accessToken ?? "";
        },
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    const refresh = () => {
      // Coalesce bursts from up to 50 players so legitimate clients do not hit the API rate limit.
      if (refreshTimer !== undefined || disposed) return;
      refreshTimer = setTimeout(() => {
        refreshTimer = undefined;
        if (!disposed)
          void cache.invalidateQueries({ queryKey: ["battle", id] });
      }, 750);
    };
    hub.on("RoomChanged", refresh);
    hub.onreconnecting(() => {
      if (!disposed) setConnection("Đang kết nối lại…");
    });
    const watch = async () => {
      await hub.invoke("Watch", id);
      if (!disposed) {
        setConnection("Trực tiếp");
        refresh();
      }
    };
    hub.onreconnected(() => {
      void watch().catch(() => {
        if (!disposed) setConnection("Đang đồng bộ dự phòng");
      });
    });
    const start = async () => {
      if (disposed || starting) return;
      starting = true;
      try {
        if (hub.state === HubConnectionState.Disconnected) await hub.start();
        if (disposed) {
          await hub.stop();
          return;
        }
        if (hub.state === HubConnectionState.Connected) await watch();
      } catch {
        if (!disposed) {
          setConnection("Đang đồng bộ dự phòng");
          clearTimeout(retry);
          retry = setTimeout(() => void start(), 5000);
        }
      } finally {
        starting = false;
      }
    };
    hub.onclose(() => {
      if (!disposed) {
        setConnection("Đang kết nối lại…");
        clearTimeout(retry);
        retry = setTimeout(() => void start(), 5000);
      }
    });
    void start();
    return () => {
      disposed = true;
      clearTimeout(retry);
      clearTimeout(refreshTimer);
      void hub.stop();
    };
  }, [id, member, cache]);
  return { ...room, connection };
}
