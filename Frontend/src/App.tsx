import { BrowserRouter, Routes, Route, Link, useLocation, useNavigate } from 'react-router-dom';
import { QuestBoard } from './features/quests/components/QuestBoard';
import { ProfilePage } from './pages/ProfilePage';
import { DashboardPage } from './pages/DashboardPage';
import { CreateQuestForm } from './features/quests/components/CreateQuestForm';
import { useQuestStore } from './features/quests/useQuestStore';
import { User as UserIcon, Swords, Users, BarChart, LogOut, Copy, Check, Shield } from 'lucide-react';
import { InviteAllyModal } from './features/parties/components/InviteAllyModal';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from './features/auth/useAuthStore';
import './i18n';

function Layout({ children }: { children: React.ReactNode }) {
  const { t } = useTranslation();
  const userXp = useQuestStore(state => state.userXp);
  const location = useLocation();
  const navigate = useNavigate();

  const username = useAuthStore(state => state.username);
  const partyId = useAuthStore(state => state.partyId);
  const partyName = useAuthStore(state => state.partyName);
  const logout = useAuthStore(state => state.logout);

  const [showInviteModal, setShowInviteModal] = useState(false);
  const [copiedPartyId, setCopiedPartyId] = useState(false);

  const handleCopyPartyId = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (partyId) {
      navigator.clipboard.writeText(partyId);
      setCopiedPartyId(true);
      setTimeout(() => setCopiedPartyId(false), 2000);
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const getRank = (xp: number) => {
    if (xp < 100) return 'Novato';
    if (xp < 300) return 'Aventurero';
    if (xp < 1000) return 'Guerrero';
    return 'Leyenda';
  };

  const heroInitial = (username || 'H').charAt(0).toUpperCase();

  return (
    <div className="min-h-screen bg-gray-900 text-white flex flex-col">
      <header className="px-3 sm:px-5 py-2.5 sm:py-3 border-b border-gray-800 bg-gray-950 flex justify-between items-center shadow-lg sticky top-0 z-40">
        {/* Brand & Nav */}
        <div className="flex items-center gap-3 sm:gap-7">
          <Link to="/" className="flex items-center gap-2 group shrink-0">
            <span className="text-xl sm:text-2xl transform group-hover:scale-110 transition-transform">⚔️</span>
            <span className="text-lg sm:text-xl font-black text-amber-500 tracking-wider hidden sm:inline">SPARNAROK</span>
          </Link>
          <nav className="flex items-center gap-2 sm:gap-5 text-sm font-semibold">
            <Link 
              to="/" 
              className={`flex items-center gap-2 py-1 px-2.5 rounded-md transition-all ${
                location.pathname === '/' 
                  ? 'text-amber-400 bg-amber-500/10 border border-amber-500/20' 
                  : 'text-gray-400 hover:text-white hover:bg-gray-800/50'
              }`}
              title={t('header.board', 'Tablero')}
            >
              <Swords size={16}/> <span className="hidden md:block">{t('header.board', 'Tablero')}</span>
            </Link>
            <Link 
              to="/profile" 
              className={`flex items-center gap-2 py-1 px-2.5 rounded-md transition-all ${
                location.pathname === '/profile' 
                  ? 'text-amber-400 bg-amber-500/10 border border-amber-500/20' 
                  : 'text-gray-400 hover:text-white hover:bg-gray-800/50'
              }`}
              title={t('header.profile', 'Perfil Habilidades')}
            >
              <UserIcon size={16}/> <span className="hidden md:block">{t('header.profile', 'Perfil Habilidades')}</span>
            </Link>
            <Link 
              to="/party/dashboard" 
              className={`flex items-center gap-2 py-1 px-2.5 rounded-md transition-all ${
                location.pathname === '/party/dashboard' 
                  ? 'text-amber-400 bg-amber-500/10 border border-amber-500/20' 
                  : 'text-gray-400 hover:text-white hover:bg-gray-800/50'
              }`}
              title={t('header.analytics', 'Analytics')}
            >
              <BarChart size={16}/> <span className="hidden md:block">{t('header.analytics', 'Analytics')}</span>
            </Link>
          </nav>
        </div>

        {/* Right Section: Party Tenant info + Hero Profile + Actions */}
        <div className="flex items-center gap-2 sm:gap-4">
          {/* Party (Tenant) Indicator */}
          <div className="hidden sm:flex items-center gap-2 bg-gray-900 border border-gray-800 rounded-lg px-2.5 sm:px-3 py-1.5 text-xs max-w-xs">
            <Shield size={14} className="text-amber-400 shrink-0" />
            <div className="flex flex-col min-w-0">
              <span className="font-bold text-gray-200 truncate max-w-[100px] md:max-w-[140px]" title={partyName || 'Party'}>
                {partyName || 'Party Activa'}
              </span>
              <span className="font-mono text-[10px] text-gray-400 truncate max-w-[100px] md:max-w-[140px]" title={partyId || ''}>
                ID: {partyId ? `${partyId.slice(0, 8)}...` : 'N/A'}
              </span>
            </div>
            {partyId && (
              <button
                onClick={handleCopyPartyId}
                title="Copiar Party ID"
                className="text-gray-400 hover:text-amber-400 ml-1 p-1 rounded hover:bg-gray-800 transition-colors shrink-0"
              >
                {copiedPartyId ? <Check size={13} className="text-emerald-400" /> : <Copy size={13} />}
              </button>
            )}
          </div>

          {/* Invite Ally Button */}
          <button 
            onClick={() => setShowInviteModal(true)} 
            className="bg-gray-800 hover:bg-gray-700 text-amber-400 hover:text-amber-300 text-xs px-2.5 sm:px-3 py-1.5 sm:py-2 rounded-lg font-bold border border-amber-500/30 flex items-center gap-1.5 transition-all shadow-sm shrink-0"
          >
            <Users size={14} />
            <span className="hidden sm:inline">{t('header.invite', 'Invitar')}</span>
          </button>

          {/* Hero Identity Badge (Avatar + Name + Rank) */}
          <div className="flex items-center gap-2 sm:gap-3 border-l border-gray-800 pl-2 sm:pl-4">
            <div className="flex items-center gap-2.5">
              <div className="relative shrink-0">
                <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-amber-600 via-amber-500 to-amber-300 text-gray-950 font-black flex items-center justify-center text-sm shadow-md border border-amber-400/50">
                  {heroInitial}
                </div>
                <div className="w-2.5 h-2.5 rounded-full bg-emerald-500 absolute -bottom-0.5 -right-0.5 border-2 border-gray-950" title="En línea" />
              </div>
              <div className="hidden sm:flex flex-col max-w-xs">
                <div className="text-xs font-bold text-white flex items-center gap-1 truncate max-w-[110px] md:max-w-none">
                  <span className="truncate">{username || 'Héroe Anónimo'}</span>
                </div>
                <div className="text-[10px] font-semibold text-amber-400 truncate max-w-[110px] md:max-w-none">
                  XP: {userXp} • {getRank(userXp)}
                </div>
              </div>
            </div>

            {/* Logout Button */}
            <button
              onClick={handleLogout}
              title={t('header.logout', 'Cerrar Sesión')}
              className="text-gray-400 hover:text-red-400 p-1.5 rounded-lg hover:bg-gray-800 transition-colors ml-1 shrink-0"
            >
              <LogOut size={16} />
            </button>
          </div>
        </div>
      </header>

      <main className="flex-1 overflow-hidden h-[calc(100vh-61px)]">
        {children}
      </main>
      
      {showInviteModal && <InviteAllyModal onClose={() => setShowInviteModal(false)} />}
    </div>
  );
}

import { LoginForm } from './features/auth/components/LoginForm';
import { RegisterForm } from './features/auth/components/RegisterForm';
import { ProtectedRoute } from './components/ProtectedRoute';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginForm />} />
        <Route path="/register" element={<RegisterForm />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<Layout><QuestBoard /></Layout>} />
          <Route path="/profile" element={<Layout><ProfilePage /></Layout>} />
          <Route path="/party/dashboard" element={<Layout><DashboardPage /></Layout>} />
          <Route path="/quests/new" element={<Layout><CreateQuestForm /></Layout>} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
