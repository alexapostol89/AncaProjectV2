import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { AlertCircle, Info, UserPlus } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import { useTranslation } from 'react-i18next';
import { isStrongPassword, isValidEmail } from '../services/authService';
import { authErrorKey } from './authErrors';
import '../Css/Contact.css';
import '../Css/Auth.css';

/** Registration page. Creates a normal user account and logs it in straight away. */
export default function Register() {
    const { t } = useTranslation('translation', { keyPrefix: 'auth' });
    const { register, isLoggedIn } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const from = (location.state as { from?: string } | null)?.from;

    const [form, setForm] = useState({ fullName: '', email: '', password: '', confirmPassword: '' });
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [serverError, setServerError] = useState('');
    const [submitting, setSubmitting] = useState(false);

    if (isLoggedIn && !submitting) return <Navigate to={from ?? '/'} replace />;

    function updateField(field: keyof typeof form, value: string) {
        setForm((prev) => ({ ...prev, [field]: value }));
    }

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        const validationErrors: Record<string, string> = {};
        if (form.fullName.trim().length < 2) validationErrors.fullName = t('nameError');
        if (!isValidEmail(form.email)) validationErrors.email = t('emailError');
        if (!isStrongPassword(form.password)) validationErrors.password = t('passwordWeak');
        if (form.confirmPassword !== form.password) validationErrors.confirmPassword = t('passwordMismatch');
        setErrors(validationErrors);
        if (Object.keys(validationErrors).length) return;

        setSubmitting(true);
        setServerError('');
        try {
            await register(form.fullName.trim(), form.email.trim(), form.password);
            navigate(from ?? '/', { replace: true });
        } catch (err) {
            setServerError(t(authErrorKey(err)));
            setSubmitting(false);
        }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('registerSubtitle')}</span><h1>{t('registerTitle')}</h1></div>
        <div className="auth-card">
            {from && <div className="form-status auth-info" role="status"><Info size={20} /><p>{t('loginRequired')}</p></div>}
            {serverError && <div className="form-status form-status-error" role="alert"><AlertCircle size={20} /><p>{serverError}</p></div>}
            <form onSubmit={handleSubmit} noValidate>
                <div className="form-group">
                    <label htmlFor="register-name">{t('fullName')}</label>
                    <input id="register-name" type="text" autoComplete="name" value={form.fullName} onChange={(e) => updateField('fullName', e.target.value)} aria-invalid={!!errors.fullName} />
                    {errors.fullName && <span className="field-error">{errors.fullName}</span>}
                </div>
                <div className="form-group">
                    <label htmlFor="register-email">{t('email')}</label>
                    <input id="register-email" type="email" autoComplete="email" value={form.email} onChange={(e) => updateField('email', e.target.value)} aria-invalid={!!errors.email} />
                    {errors.email && <span className="field-error">{errors.email}</span>}
                </div>
                <div className="form-group">
                    <label htmlFor="register-password">{t('password')}</label>
                    <input id="register-password" type="password" autoComplete="new-password" value={form.password} onChange={(e) => updateField('password', e.target.value)} aria-invalid={!!errors.password} />
                    {errors.password ? <span className="field-error">{errors.password}</span> : <span className="field-hint">{t('passwordHint')}</span>}
                </div>
                <div className="form-group">
                    <label htmlFor="register-confirm">{t('confirmPassword')}</label>
                    <input id="register-confirm" type="password" autoComplete="new-password" value={form.confirmPassword} onChange={(e) => updateField('confirmPassword', e.target.value)} aria-invalid={!!errors.confirmPassword} />
                    {errors.confirmPassword && <span className="field-error">{errors.confirmPassword}</span>}
                </div>
                <button type="submit" className="btn-primary auth-submit" disabled={submitting}>
                    <UserPlus size={18} />{submitting ? t('registering') : t('registerButton')}
                </button>
            </form>
            <p className="auth-switch">
                {t('haveAccount')} <Link to="/autentificare" state={location.state}>{t('loginLink')}</Link>
            </p>
        </div>
    </div>;
}
