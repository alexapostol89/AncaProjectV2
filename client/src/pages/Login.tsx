import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { AlertCircle, Info, LogIn } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import { useTranslation } from 'react-i18next';
import { isValidEmail } from '../services/authService';
import { authErrorKey } from './authErrors';
import '../Css/Contact.css';
import '../Css/Auth.css';

/**
 * Login page. If another page sent the user here (e.g. "Book a session"),
 * it passes { from: '/booking' } in the router state, and after logging in
 * we send the user back there.
 */
export default function Login() {
    const { t } = useTranslation('translation', { keyPrefix: 'auth' });
    const { login, isLoggedIn } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const from = (location.state as { from?: string } | null)?.from;

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [serverError, setServerError] = useState('');
    const [submitting, setSubmitting] = useState(false);

    // Already logged in (e.g. opened /autentificare by hand): nothing to do here.
    if (isLoggedIn && !submitting) return <Navigate to={from ?? '/'} replace />;

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        const validationErrors: Record<string, string> = {};
        if (!isValidEmail(email)) validationErrors.email = t('emailError');
        if (!password) validationErrors.password = t('passwordRequired');
        setErrors(validationErrors);
        if (Object.keys(validationErrors).length) return;

        setSubmitting(true);
        setServerError('');
        try {
            await login(email.trim(), password);
            navigate(from ?? '/', { replace: true });
        } catch (err) {
            setServerError(t(authErrorKey(err)));
            setSubmitting(false);
        }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('loginSubtitle')}</span><h1>{t('loginTitle')}</h1></div>
        <div className="auth-card">
            {from && <div className="form-status auth-info" role="status"><Info size={20} /><p>{t('loginRequired')}</p></div>}
            {serverError && <div className="form-status form-status-error" role="alert"><AlertCircle size={20} /><p>{serverError}</p></div>}
            <form onSubmit={handleSubmit} noValidate>
                <div className="form-group">
                    <label htmlFor="login-email">{t('email')}</label>
                    <input id="login-email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} aria-invalid={!!errors.email} />
                    {errors.email && <span className="field-error">{errors.email}</span>}
                </div>
                <div className="form-group">
                    <label htmlFor="login-password">{t('password')}</label>
                    <input id="login-password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} aria-invalid={!!errors.password} />
                    {errors.password && <span className="field-error">{errors.password}</span>}
                </div>
                <button type="submit" className="btn-primary auth-submit" disabled={submitting}>
                    <LogIn size={18} />{submitting ? t('loggingIn') : t('loginButton')}
                </button>
            </form>
            <p className="auth-switch">
                {t('noAccount')} <Link to="/inregistrare" state={location.state}>{t('registerLink')}</Link>
            </p>
        </div>
    </div>;
}
