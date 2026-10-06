
import { QuestBoard } from './features/quests/components/QuestBoard';
import './i18n'; // Ensure i18n is initialized

function App() {
  return (
    <div className="min-h-screen bg-gray-900 text-white">
      <header className="p-4 border-b border-gray-800 bg-gray-950 flex justify-between items-center shadow-md">
        <h1 className="text-2xl font-bold text-amber-500 tracking-wider">⚔️ SPARNAROK</h1>
        <div className="text-sm font-semibold text-gray-400 bg-gray-800 px-3 py-1 rounded-full border border-gray-700">
          Rank: Novato
        </div>
      </header>
      <main className="h-[calc(100vh-73px)]">
        <QuestBoard />
      </main>
    </div>
  );
}

export default App;
