import { useState, type FormEvent } from 'react';
import { Mail, Phone, MapPin, Clock, Send, CheckCircle2, AlertCircle } from 'lucide-react';
import { submitContactForm, ContactApiError } from '../services/contactService';
import { useLanguage } from '../i18n';
import '../Css/Contact.css';

type FormStatus = 'idle' | 'submitting' | 'success' | 'error';
interface FormState { name: string; email: string; phone: string; sessionType: string; message: string; consent: boolean; website: string }
const initialState: FormState = { name: '', email: '', phone: '', sessionType: 'Online (Google Meet / Zoom)', message: '', consent: false, website: '' };

export default function Contact() {
    const { messages, language } = useLanguage();
    const t = messages.contact;
    const [form, setForm] = useState<FormState>(initialState);
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [status, setStatus] = useState<FormStatus>('idle');
    const [statusMessage, setStatusMessage] = useState('');
    const localizedSessionTypes = language === 'ro'
        ? ['Online (Google Meet / Zoom)', 'Fizic (La Cabinet)', 'Nu sunt sigur(ă), aș dori o recomandare']
        : language === 'en'
            ? ['Online (Google Meet / Zoom)', 'In person (at the practice)', 'I’m not sure; I’d like a recommendation']
            : ['Online (Google Meet / Zoom)', 'Fysisk (i klinikken)', 'Jeg er ikke sikker og vil gerne have en anbefaling'];
    const sessionIndex = form.sessionType.toLowerCase().includes('online') ? 0 : /fizic|in person|fysisk/i.test(form.sessionType) ? 1 : 2;

    function updateField<K extends keyof FormState>(field: K, value: FormState[K]) {
        setForm((prev) => ({ ...prev, [field]: value }));
        if (errors[field]) setErrors((prev) => { const next = { ...prev }; delete next[field]; return next; });
    }

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        if (status === 'submitting') return;
        const validationErrors: Record<string, string> = {};
        if (form.name.trim().length < 2) validationErrors.name = t.nameError;
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) validationErrors.email = t.emailError;
        if (form.message.trim().length < 10) validationErrors.message = t.messageError;
        if (!form.consent) validationErrors.consent = t.consentError;
        if (Object.keys(validationErrors).length) { setErrors(validationErrors); return; }
        setStatus('submitting'); setErrors({});
        try {
            await submitContactForm({ name: form.name.trim(), email: form.email.trim(), phone: form.phone.trim() || undefined, sessionType: localizedSessionTypes[sessionIndex], message: form.message.trim(), consent: form.consent, website: form.website });
            setStatus('success'); setStatusMessage(t.sent); setForm(initialState);
        } catch (err) {
            if (err instanceof ContactApiError && err.fieldErrors) {
                const mapped: Record<string, string> = {};
                const fieldMap: Record<string, string> = { Name: 'name', Email: 'email', Phone: 'phone', SessionType: 'sessionType', Message: 'message', Consent: 'consent' };
                for (const [key, value] of Object.entries(err.fieldErrors)) {
                    const field = fieldMap[key] ?? key.toLowerCase();
                    if (value.length) mapped[field] = field === 'name' ? t.nameError : field === 'email' ? t.emailError : field === 'message' ? t.messageError : field === 'consent' ? t.consentError : t.serverError;
                }
                setErrors(mapped); setStatus('idle'); return;
            }
            setStatus('error'); setStatusMessage(t.serverError);
        }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t.subtitle}</span><h1>{t.title}</h1></div>
        <div className="contact-wrapper">
            <div className="contact-info">
                <h2>{t.talk}</h2><p>{t.intro}</p>
                <div className="contact-details">
                    <div className="contact-detail-item"><div className="contact-icon"><Mail size={20} /></div><div><span className="detail-label">Email</span><p>psihologancaalexandra@gmail.com</p></div></div>
                    <div className="contact-detail-item"><div className="contact-icon"><Phone size={20} /></div><div><span className="detail-label">{t.phone} / WhatsApp</span><p>+4 0745 435 146</p></div></div>
                    <div className="contact-detail-item"><div className="contact-icon"><MapPin size={20} /></div><div><span className="detail-label">{t.location}</span><p>{t.address}</p></div></div>
                    <div className="contact-detail-item"><div className="contact-icon"><Clock size={20} /></div><div><span className="detail-label">{t.hours}</span><p>{t.schedule}</p></div></div>
                </div>
            </div>
            <div className="contact-form-card"><h3>{t.form}</h3>
                {status === 'success' ? <div className="form-status form-status-success" role="status"><CheckCircle2 size={22} /><p>{statusMessage}</p></div> :
                    <form onSubmit={handleSubmit} noValidate>
                        {status === 'error' && <div className="form-status form-status-error" role="alert"><AlertCircle size={20} /><p>{statusMessage}</p></div>}
                        <div className="form-group"><label htmlFor="contact-name">{t.name}</label><input id="contact-name" type="text" placeholder={language === 'ro' ? 'Ex: Maria Popescu' : language === 'en' ? 'e.g. Maria Popescu' : 'F.eks. Maria Popescu'} value={form.name} onChange={(e) => updateField('name', e.target.value)} aria-invalid={!!errors.name} aria-describedby={errors.name ? 'error-name' : undefined} required />{errors.name && <span className="field-error" id="error-name">{errors.name}</span>}</div>
                        <div className="form-group"><label htmlFor="contact-email">{t.email}</label><input id="contact-email" type="email" placeholder="maria@example.com" value={form.email} onChange={(e) => updateField('email', e.target.value)} aria-invalid={!!errors.email} aria-describedby={errors.email ? 'error-email' : undefined} required />{errors.email && <span className="field-error" id="error-email">{errors.email}</span>}</div>
                        <div className="form-group"><label htmlFor="contact-phone">{t.phone}</label><input id="contact-phone" type="tel" placeholder="+40 ..." value={form.phone} onChange={(e) => updateField('phone', e.target.value)} /></div>
                        <div className="form-group"><label htmlFor="contact-session-type">{t.session}</label><select id="contact-session-type" value={localizedSessionTypes[sessionIndex]} onChange={(e) => updateField('sessionType', e.target.value)}>{localizedSessionTypes.map((option) => <option key={option}>{option}</option>)}</select></div>
                        <div className="form-group"><label htmlFor="contact-message">{t.message}</label><textarea id="contact-message" rows={4} placeholder={t.placeholder} value={form.message} onChange={(e) => updateField('message', e.target.value)} aria-invalid={!!errors.message} aria-describedby={errors.message ? 'error-message' : undefined} required />{errors.message && <span className="field-error" id="error-message">{errors.message}</span>}<span className="field-hint">{t.hint}</span></div>
                        <div className="form-group form-honeypot" aria-hidden="true"><label htmlFor="contact-website">Website</label><input id="contact-website" type="text" tabIndex={-1} autoComplete="off" value={form.website} onChange={(e) => updateField('website', e.target.value)} /></div>
                        <div className="form-group form-consent"><label className="checkbox-label"><input type="checkbox" checked={form.consent} onChange={(e) => updateField('consent', e.target.checked)} aria-invalid={!!errors.consent} aria-describedby={errors.consent ? 'error-consent' : undefined} /><span>{t.consent}</span></label>{errors.consent && <span className="field-error" id="error-consent">{errors.consent}</span>}</div>
                        <button type="submit" className="btn-primary" disabled={status === 'submitting'} style={{ width: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}><Send size={18} />{status === 'submitting' ? t.sending : t.send}</button>
                    </form>}
            </div>
        </div>
    </div>;
}
