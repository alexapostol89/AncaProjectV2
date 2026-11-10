import { UserCheck, Compass, Smile } from 'lucide-react';
import { Link } from 'react-router-dom';
import '../Css/About.css';
import { useLanguage } from '../i18n';

export default function About() {
    const { messages } = useLanguage();
    const t = messages.about;
    return (
        <div className="page-content about-page">
            {/* Header */}
            <div className="page-header">
                <span className="page-subtitle">{t.subtitle}</span>
               
            </div>

            {/* Top Grid: Photo + Initial Bio */}
            <div className="about-grid">
                <div className="about-image-card">
                    <img
                        src="/1.jpg"
                        alt="Anca Alexandra Berbunschi - Psiholog"
                        className="about-portrait-img"
                    />
                </div>

                <div className="about-text-card">
                    <h2>{t.hello}</h2>
                    <p className="lead-text">{t.lead}</p>
                    <p>{t.intro}</p>

                    <div className="values-list">
                        <div className="value-item">
                            <div className="value-icon"><UserCheck size={20} /></div>
                            <div>
                                <h4>{t.value1}</h4><p>{t.value1d}</p>
                            </div>
                        </div>
                        <div className="value-item">
                            <div className="value-icon"><Compass size={20} /></div>
                            <div>
                                <h4>{t.value2}</h4><p>{t.value2d}</p>
                            </div>
                        </div>
                        <div className="value-item">
                            <div className="value-icon"><Smile size={20} /></div>
                            <div>
                                <h4>{t.value3}</h4><p>{t.value3d}</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Extended Personal Story Section */}
            <section className="about-story-section">
                <div className="story-card">
                    {t.story.map((paragraph, index) => <p key={index} className={index === t.story.length - 1 ? 'story-highlight' : undefined}>{paragraph}</p>)}
                </div>
            </section>

            {/* Testimonial Block */}
            <section className="about-testimonial-section">
                <blockquote className="testimonial-quote">
                    <p>„M-am simțit ascultată, ghidată, susținută, validată și înțeleasă. Pentru prima dată nu era cineva care mă judeca, ci cineva care mă vedea exact așa cum sunt.”</p>
                    <footer>— S.</footer>
                </blockquote>
            </section>

            {/* Services & How to Work Together */}
            <section className="about-offerings-section">
                <h3>{t.choose}</h3>
                <div className="offerings-grid">
                    <div className="offering-item">
                        <span className="bullet-emoji">🟡</span>
                        <p>{t.offerings[0]}</p>
                    </div>
                    <div className="offering-item">
                        <span className="bullet-emoji">🟡</span>
                        <p>{t.offerings[1]}</p>
                    </div>
                    <div className="offering-item">
                        <span className="bullet-emoji">🟡</span>
                        <p>{t.offerings[2]}</p>
                    </div>
                </div>
            </section>

            {/* Final Call to Action */}
            <section className="about-cta-section">
                <div className="cta-box">
                    <h3>{t.next}</h3>
                    <p>{t.nextText}</p>
                    <p className="cta-highlight">{t.free}</p>
                    <div className="cta-buttons">
                        <Link to="/contact" className="btn-primary">{t.book}</Link>
                    </div>
                </div>
            </section>
        </div>
    );
}
