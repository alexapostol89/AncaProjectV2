import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Navbar from './components/Navbar';
import Footer from './components/Footer';
import Home from './pages/Home';
import About from './pages/About';
import Services from './pages/Services';
import Education from './pages/Education';
import Pricing from './pages/Pricing';
import Contact from './pages/Contact';
import ExpatJournal from './pages/ExpatJournal';
import "./Css/App.css";
import { LanguageProvider } from './i18n';

export default function App() {
    return (
        <LanguageProvider>
        <Router>
            <div className="app-layout">
                <Navbar />
                <main className="main-content">
                    <Routes>
                        <Route path="/" element={<Home />} />
                        <Route path="/despre" element={<About />} />
                        <Route path="/servicii" element={<Services />} />
                        <Route path="/studii" element={<Education />} />
                        <Route path="/preturi" element={<Pricing />} />
                        <Route path="/contact" element={<Contact />} />
                        <Route path="/jurnal" element={<ExpatJournal />} />
                    </Routes>
                </main>
                <Footer />
            </div>
        </Router>
        </LanguageProvider>
    );
}
