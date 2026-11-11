import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, HeartHandshake, Sparkles, Sun, ArrowRight } from 'lucide-react';
import BookingLink from '../components/BookingLink';
import '../Css/Home.css';

const slides = [
    'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=1920&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1518495973542-4542c06a5843?q=80&w=1920&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1470240731273-7821a6eeb6bd?q=80&w=1920&auto=format&fit=crop',
];

export default function Home() {
    const { t } = useTranslation();
    const [currentSlide, setCurrentSlide] = useState(0);

    useEffect(() => {
        const timer = setInterval(() => {
            setCurrentSlide((prev) => (prev + 1) % slides.length);
        }, 6000);
        return () => clearInterval(timer);
    }, []);

    const prevSlide = () => setCurrentSlide((prev) => (prev - 1 + slides.length) % slides.length);
    const nextSlide = () => setCurrentSlide((prev) => (prev + 1) % slides.length);

    const services = [
        { icon: <HeartHandshake size={28} />, title: t('home.individual'), desc: t('home.individualDesc') },
        { icon: <Sparkles size={28} />, title: t('home.growth'), desc: t('home.growthDesc') },
        { icon: <Sun size={28} />, title: t('home.counseling'), desc: t('home.counselingDesc') },
    ];

    return (
        <div className="page-container">
            {/* Hero carousel: background photos + card with picture and text */}
            <section className="hero-slider-container">
                {slides.map((img, index) => (
                    <div
                        key={index}
                        className={`slide ${index === currentSlide ? 'active' : ''}`}
                        style={{ backgroundImage: `url(${img})` }}
                    >
                        <div className="slide-overlay"></div>
                    </div>
                ))}

                <div className="hero-content-wrapper">
                    <div className="hero-card hero-split-card">
                        <div className="hero-card-img-wrapper">
                            <img src="/2.jpg" alt={t('home.photoAlt')} className="hero-card-img" />
                        </div>

                        <div className="hero-card-text-wrapper">
                            <span className="page-subtitle">{t('home.subtitle')}</span>
                            <h1>{t('home.title')}</h1>
                            <p className="lead-text">{t('home.lead')}</p>
                            <p className="hero-description">{t('home.description')}</p>
                            <div className="hero-cta-group" style={{ justifyContent: 'flex-start', marginTop: '1.5rem' }}>
                                <BookingLink className="btn-primary">{t('home.book')}</BookingLink>
                                <Link to="/despre" className="btn-secondary">{t('home.about')}</Link>
                            </div>
                        </div>
                    </div>
                </div>

                <button className="slider-btn prev" onClick={prevSlide} aria-label={t('home.prev')}>
                    <ChevronLeft size={24} />
                </button>
                <button className="slider-btn next" onClick={nextSlide} aria-label={t('home.next')}>
                    <ChevronRight size={24} />
                </button>

                <div className="slider-dots">
                    {slides.map((_, index) => (
                        <button
                            key={index}
                            className={`dot ${index === currentSlide ? 'active' : ''}`}
                            onClick={() => setCurrentSlide(index)}
                            aria-label={t('home.slide', { number: index + 1 })}
                        />
                    ))}
                </div>
            </section>

            {/* Services overview */}
            <section className="services-preview">
                <div className="section-header">
                    <h2>{t('home.support')}</h2>
                    <p>{t('home.journey')}</p>
                </div>

                <div className="grid-cards">
                    {services.map((service) => (
                        <div className="card" key={service.title}>
                            <div className="card-icon">{service.icon}</div>
                            <h3>{service.title}</h3>
                            <p>{service.desc}</p>
                            <Link to="/servicii" className="card-link">{t('home.details')} <ArrowRight size={16} /></Link>
                        </div>
                    ))}
                </div>
            </section>
        </div>
    );
}
