import { Globe, Quote, Sparkles, CheckCircle2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import '../Css/ExpatJournal.css';

const amazonLinks = [
    { country: 'US', url: 'https://a.co/d/eNDMW6w' },
    { country: 'UK', url: 'https://amzn.eu/d/2tNYnRU' },
    { country: 'FR', url: 'https://amzn.eu/d/diLuTgB' },
    { country: 'ES', url: 'https://amzn.eu/d/f45Fs1k' },
    { country: 'DE', url: 'https://amzn.eu/d/5b9K8cO' },
    { country: 'IT', url: 'https://amzn.eu/d/g1MEwvq' },
    { country: 'NL', url: 'https://amzn.eu/d/dfGMelB' },
    { country: 'PL', url: 'https://amzn.eu/d/fIovlyu' },
    { country: 'CA', url: 'https://a.co/d/3L0AhWu' },
];

export default function ExpatJournal() {
    const { t } = useTranslation();
    const why = t('journal.why', { returnObjects: true });
    const features = t('journal.features', { returnObjects: true });
    const refuge = t('journal.refuge', { returnObjects: true });
    const chapters = t('journal.chapters', { returnObjects: true });

    return (
        <div className="page-content journal-page">
            <div className="page-header">
                <span className="page-subtitle">{t('journal.subtitle')}</span>
                <h1>{t('journal.title')}</h1>
                <p className="journal-tagline">{t('journal.tagline')}</p>
            </div>

            {/* First testimonial */}
            <div className="journal-hero-quote">
                <Quote className="quote-icon-top" size={36} />
                <p>{t('journal.quote1')}</p>
                <span>{t('journal.quote1Source')}</span>
            </div>

            {/* Why I wrote it + what's inside */}
            <div className="journal-story-grid">
                <div className="journal-text-block">
                    <h2>{t('journal.whyTitle')}</h2>
                    {why.map((paragraph, idx) => (
                        // The 3rd paragraph is highlighted; the last one ends with the bold promise.
                        <p key={idx} className={idx === 2 ? 'highlight-text' : undefined}>
                            {paragraph}
                            {idx === why.length - 1 && <> <strong>{t('journal.whyPromise')}</strong></>}
                        </p>
                    ))}
                </div>

                <div className="journal-features-card">
                    <h3>{t('journal.featuresTitle')}</h3>
                    <ul className="features-list">
                        {features.map((feature) => (
                            <li key={feature}><CheckCircle2 size={18} color="var(--primary)" /> {feature}</li>
                        ))}
                    </ul>
                    <div className="target-audience">
                        <Sparkles size={20} />
                        <p>{t('journal.audience')}</p>
                    </div>
                </div>
            </div>

            {/* Second testimonial */}
            <div className="journal-review-box">
                <p>{t('journal.quote2')}</p>
                <span className="reviewer-name">{t('journal.quote2Author')}</span>
            </div>

            {/* Deep dive */}
            <div className="journal-deep-dive">
                <h3>{t('journal.refugeTitle')}</h3>
                {refuge.map((paragraph, idx) => <p key={idx}>{paragraph}</p>)}
            </div>

            {/* Psychologist's recommendation */}
            <div className="psychologist-review-card">
                <div className="psy-content">
                    <Quote size={40} className="psy-quote-icon" />
                    <p>{t('journal.psyQuote')}</p>
                    <span className="psy-author">{t('journal.psyAuthor')}</span>
                </div>
            </div>

            {/* Table of contents */}
            <div className="journal-toc-section">
                <h3>{t('journal.tocTitle')}</h3>
                <div className="toc-grid">
                    <div className="toc-item"><span>{t('journal.tocIntroLabel')}</span> {t('journal.tocIntro')}</div>
                    {chapters.map((chapter) => (
                        <div key={chapter} className="toc-item"><span>{t('journal.chapterLabel')}</span> {chapter}</div>
                    ))}
                    <div className="toc-item"><span>{t('journal.tocOutroLabel')}</span> {t('journal.tocOutro')}</div>
                </div>
            </div>

            {/* Buy */}
            <div className="journal-purchase-box">
                <h3>{t('journal.buyTitle')}</h3>
                <p>{t('journal.buyText')}</p>
                <div className="amazon-links-grid">
                    {amazonLinks.map((item) => (
                        <a key={item.country} href={item.url} target="_blank" rel="noopener noreferrer" className="amazon-btn">
                            <Globe size={16} /> Amazon <strong>{item.country}</strong>
                        </a>
                    ))}
                </div>
            </div>
        </div>
    );
}
