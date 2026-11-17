import { useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { AlertCircle, CheckCircle, KeyRound } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { isStrongPassword, resetPassword } from '../services/authService';
import { authErrorKey } from './authErrors';
import '../Css/Contact.css';
import '../Css/Auth.css';

/**
 * Opened from the reset email: /reset-password?email=...&token=...
 * The user picks a new password; afterwards they log in with it.
 */
export default function ResetPassword() {
    const { t } = useTranslation('translation', { keyPrefix: 'auth' });
    const [params] = useSearchParams();
    const email = params.get('email') ?? '';
    const token = params.get('token') ?? '';

    const [password, setPassword] = useState('');
    const [confirm, setConfirm] = useState('');
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [serverError, setServerError] = useState('');
    const [submitting, setSubmitting] = useState(false);
    const [done, setDone] = useState(false);

    const linkBroken = !email || !token;

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        const validationErrors: Record<string, string> = {};
        if (!isStrongPassword(password)) validationErrors.password = t('passwordWeak');
        if (confirm !== password) validationErrors.confirm = t('passwordMismatch');
        setErrors(validationErrors);
        if (Object.keys(validationErrors).length) return;

        setServerError('');
        setSubmitting(true);
        try {
            await resetPassword(email, token, password);
            setDone(true);
        } catch (err) {
            setServerError(t(authErrorKey(err)));
        } finally {
            setSubmitting(false);
        }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('resetSubtitle')}</span><h1>{t('resetTitle')}</h1></div>
        <div className="auth-card">
            {done ? <>
                <div className="form-status form-status-success auth-success" role="status"><CheckCircle size={20} /><p>{t('resetDone')}</p></div>
                <Link to="/autentificare" className="btn-primary auth-submit">{t('loginButton')}</Link>
            </> : linkBroken ? <>
                <div className="form-status form-status-error" role="alert"><AlertCircle size={20} /><p>{t('invalidLink')}</p></div>
                <p className="auth-switch"><Link to="/parola-uitata">{t('requestNewLink')}</Link></p>
            </> : <>
                <p className="auth-lead">{t('resetLead', { email })}</p>
                {serverError && <div className="form-status form-status-error" role="alert"><AlertCircle size={20} /><p>{serverError}</p></div>}
                <form onSubmit={handleSubmit} noValidate>
                    {/* Hidden username field so password managers save the new password for the right account. */}
                    <input type="email" autoComplete="username" value={email} readOnly hidden />
                    <div className="form-group">
                        <label htmlFor="reset-password">{t('newPassword')}</label>
                        <input id="reset-password" type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} aria-invalid={!!errors.password} />
                        {errors.password ? <span className="field-error">{errors.password}</span> : <span className="field-hint">{t('passwordHint')}</span>}
                    </div>
                    <div className="form-group">
                        <label htmlFor="reset-confirm">{t('confirmPassword')}</label>
                        <input id="reset-confirm" type="password" autoComplete="new-password" value={confirm} onChange={(e) => setConfirm(e.target.value)} aria-invalid={!!errors.confirm} />
                        {errors.confirm && <span className="field-error">{errors.confirm}</span>}
                    </div>
                    <button type="submit" className="btn-primary auth-submit" disabled={submitting}>
                        <KeyRound size={18} />{submitting ? t('resetSaving') : t('resetButton')}
                    </button>
                </form>
                {serverError === t('invalidLink') && <p className="auth-switch"><Link to="/parola-uitata">{t('requestNewLink')}</Link></p>}
            </>}
        </div>
    </div>;
}
