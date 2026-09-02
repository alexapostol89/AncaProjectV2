import { useState, type FormEvent } from 'react';
import { Mail, Phone, MapPin, Clock, Send, CheckCircle2, AlertCircle } from 'lucide-react';
import { submitContactForm, ContactApiError, type ContactFieldErrors } from '../services/contactService';
import '../Css/Contact.css';

type FormStatus = 'idle' | 'submitting' | 'success' | 'error';

interface FormState {
    name: string;
    email: string;
    phone: string;
    sessionType: string;
    message: string;
    consent: boolean;
    website: string; // honeypot — must stay empty; hidden from real visitors via CSS
}

const initialState: FormState = {
    name: '',
    email: '',
    phone: '',
    sessionType: 'Online (Google Meet / Zoom)',
    message: '',
    consent: false,
    website: '',
};

function validate(form: FormState): Record<string, string> {
    const errors: Record<string, string> = {};

    if (form.name.trim().length < 2) {
        errors.name = 'Te rugăm să introduci numele tău.';
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
        errors.email = 'Adresa de email nu este validă.';
    }

    if (form.message.trim().length < 10) {
        errors.message = 'Mesajul trebuie să aibă cel puțin 10 caractere.';
    }

    if (!form.consent) {
        errors.consent = 'Este necesar acordul pentru a putea trimite mesajul.';
    }

    return errors;
}

// Maps the backend's PascalCase field names to our form's field names.
const backendFieldMap: Record<string, keyof FormState> = {
    Name: 'name',
    Email: 'email',
    Phone: 'phone',
    SessionType: 'sessionType',
    Message: 'message',
    Consent: 'consent',
};

function mapBackendErrors(fieldErrors: ContactFieldErrors): Record<string, string> {
    const mapped: Record<string, string> = {};
    for (const [key, messages] of Object.entries(fieldErrors)) {
        const field = backendFieldMap[key] ?? key.toLowerCase();
        if (messages.length > 0) {
            mapped[field] = messages[0];
        }
    }
    return mapped;
}

