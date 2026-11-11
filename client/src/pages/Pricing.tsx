import { Check, Sparkles } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import BookingLink from '../components/BookingLink';
import '../Css/Pricing.css';

/** Prices are data, not text: same in every language. Same order as pricing.plans in the locale files. */
const prices = [
    { amount: '200', popular: false },
    { amount: '720', popular: true },
    { amount: '1360', popular: false },
];

export default function Pricing() {
    const { t } = useTranslation();
    const plans = t('pricing.plans', { returnObjects: true });

    return (
        <div className="page-content">
            <div className="page-header">
                <span className="page-subtitle">{t('pricing.subtitle')}</span>
                <h1>{t('pricing.title')}</h1>
            </div>

            <div className="pricing-grid">
                {plans.map((plan, idx) => {
                    const { amount, popular } = prices[idx];
                    return (
                        <div key={plan.name} className={`pricing-card ${popular ? 'popular' : ''}`}>
                            {popular && <div className="popular-badge"><Sparkles size={14} /> {t('pricing.popular')}</div>}
                            <h3>{plan.name}</h3>
                            <p className="pricing-desc">{plan.description}</p>
                            <div className="pricing-number">
                                <span className="amount">{amount}</span>
                                <span className="currency">RON</span>
                                <span className="period">{plan.period}</span>
                            </div>

                            <ul className="pricing-features">
                                {plan.features.map((feature) => (
                                    <li key={feature}><Check size={16} color="var(--primary)" /> {feature}</li>
                                ))}
                            </ul>

                            <BookingLink className={popular ? 'btn-primary' : 'btn-secondary'} style={{ width: '100%', textAlign: 'center', display: 'block' }}>
                                {t('pricing.choose')}
                            </BookingLink>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}
