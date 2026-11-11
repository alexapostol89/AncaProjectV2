import { useState, type FormEvent } from 'react';
import { Mail, Phone, MapPin, Clock, Send, CheckCircle2, AlertCircle } from 'lucide-react';
import { submitContactForm, ContactApiError } from '../services/contactService';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/useAuth';
import '../Css/Contact.css';

type FormStatus = 'idle' | 'submitting' | 'success' | 'error';
/** Session types; the label shown (and sent in the email) is the translation contact.online / physical / unsure. */
const sessionTypes = ['online', 'physical', 'unsure'] as const;
type SessionType = (typeof sessionTypes)[number];
interface FormState { name: string; email: string; phone: string; sessionType: SessionType; message: string; consent: boolean; website: string }
const initialState: FormState = { name: '', email: '', phone: '', sessionType: 'online', message: '', consent: false, website: '' };

export default function Contact() {
    // keyPrefix: t('name') here means the key 'contact.name' in the locale files.
    const { t } = useTranslation('translation', { keyPrefix: 'contact' });
    const { user } = useAuth();
    // Logged-in users get their name and email filled in.
    const emptyForm: FormState = { ...initialState, name: user?.fullName ?? '', email: user?.email ?? '' };
    const [form, setForm] = useState<FormState>(emptyForm);
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [status, setStatus] = useState<FormStatus>('idle');
    const [statusMessage, setStatusMessage] = useState('');

    function updateField<K extends keyof FormState>(field: K, value: FormState[K]) {
        setForm((prev) => ({ ...prev, [field]: value }));
        if (errors[field]) setErrors((prev) => { const next = { ...prev }; delete next[field]; return next; });
    }

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        if (status === 'submitting') return;
        const validationErrors: Record<string, string> = {};
        if (form.name.trim().length < 2) validationErrors.name = t('nameError');
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) validationErrors.email = t('emailError');
        if (form.message.trim().length < 10) validationErrors.message = t('messageError');
        if (!form.consent) validationErrors.consent = t('consentError');
        if (Object.keys(validationErrors).length) { setErrors(validationErrors); return; }
        setStatus('submitting'); setErrors({});
        try {
            await submitContactForm({ name: form.name.trim(), email: form.email.trim(), phone: form.phone.trim() || undefined, sessionType: t(form.sessionType), message: form.message.trim(), consent: form.consent, website: form.website });
            setStatus('success'); setStatusMessage(t('sent')); setForm(emptyForm);
        } catch (err) {
            if (err instanceof ContactApiError && err.fieldErrors) {
                const mapped: Record<string, string> = {};
                const fieldMap: Record<string, string> = { Name: 'name', Email: 'email', Phone: 'phone', SessionType: 'sessionType', Message: 'message', Consent: 'consent' };
                for (const [key, value] of Object.entries(err.fieldErrors)) {
                    const field = fieldMap[key] ?? key.toLowerCase();
                    if (value.length) mapped[field] = field === 'name' ? t('nameError') : field === 'email' ? t('emailError') : field === 'message' ? t('messageError') : field === 'consent' ? t('consentError') : t('serverError');
                }
                setErrors(mapped); setStatus('idle'); return;
            }
            setStatus('error'); setStatusMessage(t('serverError'));
        }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('subtitle')}</span><h1>{t('title')}</h1></div>
        <div className="contact-wrapper">
            <div className="contact-info">
                <h2>{t('talk')}</h2><p>{t('intro')}</p>
                <div className="contact-details">
                    <div className="contact-detail-item"><div className="contact-icon"><Mail size={20} /></div><div><span className="detail-label">Email</span><p>psihologancaalexandra@gmail.com</p></div></div>
                    <div className="contact-detail-item"><div className="contact-icon"><Phone size={20} /></div><div><span className="detail-label">{t('phone')} / WhatsApp</span><p>+4 0745 435 146</p></div></div>
                    <div className="contact-detail-item"><div className="contact-icon"><MapPin size={20} /></div><div><span className="detail-label">{t('location')}</span><p>{t('address')}</p></div></div>
                    <div className="contact-detail-item"><div className="contact-icon"><Clock size={20} /></div><div><span className="detail-label">{t('hours')}</span><p>{t('schedule')}</p></div></div>
                </div>
            </div>
            <div className="contact-form-card"><h3>{t('form')}</h3>
                {status === 'success' ? <div className="form-status form-status-success" role="status"><CheckCircle2 size={22} /><p>{statusMessage}</p></div> :
                    <form onSubmit={handleSubmit} noValidate>
                        {status === 'error' && <div className="form-status form-status-error" role="alert"><AlertCircle size={20} /><p>{statusMessage}</p></div>}
                        <div className="form-group"><label htmlFor="contact-name">{t('name')}</label><input id="contact-name" type="text" placeholder={t('namePlaceholder')} value={form.name} onChange={(e) => updateField('name', e.target.value)} aria-invalid={!!errors.name} aria-describedby={errors.name ? 'error-name' : undefined} required />{errors.name && <span className="field-error" id="error-name">{errors.name}</span>}</div>
                        <div className="form-group"><label htmlFor="contact-email">{t('email')}</label><input id="contact-email" type="email" placeholder="maria@example.com" value={form.email} onChange={(e) => updateField('email', e.target.value)} aria-invalid={!!errors.email} aria-describedby={errors.email ? 'error-email' : undefined} required />{errors.email && <span className="field-error" id="error-email">{errors.email}</span>}</div>
                        <div className="form-group"><label htmlFor="contact-phone">{t('phone')}</label><input id="contact-phone" type="tel" placeholder="+40 ..." value={form.phone} onChange={(e) => updateField('phone', e.target.value)} /></div>
                        <div className="form-group"><label htmlFor="contact-session-type">{t('session')}</label><select id="contact-session-type" value={form.sessionType} onChange={(e) => updateField('sessionType', e.target.value as SessionType)}>{sessionTypes.map((type) => <option key={type} value={type}>{t(type)}</option>)}</select></div>
                        <div className="form-group"><label htmlFor="contact-message">{t('message')}</label><textarea id="contact-message" rows={4} placeholder={t('placeholder')} value={form.message} onChange={(e) => updateField('message', e.target.value)} aria-invalid={!!errors.message} aria-describedby={errors.message ? 'error-message' : undefined} required />{errors.message && <span className="field-error" id="error-message">{errors.message}</span>}<span className="field-hint">{t('hint')}</span></div>
                        <div className="form-group form-honeypot" aria-hidden="true"><label htmlFor="contact-website">Website</label><input id="contact-website" type="text" tabIndex={-1} autoComplete="off" value={form.website} onChange={(e) => updateField('website', e.target.value)} /></div>
                        <div className="form-group form-consent"><label className="checkbox-label"><input type="checkbox" checked={form.consent} onChange={(e) => updateField('consent', e.target.checked)} aria-invalid={!!errors.consent} aria-describedby={errors.consent ? 'error-consent' : undefined} /><span>{t('consent')}</span></label>{errors.consent && <span className="field-error" id="error-consent">{errors.consent}</span>}</div>
                        <button type="submit" className="btn-primary" disabled={status === 'submitting'} style={{ width: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}><Send size={18} />{status === 'submitting' ? t('sending') : t('send')}</button>
                    </form>}
            </div>
        </div>
    </div>;
}
