import type { PropsWithChildren } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuthStore } from "@/features/auth/store/authStore";

export function AdminRoute({ children }: PropsWithChildren) {
  const user = useAuthStore((state) => state.user);
  const location = useLocation();
  if (!user)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (user.role !== "Admin") return <Navigate to="/home" replace />;
  return children;
}
