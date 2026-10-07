import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Radar, RadarChart, PolarGrid, PolarAngleAxis, PolarRadiusAxis, ResponsiveContainer, Tooltip } from 'recharts';
import { Trophy, Wand2 } from 'lucide-react';
import { SessionZeroModal } from '../features/parties/components/SessionZeroModal';

interface Performer {
  userId: string;
  username: string;
  xpGained: number;
  isSessionZeroCompleted: boolean;
}

export const DashboardPage = () => {
  const { t } = useTranslation();
  
  // Mock data as if it came from the API (for demonstration/fast testing)
  const distributionData = [
    { subject: 'Backend', A: 5000, fullMark: 10000 },
    { subject: 'Frontend', A: 3000, fullMark: 10000 },
    { subject: 'DevOps', A: 1000, fullMark: 10000 },
    { subject: 'Arquitectura', A: 2000, fullMark: 10000 },
    { subject: 'Seguridad', A: 500, fullMark: 10000 },
    { subject: 'QA', A: 1500, fullMark: 10000 },
  ];

  const [topPerformers, setTopPerformers] = useState<Performer[]>([
    { userId: '1', username: 'Alex', xpGained: 1200, isSessionZeroCompleted: true },
    { userId: '2', username: 'Mery', xpGained: 950, isSessionZeroCompleted: true },
    { userId: '3', username: 'Sam', xpGained: 800, isSessionZeroCompleted: false },
  ]);

  const [activeSessionZeroUser, setActiveSessionZeroUser] = useState<string | null>(null);

  const handleSaveSessionZero = (userId: string, xpMap: Record<string, number>) => {
    // Optimistic Update
    setTopPerformers(prev => prev.map(p => {
      if (p.userId === userId) {
        return { ...p, isSessionZeroCompleted: true, xpGained: p.xpGained + Object.values(xpMap).reduce((a, b) => a + b, 0) };
      }
      return p;
    }));
    setActiveSessionZeroUser(null);
  };

  return (
    <div className="w-full h-full flex flex-col p-6 bg-gray-900 overflow-y-auto">
      <div className="mb-8">
        <h2 className="text-3xl font-bold text-amber-500">{t('dashboard.title', 'Manager Analytics')}</h2>
        <p className="text-gray-400">{t('dashboard.subtitle', 'Huella técnica y rendimiento de tu Party.')}</p>
      </div>

      <div className="flex gap-8">
        {/* Gráfico de Radar */}
        <div className="flex-1 bg-gray-950 p-6 rounded-xl border border-gray-800 shadow-xl">
          <h3 className="text-xl font-bold text-white mb-6 text-center">{t('dashboard.radarTitle', 'Distribución de Habilidades del Equipo')}</h3>
          <div className="h-96 w-full">
            <ResponsiveContainer width="100%" height="100%">
              <RadarChart cx="50%" cy="50%" outerRadius="80%" data={distributionData}>
                <PolarGrid stroke="#374151" />
                <PolarAngleAxis dataKey="subject" tick={{ fill: '#9ca3af', fontSize: 12 }} />
                <PolarRadiusAxis angle={30} domain={[0, 10000]} tick={false} axisLine={false} />
                <Tooltip contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151' }} />
                <Radar name="XP Acumulada" dataKey="A" stroke="#f59e0b" fill="#f59e0b" fillOpacity={0.5} />
              </RadarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Top Héroes */}
        <div className="w-96 flex flex-col gap-4">
          <h3 className="text-xl font-bold text-white mb-2 flex items-center gap-2">
            <Trophy className="text-amber-500" /> {t('dashboard.topHeroes', 'Top Performers (30 Días)')}
          </h3>
          {topPerformers.map((hero, idx) => (
            <div key={hero.userId} className="bg-gray-950 p-4 rounded-xl border border-gray-800 flex items-center justify-between shadow-lg">
              <div className="flex items-center gap-4">
                <div className={`w-10 h-10 rounded-full flex items-center justify-center font-bold text-gray-900 ${idx === 0 ? 'bg-amber-400' : idx === 1 ? 'bg-gray-300' : 'bg-amber-700'}`}>
                  {idx + 1}
                </div>
                <span className="font-bold text-white text-lg">{hero.username}</span>
              </div>
              <div className="flex items-center gap-3">
                {!hero.isSessionZeroCompleted && (
                  <button 
                    onClick={() => setActiveSessionZeroUser(hero.userId)}
                    className="text-xs bg-indigo-600 hover:bg-indigo-500 text-white px-2 py-1 rounded flex items-center gap-1 font-bold transition-colors"
                  >
                    <Wand2 size={12} /> {t('dashboard.calibrate', 'Calibrar')}
                  </button>
                )}
                <div className="text-emerald-400 font-bold">
                  +{hero.xpGained} XP
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
      
      {activeSessionZeroUser && (
        <SessionZeroModal 
          userId={activeSessionZeroUser} 
          onClose={() => setActiveSessionZeroUser(null)} 
          onSave={(xpMap) => handleSaveSessionZero(activeSessionZeroUser, xpMap)}
        />
      )}
    </div>
  );
};
