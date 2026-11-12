import { useCallback, useEffect, useState } from 'react';
import { AlertCircle, Check, Mail, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/useAuth';
import {
    approveBooking, formatSessionTime, getAdminBookings, notifyBookingsChanged, rejectBooking,
    type AdminBooking, type BookingStatus,
} from '../services/bookingService';
import { bookingErrorKey } from './bookingErrors';
import '../Css/Contact.css';

const FILTERS: (BookingStatus | null)[] = ['Pending', 'Approved', 'Rejected', null];

/**
 * Admin-only: session requests waiting for a decision.
 * Approve → Google Calendar event + invitation and confirmation email to the client.
 * Reject  → slot is freed and the client gets an email (with the optional reason).
 */
export default function AdminBookings() {
    const { t, i18n } = useTranslation();
    const { token } = useAuth();
    const [filter, setFilter] = useState<BookingStatus | null>('Pending');
    const [bookings, setBookings] = useState<AdminBooking[]>([]);
    const [loading, setLoading] = useState(true);
    const [workingId, setWorkingId] = useState<string | null>(null);
    const [rejecting, setRejecting] = useState<{ id: string; reason: string } | null>(null);
    const [error, setError] = useState('');

    const load = useCallback(async () => {
        if (!token) return;
        setLoading(true);
        try {
            setBookings(await getAdminBookings(filter, token));
            notifyBookingsChanged(); // keep the navbar bell in sync with what's on screen
        } catch (e) {
            setError(t(`bookingErrors.${bookingErrorKey(e)}`));
        } finally {
            setLoading(false);
        }
    }, [filter, token, t]);

    useEffect(() => { void load(); }, [load]);

    async function decide(id: string, action: () => Promise<AdminBooking>) {
        setWorkingId(id); setError('');
        try {
            await action();
            notifyBookingsChanged();
            setRejecting(null);
            await load();
        } catch (e) {
            setError(t(`bookingErrors.${bookingErrorKey(e)}`));
        } finally {
            setWorkingId(null);
        }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('adminBookings.subtitle')}</span><h1>{t('adminBookings.title')}</h1></div>
        <div className="contact-form-card admin-bookings">
            <div className="booking-filters" role="group" aria-label={t('adminBookings.filterLabel')}>
                {FILTERS.map((f) => <button key={f ?? 'all'} type="button" aria-pressed={filter === f}
                    className={filter === f ? 'btn-primary' : 'btn-nav-outline'} onClick={() => setFilter(f)}>
                    {t(`adminBookings.filters.${f ?? 'All'}`)}
                </button>)}
            </div>

            {error && <div className="form-status form-status-error" role="alert"><AlertCircle size={18} /><p>{error}</p></div>}
            {loading && <p role="status">{t('adminBookings.loading')}</p>}
            {!loading && bookings.length === 0 && <p>{t(filter === 'Pending' ? 'adminBookings.emptyPending' : 'adminBookings.empty')}</p>}

            {!loading && bookings.length > 0 && <ul className="booking-list">
                {bookings.map((b) => {
                    const past = new Date(b.start).getTime() <= Date.now();
                    const working = workingId === b.id;
                    return <li key={b.id} className="booking-row booking-row-admin">
                        <div className="booking-row-main">
                            <strong>{formatSessionTime(b.start, i18n.language)}</strong>
                            <span>{b.clientName} · <a href={`mailto:${b.clientEmail}`}><Mail size={14} /> {b.clientEmail}</a></span>
                            <small>{t('adminBookings.requestedAt', { date: formatSessionTime(b.createdAt, i18n.language) })}</small>
                            {b.status === 'Pending' && past && <small className="booking-reason">{t('adminBookings.expired')}</small>}
                            {b.rejectionReason && <p className="booking-reason">{b.rejectionReason}</p>}
                        </div>

                        {b.status !== 'Pending'
                            ? <span className={`booking-badge booking-badge-${b.status.toLowerCase()}`}>{t(`booking.status.${b.status}`)}</span>
                            : rejecting?.id === b.id
                                ? <div className="booking-reject">
                                    <label htmlFor={`reason-${b.id}`}>{t('adminBookings.reasonLabel')}</label>
                                    <textarea id={`reason-${b.id}`} rows={2} maxLength={500} value={rejecting.reason}
                                        onChange={(e) => setRejecting({ id: b.id, reason: e.target.value })} />
                                    <div className="booking-actions">
                                        <button type="button" className="btn-primary" disabled={working}
                                            onClick={() => decide(b.id, () => rejectBooking(b.id, rejecting.reason.trim(), token!))}>
                                            {t('adminBookings.confirmReject')}
                                        </button>
                                        <button type="button" className="btn-nav-outline" disabled={working} onClick={() => setRejecting(null)}>
                                            {t('adminBookings.cancel')}
                                        </button>
                                    </div>
                                </div>
                                : <div className="booking-actions">
                                    {!past && <button type="button" className="btn-primary" disabled={working}
                                        onClick={() => decide(b.id, () => approveBooking(b.id, token!))}>
                                        <Check size={16} /> {working ? t('adminBookings.working') : t('adminBookings.approve')}
                                    </button>}
                                    <button type="button" className="btn-nav-outline" disabled={working}
                                        onClick={() => setRejecting({ id: b.id, reason: '' })}>
                                        <X size={16} /> {t('adminBookings.reject')}
                                    </button>
                                </div>}
                    </li>;
                })}
            </ul>}
        </div>
    </div>;
}
