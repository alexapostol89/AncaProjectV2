import { Mail, Phone, MapPin, Clock, Send } from 'lucide-react';
import '../Css/Contact.css';
export default function Contact() {
    return (
        <div className="page-content">
            <div className="page-header">
                <span className="page-subtitle">Fă primul pas</span>
                <h1>Contact & Programări</h1>
            </div>

            <div className="contact-wrapper">
                <div className="contact-info">
                    <h2>Hai să stăm de vorbă</h2>
                    <p>
                        Dacă simți că a sosit momentul să începi terapia sau ai întrebări despre cum funcționează ședințele, nu ezita să-mi scrii.
                    </p>

                    <div className="contact-details">
                        <div className="contact-detail-item">
                            <div className="contact-icon"><Mail size={20} /></div>
                            <div>
                                <span className="detail-label">Email</span>
                                <p>psihologancaalexandra@gmail.com</p>
                            </div>
                        </div>

                        <div className="contact-detail-item">
                            <div className="contact-icon"><Phone size={20} /></div>
                            <div>
                                <span className="detail-label">Telefon / WhatsApp</span>
                                <p>+4 0745 435 146</p>
                            </div>
                        </div>

                        <div className="contact-detail-item">
                            <div className="contact-icon"><MapPin size={20} /></div>
                            <div>
                                <span className="detail-label">Locație</span>
                                <p>Cabinet: Iași, România / Ședințe Online pe Zoom / WhatsApp</p>
                            </div>
                        </div>

                        <div className="contact-detail-item">
                            <div className="contact-icon"><Clock size={20} /></div>
                            <div>
                                <span className="detail-label">Program</span>
                                <p>Luni – Vineri: 09:00 – 19:00</p>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="contact-form-card">
                    <h3>Trimite un mesaj</h3>
                    <form onSubmit={(e) => { e.preventDefault(); alert('Mesaj trimis cu succes!'); }}>
                        <div className="form-group">
                            <label>Nume și Prenume</label>
                            <input type="text" placeholder="Ex: Maria Popescu" required />
                        </div>

                        <div className="form-group">
                            <label>Email</label>
                            <input type="email" placeholder="maria@exemplu.ro" required />
                        </div>

                        <div className="form-group">
                            <label>Telefon</label>
                            <input type="tel" placeholder="07xxxxxxxx" />
                        </div>

                        <div className="form-group">
                            <label>Tip ședință preferată</label>
                            <select>
                                <option>Online (Google Meet / Zoom)</option>
                                <option>Fizic (La Cabinet)</option>
                                <option>Nu sunt sigur(ă), aș dori o recomandare</option>
                            </select>
                        </div>

                        <div className="form-group">
                            <label>Mesaj / Ce te aduce la terapie?</label>
                            <textarea rows={4} placeholder="Scrie câteva gânduri..."></textarea>
                        </div>

                        <button type="submit" className="btn-primary" style={{ width: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}>
                            <Send size={18} /> Trimite Mesajul
                        </button>
                    </form>
                </div>
            </div>
        </div>
    );
}