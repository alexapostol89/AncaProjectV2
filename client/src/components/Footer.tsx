import { Heart } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAudience } from './useAudience';

export default function Footer() {
    const { t } = useTranslation();
    const isAdmin = useAudience() === 'Admin';
    return (
        <footer className="footer">
            <div className="footer-content">
                <div className="footer-brand">
                    <div className="brand-logo">Ψ</div>
                    <p className="footer-tagline">
                        Anca Alexandra Berbunschi <br />
                        <span>{t('footer.tagline')}</span>
                    </p>
                </div>

                <div className="footer-links">
                    {isAdmin ? <>
                        <Link to="/admin/utilizatori">{t('nav.adminUsers')}</Link>
                        <Link to="/admin/programari">{t('nav.adminBookings')}</Link>
                        <Link to="/admin/calendar">{t('nav.adminCalendar')}</Link>
                    </> : <>
                        <Link to="/">{t('nav.home')}</Link>
                        <Link to="/despre">{t('nav.about')}</Link>
                        <Link to="/servicii">{t('nav.services')}</Link>
                        <Link to="/studii">{t('nav.education')}</Link>
                        <Link to="/preturi">{t('nav.pricing')}</Link>
                        <Link to="/jurnal">{t('nav.journal')}</Link>
                        <Link to="/contact">{t('nav.contact')}</Link>
                    </>}
                </div>
            </div>

            <div className="footer-bottom">
                <p>© {new Date().getFullYear()} Anca Alexandra Berbunschi. {t('footer.copyright')} <Heart size={14} className="heart-icon" /></p>
            </div>
        </footer>
    );
}
