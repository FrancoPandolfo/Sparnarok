import { SkillTree } from '../features/profile/components/SkillTree';

export const ProfilePage = () => {
  return (
    <div className="w-full h-full flex flex-col">
      <div className="p-4 bg-gray-950 border-b border-gray-800">
        <h2 className="text-xl font-bold text-amber-500">Árbol de Habilidades (Constelación)</h2>
        <p className="text-sm text-gray-400">Invierte tu XP en desbloquear nuevas ramas.</p>
      </div>
      <div className="flex-1">
        <SkillTree />
      </div>
    </div>
  );
};
