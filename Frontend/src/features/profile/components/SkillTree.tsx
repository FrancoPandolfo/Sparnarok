import { useCallback, useState } from 'react';
import { ReactFlow, Controls, Background, useNodesState, useEdgesState, MarkerType } from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { AnimatePresence } from 'framer-motion';
import { Lock, Unlock } from 'lucide-react';
import { PaywallModal } from './PaywallModal';

const initialNodes = [
  { id: '1', position: { x: 250, y: 50 }, data: { label: 'Fundamentos', isPremium: false, unlocked: true } },
  { id: '2', position: { x: 100, y: 150 }, data: { label: 'Frontend', isPremium: false, unlocked: true } },
  { id: '3', position: { x: 400, y: 150 }, data: { label: 'Backend', isPremium: false, unlocked: false } },
  { id: '4', position: { x: 400, y: 250 }, data: { label: 'Arquitectura Cloud', isPremium: true, unlocked: false } },
];
const initialEdges = [
  { id: 'e1-2', source: '1', target: '2' },
  { id: 'e1-3', source: '1', target: '3' },
  { id: 'e3-4', source: '3', target: '4' },
];

export const SkillTree = () => {
  const [nodes, , onNodesChange] = useNodesState(initialNodes);
  const [edges, , onEdgesChange] = useEdgesState(initialEdges);
  const [showPaywall, setShowPaywall] = useState(false);

  const onNodeClick = useCallback((_: any, node: any) => {
    if (node.data.isPremium && !node.data.unlocked) {
      setShowPaywall(true);
    }
  }, []);

  return (
    <div className="w-full h-full bg-gray-900">
      <ReactFlow
        nodes={nodes.map(n => ({
          ...n,
          style: {
            background: n.data.unlocked ? '#10b981' : '#374151',
            color: '#fff',
            border: n.data.isPremium ? '2px solid #fbbf24' : '1px solid #4b5563',
            borderRadius: '8px',
            padding: '10px',
            width: 150,
            textAlign: 'center',
            boxShadow: n.data.unlocked ? '0 0 15px rgba(16, 185, 129, 0.4)' : 'none'
          },
          data: {
            ...n.data,
            label: (
              <div className="flex flex-col items-center gap-1">
                {n.data.isPremium ? <Lock size={16} className="text-amber-400" /> : <Unlock size={16} className={n.data.unlocked ? "text-emerald-100" : "text-gray-400"} />}
                <span className="font-bold text-sm">{n.data.label as string}</span>
              </div>
            )
          }
        })) as any}
        edges={edges.map(e => ({
          ...e,
          animated: true,
          style: { stroke: '#fbbf24' },
          markerEnd: { type: MarkerType.ArrowClosed, color: '#fbbf24' }
        }))}
        onNodesChange={onNodesChange as any}
        onEdgesChange={onEdgesChange}
        onNodeClick={onNodeClick}
        fitView
      >
        <Background color="#1f2937" gap={16} />
        <Controls />
      </ReactFlow>
      
      <AnimatePresence>
        {showPaywall && <PaywallModal onClose={() => setShowPaywall(false)} />}
      </AnimatePresence>
    </div>
  );
};
