import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import * as authService from '../services/authService';
import type { AuthResponse } from '../services/authService';
import { AuthContext, type AuthContextValue } from './useAuth';

/**
 * Who is logged in, shared with the whole app.
 * The session ({ token, expiresAt, user }) is kept in localStorage so a page
 * reload doesn't log you out. When the token expires (8h), the user is logged out.
 */
const STORAGE_KEY = 'anca-auth';

/** Reads the saved session, ignoring it if it's missing, broken or expired. */
function loadSession(): AuthResponse | null {
    try {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (!saved) return null;
        const session = JSON.parse(saved) as AuthResponse;
        return new Date(session.expiresAt).getTime() > Date.now() ? session : null;
    } catch {
        return null;
    }
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [session, setSession] = useState<AuthResponse | null>(loadSession);

    // Keep localStorage in sync with the current session.
    useEffect(() => {
        try {
            if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
            else localStorage.removeItem(STORAGE_KEY);
        } catch {
            // storage unavailable (private mode) — the session just won't survive a reload
        }
    }, [session]);

    // Log out automatically the moment the token expires.
    useEffect(() => {
        if (!session) return;
        const msLeft = new Date(session.expiresAt).getTime() - Date.now();
        const timer = setTimeout(() => setSession(null), msLeft);
        return () => clearTimeout(timer);
    }, [session]);

    const login = useCallback(async (email: string, password: string) => {
        setSession(await authService.login(email, password));
    }, []);

    const register = useCallback(async (fullName: string, email: string, password: string) => {
        setSession(await authService.register(fullName, email, password));
    }, []);

    const logout = useCallback(() => setSession(null), []);

    const value = useMemo<AuthContextValue>(() => ({
        user: session?.user ?? null,
        token: session?.token ?? null,
        isLoggedIn: session !== null,
        login,
        register,
        logout,
    }), [session, login, register, logout]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
