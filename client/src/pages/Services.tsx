import { CheckCircle2, ShieldCheck, Video, Users } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import BookingLink from '../components/BookingLink';

/** Icons in the same order as services.items in the locale files. */
const icons = [<ShieldCheck size={32} />, <Users size={32} />, <Video size={32} />];

export default function Services() {
    const { t } = useTranslation();
    const services = t('services.items', { returnObjects: true });

    return (
        <div className="page-content">
            <div className="page-header">
                <span className="page-subtitle">{t('services.subtitle')}</span>
                <h1>{t('services.title')}</h1>
            </div>

            <div className="grid-cards">
                {services.map((service, idx) => (
                    <div key={service.title} className="card service-card">
                        <div className="card-icon">{icons[idx]}</div>
                        <h3>{service.title}</h3>
                        <p>{service.description}</p>
                        <ul className="service-features">
                            {service.details.map((detail) => (
                                <li key={detail}><CheckCircle2 size={16} color="var(--primary)" /> {detail}</li>
                            ))}
                        </ul>
                        <BookingLink className="btn-primary" style={{ display: 'inline-block', textAlign: 'center', marginTop: '1.5rem' }}>
                            {t('services.book')}
                        </BookingLink>
                    </div>
                ))}
            </div>
        </div>
    );
}
