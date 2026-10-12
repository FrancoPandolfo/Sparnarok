import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { X, Users, Crown, Copy, Check, Send, AlertCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../../auth/useAuthStore';

interface Props {
  onClose: () => void;
}

export const InviteAllyModal = ({ onClose }: Props) => {
  const { t } = useTranslation();
  const partyId = useAuthStore(state => state.partyId);
  const partyName = useAuthStore(state => state.partyName);
  const token = useAuthStore(state => state.token);
  const setParty = useAuthStore(state => state.setParty);

  const [inviteInput, setInviteInput] = useState('');
  const [copied, setCopied] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');
  const [showPaywall, setShowPaywall] = useState(false);

  useEffect(() => {
    if (!partyId && token) {
      fetch('/api/me/parties', {
        headers: { Authorization: `Bearer ${token}` }
      })
        .then(res => res.ok ? res.json() : [])
        .then(data => {
          if (Array.isArray(data) && data.length > 0) {
            setParty(data[0].id, data[0].name);
          }
        })
        .catch(() => {});
    }
  }, [partyId, token, setParty]);

  const handleCopyId = () => {
    if (partyId) {
      navigator.clipboard.writeText(partyId);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inviteInput.trim()) return;
    if (!partyId) {
      setErrorMsg('Error crítico: No tienes una Party activa (ID no asignada). Cierra sesión y vuelve a ingresar.');
      return;
    }

    setIsLoading(true);
    setErrorMsg('');
    setSuccessMsg('');

    try {
      const response = await fetch(`/api/parties/${partyId}/invite`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify({
          email: inviteInput.trim(),
          username: inviteInput.trim()
        })
      });

      const data = await response.json().catch(() => ({}));

      if (response.status === 403 && data.code === 'ERR_PARTY_LIMIT_REACHED') {
        setShowPaywall(true);
        return;
      }

      if (!response.ok) {
        throw new Error(data.message || 'Error al invitar al aliado.');
      }

      setSuccessMsg(data.message || 'Invitación enviada con éxito');
      setInviteInput('');
    } catch (err: any) {
      setErrorMsg(err.message || 'Fallo de conexión al enviar invitación.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
      <motion.div
        initial={{ opacity: 0, scale: 0.95, y: 15 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.95, y: 15 }}
        className="bg-gray-900 border border-gray-700 rounded-xl p-4 sm:p-6 w-[calc(100%-2rem)] md:w-full max-w-md max-h-[90vh] overflow-y-auto shadow-[0_0_40px_rgba(245,158,11,0.25)] relative"
      >
        <button 
          onClick={onClose} 
          className="absolute top-4 right-4 text-gray-400 hover:text-white transition-colors"
          aria-label="Cerrar"
        >
          <X size={20} />
        </button>

        {showPaywall ? (
          <div className="flex flex-col items-center text-center space-y-4 mt-2">
            <div className="w-16 h-16 bg-amber-500/20 rounded-full flex items-center justify-center relative">
              <Users size={32} className="text-gray-300" />
              <Crown size={16} className="text-amber-400 absolute -top-1 -right-1" />
            </div>
            <h2 className="text-2xl font-bold text-white">{t('invitePaywall.title', 'Límite de Party Alcanzado')}</h2>
            <p className="text-gray-400 text-sm">
              {t('invitePaywall.description', 'Tu Party actual está en el plan Gratuito y ha alcanzado el límite de 3 miembros. Haz un upgrade al plan Pro para reclutar aliados ilimitados.')}
            </p>
            <button className="w-full py-3 mt-2 bg-amber-500 hover:bg-amber-600 text-gray-900 font-bold rounded-lg transition-colors shadow-[0_0_15px_rgba(245,158,11,0.5)]">
              {t('invitePaywall.upgradeButton', 'Actualizar a Pro')}
            </button>
          </div>
        ) : (
          <div>
            <div className="flex items-center gap-3 mb-4">
              <div className="p-2.5 bg-amber-500/10 rounded-lg border border-amber-500/30 text-amber-400">
                <Users size={22} />
              </div>
              <div>
                <h2 className="text-xl font-bold text-white">Reclutar Aliados para la Party</h2>
                <p className="text-xs text-gray-400">Suma héroes para compartir Quests y sincronizar XP</p>
              </div>
            </div>

            {/* Party Info Box */}
            <div className="bg-gray-950 border border-gray-800 rounded-lg p-3.5 mb-4">
              <div className="text-xs font-semibold text-gray-400 mb-1">Party Activa (Tenant):</div>
              <div className="text-sm font-bold text-amber-400 truncate">{partyName || 'Mi Party'}</div>
              <div className="mt-2 flex items-center justify-between text-xs text-gray-500 bg-gray-900 px-2.5 py-1.5 rounded border border-gray-800">
                <span className="font-mono truncate mr-2">ID: {partyId || 'No asignada'}</span>
                <button
                  type="button"
                  onClick={handleCopyId}
                  className="text-amber-400 hover:text-amber-300 font-semibold flex items-center gap-1 transition-colors flex-shrink-0"
                >
                  {copied ? <Check size={14} className="text-emerald-400" /> : <Copy size={14} />}
                  <span>{copied ? 'Copiado' : 'Copiar'}</span>
                </button>
              </div>
            </div>

            {successMsg && (
              <div className="bg-emerald-500/10 border border-emerald-500 text-emerald-400 p-3 rounded mb-4 text-xs font-semibold flex items-center gap-2">
                <Check size={16} />
                <span>{successMsg}</span>
              </div>
            )}

            {errorMsg && (
              <div className="bg-red-500/10 border border-red-500 text-red-400 p-3 rounded mb-4 text-xs font-semibold flex items-center gap-2">
                <AlertCircle size={16} />
                <span>{errorMsg}</span>
              </div>
            )}

            <form onSubmit={handleInvite} className="space-y-4">
              <div>
                <label className="block text-gray-300 text-xs font-bold mb-1">
                  Email o Nombre de Usuario del Héroe
                </label>
                <input
                  type="text"
                  required
                  value={inviteInput}
                  onChange={e => setInviteInput(e.target.value)}
                  placeholder="ej: usuario@correo.com o AlexDev"
                  disabled={isLoading}
                  className="w-full bg-gray-950 border border-gray-700 text-white text-sm rounded p-2.5 focus:outline-none focus:border-amber-500 transition-colors disabled:opacity-50"
                />
              </div>

              <button
                type="submit"
                disabled={isLoading || !inviteInput.trim()}
                className="w-full bg-amber-500 hover:bg-amber-600 disabled:bg-amber-600/50 disabled:cursor-not-allowed text-gray-900 font-bold py-2.5 rounded transition-colors flex items-center justify-center gap-2 shadow-lg shadow-amber-500/20 text-sm"
              >
                <Send size={16} />
                <span>{isLoading ? 'Enviando Invitación...' : 'Enviar Invitación'}</span>
              </button>
            </form>
          </div>
        )}
      </motion.div>
    </div>
  );
};
