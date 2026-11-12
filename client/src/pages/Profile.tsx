import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AlertCircle, CalendarDays, CheckCircle2, KeyRound, Trash2, UserRound } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/useAuth';
import { changePassword, deleteAccount, formatDate, updateProfile } from '../services/accountService';
import { isStrongPassword } from '../services/authService';
import { accountErrorKey } from './accountErrors';
import '../Css/Contact.css';
import '../Css/Account.css';

type Status = { type: 'success' | 'error'; text: string } | null;

function StatusBox({ status }: { status: Status }) {
    if (!status) return null;
    return <div className={`form-status form-status-${status.type}`} role={status.type === 'error' ? 'alert' : 'status'}>
        {status.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}<p>{status.text}</p>
    </div>;
}

/** "My profile" for any logged-in user: details, name change, password change, account deletion. */
export default function Profile() {
    const { t, i18n } = useTranslation();
    const { user, token, updateUser, logout } = useAuth();
    const navigate = useNavigate();

    const [fullName, setFullName] = useState(user?.fullName ?? '');
    const [detailsStatus, setDetailsStatus] = useState<Status>(null);
    const [savingDetails, setSavingDetails] = useState(false);

    const [passwords, setPasswords] = useState({ current: '', next: '', confirm: '' });
    const [passwordStatus, setPasswordStatus] = useState<Status>(null);
    const [savingPassword, setSavingPassword] = useState(false);

    const [deleteOpen, setDeleteOpen] = useState(false);
    const [deletePassword, setDeletePassword] = useState('');
    const [deleteStatus, setDeleteStatus] = useState<Status>(null);
    const [deleting, setDeleting] = useState(false);

    if (!user || !token) return null; // RequireRole already sends guests to login

    const initials = user.fullName.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]!.toUpperCase()).join('');
    const nameChanged = fullName.trim() !== user.fullName;

    async function saveDetails(event: FormEvent) {
        event.preventDefault();
        const name = fullName.trim();
        if (name.length < 2 || name.length > 120) { setDetailsStatus({ type: 'error', text: t('profile.nameInvalid') }); return; }
        setSavingDetails(true); setDetailsStatus(null);
        try {
            const updated = await updateProfile(name, token!);
            updateUser(updated);
            setFullName(updated.fullName);
            setDetailsStatus({ type: 'success', text: t('profile.saved') });
        } catch (error) {
            setDetailsStatus({ type: 'error', text: t(`accountErrors.${accountErrorKey(error)}`) });
        } finally {
            setSavingDetails(false);
        }
    }

    async function savePassword(event: FormEvent) {
        event.preventDefault();
        if (!isStrongPassword(passwords.next)) { setPasswordStatus({ type: 'error', text: t('profile.passwordWeak') }); return; }
        if (passwords.next !== passwords.confirm) { setPasswordStatus({ type: 'error', text: t('profile.passwordMismatch') }); return; }
        setSavingPassword(true); setPasswordStatus(null);
        try {
            await changePassword(passwords.current, passwords.next, token!);
            setPasswords({ current: '', next: '', confirm: '' });
            setPasswordStatus({ type: 'success', text: t('profile.passwordChanged') });
        } catch (error) {
            setPasswordStatus({ type: 'error', text: t(`accountErrors.${accountErrorKey(error)}`) });
        } finally {
            setSavingPassword(false);
        }
    }

    async function confirmDelete(event: FormEvent) {
        event.preventDefault();
        setDeleting(true); setDeleteStatus(null);
        try {
            await deleteAccount(deletePassword, token!);
            logout();
            navigate('/', { replace: true });
        } catch (error) {
            setDeleteStatus({ type: 'error', text: t(`accountErrors.${accountErrorKey(error)}`) });
            setDeleting(false);
        }
    }

    return <div className="page-content">
        <div className="page-header"><span className="page-subtitle">{t('profile.subtitle')}</span><h1>{t('profile.title')}</h1></div>

        <div className="account-grid">
            <section className="contact-form-card account-summary" aria-labelledby="profile-summary">
                <div className="account-avatar" aria-hidden="true">{initials || <UserRound size={28} />}</div>
                <h2 id="profile-summary">{user.fullName}</h2>
                <p className="account-email">{user.email}</p>
                <span className={`role-badge role-badge-${user.role.toLowerCase()}`}>{t(`roles.${user.role}`)}</span>
                <dl className="account-facts">
                    <div><dt>{t('profile.memberSince')}</dt><dd>{formatDate(user.createdAt, i18n.language)}</dd></div>
                </dl>
                {user.role === 'User' && <Link to="/booking" className="btn-nav-outline account-link"><CalendarDays size={16} /> {t('profile.mySessions')}</Link>}
            </section>

            <div className="account-forms">
                <form className="contact-form-card" onSubmit={saveDetails} noValidate>
                    <h2><UserRound size={20} /> {t('profile.detailsTitle')}</h2>
                    <StatusBox status={detailsStatus} />
                    <div className="form-group">
                        <label htmlFor="profile-name">{t('profile.name')}</label>
                        <input id="profile-name" value={fullName} maxLength={120} autoComplete="name" onChange={(e) => setFullName(e.target.value)} />
                    </div>
                    <div className="form-group">
                        <label htmlFor="profile-email">{t('profile.email')}</label>
                        <input id="profile-email" value={user.email} readOnly aria-describedby="profile-email-hint" />
                        <small id="profile-email-hint" className="account-hint">{t('profile.emailHint')}</small>
                    </div>
                    <button type="submit" className="btn-primary" disabled={!nameChanged || savingDetails}>{savingDetails ? t('profile.saving') : t('profile.save')}</button>
                </form>

                <form className="contact-form-card" onSubmit={savePassword} noValidate>
                    <h2><KeyRound size={20} /> {t('profile.passwordTitle')}</h2>
                    <StatusBox status={passwordStatus} />
                    <div className="form-group">
                        <label htmlFor="current-password">{t('profile.currentPassword')}</label>
                        <input id="current-password" type="password" autoComplete="current-password" value={passwords.current} onChange={(e) => setPasswords({ ...passwords, current: e.target.value })} />
                    </div>
                    <div className="form-group">
                        <label htmlFor="new-password">{t('profile.newPassword')}</label>
                        <input id="new-password" type="password" autoComplete="new-password" aria-describedby="new-password-hint" value={passwords.next} onChange={(e) => setPasswords({ ...passwords, next: e.target.value })} />
                        <small id="new-password-hint" className="account-hint">{t('profile.passwordRules')}</small>
                    </div>
                    <div className="form-group">
                        <label htmlFor="confirm-password">{t('profile.confirmPassword')}</label>
                        <input id="confirm-password" type="password" autoComplete="new-password" value={passwords.confirm} onChange={(e) => setPasswords({ ...passwords, confirm: e.target.value })} />
                    </div>
                    <button type="submit" className="btn-primary" disabled={!passwords.current || !passwords.next || savingPassword}>{savingPassword ? t('profile.saving') : t('profile.changePassword')}</button>
                </form>

                {/* Admin accounts are managed by the developers, so only clients can delete their account. */}
                {user.role === 'User' && <section className="contact-form-card account-danger" aria-labelledby="delete-title">
                    <h2 id="delete-title"><Trash2 size={20} /> {t('profile.deleteTitle')}</h2>
                    <p>{t('profile.deleteText')}</p>
                    <StatusBox status={deleteStatus} />
                    {!deleteOpen
                        ? <button type="button" className="btn-danger-outline" onClick={() => setDeleteOpen(true)}>{t('profile.deleteButton')}</button>
                        : <form onSubmit={confirmDelete} noValidate>
                            <div className="form-group">
                                <label htmlFor="delete-password">{t('profile.deleteConfirmLabel')}</label>
                                <input id="delete-password" type="password" autoComplete="current-password" value={deletePassword} onChange={(e) => setDeletePassword(e.target.value)} />
                            </div>
                            <div className="booking-actions">
                                <button type="submit" className="btn-danger" disabled={!deletePassword || deleting}>{deleting ? t('profile.deleting') : t('profile.deleteConfirm')}</button>
                                <button type="button" className="btn-nav-outline" disabled={deleting} onClick={() => { setDeleteOpen(false); setDeletePassword(''); setDeleteStatus(null); }}>{t('profile.cancel')}</button>
                            </div>
                        </form>}
                </section>}
            </div>
        </div>
    </div>;
}
