import { apiRequest } from "./api";

export type BookingStatus = "Pending" | "Approved" | "Rejected";

export interface Slot {
    start: string;
    end: string;
}

export interface MyBooking {
    id: string;
    start: string;
    end: string;
    status: BookingStatus;
    rejectionReason: string | null;
    createdAt: string;
}

export interface AdminBooking {
    id: string;
    start: string;
    end: string;
    status: BookingStatus;
    clientName: string;
    clientEmail: string;
    createdAt: string;
    decidedAt: string | null;
    rejectionReason: string | null;
}

export const getAvailability = (date: string, token: string) =>
    apiRequest<{ slots: Slot[] }>(`/api/bookings/availability?date=${encodeURIComponent(date)}`, { token }).then((r) => r.slots ?? []);

/** Sends a request; it stays Pending until the admin approves it. */
export const requestBooking = (start: string, token: string) =>
    apiRequest<MyBooking>("/api/bookings", { method: "POST", body: { start }, token });

export const getMyBookings = (token: string) => apiRequest<MyBooking[]>("/api/bookings/mine", { token });

export const getAdminBookings = (status: BookingStatus | null, token: string) =>
    apiRequest<AdminBooking[]>(`/api/admin/bookings${status ? `?status=${status}` : ""}`, { token });

export const getPendingCount = (token: string) =>
    apiRequest<{ count: number }>("/api/admin/bookings/pending-count", { token }).then((r) => r.count);

/** Fired after the admin approves/rejects, so the navbar bell refreshes immediately. */
export const BOOKINGS_CHANGED_EVENT = "bookings-changed";
export const notifyBookingsChanged = () => window.dispatchEvent(new Event(BOOKINGS_CHANGED_EVENT));

export const approveBooking = (id: string, token: string) =>
    apiRequest<AdminBooking>(`/api/admin/bookings/${id}/approve`, { method: "POST", token });

export const rejectBooking = (id: string, reason: string, token: string) =>
    apiRequest<AdminBooking>(`/api/admin/bookings/${id}/reject`, { method: "POST", body: { reason: reason || null }, token });

/** "Mon, 17.11.2026, 14:10" in Romania time, in the visitor's language. */
export const formatSessionTime = (iso: string, locale?: string) =>
    new Intl.DateTimeFormat(locale, {
        weekday: "short", day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit", hourCycle: "h23", timeZone: "Europe/Bucharest",
    }).format(new Date(iso));
