import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { DragDropContext, Droppable } from '@hello-pangea/dnd';
import type { DropResult } from '@hello-pangea/dnd';
import { motion, AnimatePresence } from 'framer-motion';
import { useQuestStore } from '../useQuestStore';
import { QuestState } from '../types';
import { QuestCard } from './QuestCard';

const COLUMNS = [
  { id: QuestState.Pending.toString(), i18nKey: 'quest.board.columns.pending' },
  { id: QuestState.InProgress.toString(), i18nKey: 'quest.board.columns.inProgress' },
  { id: QuestState.Completed.toString(), i18nKey: 'quest.board.columns.completed' },
];

export const QuestBoard = () => {
  const { t } = useTranslation();
  const { quests, updateQuestState } = useQuestStore();
  const [xpPopup, setXpPopup] = useState<{ visible: boolean; xp: number }>({ visible: false, xp: 0 });

  const onDragEnd = (result: DropResult) => {
    if (!result.destination) return;
    
    const sourceCol = result.source.droppableId;
    const destCol = result.destination.droppableId;
    const questId = result.draggableId;

    if (sourceCol === destCol) return;

    const newState = parseInt(destCol) as QuestState;
    // Harcoded userId for demo purposes
    updateQuestState(questId, newState, '11111111-1111-1111-1111-111111111111');

    if (newState === QuestState.Completed) {
      const quest = quests.find(q => q.id === questId);
      const xp = quest?.rewards?.reduce((acc, r) => acc + r.xpAmount, 0) || 0;
      if (xp > 0) {
        setXpPopup({ visible: true, xp });
        setTimeout(() => setXpPopup({ visible: false, xp: 0 }), 2500);
      }
    }
  };

  return (
    <div className="p-6 h-full flex flex-col relative bg-gray-900 min-h-screen">
      <AnimatePresence>
        {xpPopup.visible && (
          <motion.div
            initial={{ opacity: 0, y: 50, scale: 0.5 }}
            animate={{ opacity: 1, y: 0, scale: 1.2 }}
            exit={{ opacity: 0, y: -50 }}
            className="absolute top-1/4 left-1/2 transform -translate-x-1/2 bg-amber-500 text-black px-6 py-3 rounded-full font-bold shadow-xl z-50 text-xl"
          >
            {t('quest.board.xpGained', { xp: xpPopup.xp })}
          </motion.div>
        )}
      </AnimatePresence>

      <DragDropContext onDragEnd={onDragEnd}>
        <div className="flex gap-6 h-full flex-grow">
          {COLUMNS.map((col) => {
            const colQuests = quests.filter(q => q.state.toString() === col.id);
            return (
              <div key={col.id} className="flex flex-col w-1/3 bg-gray-950 rounded-xl p-4">
                <h2 className="text-gray-300 font-bold mb-4 uppercase tracking-wider text-sm">
                  {t(col.i18nKey)} ({colQuests.length})
                </h2>
                <Droppable droppableId={col.id}>
                  {(provided) => (
                    <div
                      ref={provided.innerRef}
                      {...provided.droppableProps}
                      className="flex-grow flex flex-col min-h-[200px]"
                    >
                      {colQuests.map((q, index) => (
                        <QuestCard key={q.id} quest={q} index={index} />
                      ))}
                      {provided.placeholder}
                    </div>
                  )}
                </Droppable>
              </div>
            );
          })}
        </div>
      </DragDropContext>
    </div>
  );
};
