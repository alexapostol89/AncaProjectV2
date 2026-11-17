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
import Login from './pages/Login';
import Register from './pages/Register';
import Booking from './pages/Booking';
import AdminBookings from './pages/AdminBookings';
import AdminUsers from './pages/AdminUsers';
import AdminCalendar from './pages/AdminCalendar';
import Profile from './pages/Profile';
import "./Css/App.css";
import { AuthProvider } from './context/AuthContext';
import RequireRole from './components/RequireRole';

export default function App() {
    return (
        <AuthProvider>
        <Router>
            <div className="app-layout">
                <Navbar />
                <main className="main-content">
                    <Routes>
                        <Route path="/" element={<RequireRole allow={['Guest', 'User']}><Home /></RequireRole>} />
                        <Route path="/despre" element={<RequireRole allow={['Guest', 'User']}><About /></RequireRole>} />
                        <Route path="/servicii" element={<RequireRole allow={['Guest', 'User']}><Services /></RequireRole>} />
                        <Route path="/studii" element={<RequireRole allow={['Guest', 'User']}><Education /></RequireRole>} />
                        <Route path="/preturi" element={<RequireRole allow={['Guest', 'User']}><Pricing /></RequireRole>} />
                        <Route path="/contact" element={<RequireRole allow={['Guest', 'User']}><Contact /></RequireRole>} />
                        <Route path="/booking" element={<RequireRole allow={['User']}><Booking /></RequireRole>} />
                        <Route path="/profil" element={<RequireRole allow={['User', 'Admin']}><Profile /></RequireRole>} />
                        <Route path="/admin/utilizatori" element={<RequireRole allow={['Admin']}><AdminUsers /></RequireRole>} />
                        <Route path="/admin/programari" element={<RequireRole allow={['Admin']}><AdminBookings /></RequireRole>} />
                        <Route path="/admin/calendar" element={<RequireRole allow={['Admin']}><AdminCalendar /></RequireRole>} />
                        <Route path="/jurnal" element={<RequireRole allow={['Guest', 'User']}><ExpatJournal /></RequireRole>} />
                        <Route path="/autentificare" element={<Login />} />
                        <Route path="/inregistrare" element={<Register />} />
                    </Routes>
                </main>
                <Footer />
            </div>
        </Router>
        </AuthProvider>
    );
}
