import { Heart } from 'lucide-react';
import { Link } from 'react-router-dom';

export default function Footer() {
    return (
        <footer className="footer">
            <div className="footer-content">
                <div className="footer-brand">
                    <div className="brand-logo">Ψ</div>
                    <p className="footer-tagline">
                        Anca Alexandra Berbunschi <br />
                        <span>Cabinet Individual de Psihologie</span>
                    </p>
                </div>

                <div className="footer-links">
                    <Link to="/">Acasă</Link>
                    <Link to="/despre">Despre mine</Link>
                    <Link to="/servicii">Servicii</Link>
                    <Link to="/studii">Studii</Link>
                    <Link to="/preturi">Prețuri</Link>
                    <Link to="/jurnal">Jurnalul femeii expat</Link>
                    <Link to="/contact">Contact</Link>
                </div>
            </div>

            <div className="footer-bottom">
                <p>© {new Date().getFullYear()} Anca Alexandra Berbunschi. Creat cu <Heart size={14} className="heart-icon" /> pentru echilibru interior.</p>
            </div>
        </footer>
    );
}