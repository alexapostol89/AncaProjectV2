import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertCircle, ChevronLeft, ChevronRight, Mail, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/useAuth';
import { BOOKINGS_CHANGED_EVENT, getAdminBookings, type AdminBooking } from '../services/bookingService';
import { bookingErrorKey } from './bookingErrors';
import '../Css/Contact.css';
import '../Css/Calendar.css';

/**
 * Admin-only calendar: an overview of sessions by day, week, month or year.
 * Shows confirmed and pending sessions (declined ones are hidden).
 * All times are shown in the practice's time zone (Romania), like the rest of the admin pages.
 */

const ZONE = 'Europe/Bucharest';
const VIEWS = ['day', 'week', 'month', 'year'] as const;
type View = (typeof VIEWS)[number];

/** A calendar date with no time zone attached (month is 1–12). */
interface Day { y: number; m: number; d: number }

interface CalEvent {
    booking: AdminBooking;
    day: string;      // "2026-11-17" in Romania time
    startMin: number; // minutes since midnight, Romania time
    endMin: number;
}

// ---- date helpers (UTC-based arithmetic, so the browser's own zone never interferes) ----

const toUtc = ({ y, m, d }: Day) => new Date(Date.UTC(y, m - 1, d));
const fromUtc = (date: Date): Day => ({ y: date.getUTCFullYear(), m: date.getUTCMonth() + 1, d: date.getUTCDate() });
const addDays = (day: Day, n: number) => fromUtc(new Date(toUtc(day).getTime() + n * 86_400_000));
const addMonths = (day: Day, n: number): Day => {
    const first = new Date(Date.UTC(day.y, day.m - 1 + n, 1));
    const lastDay = new Date(Date.UTC(first.getUTCFullYear(), first.getUTCMonth() + 1, 0)).getUTCDate();
    return { y: first.getUTCFullYear(), m: first.getUTCMonth() + 1, d: Math.min(day.d, lastDay) };
};
const keyOf = ({ y, m, d }: Day) => `${y}-${String(m).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
/** Monday of the week containing `day`. */
const startOfWeek = (day: Day) => addDays(day, -((toUtc(day).getUTCDay() + 6) % 7));
const daysInMonth = (y: number, m: number) => new Date(Date.UTC(y, m, 0)).getUTCDate();

const zoneParts = new Intl.DateTimeFormat('en-GB', {
    timeZone: ZONE, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
});
/** The Romania-time date and minute-of-day of an instant. */
function inZone(iso: string | Date) {
    const parts = Object.fromEntries(zoneParts.formatToParts(new Date(iso)).map((p) => [p.type, p.value]));
    const day: Day = { y: +parts.year, m: +parts.month, d: +parts.day };
    return { day, minutes: +parts.hour * 60 + +parts.minute };
}

const pad = (n: number) => String(n).padStart(2, '0');
const clock = (min: number) => `${pad(Math.floor(min / 60))}:${pad(min % 60)}`;

/** Six full weeks (Mon–Sun) covering the month, as shown in month/year views. */
function monthGrid(y: number, m: number): Day[] {
    const first = startOfWeek({ y, m, d: 1 });
    return Array.from({ length: 42 }, (_, i) => addDays(first, i));
}

export default function AdminCalendar() {
    const { t, i18n } = useTranslation();
    const locale = i18n.language;
    const { token } = useAuth();
    const today = useMemo(() => inZone(new Date()).day, []);
    const [view, setView] = useState<View>('week');
    const [cursor, setCursor] = useState<Day>(today);
    const [bookings, setBookings] = useState<AdminBooking[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [selected, setSelected] = useState<AdminBooking | null>(null);

    const load = useCallback(async () => {
        if (!token) return;
        try {
            setBookings(await getAdminBookings(null, token));
            setError('');
        } catch (e) {
            setError(t(`bookingErrors.${bookingErrorKey(e)}`));
        } finally {
            setLoading(false);
        }
    }, [token, t]);

    useEffect(() => {
        void load();
        window.addEventListener(BOOKINGS_CHANGED_EVENT, load);
        return () => window.removeEventListener(BOOKINGS_CHANGED_EVENT, load);
    }, [load]);

    // Escape closes the session details.
    useEffect(() => {
        if (!selected) return;
        const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') setSelected(null); };
        window.addEventListener('keydown', onKey);
        return () => window.removeEventListener('keydown', onKey);
    }, [selected]);

    /** Visible sessions grouped by Romania-time day, sorted by start. */
    const byDay = useMemo(() => {
        const map = new Map<string, CalEvent[]>();
        for (const b of bookings) {
            if (b.status === 'Rejected') continue;
            const start = inZone(b.start);
            const end = inZone(b.end);
            const ev: CalEvent = {
                booking: b,
                day: keyOf(start.day),
                startMin: start.minutes,
                endMin: keyOf(end.day) === keyOf(start.day) ? Math.max(end.minutes, start.minutes + 15) : 24 * 60,
            };
            map.set(ev.day, [...(map.get(ev.day) ?? []), ev]);
        }
        map.forEach((list) => list.sort((a, b) => a.startMin - b.startMin));
        return map;
    }, [bookings]);

    const eventsOn = (day: Day) => byDay.get(keyOf(day)) ?? [];
    const isToday = (day: Day) => keyOf(day) === keyOf(today);

    // ---- formatting in the visitor's language ----
    const fmt = (day: Day, options: Intl.DateTimeFormatOptions) =>
        new Intl.DateTimeFormat(locale, { ...options, timeZone: 'UTC' }).format(toUtc(day));
    const weekdayNames = useMemo(() => {
        const monday = { y: 2024, m: 1, d: 1 }; // a Monday
        return Array.from({ length: 7 }, (_, i) =>
            new Intl.DateTimeFormat(locale, { weekday: 'short', timeZone: 'UTC' }).format(toUtc(addDays(monday, i))));
    }, [locale]);

    const title = (() => {
        switch (view) {
            case 'day': return fmt(cursor, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
            case 'week': {
                const start = startOfWeek(cursor);
                const end = addDays(start, 6);
                return `${fmt(start, { day: 'numeric', month: 'short' })} – ${fmt(end, { day: 'numeric', month: 'short', year: 'numeric' })}`;
            }
            case 'month': return fmt(cursor, { month: 'long', year: 'numeric' });
            case 'year': return String(cursor.y);
        }
    })();

    function move(step: number) {
        setCursor((c) => {
            switch (view) {
                case 'day': return addDays(c, step);
                case 'week': return addDays(c, step * 7);
                case 'month': return addMonths(c, step);
                case 'year': return addMonths(c, step * 12);
            }
        });
    }

    function openDay(day: Day) {
        setCursor(day);
        setView('day');
    }

    // ---- views ----

    function renderTimeGrid(days: Day[]) {
        const events = days.flatMap(eventsOn);
        // Default working hours 08–20, widened to fit any session outside them.
        const firstHour = Math.min(8, ...events.map((e) => Math.floor(e.startMin / 60)));
        const lastHour = Math.max(20, ...events.map((e) => Math.ceil(e.endMin / 60)));
        const hours = Array.from({ length: lastHour - firstHour }, (_, i) => firstHour + i);
        const HOUR_PX = 56;
        const now = inZone(new Date());

        return <div className={`cal-timegrid cal-cols-${days.length}`}>
            <div className="cal-timegrid-head">
                <div />
                {days.map((day) => <button key={keyOf(day)} type="button" className={`cal-col-head ${isToday(day) ? 'is-today' : ''}`}
                    onClick={() => openDay(day)} disabled={days.length === 1}>
                    <span>{fmt(day, { weekday: 'short' })}</span>
                    <strong>{day.d}</strong>
                </button>)}
            </div>
            <div className="cal-timegrid-body" style={{ height: hours.length * HOUR_PX }}>
                <div className="cal-hours">
                    {hours.map((h) => <div key={h} className="cal-hour-label" style={{ height: HOUR_PX }}>{clock(h * 60)}</div>)}
                </div>
                {days.map((day) => <div key={keyOf(day)} className={`cal-day-col ${isToday(day) ? 'is-today' : ''}`}>
                    {hours.map((h) => <div key={h} className="cal-hour-line" style={{ height: HOUR_PX }} />)}
                    {isToday(day) && now.minutes >= firstHour * 60 && now.minutes <= lastHour * 60 &&
                        <div className="cal-now" style={{ top: ((now.minutes - firstHour * 60) / 60) * HOUR_PX }} />}
                    {eventsOn(day).map((ev) => <button key={ev.booking.id} type="button"
                        className={`cal-event cal-event-${ev.booking.status.toLowerCase()}`}
                        style={{
                            top: ((ev.startMin - firstHour * 60) / 60) * HOUR_PX,
                            height: Math.max(((ev.endMin - ev.startMin) / 60) * HOUR_PX - 2, 22),
                        }}
                        onClick={() => setSelected(ev.booking)}>
                        <strong>{clock(ev.startMin)}–{clock(ev.endMin)}</strong>
                        <span>{ev.booking.clientName}</span>
                    </button>)}
                </div>)}
            </div>
        </div>;
    }

    function renderMonth() {
        return <div className="cal-month">
            {weekdayNames.map((name) => <div key={name} className="cal-month-weekday">{name}</div>)}
            {monthGrid(cursor.y, cursor.m).map((day) => {
                const events = eventsOn(day);
                return <div key={keyOf(day)}
                    className={['cal-month-cell', day.m !== cursor.m ? 'is-other' : '', isToday(day) ? 'is-today' : ''].join(' ')}>
                    <button type="button" className="cal-month-date" onClick={() => openDay(day)}
                        aria-label={fmt(day, { weekday: 'long', day: 'numeric', month: 'long' })}>{day.d}</button>
                    {events.slice(0, 3).map((ev) => <button key={ev.booking.id} type="button"
                        className={`cal-chip cal-event-${ev.booking.status.toLowerCase()}`} onClick={() => setSelected(ev.booking)}>
                        <strong>{clock(ev.startMin)}</strong> {ev.booking.clientName}
                    </button>)}
                    {events.length > 3 && <button type="button" className="cal-more" onClick={() => openDay(day)}>
                        {t('adminCalendar.more', { count: events.length - 3 })}
                    </button>}
                </div>;
            })}
        </div>;
    }

    function renderYear() {
        return <div className="cal-year">
            {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => {
                const total = Array.from({ length: daysInMonth(cursor.y, m) }, (_, i) => eventsOn({ y: cursor.y, m, d: i + 1 }).length)
                    .reduce((a, b) => a + b, 0);
                return <div key={m} className="cal-mini">
                    <button type="button" className="cal-mini-title" onClick={() => { setCursor({ y: cursor.y, m, d: 1 }); setView('month'); }}>
                        {fmt({ y: cursor.y, m, d: 1 }, { month: 'long' })}
                        {total > 0 && <span className="cal-mini-total">{total}</span>}
                    </button>
                    <div className="cal-mini-grid">
                        {weekdayNames.map((name) => <span key={name} className="cal-mini-weekday">{name.slice(0, 2)}</span>)}
                        {monthGrid(cursor.y, m).map((day) => {
                            if (day.m !== m) return <span key={keyOf(day)} />;
                            const count = eventsOn(day).length;
                            return <button key={keyOf(day)} type="button" onClick={() => openDay(day)}
                                className={['cal-mini-day', count ? 'has-events' : '', isToday(day) ? 'is-today' : ''].join(' ')}
                                title={count ? t('adminCalendar.sessions', { count }) : undefined}>
                                {day.d}
                            </button>;
                        })}
                    </div>
                </div>;
            })}
        </div>;
    }

    const counts = (() => {
        let from: Day, days: number;
        switch (view) {
            case 'day': from = cursor; days = 1; break;
            case 'week': from = startOfWeek(cursor); days = 7; break;
            case 'month': from = { ...cursor, d: 1 }; days = daysInMonth(cursor.y, cursor.m); break;
            case 'year': from = { y: cursor.y, m: 1, d: 1 }; days = cursor.y % 4 === 0 && (cursor.y % 100 !== 0 || cursor.y % 400 === 0) ? 366 : 365; break;
        }
        const events = Array.from({ length: days }, (_, i) => eventsOn(addDays(from, i))).flat();
        return {
            approved: events.filter((e) => e.booking.status === 'Approved').length,
            pending: events.filter((e) => e.booking.status === 'Pending').length,
        };
    })();

    return <div className="page-content page-content-wide">
        <div className="page-header"><span className="page-subtitle">{t('adminCalendar.subtitle')}</span><h1>{t('adminCalendar.title')}</h1></div>
        <div className="contact-form-card cal-card">
            <div className="cal-toolbar">
                <div className="cal-nav">
                    <button type="button" className="btn-nav-outline cal-icon-btn" onClick={() => move(-1)} aria-label={t('adminCalendar.previous')}><ChevronLeft size={18} /></button>
                    <button type="button" className="btn-nav-outline" onClick={() => setCursor(today)}>{t('adminCalendar.today')}</button>
                    <button type="button" className="btn-nav-outline cal-icon-btn" onClick={() => move(1)} aria-label={t('adminCalendar.next')}><ChevronRight size={18} /></button>
                    <h2 className="cal-title" aria-live="polite">{title}</h2>
                </div>
                <div className="booking-filters" role="group" aria-label={t('adminCalendar.viewLabel')}>
                    {VIEWS.map((v) => <button key={v} type="button" aria-pressed={view === v}
                        className={view === v ? 'btn-primary' : 'btn-nav-outline'} onClick={() => setView(v)}>
                        {t(`adminCalendar.views.${v}`)}
                    </button>)}
                </div>
            </div>

            <div className="cal-summary">
                <span className="booking-badge booking-badge-approved">{t('adminCalendar.confirmedCount', { count: counts.approved })}</span>
                <span className="booking-badge booking-badge-pending">{t('adminCalendar.pendingCount', { count: counts.pending })}</span>
                {counts.pending > 0 && <Link to="/admin/programari">{t('adminCalendar.reviewPending')}</Link>}
            </div>

            {error && <div className="form-status form-status-error" role="alert"><AlertCircle size={18} /><p>{error}</p></div>}
            {loading
                ? <p role="status">{t('adminCalendar.loading')}</p>
                : <>
                    {view === 'day' && renderTimeGrid([cursor])}
                    {view === 'week' && renderTimeGrid(Array.from({ length: 7 }, (_, i) => addDays(startOfWeek(cursor), i)))}
                    {view === 'month' && renderMonth()}
                    {view === 'year' && renderYear()}
                    {view === 'day' && eventsOn(cursor).length === 0 && <p className="cal-empty">{t('adminCalendar.emptyDay')}</p>}
                </>}
        </div>

        {selected && <div className="cal-dialog-backdrop" onClick={() => setSelected(null)}>
            <div className="cal-dialog" role="dialog" aria-modal="true" aria-labelledby="cal-dialog-title" onClick={(e) => e.stopPropagation()}>
                <button type="button" className="cal-dialog-close" onClick={() => setSelected(null)} aria-label={t('adminCalendar.close')}><X size={18} /></button>
                <span className={`booking-badge booking-badge-${selected.status.toLowerCase()}`}>{t(`booking.status.${selected.status}`)}</span>
                <h3 id="cal-dialog-title">{selected.clientName}</h3>
                <p><a href={`mailto:${selected.clientEmail}`}><Mail size={14} /> {selected.clientEmail}</a></p>
                <p><strong>{new Intl.DateTimeFormat(locale, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', timeZone: ZONE }).format(new Date(selected.start))}</strong></p>
                <p>{clock(inZone(selected.start).minutes)} – {clock(inZone(selected.end).minutes)}</p>
                {selected.status === 'Pending' && <Link to="/admin/programari" className="btn-primary">{t('adminCalendar.reviewRequest')}</Link>}
            </div>
        </div>}
    </div>;
}
