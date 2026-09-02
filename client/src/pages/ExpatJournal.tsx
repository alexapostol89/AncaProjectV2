import { Globe, Quote, Sparkles, CheckCircle2 } from 'lucide-react';
import '../Css/ExpatJournal.css';

const chapters = [
    "1. Identitate și transformare personală",
    "2. Viața de expat – adaptare și apartenență",
    "3. Relația cu sine",
    "4. Relațiile cu ceilalți",
    "5. Emoții, frici și vulnerabilități",
    "6. Dorul și legătura cu România",
    "7. Integrarea culturală",
    "8. Resurse, obiceiuri și spații de siguranță",
    "9. Reziliență și lecții personale",
    "10. Recunoștință și celebrare de sine"
];

const amazonLinks = [
    { country: "US", url: "https://a.co/d/eNDMW6w" },
    { country: "UK", url: "https://amzn.eu/d/2tNYnRU" },
    { country: "FR", url: "https://amzn.eu/d/diLuTgB" },
    { country: "ES", url: "https://amzn.eu/d/f45Fs1k" },
    { country: "DE", url: "https://amzn.eu/d/5b9K8cO" },
    { country: "IT", url: "https://amzn.eu/d/g1MEwvq" },
    { country: "NL", url: "https://amzn.eu/d/dfGMelB" },
    { country: "PL", url: "https://amzn.eu/d/fIovlyu" },
    { country: "CA", url: "https://a.co/d/3L0AhWu" }
];

