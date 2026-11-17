import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LogIn, LogOut, Menu, UserRound, X } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import LanguageSelector from './LanguageSelector';
import { usePendingBookings } from './usePendingBookings';
import type en from '../locales/en.json';

type NavItem = { to: string; key: `nav.${keyof typeof en.nav}`; highlight?: boolean; badge?: boolean };

/** What visitors and clients see: the practice's public pages. */
const publicLinks: NavItem[] = [
    { to: '/', key: 'nav.home' },
    { to: '/despre', key: 'nav.about' },
    { to: '/servicii', key: 'nav.services' },
    { to: '/studii', key: 'nav.education' },
    { to: '/preturi', key: 'nav.pricing' },
    { to: '/jurnal', key: 'nav.journal', highlight: true },
    { to: '/contact', key: 'nav.contact' },
];

/** What the admin sees: only her work tools (the public pages are hidden for her). */
const adminLinks: NavItem[] = [
    { to: '/admin/utilizatori', key: 'nav.adminUsers' },
    { to: '/admin/programari', key: 'nav.adminBookings', badge: true },
    { to: '/admin/calendar', key: 'nav.adminCalendar' },
];

/**
 * Top bar. Desktop: links in a row. Tablet/phone: links move into a dropdown
 * opened with the ☰ button (see the 1100px / 600px media queries in App.css).
 */
export default function Navbar() {
    const { t } = useTranslation();
    const { user, logout } = useAuth();
    const [menuOpen, setMenuOpen] = useState(false);
    const closeMenu = () => setMenuOpen(false);
    const isAdmin = user?.role === 'Admin';
    const pendingBookings = usePendingBookings();
    const pendingLabel = t('nav.pendingRequests', { count: pendingBookings });

    return (
        <nav className="navbar">
            <NavLink to={isAdmin ? '/admin/calendar' : '/'} className="nav-brand" onClick={closeMenu}>
                <div className="brand-logo">Ψ</div>
                <div>
                    <div className="brand-name">Anca Alexandra Berbunschi</div>
                    <span className="brand-title">Psiholog • Psihoterapeut Integrativ</span>
                </div>
            </NavLink>

            <ul id="main-menu" className={`nav-links ${menuOpen ? 'open' : ''}`}>
                {(isAdmin ? adminLinks : publicLinks).map((link) => (
                    <li key={link.to}>
                        <NavLink
                            to={link.to}
                            end={link.to === '/'}
                            onClick={closeMenu}
                            className={({ isActive }) => [link.highlight ? 'btn-nav' : '', isActive ? 'active-link' : ''].join(' ').trim()}
                            title={link.badge && pendingBookings > 0 ? pendingLabel : undefined}
                        >
                            {t(link.key)}
                            {link.badge && pendingBookings > 0 && (
                                <span className="nav-link-badge" aria-label={pendingLabel}>{pendingBookings > 99 ? '99+' : pendingBookings}</span>
                            )}
                        </NavLink>
                    </li>
                ))}
                {/* On phones the language flags live inside the menu (no room in the bar). */}
                <li className="menu-language">
                    <LanguageSelector />
                </li>
            </ul>

            <div className="nav-actions">
                <LanguageSelector className="bar-language" />
                {user ? (
                    <div className="nav-user">
                        <NavLink
                            to="/profil"
                            className={({ isActive }) => `nav-profile ${isActive ? 'active' : ''}`.trim()}
                            onClick={closeMenu}
                            aria-label={t('nav.profile')}
                            title={`${t('nav.profile')} · ${user.email}`}
                        >
                            <UserRound size={16} /> <span className="nav-user-name">{user.fullName.split(' ')[0]}</span>
                        </NavLink>
                        <button type="button" className="btn-nav-outline" onClick={() => { logout(); closeMenu(); }} aria-label={t('nav.logout')} title={t('nav.logout')}>
                            <LogOut size={16} /> <span className="nav-action-label">{t('nav.logout')}</span>
                        </button>
                    </div>
                ) : (
                    <NavLink to="/autentificare" className="btn-nav-outline" onClick={closeMenu} aria-label={t('nav.login')} title={t('nav.login')}>
                        <LogIn size={16} /> <span className="nav-action-label">{t('nav.login')}</span>
                    </NavLink>
                )}
                <button
                    type="button"
                    className="menu-toggle"
                    onClick={() => setMenuOpen((open) => !open)}
                    aria-expanded={menuOpen}
                    aria-controls="main-menu"
                    aria-label={menuOpen ? t('nav.closeMenu') : t('nav.openMenu')}
                >
                    {menuOpen ? <X size={22} /> : <Menu size={22} />}
                    {/* Links are hidden in the dropdown on small screens, so show waiting requests on the ☰ button too. */}
                    {!menuOpen && pendingBookings > 0 && <span className="nav-badge menu-toggle-badge" aria-hidden="true">{pendingBookings > 99 ? '99+' : pendingBookings}</span>}
                </button>
            </div>
        </nav>
    );
}
