import { Check, Sparkles } from 'lucide-react';
import { Link } from 'react-router-dom';
import '../Css/Pricing.css';
import { useLanguage } from '../i18n';

export default function Pricing() {
    const { messages } = useLanguage();
    const t = messages.pricing;
    const plans = [
        {
            name: 'Ședință Individuală',
            price: '200',
            period: '/ ședință (50 min)',
            description: 'Ideală pentru începerea procesului terapeutic sau întâlniri ocazionale.',
            features: [
                'Durată 50 minute',
                'Format fizic sau online',
                'Evaluare inițială & plan terapeutic',
                'Confidențialitate completă'
            ],
            popular: false
        },
        {
            name: 'Pachet 4 Ședințe (Abonament)',
            price: '720',
            period: '/ lună (4 ședințe)',
            description: 'Cea mai recomandată opțiune pentru continuitate și rezultate durabile.',
            features: [
                '4 ședințe a câte 50 minute',
                'Interval orar fix garantat săptămânal',
                'Economisești 10% din tarif',
                'Materiale de lucru și reflecție incluse',
                'Suport pe email între ședințe'
            ],
            popular: true
        },
        {
            name: 'Pachet 8 Ședințe',
            price: '1360',
            period: '/ 2 luni (8 ședințe)',
            description: 'Pentru un proces terapeutic profund și stabil pe termen mediu.',
            features: [
                '8 ședințe a câte 50 minute',
                'Economisești 15%',
                'Prioritate la programări',
                'Flexibilitate reprogramare 24h înainte'
            ],
            popular: false
        }
    ];

    return (
        <div className="page-content">
            <div className="page-header">
                <span className="page-subtitle">{t.subtitle}</span>
                <h1>{t.title}</h1>
            </div>

            <div className="pricing-grid">
                {plans.map((p, idx) => (
                    <div key={idx} className={`pricing-card ${p.popular ? 'popular' : ''}`}>
                        {p.popular && <div className="popular-badge"><Sparkles size={14} /> {t.popular}</div>}
                        <h3>{t.plans[idx][0]}</h3>
                        <p className="pricing-desc">{t.plans[idx][2]}</p>
                        <div className="pricing-number">
                            <span className="amount">{p.price}</span>
                            <span className="currency">RON</span>
                            <span className="period">{t.plans[idx][1]}</span>
                        </div>

                        <ul className="pricing-features">
                            {p.features.map((_, i) => (
                                <li key={i}><Check size={16} color="var(--primary)" /> {t.plans[idx][3][i]}</li>
                            ))}
                        </ul>

                        <Link to="/contact" className={p.popular ? 'btn-primary' : 'btn-secondary'} style={{ width: '100%', textAlign: 'center', display: 'block' }}>
                            {t.choose}
                        </Link>
                    </div>
                ))}
            </div>
        </div>
    );
}