export default function ExpatJournal() {
    return (
        <div className="page-content journal-page">
            {/* Header Section */}
            <div className="page-header">
                <span className="page-subtitle">Ghid de transformare & introspecție</span>
                <h1>Jurnalul femeii expat</h1>
                <p className="journal-tagline">Gânduri și stări ale unei femei ce înflorește departe de rădăcini</p>
            </div>

            {/* First Testimonial Highlight */}
            <div className="journal-hero-quote">
                <Quote className="quote-icon-top" size={36} />
                <p>
                    „Autoarea a creat un instrument care adaugă cu adevărat valoare vieții mele. Completarea acestui ghid se simte ca un ritual – un moment hygge sculptat din agitația zilnică, în care pot avea grijă de mine și îmi pot procesa emoțiile de expat. Pe lângă toate acestea, vine cu un grup uimitor de femei din întreaga lume – o comunitate cu care mă pot conecta la doar un clic distanță. Cel mai bun cadou pe care ți-l poți face 💯”
                </p>
                <span>(clientă amazon)</span>
            </div>

            {/* Storytelling Section */}
            <div className="journal-story-grid">
                <div className="journal-text-block">
                    <h2>De ce am scris acest jurnal</h2>
                    <p>
                        Am plecat din România entuziasmată pentru că mă săturasem să plâng, să trăiesc furie, vină. Am plecat pentru că nu mai puteam să rămân.
                    </p>
                    <p>
                        Dacă și tu ești o femeie care trăiește departe de țara în care s-a format ca om, dar în care nu se mai regăsea acolo, atunci știi despre ce vorbesc.
                    </p>
                    <p className="highlight-text">
                        Jurnalul femeii expat nu e un jurnal clasic. Nu e cu inimioare și afirmații motivaționale. E un spațiu terapeutic. Clar, sincer, incomod uneori, dar profund eliberator.
                    </p>
                    <p>
                        Un loc unde te poți așeza în sfârșit cu tine, fără să îți ceri scuze că simți prea mult sau că nu ești suficientă.
                    </p>
                    <p>
                        Conține întrebări care sapă în straturi adânci, exerciții gândite de o femeie psiholog care a plecat, s-a rupt de rădăcini, a plâns de dor și greu și a învățat să se strângă la loc. <strong>Nu promite vindecare. Promite că nu o să mai simți că ești singura.</strong>
                    </p>
                </div>

                <div className="journal-features-card">
                    <h3>Ce conține jurnalul?</h3>
                    <ul className="features-list">
                        <li><CheckCircle2 size={18} color="var(--primary)" /> În limba română</li>
                        <li><CheckCircle2 size={18} color="var(--primary)" /> 52 de teme terapeutice dedicate femeilor expat</li>
                        <li><CheckCircle2 size={18} color="var(--primary)" /> Întrebări despre emoții, relații, nevoi, dor și identitate</li>
                        <li><CheckCircle2 size={18} color="var(--primary)" /> Momente de introspecție și prezență cu tine</li>
                        <li><CheckCircle2 size={18} color="var(--primary)" /> Spațiu pentru recunoștință, reflecție și planuri de bine</li>
                    </ul>
                    <div className="target-audience">
                        <Sparkles size={20} />
                        <p>E pentru tine dacă înflorești departe de rădăcini. Un cadou sincer pentru orice femeie româncă expat sau pentru tine însăți.</p>
                    </div>
                </div>
            </div>

            {/* Second Testimonial */}
            <div className="journal-review-box">
                <p>
                    „Autoarea acestui jurnal pune importante întrebări legate de noua viață, identitate și percepția de sine. Jurnalul aprofundează și facilitează o analiză bună a situației actuale. Este un instrument excelent pentru a te regăsi sau pentru a te conecta cu (noul?) sine. Îl recomand cu căldură dacă ai nevoie de un ghid pentru a face ordine în gândurile, sentimentele și viața ta.”
                </p>
                <span className="reviewer-name">— Cristina-Maria Amorsen, clientă amazon</span>
            </div>

            {/* Deep Dive Section */}
            <div className="journal-deep-dive">
                <h3>Un loc de refugiu și ordonare interioară</h3>
                <p>
                    Dacă și tu ești femeie expat, știi deja că dincolo de peisaje frumoase și culturi noi, vin la pachet și dorul, fricile, întrebările fără răspuns, emoțiile greu de dus și nevoile pe care le tot amâni. Jurnalul acesta e locul tău de refugiu. Spațiul în care îți poți pune ordine în gânduri, emoții și dorințe, fără mască.
                </p>
                <p>
                    Nu e doar un jurnal în care să scrii ce-ai făcut azi. E un ghid pe 52 de săptămâni care te invită să te așezi cu tine, să-ți iei o pauză de la to-do list-ul vieții departe de rădăcini și să-ți întrebi sufletul cum se simte. Prin teme săptămânale, întrebări cu sens și momente de introspecție, jurnalul te ajută să îți înțelegi emoțiile, să identifici ce relații te hrănesc și ce te consumă, și să descoperi ce înseamnă „acasă” pentru tine, dincolo de coordonate pe hartă.
                </p>
            </div>

            {/* Psychologist Recommendation / Review */}
            <div className="psychologist-review-card">
                <div className="psy-content">
                    <Quote size={40} className="psy-quote-icon" />
                    <p>
                        „Citind jurnalul scris de Anca, am realizat cât de mult mi-aș fi dorit un astfel de instrument care să documenteze experiența mea de femeie expat, acum mai bine de 10 ani. Multe din trăirile mele de atunci s-au pierdut, iar când încerc să mi le reamintesc, realizez că percepția mea s-a modificat între timp... Prin acest jurnal, Anca reușește să cartografieze o călătorie extrem de profundă, trasând granițele unei noi identități și desenând reliefuri emoționale de o frumusețe fără margini. La capătul acestei călătorii te vei găsi pe tine, mult mai puternică, mai înțeleaptă și mai conștientă, iar tot ce ai consemnat în acest jurnal să fie cea mai frumoasă poveste de succes: A TA!”
                    </p>
                    <span className="psy-author">Roxana Dumitrașcu – psiholog</span>
                </div>
            </div>

            {/* Table of Contents */}
            <div className="journal-toc-section">
                <h3>Cuprins</h3>
                <div className="toc-grid">
                    <div className="toc-item"><span>Intro</span> Introducere: Despre jurnal, scopul său și gândurile autoarei</div>
                    {chapters.map((chap, idx) => (
                        <div key={idx} className="toc-item"><span>Cap</span> {chap}</div>
                    ))}
                    <div className="toc-item"><span>Outro</span> Final de jurnal & Reflecții de încheiere – cine sunt azi, după această călătorie?</div>
                </div>
            </div>

            {/* Purchase Call to Action */}
            <div className="journal-purchase-box">
                <h3>Comandă-l acum</h3>
                <p>Alege site-ul Amazon corespunzător țării în care locuiești:</p>
                <div className="amazon-links-grid">
                    {amazonLinks.map((item, idx) => (
                        <a key={idx} href={item.url} target="_blank" rel="noopener noreferrer" className="amazon-btn">
                            <Globe size={16} /> Amazon <strong>{item.country}</strong>
                        </a>
                    ))}
                </div>
            </div>
        </div>
    );
}