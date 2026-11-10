import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import '../Css/Home.css';
import { ChevronLeft, ChevronRight, HeartHandshake, Sparkles, Sun, ArrowRight } from 'lucide-react';
import { useLanguage } from '../i18n';

const slides = [
    'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=1920&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1518495973542-4542c06a5843?q=80&w=1920&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1470240731273-7821a6eeb6bd?q=80&w=1920&auto=format&fit=crop',
];

export default function Home() {
    const { messages } = useLanguage();
    const t = messages.home;
    const [currentSlide, setCurrentSlide] = useState(0);

    useEffect(() => {
        const timer = setInterval(() => {
            setCurrentSlide((prev) => (prev + 1) % slides.length);
        }, 6000);
        return () => clearInterval(timer);
    }, []);

    const prevSlide = () => {
        setCurrentSlide((prev) => (prev - 1 + slides.length) % slides.length);
    };

    const nextSlide = () => {
        setCurrentSlide((prev) => (prev + 1) % slides.length);
    };

    return (
        <div className="page-container">
            {/* Hero Carousel with Picture + Text Split Card */}
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
                        {/* Left Side: Picture 2.jpg */}
                        <div className="hero-card-img-wrapper">
                            <img src="/2.jpg" alt="Anca - Psihoterapeut" className="hero-card-img" />
                        </div>

                        {/* Right Side: Text & Actions */}
                        <div className="hero-card-text-wrapper">
                            <span className="page-subtitle">{t.subtitle}</span>
                            <h1>{t.title}</h1>
                            <p className="lead-text">{t.lead}</p>
                            <p className="hero-description">{t.description}</p>
                            <div className="hero-cta-group" style={{ justifyContent: 'flex-start', marginTop: '1.5rem' }}>
                                <Link to="/contact" className="btn-primary">{t.book}</Link>
                                <Link to="/despre" className="btn-secondary">{t.about}</Link>
                            </div>
                        </div>
                    </div>
                </div>

                <button className="slider-btn prev" onClick={prevSlide} aria-label={t.prev}>
                    <ChevronLeft size={24} />
                </button>
                <button className="slider-btn next" onClick={nextSlide} aria-label={t.next}>
                    <ChevronRight size={24} />
                </button>

                <div className="slider-dots">
                    {slides.map((_, index) => (
                        <button
                            key={index}
                            className={`dot ${index === currentSlide ? 'active' : ''}`}
                            onClick={() => setCurrentSlide(index)}
                            aria-label={`Slide ${index + 1}`}
                        />
                    ))}
                </div>
            </section>

            {/* Services Overview */}
            <section className="services-preview">
                <div className="section-header">
                    <h2>{t.support}</h2>
                    <p>{t.journey}</p>
                </div>

                <div className="grid-cards">
                    <div className="card">
                        <div className="card-icon"><HeartHandshake size={28} /></div>
                        <h3>{t.individual}</h3>
                        <p>{t.individualDesc}</p>
                        <Link to="/servicii" className="card-link">{t.details} <ArrowRight size={16} /></Link>
                    </div>

                    <div className="card">
                        <div className="card-icon"><Sparkles size={28} /></div>
                        <h3>{t.growth}</h3>
                        <p>{t.growthDesc}</p>
                        <Link to="/servicii" className="card-link">{t.details} <ArrowRight size={16} /></Link>
                    </div>

                    <div className="card">
                        <div className="card-icon"><Sun size={28} /></div>
                        <h3>{t.counseling}</h3>
                        <p>{t.counselingDesc}</p>
                        <Link to="/servicii" className="card-link">{t.details} <ArrowRight size={16} /></Link>
                    </div>
                </div>
            </section>
        </div>
    );
}
