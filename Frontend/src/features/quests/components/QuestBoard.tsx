import { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { DragDropContext, Droppable } from '@hello-pangea/dnd';
import type { DropResult } from '@hello-pangea/dnd';
import { motion, AnimatePresence } from 'framer-motion';
import { Plus, MoreVertical, Edit2, Trash2, X, Check } from 'lucide-react';
import { useQuestStore } from '../useQuestStore';
import { QuestState } from '../types';
import { QuestCard } from './QuestCard';
import { CreateQuestForm } from './CreateQuestForm';
import { PartyActivityFeed } from '../../parties/components/PartyActivityFeed';
import { usePartyHub } from '../../../hooks/usePartyHub';
import { useAuthStore } from '../../auth/useAuthStore';

interface ColumnItem {
  id: string;
  name: string;
  dbId?: string;
  i18nKey?: string;
}

const DEFAULT_COLUMNS: ColumnItem[] = [
  { id: QuestState.Pending.toString(), name: 'Pendiente', i18nKey: 'quest.board.columns.pending' },
  { id: QuestState.InProgress.toString(), name: 'En Progreso', i18nKey: 'quest.board.columns.inProgress' },
  { id: QuestState.Completed.toString(), name: 'Completada', i18nKey: 'quest.board.columns.completed' },
];

export const QuestBoard = () => {
  const { t } = useTranslation();
  const { quests, updateQuestState, setQuestStateLocal } = useQuestStore();
  const token = useAuthStore(state => state.token);
  const partyId = useAuthStore(state => state.partyId) || 'party1';
  const userId = useAuthStore(state => state.userId) || '11111111-1111-1111-1111-111111111111';

  const [columns, setColumns] = useState<ColumnItem[]>(DEFAULT_COLUMNS);
  const [xpPopup, setXpPopup] = useState<{ visible: boolean; xp: number }>({ visible: false, xp: 0 });
  const [showCreateQuestModal, setShowCreateQuestModal] = useState(false);
  const [showAddColumnModal, setShowAddColumnModal] = useState(false);
  const [newColumnName, setNewColumnName] = useState('');

  // Column dropdown & editing state
  const [activeMenuColId, setActiveMenuColId] = useState<string | null>(null);
  const [editingColId, setEditingColId] = useState<string | null>(null);
  const [editingColName, setEditingColName] = useState('');
  const menuRef = useRef<HTMLDivElement>(null);

  const connection = usePartyHub(partyId, token || '');

  // Close menu on outside click
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setActiveMenuColId(null);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Fetch columns from backend for active party
  useEffect(() => {
    let isMounted = true;
    const fetchColumns = async () => {
      if (!partyId || partyId === 'party1') return;
      try {
        const response = await fetch(`/api/parties/${partyId}/columns`, {
          headers: token ? { Authorization: `Bearer ${token}` } : {}
        });
        if (response.ok) {
          const data = await response.json();
          if (Array.isArray(data) && data.length > 0 && isMounted) {
            const mapped = data.map((col: any, index: number) => ({
              id: index < 3 ? index.toString() : col.id,
              name: col.name,
              dbId: col.id,
              i18nKey: index === 0 ? 'quest.board.columns.pending' : index === 1 ? 'quest.board.columns.inProgress' : index === 2 ? 'quest.board.columns.completed' : undefined
            }));
            setColumns(mapped);
          }
        }
      } catch {
        // Fallback to default columns
      }
    };

    fetchColumns();
    return () => { isMounted = false; };
  }, [partyId, token]);

  useEffect(() => {
    if (!connection) return;
    const onQuestUpdated = (payload: any) => {
      const sourceUserId = payload?.sourceUserId || payload?.SourceUserId;
      if (sourceUserId && sourceUserId === userId) return;

      const rawState = payload?.newState ?? payload?.NewState;
      const questId = payload?.questId || payload?.QuestId;
      if (!questId) return;

      const newState = QuestState[rawState as keyof typeof QuestState] ?? (typeof rawState === 'number' ? rawState : parseInt(rawState));
      if (newState !== undefined && !isNaN(newState)) {
        setQuestStateLocal(questId, newState as unknown as QuestState);
      }
    };
    connection.on('QuestUpdated', onQuestUpdated);
    return () => { connection.off('QuestUpdated', onQuestUpdated); };
  }, [connection, setQuestStateLocal, userId]);

  const onDragEnd = (result: DropResult) => {
    if (!result.destination) return;
    
    const sourceCol = result.source.droppableId;
    const destCol = result.destination.droppableId;
    const questId = result.draggableId;

    if (sourceCol === destCol) return;

    const newState = parseInt(destCol) as QuestState;
    updateQuestState(questId, newState, userId);

    if (newState === QuestState.Completed) {
      const quest = quests.find(q => q.id === questId);
      const xp = quest?.rewards?.reduce((acc, r) => acc + r.xpAmount, 0) || 0;
      if (xp > 0) {
        setXpPopup({ visible: true, xp });
        setTimeout(() => setXpPopup({ visible: false, xp: 0 }), 2500);
      }
    }
  };

  const handleCreateColumn = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newColumnName.trim()) return;

    const trimmedName = newColumnName.trim();
    try {
      if (partyId && partyId !== 'party1') {
        const res = await fetch(`/api/parties/${partyId}/columns`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {})
          },
          body: JSON.stringify({ name: trimmedName })
        });
        if (res.ok) {
          const created = await res.json();
          setColumns(prev => [...prev, { id: created.id, name: created.name, dbId: created.id }]);
        } else {
          setColumns(prev => [...prev, { id: `col-${Date.now()}`, name: trimmedName }]);
        }
      } else {
        setColumns(prev => [...prev, { id: `col-${Date.now()}`, name: trimmedName }]);
      }
    } catch {
      setColumns(prev => [...prev, { id: `col-${Date.now()}`, name: trimmedName }]);
    }

    setNewColumnName('');
    setShowAddColumnModal(false);
  };

  const handleUpdateColumnName = async (colId: string) => {
    if (!editingColName.trim()) {
      setEditingColId(null);
      return;
    }
    const targetCol = columns.find(c => c.id === colId);
    const updatedName = editingColName.trim();

    setColumns(prev => prev.map(c => c.id === colId ? { ...c, name: updatedName, i18nKey: undefined } : c));
    setEditingColId(null);
    setActiveMenuColId(null);

    if (targetCol?.dbId && partyId) {
      try {
        await fetch(`/api/parties/${partyId}/columns/${targetCol.dbId}`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {})
          },
          body: JSON.stringify({ name: updatedName })
        });
      } catch {
        // Ignored
      }
    }
  };

  const handleDeleteColumn = async (colId: string) => {
    const targetCol = columns.find(c => c.id === colId);
    setColumns(prev => prev.filter(c => c.id !== colId));
    setActiveMenuColId(null);

    if (targetCol?.dbId && partyId) {
      try {
        await fetch(`/api/parties/${partyId}/columns/${targetCol.dbId}`, {
          method: 'DELETE',
          headers: token ? { Authorization: `Bearer ${token}` } : {}
        });
      } catch {
        // Ignored
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
        <div className="flex gap-6 h-full flex-grow overflow-x-auto pb-4">
          <div className="flex flex-1 gap-6 min-w-0">
            {columns.map((col) => {
              const colQuests = quests.filter(q => q.state.toString() === col.id);
              const isEditingThis = editingColId === col.id;

              return (
                <div key={col.id} className="flex flex-col w-80 shrink-0 bg-gray-950 border border-gray-800 rounded-xl p-4 shadow-md">
                  {/* Column Header */}
                  <div className="flex items-center justify-between mb-4 relative">
                    {isEditingThis ? (
                      <div className="flex items-center gap-1.5 w-full">
                        <input
                          type="text"
                          autoFocus
                          value={editingColName}
                          onChange={e => setEditingColName(e.target.value)}
                          onKeyDown={e => e.key === 'Enter' && handleUpdateColumnName(col.id)}
                          className="bg-gray-900 border border-amber-500 text-white text-xs rounded px-2 py-1 w-full focus:outline-none"
                        />
                        <button
                          onClick={() => handleUpdateColumnName(col.id)}
                          className="text-emerald-400 hover:text-emerald-300 p-1"
                        >
                          <Check size={14} />
                        </button>
                        <button
                          onClick={() => setEditingColId(null)}
                          className="text-gray-400 hover:text-gray-300 p-1"
                        >
                          <X size={14} />
                        </button>
                      </div>
                    ) : (
                      <>
                        <h2 className="text-gray-300 font-bold uppercase tracking-wider text-sm truncate pr-2">
                          {col.i18nKey ? t(col.i18nKey, col.name) : col.name} ({colQuests.length})
                        </h2>
                        
                        <div className="relative">
                          <button
                            onClick={() => setActiveMenuColId(activeMenuColId === col.id ? null : col.id)}
                            className="text-gray-500 hover:text-white p-1 rounded hover:bg-gray-800 transition-colors"
                            aria-label="Opciones de columna"
                          >
                            <MoreVertical size={16} />
                          </button>

                          {activeMenuColId === col.id && (
                            <div
                              ref={menuRef}
                              className="absolute right-0 top-7 w-40 bg-gray-900 border border-gray-700 rounded-lg shadow-xl z-30 py-1"
                            >
                              <button
                                onClick={() => {
                                  setEditingColId(col.id);
                                  setEditingColName(col.name);
                                  setActiveMenuColId(null);
                                }}
                                className="w-full text-left px-3 py-1.5 text-xs text-gray-300 hover:bg-gray-800 hover:text-amber-400 flex items-center gap-2"
                              >
                                <Edit2 size={13} />
                                <span>{t('quest.board.editColumn', 'Editar Nombre')}</span>
                              </button>
                              <button
                                onClick={() => handleDeleteColumn(col.id)}
                                className="w-full text-left px-3 py-1.5 text-xs text-red-400 hover:bg-gray-800 flex items-center gap-2"
                              >
                                <Trash2 size={13} />
                                <span>{t('quest.board.deleteColumn', 'Eliminar Columna')}</span>
                              </button>
                            </div>
                          )}
                        </div>
                      </>
                    )}
                  </div>

                  {/* Droppable Quest Container */}
                  <Droppable droppableId={col.id}>
                    {(provided) => (
                      <div
                        ref={provided.innerRef}
                        {...provided.droppableProps}
                        className="flex-grow flex flex-col min-h-[220px] overflow-y-auto pr-1"
                      >
                        {colQuests.map((q, index) => (
                          <QuestCard key={q.id} quest={q} index={index} />
                        ))}
                        {provided.placeholder}
                      </div>
                    )}
                  </Droppable>

                  {/* Add Quest Button in Column */}
                  <button
                    onClick={() => setShowCreateQuestModal(true)}
                    className="w-full mt-3 py-2 border border-dashed border-gray-800 hover:border-amber-500/50 hover:bg-amber-500/5 text-gray-400 hover:text-amber-400 rounded-lg text-xs font-semibold flex items-center justify-center gap-1.5 transition-all group"
                  >
                    <Plus size={14} className="group-hover:scale-110 transition-transform" />
                    <span>{t('quest.board.addQuest', '+ Añadir Quest')}</span>
                  </button>
                </div>
              );
            })}

            {/* Nueva Columna Button */}
            <div className="w-64 shrink-0 flex items-start">
              <button
                onClick={() => setShowAddColumnModal(true)}
                className="w-full py-4 border-2 border-dashed border-gray-800 hover:border-amber-500/50 hover:bg-gray-950/60 text-gray-400 hover:text-amber-400 rounded-xl text-sm font-bold flex items-center justify-center gap-2 transition-all shadow-sm"
              >
                <Plus size={18} />
                <span>{t('quest.board.addColumn', 'Nueva Columna')}</span>
              </button>
            </div>
          </div>

          {/* Activity Feed Sidebar */}
          <div className="w-80 shrink-0">
            <PartyActivityFeed connection={connection} />
          </div>
        </div>
      </DragDropContext>

      {/* Modal: Create Quest */}
      {showCreateQuestModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <CreateQuestForm onClose={() => setShowCreateQuestModal(false)} />
        </div>
      )}

      {/* Modal: New Column */}
      {showAddColumnModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.95 }}
            className="bg-gray-900 border border-gray-800 rounded-xl p-6 max-w-sm w-full shadow-2xl relative"
          >
            <button
              onClick={() => setShowAddColumnModal(false)}
              className="absolute top-4 right-4 text-gray-400 hover:text-white"
            >
              <X size={18} />
            </button>
            <h3 className="text-lg font-bold text-amber-500 mb-4 flex items-center gap-2">
              <Plus size={18} /> {t('quest.board.newColumnTitle', 'Nueva Columna')}
            </h3>
            <form onSubmit={handleCreateColumn} className="space-y-4">
              <div>
                <label className="block text-gray-300 text-xs font-bold mb-1">
                  Nombre de la Columna
                </label>
                <input
                  type="text"
                  autoFocus
                  required
                  value={newColumnName}
                  onChange={e => setNewColumnName(e.target.value)}
                  placeholder="Ej: Testing, En Revisión"
                  className="w-full bg-gray-950 border border-gray-700 text-white text-sm rounded-lg p-2.5 focus:outline-none focus:border-amber-500"
                />
              </div>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setShowAddColumnModal(false)}
                  className="px-3 py-1.5 bg-gray-800 hover:bg-gray-700 text-gray-300 text-xs font-bold rounded-lg"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 bg-amber-500 hover:bg-amber-600 text-gray-900 text-xs font-bold rounded-lg shadow-sm"
                >
                  Crear Columna
                </button>
              </div>
            </form>
          </motion.div>
        </div>
      )}
    </div>
  );
};
