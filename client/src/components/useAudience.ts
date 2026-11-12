import { useAuth } from '../context/useAuth';

/** Who is looking at the page: not logged in, a client, or the admin. */
export type Audience = 'Guest' | 'User' | 'Admin';

/** The current visitor's audience. */
export function useAudience(): Audience {
    const { user } = useAuth();
    return user ? user.role : 'Guest';
}
