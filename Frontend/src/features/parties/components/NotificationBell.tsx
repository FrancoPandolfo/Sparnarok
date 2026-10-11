import React, { useState, useEffect, useRef } from 'react';
import { Bell, Check, X, Shield, Clock } from 'lucide-react';
import { useAuthStore } from '../../auth/useAuthStore';

interface InvitationItem {
  id: string;
  partyId: string;
  partyName: string;
  inviterId: string;
  inviterName: string;
  inviteeEmail: string;
  status: string;
  createdAt: string;
}

export const NotificationBell: React.FC = () => {
  const token = useAuthStore(state => state.token);
  const setParty = useAuthStore(state => state.setParty);

  const [invitations, setInvitations] = useState<InvitationItem[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [processingId, setProcessingId] = useState<string | null>(null);
  const popoverRef = useRef<HTMLDivElement>(null);

  const loadInvitations = async () => {
    if (!token) return;
    try {
      const res = await fetch('/api/me/invitations', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setInvitations(data);
      }
    } catch {
      // Ignored
    }
  };

  useEffect(() => {
    loadInvitations();
    // Poll invitations periodically every 15s in background
    const interval = setInterval(loadInvitations, 15000);
    return () => clearInterval(interval);
  }, [token]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (popoverRef.current && !popoverRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleAccept = async (inv: InvitationItem) => {
    if (!token) return;
    setProcessingId(inv.id);
    try {
      const res = await fetch(`/api/me/invitations/${inv.id}/accept`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        }
      });
      if (res.ok) {
        const data = await res.json().catch(() => ({}));
        // Switch to the newly accepted party
        setParty(inv.partyId, inv.partyName || data.partyName || 'Party');
        // Refresh invitations list
        await loadInvitations();
      }
    } catch {
      // Ignored
    } finally {
      setProcessingId(null);
    }
  };

  const handleDecline = async (inv: InvitationItem) => {
    if (!token) return;
    setProcessingId(inv.id);
    try {
      const res = await fetch(`/api/me/invitations/${inv.id}/decline`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        }
      });
      if (res.ok) {
        await loadInvitations();
      }
    } catch {
      // Ignored
    } finally {
      setProcessingId(null);
    }
  };

  return (
    <div className="relative" ref={popoverRef}>
      <button
        onClick={() => {
          setIsOpen(!isOpen);
          if (!isOpen) loadInvitations();
        }}
        className="relative text-gray-400 hover:text-amber-400 p-2 rounded-lg hover:bg-gray-800 transition-colors shrink-0"
        title="Centro de Notificaciones"
        aria-label="Notificaciones de invitaciones"
      >
        <Bell size={17} />
        {invitations.length > 0 && (
          <span className="absolute -top-0.5 -right-0.5 bg-amber-500 text-gray-950 font-black text-[10px] w-4 h-4 rounded-full flex items-center justify-center shadow-md animate-pulse">
            {invitations.length}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 bg-gray-950 border border-gray-800 rounded-xl shadow-2xl z-50 overflow-hidden animate-in fade-in zoom-in-95 duration-100">
          <div className="px-4 py-2.5 border-b border-gray-800 flex items-center justify-between bg-gray-900/60">
            <span className="text-xs font-bold text-white tracking-wide uppercase flex items-center gap-1.5">
              <Bell size={13} className="text-amber-400" />
              <span>Invitaciones ({invitations.length})</span>
            </span>
          </div>

          <div className="max-h-72 overflow-y-auto divide-y divide-gray-900">
            {invitations.length === 0 ? (
              <div className="p-5 text-center text-xs text-gray-400">
                <p>No tienes invitaciones pendientes</p>
                <p className="text-[10px] text-gray-400 mt-1">Aquí verás los reclutamientos de otras Parties</p>
              </div>
            ) : (
              invitations.map(inv => (
                <div key={inv.id} className="p-3.5 hover:bg-gray-900/40 transition-colors">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <div className="text-xs font-bold text-amber-400 truncate flex items-center gap-1">
                        <Shield size={12} className="shrink-0" />
                        <span className="truncate">{inv.partyName}</span>
                      </div>
                      <p className="text-[11px] text-gray-300 mt-0.5">
                        <span className="font-semibold text-white">{inv.inviterName}</span> te ha invitado a unirte.
                      </p>
                      <span className="text-[10px] text-gray-400 flex items-center gap-1 mt-1">
                        <Clock size={10} />
                        <span>{new Date(inv.createdAt).toLocaleDateString()}</span>
                      </span>
                    </div>

                    <div className="flex items-center gap-1 shrink-0 ml-2">
                      <button
                        onClick={() => handleAccept(inv)}
                        disabled={processingId === inv.id}
                        className="bg-emerald-600 hover:bg-emerald-500 text-white text-[11px] font-bold px-2.5 py-1 rounded-md flex items-center gap-1 transition-colors disabled:opacity-50 shadow-sm"
                        title="Aceptar invitación"
                      >
                        <Check size={12} />
                        <span>{processingId === inv.id ? '...' : 'Unirse'}</span>
                      </button>
                      <button
                        onClick={() => handleDecline(inv)}
                        disabled={processingId === inv.id}
                        className="text-gray-400 hover:text-red-400 hover:bg-gray-800 p-1 rounded transition-colors disabled:opacity-50"
                        title="Rechazar invitación"
                      >
                        <X size={13} />
                      </button>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};
