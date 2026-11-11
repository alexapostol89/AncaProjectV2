import { UserCheck, Compass, Smile } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import BookingLink from '../components/BookingLink';
import '../Css/About.css';

export default function About() {
    const { t } = useTranslation();
    const story = t('about.story', { returnObjects: true });
    const offerings = t('about.offerings', { returnObjects: true });
    const values = [
        { icon: <UserCheck size={20} />, title: t('about.value1'), desc: t('about.value1d') },
        { icon: <Compass size={20} />, title: t('about.value2'), desc: t('about.value2d') },
        { icon: <Smile size={20} />, title: t('about.value3'), desc: t('about.value3d') },
    ];

    return (
        <div className="page-content about-page">
            <div className="page-header">
                <span className="page-subtitle">{t('about.subtitle')}</span>
            </div>

            {/* Photo + short bio */}
            <div className="about-grid">
                <div className="about-image-card">
                    <img src="/1.jpg" alt={t('about.photoAlt')} className="about-portrait-img" />
                </div>

                <div className="about-text-card">
                    <h2>{t('about.hello')}</h2>
                    <p className="lead-text">{t('about.lead')}</p>
                    <p>{t('about.intro')}</p>

                    <div className="values-list">
                        {values.map((value) => (
                            <div className="value-item" key={value.title}>
                                <div className="value-icon">{value.icon}</div>
                                <div>
                                    <h4>{value.title}</h4><p>{value.desc}</p>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Personal story */}
            <section className="about-story-section">
                <div className="story-card">
                    {story.map((paragraph, index) => (
                        <p key={index} className={index === story.length - 1 ? 'story-highlight' : undefined}>{paragraph}</p>
                    ))}
                </div>
            </section>

            {/* Testimonial */}
            <section className="about-testimonial-section">
                <blockquote className="testimonial-quote">
                    <p>{t('about.testimonial')}</p>
                    <footer>— S.</footer>
                </blockquote>
            </section>

            {/* Ways to work together */}
            <section className="about-offerings-section">
                <h3>{t('about.choose')}</h3>
                <div className="offerings-grid">
                    {offerings.map((offering) => (
                        <div className="offering-item" key={offering}>
                            <span className="bullet-emoji">🟡</span>
                            <p>{offering}</p>
                        </div>
                    ))}
                </div>
            </section>

            {/* Call to action */}
            <section className="about-cta-section">
                <div className="cta-box">
                    <h3>{t('about.next')}</h3>
                    <p>{t('about.nextText')}</p>
                    <p className="cta-highlight">{t('about.free')}</p>
                    <div className="cta-buttons">
                        <BookingLink className="btn-primary">{t('about.book')}</BookingLink>
                    </div>
                </div>
            </section>
        </div>
    );
}
