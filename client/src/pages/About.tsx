import { UserCheck, Compass, Smile, Sparkles, Calendar, MessageSquare, CheckCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import '../Css/About.css';

export default function About() {
    return (
        <div className="page-content about-page">
            {/* Header */}
            <div className="page-header">
                <span className="page-subtitle">Ce trebuie să știi despre mine</span>
               
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
                    <h2>Bună, sunt Anca Alexandra Berbunschi</h2>
                    <p className="lead-text">
                        Sunt psiholog și psihoterapeut integrativ în formare, iubitoare de natură, pisici și conversații autentice care aduc claritate și sens vieții.
                    </p>
                    <p>
                        Cred cu tărie că fiecare om deține în interior resursele necesare vindecării, însă uneori avem nevoie de un ghid și de o prezență caldă care să ne lumineze drumul prin momentele de confuzie.
                    </p>

                    <div className="values-list">
                        <div className="value-item">
                            <div className="value-icon"><UserCheck size={20} /></div>
                            <div>
                                <h4>Spațiu Fără Judecată</h4>
                                <p>Confidențialitate totală, acceptare necondiționată și empatie autentică.</p>
                            </div>
                        </div>
                        <div className="value-item">
                            <div className="value-icon"><Compass size={20} /></div>
                            <div>
                                <h4>Abordare Integrativă</h4>
                                <p>Tehnici flexibile adaptate nevoilor tale emoționale și ritmului tău unic.</p>
                            </div>
                        </div>
                        <div className="value-item">
                            <div className="value-icon"><Smile size={20} /></div>
                            <div>
                                <h4>Relație Terapeutică Autentică</h4>
                                <p>Construim o alianță bazată pe încredere, siguranță și respect reciproc.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Extended Personal Story Section */}
            <section className="about-story-section">
                <div className="story-card">
                    <p>
                        Am 33 ani și îmi place vârsta mea. Am două pisici adoptate și le iubesc de leșin.
                    </p>
                    <p>
                        Psihologia a fost a doua șansă pe care mi-am oferit-o și așa m-am regăsit pe mine. Îmi place să mă dezvolt personal și profesional și, deși sunt mândră de evoluția mea ca om, tot am zile în care mă enervez, mă sperii sau demoralizez și reacționez altfel decât eficient, dar asta mă face om și de fiecare dată fac în așa fel încât să nu îi fac altuia ce mie nu îmi place.
                    </p>
                    <p>
                        În ședințe mă vei găsi prezentă cu un bagaj de informații pe care le actualizez constant din surse valide științific, pregătită să te țin cu toate stările și gândurile tale. Da, te voi ține bine și când vei simți că te prăbușești. Am fost și eu acolo de multe ori și pentru că știu cum se simte și ce nevoie există, am adunat teoria, tehnicile și metodele pe care le știu pentru a practica profesia asta.
                    </p>
                    <p>
                        Sunt onestă, directă atunci când e nevoie și cu grijă pentru omul din fața mea. Nu te voi aburi și nici nu îți voi promite vindecare miraculoasă în X timp, dar mă voi asigura că te înțelegi, că ceea ce gândești și faci nu este distructiv pentru tine, și că la finalul colaborării noastre, vei fi mai bine și îți vei spune că a meritat tot ce ai investit.
                    </p>
                    <p className="story-highlight">
                        Ce mai trebuie să știi e că eu îți voi recomanda și te voi încuraja să lucrezi cu tine și între ședințele noastre. Altfel, nu vei simți mare lucru.
                    </p>
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
                <h3>Alege ce este potrivit pentru tine</h3>
                <div className="offerings-grid">
                    <div className="offering-item">
                        <span className="bullet-emoji">🟡</span>
                        <p>Ședințe individuale online (de oriunde) și fizic în Iași</p>
                    </div>
                    <div className="offering-item">
                        <span className="bullet-emoji">🟡</span>
                        <p>Abonament intensiv de dezvoltare personală prin email</p>
                    </div>
                    <div className="offering-item">
                        <span className="bullet-emoji">🟡</span>
                        <p>Articole, resurse gratuite și jurnal ghidat pentru femei expat (RO & EN)</p>
                    </div>
                </div>
            </section>

            {/* Final Call to Action */}
            <section className="about-cta-section">
                <div className="cta-box">
                    <h3>Să facem pasul următor</h3>
                    <p>
                        Dacă simți că te afli într-un punct în care nu mai vrei să duci singur tot ce te apasă, sunt aici.
                    </p>
                    <p className="cta-highlight">
                        Programează o discuție gratuită de 15 minute, fără obligații, în care să mă cunoști și să afli dacă rezonezi cu stilul meu.
                    </p>
                    <div className="cta-buttons">
                        <Link to="/contact" className="btn-primary">Programează 15 min gratuite</Link>
                    </div>
                </div>
            </section>
        </div>
    );
}