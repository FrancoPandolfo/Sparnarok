import { Draggable } from '@hello-pangea/dnd';
import type { Quest } from '../types';

export const QuestCard = ({ quest, index }: { quest: Quest; index: number }) => {
  const totalXp = quest.rewards?.reduce((acc, r) => acc + r.xpAmount, 0) || 0;

  return (
    <Draggable draggableId={quest.id} index={index}>
      {(provided) => (
        <div
          ref={provided.innerRef}
          {...provided.draggableProps}
          {...provided.dragHandleProps}
          className="bg-gray-800 text-white p-4 rounded-lg shadow mb-3 border border-gray-700"
        >
          <div className="flex justify-between items-center mb-2">
            <span className="text-xs font-bold text-gray-400">Rank: {quest.difficulty}</span>
            {totalXp > 0 && <span className="text-xs text-amber-400 font-bold">+{totalXp} XP</span>}
          </div>
          <h3 className="font-semibold text-sm">{quest.title}</h3>
        </div>
      )}
    </Draggable>
  );
};
