import { createContext, useContext } from 'react';
import type { User } from '../services/authService';

export type AuthContextValue = {
    user: User | null;
    token: string | null;
    isLoggedIn: boolean;
    login: (email: string, password: string) => Promise<void>;
    register: (fullName: string, email: string, password: string) => Promise<void>;
    logout: () => void;
    /** Replaces the stored user after a profile change (the token stays the same). */
    updateUser: (user: User) => void;
};

/** Filled in by <AuthProvider> (AuthContext.tsx). */
export const AuthContext = createContext<AuthContextValue | null>(null);

/** Who is logged in, plus login/register/logout. Use inside any component. */
export function useAuth() {
    const context = useContext(AuthContext);
    if (!context) throw new Error('useAuth must be used inside AuthProvider');
    return context;
}
