import { apiRequest } from "./api";

/** Same shape as the backend's UserDto. */
export interface User {
    id: string;
    email: string;
    fullName: string;
    role: "Admin" | "User";
    createdAt: string;
    lastLoginAt: string | null;
}

/** Same shape as the backend's AuthResponse (returned by login and register). */
export interface AuthResponse {
    token: string;
    expiresAt: string;
    user: User;
}

export function login(email: string, password: string): Promise<AuthResponse> {
    return apiRequest<AuthResponse>("/api/auth/login", { method: "POST", body: { email, password } });
}

export function register(fullName: string, email: string, password: string): Promise<AuthResponse> {
    return apiRequest<AuthResponse>("/api/auth/register", { method: "POST", body: { fullName, email, password } });
}

/** Asks the API to email a reset link. Succeeds whether or not the email has an account (so nobody can probe for accounts). */
export function forgotPassword(email: string): Promise<void> {
    return apiRequest<unknown>("/api/auth/forgot-password", { method: "POST", body: { email } }).then(() => undefined);
}

/** Sets a new password using the email + token from the reset link. The link works once and for one hour. */
export function resetPassword(email: string, token: string, newPassword: string): Promise<void> {
    return apiRequest<unknown>("/api/auth/reset-password", { method: "POST", body: { email, token, newPassword } }).then(() => undefined);
}

/** Same rule as the backend (PasswordRules): 8+ characters, a lowercase letter, an uppercase letter and a digit. */
export function isStrongPassword(password: string): boolean {
    return password.length >= 8 && /[a-z]/.test(password) && /[A-Z]/.test(password) && /\d/.test(password);
}

export function isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
}
