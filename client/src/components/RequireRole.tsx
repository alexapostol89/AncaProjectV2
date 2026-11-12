import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAudience, type Audience } from './useAudience';

/** Where each audience lands when it opens a page that isn't meant for it. */
const HOME: Record<Audience, string> = { Guest: '/', User: '/', Admin: '/admin/programari' };

/**
 * Route guard. Renders the page only for the listed audiences:
 *  - a guest on a members-only page → login (and back here afterwards)
 *  - anyone else who isn't allowed → their own start page
 * This only hides screens; the API enforces the same rules with [Authorize(Roles = ...)].
 */
export default function RequireRole({ allow, children }: { allow: Audience[]; children: ReactNode }) {
    const audience = useAudience();
    const location = useLocation();

    if (allow.includes(audience)) return <>{children}</>;
    if (audience === 'Guest') return <Navigate to="/autentificare" state={{ from: location.pathname }} replace />;
    return <Navigate to={HOME[audience]} replace />;
}
