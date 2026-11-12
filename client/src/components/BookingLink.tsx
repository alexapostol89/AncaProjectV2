import type { CSSProperties, ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/useAuth';

/**
 * Every "book a session" button. Booking needs an account:
 *  - logged in  → straight to the booking calendar (/booking)
 *  - logged out → login page, which sends the user on to /booking afterwards
 */
export default function BookingLink({ className, style, children }: { className?: string; style?: CSSProperties; children: ReactNode }) {
    const { isLoggedIn } = useAuth();
    return (
        <Link
            to={isLoggedIn ? '/booking' : '/autentificare'}
            state={isLoggedIn ? undefined : { from: '/booking' }}
            className={className}
            style={style}
        >
            {children}
        </Link>
    );
}
