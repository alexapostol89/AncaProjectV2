import { useEffect, useState } from 'react';
import { AlertCircle, ChevronDown, ChevronUp, Mail, Search, UserRound } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/useAuth';
import {
    formatDate, getUserBookings, getUsers,
    type AdminUser, type PagedResult,
} from '../services/accountService';
import { formatSessionTime, type AdminBooking } from '../services/bookingService';
import { accountErrorKey } from './accountErrors';
import '../Css/Contact.css';
import '../Css/Account.css';

const PAGE_SIZE = 20;

/**
 * Admin-only, read-only: all accounts, newest first. Search by name/email and see each client's sessions.
 * Roles and deletions are handled by the developers in the database, not from the website.
 */
export default function AdminUsers() {
    const { t, i18n } = useTranslation();
    const { user: me, token } = useAuth();
    const [search, setSearch] = useState('');
    const [query, setQuery] = useState('');
    const [page, setPage] = useState(1);
    const [data, setData] = useState<PagedResult<AdminUser> | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [expanded, setExpanded] = useState<string | null>(null);
    const [sessions, setSessions] = useState<Record<string, AdminBooking[] | 'loading' | 'error'>>({});

    // Search after the admin stops typing for a moment, starting again from page 1.
    useEffect(() => {
        const timer = setTimeout(() => { setQuery(search); setPage(1); }, 300);
        return () => clearTimeout(timer);
    }, [search]);

    useEffect(() => {
        if (!token) return;
        let active = true;
        getUsers(page, query, token, PAGE_SIZE)
            .then((result) => { if (active) { setData(result); setError(''); } })
            .catch((e: unknown) => { if (active) setError(t(`accountErrors.${accountErrorKey(e)}`)); })
            .finally(() => { if (active) setLoading(false); });
        return () => { active = false; };
    }, [page, query, token, t]);

    const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;

    function toggleSessions(id: string) {
        if (expanded === id) { setExpanded(null); return; }
        setExpanded(id);
        if (sessions[id] && sessions[id] !== 'error') return;
        setSessions((s) => ({ ...s, [id]: 'loading' }));
        getUserBookings(id, token!)
            .then((list) => setSessions((s) => ({ ...s, [id]: list })))
            .catch(() => setSessions((s) => ({ ...s, [id]: 'error' })));
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('adminUsers.subtitle')}</span><h1>{t('adminUsers.title')}</h1></div>
        <div className="contact-form-card admin-users">
            <div className="admin-users-toolbar">
                <label className="admin-search">
                    <Search size={16} aria-hidden="true" />
                    <span className="visually-hidden">{t('adminUsers.searchLabel')}</span>
                    <input type="search" value={search} placeholder={t('adminUsers.searchPlaceholder')} onChange={(e) => setSearch(e.target.value)} />
                </label>
                {data && <span className="admin-users-count">{t('adminUsers.total', { count: data.totalCount })}</span>}
            </div>

            {error && <div className="form-status form-status-error" role="alert"><AlertCircle size={18} /><p>{error}</p></div>}
            {loading && !data && <p role="status">{t('adminUsers.loading')}</p>}
            {data && data.items.length === 0 && <p>{t(query ? 'adminUsers.noMatches' : 'adminUsers.empty')}</p>}

            {data && data.items.length > 0 && <ul className="booking-list" aria-busy={loading}>
                {data.items.map((u) => {
                    const isMe = u.id === me?.id;
                    const userSessions = sessions[u.id];
                    return <li key={u.id} className="booking-row user-row">
                        <div className="user-row-head">
                            <div className="account-avatar account-avatar-small" aria-hidden="true">
                                {u.fullName.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]!.toUpperCase()).join('') || <UserRound size={16} />}
                            </div>
                            <div className="booking-row-main">
                                <strong>{u.fullName}{isMe && <span className="user-you"> · {t('adminUsers.you')}</span>}</strong>
                                <a href={`mailto:${u.email}`}><Mail size={14} /> {u.email}</a>
                                <small>
                                    {t('adminUsers.joined', { date: formatDate(u.createdAt, i18n.language) })}
                                    {' · '}
                                    {u.lastLoginAt ? t('adminUsers.lastLogin', { date: formatDate(u.lastLoginAt, i18n.language) }) : t('adminUsers.neverLoggedIn')}
                                </small>
                            </div>
                            <span className={`role-badge role-badge-${u.role.toLowerCase()}`}>{t(`roles.${u.role}`)}</span>
                        </div>

                        {u.role === 'User' && <button type="button" className="user-sessions-toggle" aria-expanded={expanded === u.id} onClick={() => toggleSessions(u.id)}>
                            {t('adminUsers.sessions', { approved: u.approvedBookings, pending: u.pendingBookings })}
                            {expanded === u.id ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                        </button>}

                        {expanded === u.id && <div className="user-sessions">
                            {userSessions === 'loading' && <p role="status">{t('adminUsers.loadingSessions')}</p>}
                            {userSessions === 'error' && <p>{t('accountErrors.generic')}</p>}
                            {Array.isArray(userSessions) && userSessions.length === 0 && <p>{t('adminUsers.noSessions')}</p>}
                            {Array.isArray(userSessions) && userSessions.length > 0 && <ul>
                                {userSessions.map((b) => <li key={b.id}>
                                    <span>{formatSessionTime(b.start, i18n.language)}</span>
                                    <span className={`booking-badge booking-badge-${b.status.toLowerCase()}`}>{t(`booking.status.${b.status}`)}</span>
                                </li>)}
                            </ul>}
                        </div>}
                    </li>;
                })}
            </ul>}

            {data && totalPages > 1 && <nav className="admin-pagination" aria-label={t('adminUsers.pagination')}>
                <button type="button" className="btn-nav-outline" disabled={page <= 1 || loading} onClick={() => { setPage(page - 1); setLoading(true); }}>{t('adminUsers.previous')}</button>
                <span>{t('adminUsers.pageOf', { page, total: totalPages })}</span>
                <button type="button" className="btn-nav-outline" disabled={page >= totalPages || loading} onClick={() => { setPage(page + 1); setLoading(true); }}>{t('adminUsers.next')}</button>
            </nav>}
        </div>
    </div>;
}
