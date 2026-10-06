import { BrowserRouter, Routes, Route, Link, useLocation } from 'react-router-dom';
import { QuestBoard } from './features/quests/components/QuestBoard';
import { ProfilePage } from './pages/ProfilePage';
import { DashboardPage } from './pages/DashboardPage';
import { useQuestStore } from './features/quests/useQuestStore';
import { User, Swords, Users, BarChart } from 'lucide-react';
import { InvitePaywallModal } from './features/parties/components/InvitePaywallModal';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import './i18n'; 

function Layout({ children }: { children: React.ReactNode }) {
  const { t } = useTranslation();
  const userXp = useQuestStore(state => state.userXp);
  const location = useLocation();
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [currentParty, setCurrentParty] = useState('party1');
  
  const handleInvite = () => {
    // Simulating API call that returns ERR_PARTY_LIMIT_REACHED
    if (currentParty === 'party1') {
      setShowInviteModal(true);
    } else {
      alert("Invitación enviada");
    }
  };
  
  const getRank = (xp: number) => {
    if (xp < 100) return 'Novato';
    if (xp < 300) return 'Aventurero';
    if (xp < 1000) return 'Guerrero';
    return 'Leyenda';
  };

  return (
    <div className="min-h-screen bg-gray-900 text-white flex flex-col">
      <header className="p-4 border-b border-gray-800 bg-gray-950 flex justify-between items-center shadow-md">
        <div className="flex items-center gap-8">
          <h1 className="text-2xl font-bold text-amber-500 tracking-wider">⚔️ SPARNAROK</h1>
          <nav className="flex items-center gap-6 text-sm font-semibold">
            <Link to="/" className={`flex items-center gap-2 pb-1 ${location.pathname === '/' ? 'text-amber-400 border-b-2 border-amber-400' : 'text-gray-400 hover:text-white'}`}>
              <Swords size={18}/> Tablero
            </Link>
            <Link to="/profile" className={`flex items-center gap-2 pb-1 ${location.pathname === '/profile' ? 'text-amber-400 border-b-2 border-amber-400' : 'text-gray-400 hover:text-white'}`}>
              <User size={18}/> Perfil Habilidades
            </Link>
            <Link to="/party/dashboard" className={`flex items-center gap-2 pb-1 ${location.pathname === '/party/dashboard' ? 'text-amber-400 border-b-2 border-amber-400' : 'text-gray-400 hover:text-white'}`}>
              <BarChart size={18}/> Analytics
            </Link>
          </nav>
        </div>
        <div className="flex items-center gap-6">
          <select 
            value={currentParty}
            onChange={(e) => setCurrentParty(e.target.value)}
            className="bg-gray-900 border border-gray-700 text-gray-300 text-sm rounded-lg focus:ring-amber-500 focus:border-amber-500 block p-2"
          >
            <option value="party1">Gremio Frontend (Free)</option>
            <option value="party2">Ops Team (Pro)</option>
          </select>
          <button onClick={handleInvite} className="bg-gray-800 hover:bg-gray-700 text-gray-300 text-sm px-3 py-1.5 rounded-lg font-bold border border-gray-700 flex items-center gap-2 transition-colors">
            <Users size={16} /> {t('header.invite', 'Invitar')}
          </button>
          
          <div className="flex items-center gap-4 border-l border-gray-700 pl-6">
            <div className="text-sm font-bold text-amber-400">XP: {userXp}</div>
            <div className="text-sm font-semibold text-gray-400 bg-gray-800 px-3 py-1 rounded-full border border-gray-700">
              Rank: {getRank(userXp)}
            </div>
          </div>
        </div>
      </header>
      <main className="flex-1 overflow-hidden h-[calc(100vh-73px)]">
        {children}
      </main>
      
      {showInviteModal && <InvitePaywallModal onClose={() => setShowInviteModal(false)} />}
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<QuestBoard />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/party/dashboard" element={<DashboardPage />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  );
}
