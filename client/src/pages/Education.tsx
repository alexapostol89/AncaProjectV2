import { useState, useRef } from 'react';
import { GraduationCap, Award, BookOpen, HeartPulse, ChevronLeft, ChevronRight } from 'lucide-react';
import '../Css/Education.css';

const educationItems = [
    {
        icon: <GraduationCap size={24} />,
        date: "Formare Profesională",
        title: "Psihoterapeut în formare în psihoterapie integrativă",
        org: "Asociația Română de Psihoterapie Integrativă (psihoterapieintegrativa.ro)",
        desc: "Specializare avansată în metode integrative pentru înțelegerea și vindecarea proceselor psihice."
    },
    {
        icon: <BookOpen size={24} />,
        date: "Studii Universitare de Master",
        title: "Masterat în Psihologie",
        org: "Facultatea de Psihologie și Științele Educației — Universitatea “Alexandru Ioan Cuza” din Iași",
        desc: "Studii aprofundate în domeniul psihologiei și al cercetării clinice."
    },
    {
        icon: <BookOpen size={24} />,
        date: "Studii Universitare de Licență",
        title: "Licențiată în Psihologie",
        org: "Facultatea de Psihologie, Științele Educației și Asistență Socială — Universitatea “Petre Andrei” din Iași",
        desc: "Baze teoretice și practice solide în științele psihologice și asistență umană."
    },
    {
        icon: <Award size={24} />,
        date: "Atestate de Liberă Practică",
        title: "Cod personal: 29283",
        org: "Colegiul Psihologilor din România (COPSI)",
        desc: "Atestate eliberate în Psihologie clinică & Psihologie educațională, consiliere școlară și vocațională."
    },
    {
        icon: <HeartPulse size={24} />,
        date: "Specializare / Curs Avansat",
        title: "Compassionate Inquiry. Vindecarea Rănilor Traumatice Profunde",
        org: "Speaker: Gabor Maté (PESI)",
        desc: "Abordare terapeutică axată pe înțelegerea traumelor profunde și compasiune ca instrument de transformare interioară."
    }
];

export default function Education() {
    const sliderRef = useRef(null);

    const scrollLeft = () => {
        if (sliderRef.current) {
            sliderRef.current.scrollBy({ left: -380, behavior: 'smooth' });
        }
    };

    const scrollRight = () => {
        if (sliderRef.current) {
            sliderRef.current.scrollBy({ left: 380, behavior: 'smooth' });
        }
    };

    return (
        <div className="page-content education-page">
            <div className="page-header">
                <span className="page-subtitle">Pregătire și acreditări</span>
                <h1>Studii & Formare Profesională</h1>
            </div>

            {/* Horizontal Sliding Track Section */}
            <div className="edu-slider-wrapper">
                <button className="edu-slider-btn prev" onClick={scrollLeft} aria-label="Slide stânga">
                    <ChevronLeft size={24} />
                </button>

                <div className="edu-track" ref={sliderRef}>
                    {educationItems.map((item, idx) => (
                        <div className="timeline-item slide-card" key={idx}>
                            <div className="timeline-icon">{item.icon}</div>
                            <div className="timeline-content">
                                <span className="timeline-date">{item.date}</span>
                                <h3>{item.title}</h3>
                                <p className="timeline-org">{item.org}</p>
                                <p>{item.desc}</p>
                            </div>
                        </div>
                    ))}
                </div>

                <button className="edu-slider-btn next" onClick={scrollRight} aria-label="Slide dreapta">
                    <ChevronRight size={24} />
                </button>
            </div>

            {/* Bottom Area: Full-Size Certificate Image */}
            <div className="edu-certificate-section">
                <h2>Atestat de Liberă Practică</h2>
                <div className="certificate-frame">
                    <img
                        src="/3.jpg"
                        alt="Atestat de libera practica - Anca Alexandra Berbunschi"
                        className="full-certificate-img"
                    />
                </div>
            </div>
        </div>
    );
}