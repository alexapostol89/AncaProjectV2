import { useCallback, useEffect, useState } from 'react';
import { CalendarDays, Clock3, AlertCircle, CheckCircle2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/useAuth';
import { formatSessionTime, getAvailability, getMyBookings, requestBooking, type MyBooking, type Slot } from '../services/bookingService';
import { bookingErrorKey } from './bookingErrors';
import '../Css/Contact.css';

const today = () => new Date().toLocaleDateString('en-CA', { timeZone: 'Europe/Bucharest' });

/**
 * Authenticated booking. Picking a slot sends a *request*: it stays pending until the
 * practice approves it, and only then is the Google Calendar invitation sent.
 */
export default function Booking() {
    const { t, i18n } = useTranslation();
    const { token, user } = useAuth();
    const [date, setDate] = useState(today);
    const [slots, setSlots] = useState<Slot[]>([]);
    const [selected, setSelected] = useState('');
    const [busy, setBusy] = useState(false);
    const [status, setStatus] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
    const [mine, setMine] = useState<MyBooking[]>([]);

    const loadSlots = useCallback(async (signal?: { active: boolean }) => {
        if (!token || !date) return;
        setBusy(true); setSelected('');
        try {
            const result = await getAvailability(date, token);
            if (!signal || signal.active) setSlots(result);
        } catch (error) {
            if (!signal || signal.active) { setSlots([]); setStatus({ type: 'error', text: t(`bookingErrors.${bookingErrorKey(error)}`) }); }
        } finally {
            if (!signal || signal.active) setBusy(false);
        }
    }, [date, token, t]);

    const loadMine = useCallback(async () => {
        if (!token) return;
        try { setMine(await getMyBookings(token)); } catch { /* the list is secondary; booking still works */ }
    }, [token]);

    useEffect(() => {
        const signal = { active: true };
        setStatus(null);
        void loadSlots(signal);
        return () => { signal.active = false; };
    }, [loadSlots]);

    useEffect(() => { void loadMine(); }, [loadMine]);

    async function book() {
        if (!selected || !token) return;
        setBusy(true); setStatus(null);
        try {
            await requestBooking(selected, token);
            setStatus({ type: 'success', text: t('booking.requested', { email: user?.email }) });
            await Promise.all([loadSlots(), loadMine()]);
        } catch (error) {
            setStatus({ type: 'error', text: t(`bookingErrors.${bookingErrorKey(error)}`) });
            setBusy(false);
        }
    }

    const upcoming = mine.filter((b) => new Date(b.start).getTime() > Date.now() || b.status === 'Pending');

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('booking.subtitle')}</span><h1>{t('booking.title')}</h1></div>
        <div className="contact-form-card">
            <p>{t('booking.intro')}</p>
            <p className="booking-note">{t('booking.approvalNote')}</p>
            <div className="form-group"><label htmlFor="booking-date"><CalendarDays size={16} /> {t('booking.date')}</label><input id="booking-date" type="date" min={today()} value={date} onChange={(event) => setDate(event.target.value)} /></div>
            {busy && <p role="status">{t('booking.loading')}</p>}
            {!busy && slots.length > 0 && <div className="booking-slots" aria-label={t('booking.availableTimes')}>{slots.map((slot) => <button key={slot.start} type="button" className={selected === slot.start ? 'btn-primary' : 'btn-nav-outline'} aria-pressed={selected === slot.start} onClick={() => setSelected(slot.start)}><Clock3 size={16} />{new Intl.DateTimeFormat(i18n.language, { hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Bucharest' }).format(new Date(slot.start))}</button>)}</div>}
            {!busy && !status && !slots.length && <p>{t('booking.none')}</p>}
            {status && <div className={`form-status form-status-${status.type}`} role="status">{status.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}<p>{status.text}</p></div>}
            <button type="button" className="btn-primary" disabled={!selected || busy} onClick={book}>{t('booking.confirm')}</button>
        </div>

        {upcoming.length > 0 && <div className="contact-form-card booking-mine">
            <h2>{t('booking.myRequests')}</h2>
            <ul className="booking-list">
                {upcoming.map((b) => <li key={b.id} className="booking-row">
                    <div>
                        <strong>{formatSessionTime(b.start, i18n.language)}</strong>
                        {b.status === 'Rejected' && b.rejectionReason && <p className="booking-reason">{b.rejectionReason}</p>}
                    </div>
                    <span className={`booking-badge booking-badge-${b.status.toLowerCase()}`}>{t(`booking.status.${b.status}`)}</span>
                </li>)}
            </ul>
        </div>}
    </div>;
}
