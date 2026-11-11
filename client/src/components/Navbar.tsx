import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LogIn, LogOut, Menu, UserRound, X } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import LanguageSelector from './LanguageSelector';

const links = [
    { to: '/', key: 'nav.home' },
    { to: '/despre', key: 'nav.about' },
    { to: '/servicii', key: 'nav.services' },
    { to: '/studii', key: 'nav.education' },
    { to: '/preturi', key: 'nav.pricing' },
    { to: '/jurnal', key: 'nav.journal', highlight: true },
    { to: '/contact', key: 'nav.contact' },
] as const;

/**
 * Top bar. Desktop: links in a row. Tablet/phone: links move into a dropdown
 * opened with the ☰ button (see the 1100px / 600px media queries in App.css).
 */
export default function Navbar() {
    const { t } = useTranslation();
    const { user, logout } = useAuth();
    const [menuOpen, setMenuOpen] = useState(false);
    const closeMenu = () => setMenuOpen(false);

    return (
        <nav className="navbar">
            <NavLink to="/" className="nav-brand" onClick={closeMenu}>
                <div className="brand-logo">Ψ</div>
                <div>
                    <div className="brand-name">Anca Alexandra Berbunschi</div>
                    <span className="brand-title">Psiholog • Psihoterapeut Integrativ</span>
                </div>
            </NavLink>

            <ul id="main-menu" className={`nav-links ${menuOpen ? 'open' : ''}`}>
                {links.map((link) => (
                    <li key={link.to}>
                        <NavLink
                            to={link.to}
                            end={link.to === '/'}
                            onClick={closeMenu}
                            className={({ isActive }) => [('highlight' in link) ? 'btn-nav' : '', isActive ? 'active-link' : ''].join(' ').trim()}
                        >
                            {t(link.key)}
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
                        <span className="nav-user-name" title={user.email}>
                            <UserRound size={16} /> {user.fullName.split(' ')[0]}
                        </span>
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
                </button>
            </div>
        </nav>
    );
}
