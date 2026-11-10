import { CheckCircle2, ShieldCheck, Video, Users } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useLanguage } from '../i18n';

export default function Services() {
    const { messages } = useLanguage();
    const t = messages.services;
    const services = [
        {
            title: 'Psihoterapie Individuală',
            icon: <ShieldCheck size={32} />,
            desc: 'Sesiuni 1-la-1 pentru gestionarea anxietății, depresiei, atacurilor de panică, doliului sau problemelor de stimă de sine.',
            details: ['Durată: 50 minute', 'Format: Fizic sau Online', 'Frecvență: Săptămânală']
        },
        {
            title: 'Dezvoltare Personală & Autocunoaștere',
            icon: <Users size={32} />,
            desc: 'Pentru cei care vor să își descopere potențialul, să își înțeleagă tiparele comportamentale și să construiască relații mai armonioase.',
            details: ['Durată: 50 minute', 'Stabilire obiective clare', 'Exerciții practice & reflecție']
        },
        {
            title: 'Consiliere Psihologică Online',
            icon: <Video size={32} />,
            desc: 'Ședințe flexibile prin video-conferință (WhatsApp / Zoom), din confortul casei tale, indiferent de oraș sau țară.',
            details: ['Durată: 50 minute', 'Flexibilitate orară']
        }
    ];

    return (
        <div className="page-content">
            <div className="page-header">
                <span className="page-subtitle">{t.subtitle}</span>
                <h1>{t.title}</h1>
            </div>

            <div className="grid-cards">
                {services.map((s, idx) => (
                    <div key={idx} className="card service-card">
                        <div className="card-icon">{s.icon}</div>
                        <h3>{t.items[idx][0]}</h3>
                        <p>{t.items[idx][1]}</p>
                        <ul className="service-features">
                            {s.details.map((_, i) => (
                                <li key={i}><CheckCircle2 size={16} color="var(--primary)" /> {t.items[idx][i + 2]}</li>
                            ))}
                        </ul>
                        <Link to="/contact" className="btn-primary" style={{ display: 'inline-block', textAlign: 'center', marginTop: '1.5rem' }}>
                            {t.book}
                        </Link>
                    </div>
                ))}
            </div>
        </div>
    );
}
