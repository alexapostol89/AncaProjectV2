import { NavLink } from 'react-router-dom';
import { useLanguage } from '../i18n';
import LanguageSelector from './LanguageSelector';

export default function Navbar() {
    const { messages } = useLanguage();
    return (
        <nav className="navbar">
            <NavLink to="/" className="nav-brand">
                <div className="brand-logo">Ψ</div>
                <div>
                    <div className="brand-name">Anca Alexandra Berbunschi</div>
                    <span className="brand-title">Psiholog • Psihoterapeut Integrativ</span>
                </div>
            </NavLink>

            <ul className="nav-links">
                <li>
                    <NavLink to="/" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        {messages.nav.home}
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/despre" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        {messages.nav.about}
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/servicii" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        {messages.nav.services}
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/studii" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        {messages.nav.education}
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/preturi" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        {messages.nav.pricing}
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/jurnal" className={({ isActive }) => (isActive ? 'btn-nav active-link' : 'btn-nav')}>
                        {messages.nav.journal}
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/contact" className={({ isActive }) => (isActive ? 'active-link' : '')} style={{ fontWeight: 600 }}>
                        {messages.nav.contact}
                    </NavLink>
                </li>
            </ul>
            <LanguageSelector className="navbar-language" />
        </nav>
    );
}
