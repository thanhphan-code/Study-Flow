import { apiRequest } from "@/api/httpClient";

export type UserRole = "User" | "Admin";
export type AdminOverview = {
  totalUsers: number;
  activeUsers: number;
  suspendedUsers: number;
  unverifiedUsers: number;
  adminUsers: number;
  newUsersToday: number;
  pendingReports: number;
  failedDocuments: number;
  studySessionsToday: number;
};
export type AdminUser = {
  id: string;
  email: string;
  displayName: string;
  username: string | null;
  role: UserRole;
  isEmailVerified: boolean;
  isSuspended: boolean;
  suspensionReason: string | null;
  lastActiveAt: string | null;
  createdAt: string;
};
export type AuditItem = {
  id: string;
  actorUserId: string;
  actorDisplayName: string;
  targetUserId: string;
  targetDisplayName: string;
  action: string;
  reason: string;
  createdAt: string;
};
export type UserDetails = {
  user: AdminUser;
  subjects: number;
  studySets: number;
  studySessions: number;
  documents: number;
  battleParticipations: number;
  recentAudit: AuditItem[];
};
export type Paged<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

const query = (values: Record<string, string | number | undefined>) => {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== "") params.set(key, String(value));
  });
  return params.toString();
};

export const adminApi = {
  overview: () => apiRequest<AdminOverview>("/admin/overview"),
  users: (filters: {
    search?: string;
    status: string;
    role: string;
    page: number;
  }) =>
    apiRequest<Paged<AdminUser>>(
      `/admin/users?${query({ ...filters, pageSize: 20 })}`,
    ),
  user: (id: string) => apiRequest<UserDetails>(`/admin/users/${id}`),
  audit: (page: number) =>
    apiRequest<Paged<AuditItem>>(`/admin/audit?page=${page}&pageSize=30`),
  status: (id: string, suspended: boolean, reason: string) =>
    apiRequest<AdminUser>(`/admin/users/${id}/status`, {
      method: "PUT",
      body: JSON.stringify({ suspended, reason }),
    }),
  role: (id: string, role: UserRole, reason: string) =>
    apiRequest<AdminUser>(`/admin/users/${id}/role`, {
      method: "PUT",
      body: JSON.stringify({ role, reason }),
    }),
  revoke: (id: string, reason: string) =>
    apiRequest<void>(`/admin/users/${id}/revoke-sessions`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    }),
};
