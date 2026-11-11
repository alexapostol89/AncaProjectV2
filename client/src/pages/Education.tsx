import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { GraduationCap, Award, BookOpen, HeartPulse, ChevronLeft, ChevronRight } from 'lucide-react';
import '../Css/Education.css';

/** Icons in the same order as education.items in the locale files. */
const icons = [
    <GraduationCap size={24} />,
    <BookOpen size={24} />,
    <BookOpen size={24} />,
    <Award size={24} />,
    <HeartPulse size={24} />,
];

export default function Education() {
    const { t } = useTranslation();
    const items = t('education.items', { returnObjects: true });
    const sliderRef = useRef<HTMLDivElement>(null);

    const scrollBy = (direction: 1 | -1) => {
        const track = sliderRef.current;
        if (!track) return;
        // Scroll by one card (width of the first card + gap), whatever the screen size.
        const card = track.firstElementChild as HTMLElement | null;
        const step = card ? card.offsetWidth + 24 : 380;
        track.scrollBy({ left: direction * step, behavior: 'smooth' });
    };

    return (
        <div className="page-content education-page">
            <div className="page-header">
                <span className="page-subtitle">{t('education.subtitle')}</span>
                <h1>{t('education.title')}</h1>
            </div>

            {/* Horizontal slider */}
            <div className="edu-slider-wrapper">
                <button className="edu-slider-btn prev" onClick={() => scrollBy(-1)} aria-label={t('education.prev')}>
                    <ChevronLeft size={24} />
                </button>

                <div className="edu-track" ref={sliderRef}>
                    {items.map((item, idx) => (
                        <div className="timeline-item slide-card" key={item.title}>
                            <div className="timeline-icon">{icons[idx]}</div>
                            <div className="timeline-content">
                                <span className="timeline-date">{item.date}</span>
                                <h3>{item.title}</h3>
                                <p className="timeline-org">{item.org}</p>
                                <p>{item.desc}</p>
                            </div>
                        </div>
                    ))}
                </div>

                <button className="edu-slider-btn next" onClick={() => scrollBy(1)} aria-label={t('education.next')}>
                    <ChevronRight size={24} />
                </button>
            </div>

            {/* Certificate */}
            <div className="edu-certificate-section">
                <h2>{t('education.certificate')}</h2>
                <div className="certificate-frame">
                    <img src="/3.jpg" alt={t('education.certificateAlt')} className="full-certificate-img" />
                </div>
            </div>
        </div>
    );
}
