import React, { useState, useEffect, useRef } from 'react';
import { Shield, ChevronDown, Check, Copy } from 'lucide-react';
import { useAuthStore } from '../../auth/useAuthStore';

interface PartyItem {
  id: string;
  name: string;
  role: string;
  isPremium: boolean;
  joinedAt: string;
}

export const WorkspaceSwitcher: React.FC = () => {
  const token = useAuthStore(state => state.token);
  const partyId = useAuthStore(state => state.partyId);
  const partyName = useAuthStore(state => state.partyName);
  const setParty = useAuthStore(state => state.setParty);

  const [parties, setParties] = useState<PartyItem[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [copiedId, setCopiedId] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const loadParties = async () => {
    if (!token) return;
    try {
      const res = await fetch('/api/me/parties', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data: PartyItem[] = await res.json();
        setParties(data);
        if (data.length > 0) {
          const currentMatch = partyId ? data.find((p: PartyItem) => p.id === partyId) : null;
          if (currentMatch) {
            if (!partyName || partyName === 'Mi Party' || partyName !== currentMatch.name) {
              setParty(currentMatch.id, currentMatch.name);
            }
          } else {
            // Auto-select primary party if none currently selected or unassigned
            setParty(data[0].id, data[0].name);
          }
        }
      }
    } catch {
      // Ignore network errors in dev fallback
    }
  };

  useEffect(() => {
    loadParties();
  }, [token, partyId]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSelectParty = (p: PartyItem) => {
    setParty(p.id, p.name);
    setIsOpen(false);
  };

  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (partyId) {
      navigator.clipboard.writeText(partyId);
      setCopiedId(true);
      setTimeout(() => setCopiedId(false), 2000);
    }
  };

  return (
    <div className="relative" ref={dropdownRef}>
      {/* Workspace Trigger Button */}
      <button
        onClick={() => {
          setIsOpen(!isOpen);
          if (!isOpen) loadParties();
        }}
        className="flex items-center gap-2 bg-gray-900 hover:bg-gray-850 border border-gray-800 hover:border-amber-500/40 rounded-lg px-2.5 sm:px-3 py-1.5 text-xs transition-all shadow-sm max-w-xs text-left"
        title="Cambiar Workspace (Party)"
        aria-label="Selector de Workspace"
      >
        <Shield size={14} className="text-amber-400 shrink-0" />
        <div className="flex flex-col min-w-0">
          <span className="font-bold text-gray-200 truncate max-w-[90px] sm:max-w-[130px] md:max-w-[150px]">
            {partyName || 'Seleccionar Party'}
          </span>
          <span className="font-mono text-[10px] text-gray-400 truncate max-w-[90px] sm:max-w-[130px] md:max-w-[150px]">
            {partyId ? `ID: ${partyId.slice(0, 8)}...` : 'Sin Party'}
          </span>
        </div>
        <ChevronDown size={14} className={`text-gray-400 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute left-0 mt-2 w-64 bg-gray-950 border border-gray-800 rounded-xl shadow-2xl z-50 py-2 overflow-hidden animate-in fade-in zoom-in-95 duration-100">
          <div className="px-3 py-1.5 border-b border-gray-800/80 flex items-center justify-between">
            <span className="text-[11px] font-bold tracking-wider uppercase text-gray-400">Tus Parties (Workspaces)</span>
            {partyId && (
              <button
                onClick={handleCopy}
                className="text-[11px] text-amber-400 hover:text-amber-300 flex items-center gap-1 transition-colors"
                title="Copiar Party ID activa"
              >
                {copiedId ? <Check size={11} className="text-emerald-400" /> : <Copy size={11} />}
                <span>{copiedId ? 'Copiado' : 'Copiar ID'}</span>
              </button>
            )}
          </div>

          <div className="max-h-60 overflow-y-auto py-1 space-y-0.5">
            {parties.length === 0 ? (
              <div className="px-3 py-2 text-xs text-gray-400 text-center">
                {partyName || 'Party Activa'}
              </div>
            ) : (
              parties.map((p) => {
                const isSelected = p.id === partyId;
                return (
                  <button
                    key={p.id}
                    onClick={() => handleSelectParty(p)}
                    className={`w-full text-left px-3 py-2 text-xs flex items-center justify-between transition-colors ${
                      isSelected
                        ? 'bg-amber-500/15 text-amber-300 border-l-2 border-amber-500'
                        : 'text-gray-300 hover:bg-gray-900 hover:text-white'
                    }`}
                  >
                    <div className="flex flex-col min-w-0 pr-2">
                      <div className="font-bold truncate flex items-center gap-1.5">
                        <span className="truncate">{p.name}</span>
                        {p.isPremium && (
                          <span className="text-[9px] bg-amber-500/20 text-amber-400 border border-amber-500/30 px-1 py-0.2 rounded font-black">
                            PRO
                          </span>
                        )}
                      </div>
                      <span className="text-[10px] text-gray-500 capitalize">{p.role}</span>
                    </div>
                    {isSelected && <Check size={14} className="text-amber-400 shrink-0" />}
                  </button>
                );
              })
            )}
          </div>
        </div>
      )}
    </div>
  );
};
