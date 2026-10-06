import { BrowserRouter, Routes, Route, Link, useLocation } from 'react-router-dom';
import { QuestBoard } from './features/quests/components/QuestBoard';
import { ProfilePage } from './pages/ProfilePage';
import { useQuestStore } from './features/quests/useQuestStore';
import { User, Swords } from 'lucide-react';
import './i18n'; 

function Layout({ children }: { children: React.ReactNode }) {
  const userXp = useQuestStore(state => state.userXp);
  const location = useLocation();
  
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
          </nav>
        </div>
        <div className="flex items-center gap-4">
          <div className="text-sm font-bold text-amber-400">XP: {userXp}</div>
          <div className="text-sm font-semibold text-gray-400 bg-gray-800 px-3 py-1 rounded-full border border-gray-700">
            Rank: {getRank(userXp)}
          </div>
        </div>
      </header>
      <main className="flex-1 overflow-hidden h-[calc(100vh-73px)]">
        {children}
      </main>
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
        </Routes>
      </Layout>
    </BrowserRouter>
  );
}