export default function Contact() {
    const [form, setForm] = useState<FormState>(initialState);
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [status, setStatus] = useState<FormStatus>('idle');
    const [statusMessage, setStatusMessage] = useState<string>('');

    function updateField<K extends keyof FormState>(field: K, value: FormState[K]) {
        setForm((prev) => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors((prev) => {
                const next = { ...prev };
                delete next[field];
                return next;
            });
        }
    }

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();

        if (status === 'submitting') return; // prevent duplicate submissions

        const validationErrors = validate(form);
        if (Object.keys(validationErrors).length > 0) {
            setErrors(validationErrors);
            return;
        }

        setStatus('submitting');
        setErrors({});

        try {
            const response = await submitContactForm({
                name: form.name.trim(),
                email: form.email.trim(),
                phone: form.phone.trim() || undefined,
                sessionType: form.sessionType,
                message: form.message.trim(),
                consent: form.consent,
                website: form.website,
            });

            setStatus('success');
            setStatusMessage(response.message);
            setForm(initialState);
        } catch (err) {
            if (err instanceof ContactApiError && err.fieldErrors) {
                setErrors(mapBackendErrors(err.fieldErrors));
                setStatus('idle');
                return;
            }

            setStatus('error');
            setStatusMessage(
                err instanceof ContactApiError
                    ? err.message
                    : 'A apărut o eroare neașteptată. Te rugăm să încerci din nou.'
            );
        }
    }

    return (
        <div className="page-content">
            <div className="page-header">
                <span className="page-subtitle">Fă primul pas</span>
                <h1>Contact & Programări</h1>
            </div>

            <div className="contact-wrapper">
                <div className="contact-info">
                    <h2>Hai să stăm de vorbă</h2>
                    <p>
                        Dacă simți că a sosit momentul să începi terapia sau ai întrebări despre cum funcționează ședințele, nu ezita să-mi scrii.
                    </p>

                    <div className="contact-details">
                        <div className="contact-detail-item">
                            <div className="contact-icon"><Mail size={20} /></div>
                            <div>
                                <span className="detail-label">Email</span>
                                <p>psihologancaalexandra@gmail.com</p>
                            </div>
                        </div>

                        <div className="contact-detail-item">
                            <div className="contact-icon"><Phone size={20} /></div>
                            <div>
                                <span className="detail-label">Telefon / WhatsApp</span>
                                <p>+4 0745 435 146</p>
                            </div>
                        </div>

                        <div className="contact-detail-item">
                            <div className="contact-icon"><MapPin size={20} /></div>
                            <div>
                                <span className="detail-label">Locație</span>
                                <p>Cabinet: Iași, România / Ședințe Online pe Zoom / WhatsApp</p>
                            </div>
                        </div>

                        <div className="contact-detail-item">
                            <div className="contact-icon"><Clock size={20} /></div>
                            <div>
                                <span className="detail-label">Program</span>
                                <p>Luni – Vineri: 09:00 – 19:00</p>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="contact-form-card">
                    <h3>Trimite un mesaj</h3>

                    {status === 'success' ? (
                        <div className="form-status form-status-success" role="status">
                            <CheckCircle2 size={22} />
                            <p>{statusMessage}</p>
                        </div>
                    ) : (
                        <form onSubmit={handleSubmit} noValidate>
                            {status === 'error' && (
                                <div className="form-status form-status-error" role="alert">
                                    <AlertCircle size={20} />
                                    <p>{statusMessage}</p>
                                </div>
                            )}

                            <div className="form-group">
                                <label htmlFor="contact-name">Nume și Prenume</label>
                                <input
                                    id="contact-name"
                                    type="text"
                                    placeholder="Ex: Maria Popescu"
                                    value={form.name}
                                    onChange={(e) => updateField('name', e.target.value)}
                                    aria-invalid={!!errors.name}
                                    aria-describedby={errors.name ? 'error-name' : undefined}
                                    required
                                />
                                {errors.name && <span className="field-error" id="error-name">{errors.name}</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="contact-email">Email</label>
                                <input
                                    id="contact-email"
                                    type="email"
                                    placeholder="maria@exemplu.ro"
                                    value={form.email}
                                    onChange={(e) => updateField('email', e.target.value)}
                                    aria-invalid={!!errors.email}
                                    aria-describedby={errors.email ? 'error-email' : undefined}
                                    required
                                />
                                {errors.email && <span className="field-error" id="error-email">{errors.email}</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="contact-phone">Telefon</label>
                                <input
                                    id="contact-phone"
                                    type="tel"
                                    placeholder="07xxxxxxxx"
                                    value={form.phone}
                                    onChange={(e) => updateField('phone', e.target.value)}
                                />
                            </div>

                            <div className="form-group">
                                <label htmlFor="contact-session-type">Tip ședință preferată</label>
                                <select
                                    id="contact-session-type"
                                    value={form.sessionType}
                                    onChange={(e) => updateField('sessionType', e.target.value)}
                                >
                                    <option>Online (Google Meet / Zoom)</option>
                                    <option>Fizic (La Cabinet)</option>
                                    <option>Nu sunt sigur(ă), aș dori o recomandare</option>
                                </select>
                            </div>

                            <div className="form-group">
                                <label htmlFor="contact-message">Mesaj / Ce te aduce la terapie?</label>
                                <textarea
                                    id="contact-message"
                                    rows={4}
                                    placeholder="Scrie câteva gânduri..."
                                    value={form.message}
                                    onChange={(e) => updateField('message', e.target.value)}
                                    aria-invalid={!!errors.message}
                                    aria-describedby={errors.message ? 'error-message' : undefined}
                                    required
                                />
                                {errors.message && <span className="field-error" id="error-message">{errors.message}</span>}
                                <span className="field-hint">
                                    Acest formular nu este monitorizat permanent și nu este potrivit pentru urgențe. În caz de urgență, sună la 112.
                                </span>
                            </div>

                            {/* Honeypot field: hidden from real users, bots tend to fill every input. */}
                            <div className="form-group form-honeypot" aria-hidden="true">
                                <label htmlFor="contact-website">Website</label>
                                <input
                                    id="contact-website"
                                    type="text"
                                    tabIndex={-1}
                                    autoComplete="off"
                                    value={form.website}
                                    onChange={(e) => updateField('website', e.target.value)}
                                />
                            </div>

                            <div className="form-group form-consent">
                                <label className="checkbox-label">
                                    <input
                                        type="checkbox"
                                        checked={form.consent}
                                        onChange={(e) => updateField('consent', e.target.checked)}
                                        aria-invalid={!!errors.consent}
                                        aria-describedby={errors.consent ? 'error-consent' : undefined}
                                    />
                                    <span>Am citit și sunt de acord cu Politica de confidențialitate.</span>
                                </label>
                                {errors.consent && <span className="field-error" id="error-consent">{errors.consent}</span>}
                            </div>

                            <button
                                type="submit"
                                className="btn-primary"
                                disabled={status === 'submitting'}
                                style={{ width: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}
                            >
                                <Send size={18} />
                                {status === 'submitting' ? 'Se trimite...' : 'Trimite Mesajul'}
                            </button>
                        </form>
                    )}
                </div>
            </div>
        </div>
    );
}
