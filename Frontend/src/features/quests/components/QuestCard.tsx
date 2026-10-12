import { useState, useRef, useEffect } from 'react';
import { Draggable } from '@hello-pangea/dnd';
import { User, Check, UserMinus } from 'lucide-react';
import type { Quest } from '../types';
import { useQuestStore } from '../useQuestStore';
import { usePartyMembersStore } from '../../parties/usePartyMembers';

const getAvatarBg = (name: string): string => {
  const colors = [
    'bg-indigo-600 text-white',
    'bg-emerald-600 text-white',
    'bg-amber-600 text-white',
    'bg-rose-600 text-white',
    'bg-purple-600 text-white',
    'bg-cyan-600 text-white',
    'bg-blue-600 text-white',
    'bg-fuchsia-600 text-white'
  ];
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return colors[Math.abs(hash) % colors.length];
};

export const QuestCard = ({ quest, index }: { quest: Quest; index: number }) => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const assignQuest = useQuestStore(state => state.assignQuest);
  const { members, fetchMembers } = usePartyMembersStore();

  const totalXp = quest.rewards?.reduce((acc, r) => acc + r.xpAmount, 0) || 0;

  // Resolve assigned user name
  const assignedMember = members.find(m => m.id === quest.assigneeId);
  const assigneeName = quest.assigneeName || assignedMember?.username || (quest.assigneeId ? 'Héroe' : null);

  useEffect(() => {
    if (isMenuOpen && members.length === 0) {
      fetchMembers();
    }
  }, [isMenuOpen, members.length, fetchMembers]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    };
    if (isMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isMenuOpen]);

  const handleSelectMember = async (memberId: string | null, memberName?: string) => {
    setIsMenuOpen(false);
    await assignQuest(quest.id, memberId, memberName ?? null);
  };

  return (
    <Draggable draggableId={quest.id} index={index}>
      {(provided) => (
        <div
          ref={provided.innerRef}
          {...provided.draggableProps}
          {...provided.dragHandleProps}
          className="bg-gray-800 text-white p-4 rounded-lg shadow mb-3 border border-gray-700 relative group hover:border-gray-600 transition-colors"
        >
          <div className="flex justify-between items-center mb-2">
            <span className="text-xs font-bold text-gray-400">Rank: {quest.difficulty}</span>
            {totalXp > 0 && <span className="text-xs text-amber-400 font-bold">+{totalXp} XP</span>}
          </div>

          <h3 className="font-semibold text-sm mb-3 leading-snug">{quest.title}</h3>

          <div className="flex justify-between items-center pt-2 border-t border-gray-700/60 mt-2">
            <div className="flex items-center gap-1.5 flex-wrap">
              {quest.tags && quest.tags.length > 0 ? (
                quest.tags.map(t => (
                  <span key={t} className="text-[10px] bg-gray-900 text-gray-300 px-1.5 py-0.5 rounded border border-gray-700">
                    {t}
                  </span>
                ))
              ) : (
                <span className="text-[10px] text-gray-500 italic">Sin etiquetas</span>
              )}
            </div>

            {/* Avatar & Assignment Menu */}
            <div className="relative" ref={menuRef}>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setIsMenuOpen(prev => !prev);
                }}
                onPointerDown={(e) => e.stopPropagation()}
                title={assigneeName ? `Héroe: ${assigneeName} (Clic para reasignar)` : 'Sin asignar (Clic para asignar)'}
                className="focus:outline-none transition-transform hover:scale-105 active:scale-95"
                aria-label={assigneeName ? `Asignado a ${assigneeName}` : 'Asignar tarea'}
              >
                {assigneeName ? (
                  <div
                    className={`w-7 h-7 rounded-full flex items-center justify-center font-bold text-xs shadow-md border border-white/20 ${getAvatarBg(assigneeName)}`}
                  >
                    {assigneeName.charAt(0).toUpperCase()}
                  </div>
                ) : (
                  <div className="w-7 h-7 rounded-full flex items-center justify-center text-gray-400 hover:text-white bg-gray-900 border border-dashed border-gray-600 hover:border-gray-400 transition-colors">
                    <User size={13} />
                  </div>
                )}
              </button>

              {/* Contextual Dropdown Menu */}
              {isMenuOpen && (
                <div
                  onPointerDown={(e) => e.stopPropagation()}
                  onClick={(e) => e.stopPropagation()}
                  className="absolute right-0 bottom-full mb-2 w-48 bg-gray-900 border border-gray-700 rounded-xl shadow-2xl p-1.5 z-50 text-xs"
                >
                  <div className="px-2 py-1 text-[10px] font-bold text-gray-400 uppercase tracking-wider border-b border-gray-800 mb-1">
                    Asignar Héroe
                  </div>

                  <button
                    type="button"
                    onClick={() => handleSelectMember(null)}
                    className="w-full flex items-center gap-2 px-2 py-1.5 rounded-lg text-gray-400 hover:text-white hover:bg-gray-800 text-left transition-colors"
                  >
                    <div className="w-5 h-5 rounded-full bg-gray-800 border border-gray-600 flex items-center justify-center">
                      <UserMinus size={11} />
                    </div>
                    <span className="flex-1 truncate">Sin asignar</span>
                    {!quest.assigneeId && <Check size={14} className="text-amber-400" />}
                  </button>

                  {members.map(member => {
                    const isSelected = quest.assigneeId === member.id;
                    return (
                      <button
                        key={member.id}
                        type="button"
                        onClick={() => handleSelectMember(member.id, member.username)}
                        className={`w-full flex items-center gap-2 px-2 py-1.5 rounded-lg text-left transition-colors ${
                          isSelected ? 'bg-amber-500/10 text-amber-300 font-semibold' : 'text-gray-200 hover:bg-gray-800'
                        }`}
                      >
                        <div
                          className={`w-5 h-5 rounded-full flex items-center justify-center font-bold text-[10px] shrink-0 ${getAvatarBg(member.username)}`}
                        >
                          {member.username.charAt(0).toUpperCase()}
                        </div>
                        <span className="flex-1 truncate">{member.username}</span>
                        {isSelected && <Check size={14} className="text-amber-400 shrink-0" />}
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </Draggable>
  );
};
