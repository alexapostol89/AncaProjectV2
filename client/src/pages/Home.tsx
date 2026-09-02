import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import '../Css/Home.css';
import { ChevronLeft, ChevronRight, HeartHandshake, Sparkles, Sun, ArrowRight } from 'lucide-react';

const slides = [
    'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=1920&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1518495973542-4542c06a5843?q=80&w=1920&auto=format&fit=crop',
    'https://images.unsplash.com/photo-1470240731273-7821a6eeb6bd?q=80&w=1920&auto=format&fit=crop',
];

export default function Home() {
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
                            <span className="page-subtitle">Cabinet Individual</span>
                            <h1>Un spațiu de ascultare, acceptare și transformare</h1>
                            <p className="lead-text">
                                Misiunea mea este de a te însoți cu blândețe pe drumul cunoașterii de sine, oferindu-ți un mediu confidențial și lipsit de județ.
                            </p>
                            <p className="hero-description">
                                Fie că te confrunți cu stări de anxietate, treci printr-o tranziție dificilă sau pur și simplu îți dorești să înțelegi mai profund tiparele care te blochează, psihoterapia integrativă îți pune la dispoziție instrumentele necesare pentru a face o schimbare reală.
                            </p>
                            <div className="hero-cta-group" style={{ justifyContent: 'flex-start', marginTop: '1.5rem' }}>
                                <Link to="/contact" className="btn-primary">Programează o ședință</Link>
                                <Link to="/despre" className="btn-secondary">Despre mine</Link>
                            </div>
                        </div>
                    </div>
                </div>

                <button className="slider-btn prev" onClick={prevSlide} aria-label="Slide anterior">
                    <ChevronLeft size={24} />
                </button>
                <button className="slider-btn next" onClick={nextSlide} aria-label="Slide următor">
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
                    <h2>Cum te pot sprijini</h2>
                    <p>Fiecare călătorie terapeutică este unică și adaptată ritmului tău personal.</p>
                </div>

                <div className="grid-cards">
                    <div className="card">
                        <div className="card-icon"><HeartHandshake size={28} /></div>
                        <h3>Psihoterapie Individuală</h3>
                        <p>Explorarea emoțiilor, gestionarea anxietății, depășirea momentelor de cumpănă și recăpătarea clarității interioare.</p>
                        <Link to="/servicii" className="card-link">Detalii serviciu <ArrowRight size={16} /></Link>
                    </div>

                    <div className="card">
                        <div className="card-icon"><Sparkles size={28} /></div>
                        <h3>Dezvoltare Personală</h3>
                        <p>Îmbunătățirea relațiilor interpersonale, creșterea stimei de sine și construirea unor limite sănătoase.</p>
                        <Link to="/servicii" className="card-link">Detalii serviciu <ArrowRight size={16} /></Link>
                    </div>

                    <div className="card">
                        <div className="card-icon"><Sun size={28} /></div>
                        <h3>Consiliere & Suport</h3>
                        <p>Un spațiu cald, empatic și confidențial, dedicat înțelegerii dificultăților tale zilnice.</p>
                        <Link to="/servicii" className="card-link">Detalii serviciu <ArrowRight size={16} /></Link>
                    </div>
                </div>
            </section>
        </div>
    );
}