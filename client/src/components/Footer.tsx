import { Heart } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useLanguage } from '../i18n';

export default function Footer() {
    const { messages } = useLanguage();
    return (
        <footer className="footer">
            <div className="footer-content">
                <div className="footer-brand">
                    <div className="brand-logo">Ψ</div>
                    <p className="footer-tagline">
                        Anca Alexandra Berbunschi <br />
                        <span>{messages.footer.tagline}</span>
                    </p>
                </div>

                <div className="footer-links">
                    <Link to="/">{messages.nav.home}</Link>
                    <Link to="/despre">{messages.nav.about}</Link>
                    <Link to="/servicii">{messages.nav.services}</Link>
                    <Link to="/studii">{messages.nav.education}</Link>
                    <Link to="/preturi">{messages.nav.pricing}</Link>
                    <Link to="/jurnal">{messages.nav.journal}</Link>
                    <Link to="/contact">{messages.nav.contact}</Link>
                </div>
            </div>

            <div className="footer-bottom">
                <p>© {new Date().getFullYear()} Anca Alexandra Berbunschi. {messages.footer.copyright} <Heart size={14} className="heart-icon" /></p>
            </div>
        </footer>
    );
}
