import { NavLink } from 'react-router-dom';

export default function Navbar() {
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
                        Acasă
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/despre" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        Despre mine
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/servicii" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        Servicii
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/studii" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        Studii
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/preturi" className={({ isActive }) => (isActive ? 'active-link' : '')}>
                        Prețuri
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/jurnal" className={({ isActive }) => (isActive ? 'btn-nav active-link' : 'btn-nav')}>
                        Jurnalul femeii expat
                    </NavLink>
                </li>
                <li>
                    <NavLink to="/contact" className={({ isActive }) => (isActive ? 'active-link' : '')} style={{ fontWeight: 600 }}>
                        Contact
                    </NavLink>
                </li>
            </ul>
        </nav>
    );
}