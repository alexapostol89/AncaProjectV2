import { useEffect, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { CalendarDays, Clock3, AlertCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/useAuth';
import { API_URL } from '../services/api';
import '../Css/Contact.css';

type Slot = { start: string; end: string };

/** Authenticated calendar booking. The server is the source of availability and creates Google Calendar events. */
export default function Booking() {
    const { t } = useTranslation('translation', { keyPrefix: 'booking' });
    const { isLoggedIn, token, user } = useAuth();
    const [date, setDate] = useState(new Date().toLocaleDateString('en-CA', { timeZone: 'Europe/Bucharest' }));
    const [slots, setSlots] = useState<Slot[]>([]);
    const [selected, setSelected] = useState('');
    const [busy, setBusy] = useState(false);
    const [message, setMessage] = useState('');

    useEffect(() => {
        if (!isLoggedIn || !token || !date) return;
        let active = true;
        setBusy(true); setMessage(''); setSelected('');
        fetch(`${API_URL}/api/bookings/availability?date=${encodeURIComponent(date)}`, { headers: { Authorization: `Bearer ${token}` } })
            .then(async (response) => {
                const body = await response.json();
                if (!response.ok) throw new Error(body.message ?? 'Could not load available times.');
                if (active) setSlots(body.slots ?? []);
            })
            .catch((error: unknown) => { if (active) setMessage(error instanceof Error ? error.message : 'Could not load available times.'); })
            .finally(() => { if (active) setBusy(false); });
        return () => { active = false; };
    }, [date, isLoggedIn, token]);

    if (!isLoggedIn) return <Navigate to="/autentificare" state={{ from: '/booking' }} replace />;

    async function book() {
        if (!selected || !token) return;
        setBusy(true); setMessage('');
        try {
            const response = await fetch(`${API_URL}/api/bookings`, {
                method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
                body: JSON.stringify({ start: selected }),
            });
            const body = await response.json();
            if (!response.ok) throw new Error(body.message ?? 'This time is no longer available. Please choose another.');
            setMessage(t('confirmed', { email: user?.email }));
            setSelected('');
            const refreshed = await fetch(`${API_URL}/api/bookings/availability?date=${encodeURIComponent(date)}`, { headers: { Authorization: `Bearer ${token}` } });
            if (refreshed.ok) setSlots((await refreshed.json()).slots ?? []);
        } catch (error) { setMessage(error instanceof Error ? error.message : 'Booking could not be completed.'); }
        finally { setBusy(false); }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('subtitle')}</span><h1>{t('title')}</h1></div>
        <div className="contact-form-card">
            <p>{t('intro')}</p>
            <div className="form-group"><label htmlFor="booking-date"><CalendarDays size={16} /> {t('date')}</label><input id="booking-date" type="date" min={new Date().toLocaleDateString('en-CA', { timeZone: 'Europe/Bucharest' })} value={date} onChange={(event) => setDate(event.target.value)} /></div>
            {busy && <p role="status">{t('loading')}</p>}
            {!busy && slots.length > 0 && <div className="booking-slots" aria-label={t('availableTimes')}>{slots.map((slot) => <button key={slot.start} type="button" className={selected === slot.start ? 'btn-primary' : 'btn-nav-outline'} onClick={() => setSelected(slot.start)}><Clock3 size={16} />{new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Bucharest' }).format(new Date(slot.start))}</button>)}</div>}
            {!busy && !message && !slots.length && <p>{t('none')}</p>}
            {message && <div className="form-status" role="status"><AlertCircle size={18} /><p>{message}</p></div>}
            <button type="button" className="btn-primary" disabled={!selected || busy} onClick={book}>{t('confirm')}</button>
        </div>
    </div>;
}
