import { apiRequest } from "./api";
import type { User } from "./authService";
import type { AdminBooking } from "./bookingService";

// ---- Your own account (/api/auth) ------------------------------------------------

export const getMe = (token: string) => apiRequest<User>("/api/auth/me", { token });

export const updateProfile = (fullName: string, token: string) =>
    apiRequest<User>("/api/auth/me", { method: "PUT", body: { fullName }, token });

export const changePassword = (currentPassword: string, newPassword: string, token: string) =>
    apiRequest<{ message: string }>("/api/auth/change-password", { method: "POST", body: { currentPassword, newPassword }, token });

export const deleteAccount = (password: string, token: string) =>
    apiRequest<null>("/api/auth/delete-account", { method: "POST", body: { password }, token });

// ---- Admin: users (/api/admin/users) ----------------------------------------------

export interface AdminUser extends User {
    pendingBookings: number;
    approvedBookings: number;
}

export interface PagedResult<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
}

export const getUsers = (page: number, search: string, token: string, pageSize = 20) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search.trim()) params.set("search", search.trim());
    return apiRequest<PagedResult<AdminUser>>(`/api/admin/users?${params}`, { token });
};

export const getUserBookings = (id: string, token: string) =>
    apiRequest<AdminBooking[]>(`/api/admin/bookings?userId=${id}`, { token });

/** "12.11.2026" in the visitor's language. */
export const formatDate = (iso: string, locale?: string) =>
    new Intl.DateTimeFormat(locale, { day: "2-digit", month: "2-digit", year: "numeric" }).format(new Date(iso));
