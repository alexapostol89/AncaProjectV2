import { Check, Sparkles } from 'lucide-react';
import { Link } from 'react-router-dom';
import '../Css/Pricing.css';

export default function Pricing() {
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
                <span className="page-subtitle">Tarife transparente</span>
                <h1>Prețuri & Pachete</h1>
            </div>

            <div className="pricing-grid">
                {plans.map((p, idx) => (
                    <div key={idx} className={`pricing-card ${p.popular ? 'popular' : ''}`}>
                        {p.popular && <div className="popular-badge"><Sparkles size={14} /> Cel mai recomandat</div>}
                        <h3>{p.name}</h3>
                        <p className="pricing-desc">{p.description}</p>
                        <div className="pricing-number">
                            <span className="amount">{p.price}</span>
                            <span className="currency">RON</span>
                            <span className="period">{p.period}</span>
                        </div>

                        <ul className="pricing-features">
                            {p.features.map((feat, i) => (
                                <li key={i}><Check size={16} color="var(--primary)" /> {feat}</li>
                            ))}
                        </ul>

                        <Link to="/contact" className={p.popular ? 'btn-primary' : 'btn-secondary'} style={{ width: '100%', textAlign: 'center', display: 'block' }}>
                            Alege acest pachet
                        </Link>
                    </div>
                ))}
            </div>
        </div>
    );
}