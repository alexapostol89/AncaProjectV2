import { useEffect, useState } from 'react';
import { useAuth } from '../context/useAuth';
import { BOOKINGS_CHANGED_EVENT, getPendingCount } from '../services/bookingService';

const POLL_MS = 60_000;

/**
 * Number of booking requests waiting for the admin (0 for everyone else).
 * Refreshes every minute while the tab is visible, when the tab regains focus,
 * and right after the admin page loads or an approve/reject happens.
 */
export function usePendingBookings(): number {
    const { user, token } = useAuth();
    const isAdmin = user?.role === 'Admin';
    const [count, setCount] = useState(0);

    useEffect(() => {
        if (!isAdmin || !token) return;
        let active = true;
        const refresh = () => {
            getPendingCount(token)
                .then((n) => { if (active) setCount(n); })
                .catch(() => { /* keep the last known number; the bell is a hint, not critical */ });
        };
        const refreshIfVisible = () => { if (document.visibilityState === 'visible') refresh(); };

        refresh();
        const timer = setInterval(refreshIfVisible, POLL_MS);
        document.addEventListener('visibilitychange', refreshIfVisible);
        window.addEventListener(BOOKINGS_CHANGED_EVENT, refresh);
        return () => {
            active = false;
            clearInterval(timer);
            document.removeEventListener('visibilitychange', refreshIfVisible);
            window.removeEventListener(BOOKINGS_CHANGED_EVENT, refresh);
        };
    }, [isAdmin, token]);

    return isAdmin ? count : 0;
}
