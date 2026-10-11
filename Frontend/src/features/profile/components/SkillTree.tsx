import { useCallback, useState, useEffect } from 'react';
import { ReactFlow, Controls, Background, useNodesState, useEdgesState, MarkerType } from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { AnimatePresence } from 'framer-motion';
import { Lock, Unlock, Sparkles } from 'lucide-react';
import { PaywallModal } from './PaywallModal';

export interface SkillNodeData {
  id: string;
  label: string;
  isPremium: boolean;
  unlocked: boolean;
  level?: number;
  xp?: number;
  description?: string;
}

const INITIAL_NODES = [
  { id: '1', position: { x: 300, y: 30 }, data: { label: 'Iniciado', isPremium: false, unlocked: true, level: 1 } },
  { id: '2', position: { x: 120, y: 150 }, data: { label: 'Frontend', isPremium: false, unlocked: true, level: 1 } },
  { id: '3', position: { x: 480, y: 150 }, data: { label: 'Backend', isPremium: false, unlocked: false, level: 0 } },
  { id: '4', position: { x: 480, y: 280 }, data: { label: 'Arquitectura Cloud', isPremium: true, unlocked: false, level: 0 } },
  { id: '5', position: { x: 120, y: 280 }, data: { label: 'UI/UX Mágico', isPremium: false, unlocked: false, level: 0 } },
  { id: '6', position: { x: 300, y: 380 }, data: { label: 'DevOps & CI/CD', isPremium: false, unlocked: false, level: 0 } },
];

const INITIAL_EDGES = [
  { id: 'e1-2', source: '1', target: '2' },
  { id: 'e1-3', source: '1', target: '3' },
  { id: 'e2-5', source: '2', target: '5' },
  { id: 'e3-4', source: '3', target: '4' },
  { id: 'e3-6', source: '3', target: '6' },
  { id: 'e4-6', source: '4', target: '6' },
];

interface Props {
  unlockedNodeIds?: string[];
  externalNodes?: any[];
}

export const SkillTree = ({ unlockedNodeIds }: Props) => {
  const [nodes, setNodes, onNodesChange] = useNodesState(INITIAL_NODES);
  const [edges, , onEdgesChange] = useEdgesState(INITIAL_EDGES);
  const [showPaywall, setShowPaywall] = useState(false);

  // Update nodes when unlockedNodeIds changes
  useEffect(() => {
    if (unlockedNodeIds && unlockedNodeIds.length > 0) {
      setNodes((nds) =>
        nds.map((node) => {
          const isNowUnlocked = unlockedNodeIds.some(
            (nameOrId) =>
              nameOrId.toLowerCase() === node.data.label.toLowerCase() ||
              nameOrId === node.id
          );
          if (isNowUnlocked) {
            return {
              ...node,
              data: {
                ...node.data,
                unlocked: true,
                level: Math.max(node.data.level || 1, 1)
              }
            };
          }
          return node;
        })
      );
    }
  }, [unlockedNodeIds, setNodes]);

  const onNodeClick = useCallback((_: any, node: any) => {
    if (node.data.isPremium && !node.data.unlocked) {
      setShowPaywall(true);
    }
  }, []);

  return (
    <div className="w-full h-full min-h-[600px] bg-gray-900 relative">
      <ReactFlow
        nodes={nodes.map((n) => {
          const isUnlocked = n.data.unlocked;
          return {
            ...n,
            style: {
              background: isUnlocked
                ? 'linear-gradient(135deg, #059669 0%, #10b981 100%)'
                : '#1f2937',
              color: '#fff',
              border: n.data.isPremium ? '2px solid #fbbf24' : isUnlocked ? '2px solid #34d399' : '1px solid #4b5563',
              borderRadius: '12px',
              padding: '12px',
              width: 170,
              textAlign: 'center',
              boxShadow: isUnlocked
                ? '0 0 25px rgba(16, 185, 129, 0.5)'
                : '0 4px 6px -1px rgba(0, 0, 0, 0.3)',
              transition: 'all 0.5s ease-in-out'
            },
            data: {
              ...n.data,
              label: (
                <div className="flex flex-col items-center gap-1.5">
                  <div className="flex items-center gap-1">
                    {n.data.isPremium ? (
                      <Lock size={15} className="text-amber-400" />
                    ) : (
                      <Unlock size={15} className={isUnlocked ? "text-emerald-100" : "text-gray-400"} />
                    )}
                    <span className="font-bold text-xs tracking-wide">{n.data.label as string}</span>
                  </div>
                  {isUnlocked && (
                    <div className="text-[10px] bg-black/30 px-2 py-0.5 rounded-full font-bold text-emerald-100 flex items-center gap-1">
                      <Sparkles size={10} className="text-amber-300" />
                      <span>Rango {n.data.level || 1}</span>
                    </div>
                  )}
                </div>
              )
            }
          };
        }) as any}
        edges={edges.map((e) => ({
          ...e,
          animated: true,
          style: { stroke: '#fbbf24', strokeWidth: 2 },
          markerEnd: { type: MarkerType.ArrowClosed, color: '#fbbf24' }
        }))}
        onNodesChange={onNodesChange as any}
        onEdgesChange={onEdgesChange}
        onNodeClick={onNodeClick}
        fitView
      >
        <Background color="#1f2937" gap={18} />
        <Controls />
      </ReactFlow>

      <AnimatePresence>
        {showPaywall && <PaywallModal onClose={() => setShowPaywall(false)} />}
      </AnimatePresence>
    </div>
  );
};
