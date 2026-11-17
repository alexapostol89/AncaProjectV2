import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { AlertCircle, CheckCircle, Mail } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { forgotPassword, isValidEmail } from '../services/authService';
import { authErrorKey } from './authErrors';
import '../Css/Contact.css';
import '../Css/Auth.css';

/**
 * "Forgot password": the user enters their email and gets a reset link (valid 1 hour).
 * The answer is the same whether or not an account exists, so this form can't be used to find accounts.
 */
export default function ForgotPassword() {
    const { t } = useTranslation('translation', { keyPrefix: 'auth' });
    const [email, setEmail] = useState('');
    const [error, setError] = useState('');
    const [serverError, setServerError] = useState('');
    const [submitting, setSubmitting] = useState(false);
    const [sent, setSent] = useState(false);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        if (!isValidEmail(email)) { setError(t('emailError')); return; }
        setError('');
        setServerError('');
        setSubmitting(true);
        try {
            await forgotPassword(email.trim());
            setSent(true);
        } catch (err) {
            setServerError(t(authErrorKey(err)));
        } finally {
            setSubmitting(false);
        }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('forgotSubtitle')}</span><h1>{t('forgotTitle')}</h1></div>
        <div className="auth-card">
            {sent
                ? <div className="form-status form-status-success auth-success" role="status"><CheckCircle size={20} /><p>{t('forgotSent')}</p></div>
                : <>
                    <p className="auth-lead">{t('forgotLead')}</p>
                    {serverError && <div className="form-status form-status-error" role="alert"><AlertCircle size={20} /><p>{serverError}</p></div>}
                    <form onSubmit={handleSubmit} noValidate>
                        <div className="form-group">
                            <label htmlFor="forgot-email">{t('email')}</label>
                            <input id="forgot-email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} aria-invalid={!!error} />
                            {error && <span className="field-error">{error}</span>}
                        </div>
                        <button type="submit" className="btn-primary auth-submit" disabled={submitting}>
                            <Mail size={18} />{submitting ? t('forgotSending') : t('forgotButton')}
                        </button>
                    </form>
                </>}
            <p className="auth-switch"><Link to="/autentificare">{t('backToLogin')}</Link></p>
        </div>
    </div>;
}
