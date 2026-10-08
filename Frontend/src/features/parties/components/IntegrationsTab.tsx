import { useState } from 'react';
import { Webhook, RefreshCw, Key, Clipboard, CheckCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface Props {
  partyId: string;
}

export const IntegrationsTab = ({ partyId }: Props) => {
  const { t } = useTranslation();
  const [secret, setSecret] = useState('sparnarok-mock-secret-key-for-ui');
  const [copiedUrl, setCopiedUrl] = useState(false);
  const [copiedSecret, setCopiedSecret] = useState(false);
  const [isRegenerating, setIsRegenerating] = useState(false);

  const webhookUrl = `${window.location.origin}/api/webhooks/git/${partyId}`;

  const handleRegenerate = () => {
    setIsRegenerating(true);
    // Simulate API call
    setTimeout(() => {
      setSecret(`spar-sec-${Math.random().toString(36).substring(2, 15)}`);
      setIsRegenerating(false);
    }, 600);
  };

  const copyToClipboard = (text: string, setter: (val: boolean) => void) => {
    navigator.clipboard.writeText(text);
    setter(true);
    setTimeout(() => setter(false), 2000);
  };

  const mockEvents = [
    { id: 1, type: 'Git PR (github)', success: true, date: 'hace 2 min', error: '' },
    { id: 2, type: 'Git PR (bitbucket)', success: false, date: 'hace 1 hora', error: 'Firma HMAC inválida.' },
  ];

  return (
    <div className="p-6 bg-gray-950 border border-gray-800 rounded-xl mt-6">
      <div className="flex items-center gap-3 mb-6">
        <Webhook className="text-white" size={28} />
        <h3 className="text-2xl font-bold text-white">{t('integrations.title', 'Git Webhooks')}</h3>
      </div>
      
      <p className="text-gray-400 mb-8">{t('integrations.subtitle', 'Conecta tus repositorios para completar Quests automáticamente al mergear Pull Requests.')}</p>

      <div className="grid md:grid-cols-2 gap-8">
        <div className="space-y-6">
          <div>
            <label className="block text-sm font-bold text-gray-400 mb-2">{t('integrations.webhookUrl', 'Webhook URL')}</label>
            <div className="flex">
              <input type="text" readOnly value={webhookUrl} className="bg-gray-900 border border-gray-700 text-white rounded-l p-3 flex-1 font-mono text-sm" />
              <button 
                onClick={() => copyToClipboard(webhookUrl, setCopiedUrl)}
                className="bg-indigo-600 hover:bg-indigo-500 text-white px-4 rounded-r transition-colors flex items-center justify-center w-14"
              >
                {copiedUrl ? <CheckCircle size={18} /> : <Clipboard size={18} />}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-sm font-bold text-gray-400 mb-2">{t('integrations.secret', 'Secret Token (HMAC SHA-256)')}</label>
            <div className="flex">
              <input type="text" readOnly value={secret} className="bg-gray-900 border border-gray-700 text-amber-500 font-bold rounded-l p-3 flex-1 font-mono text-sm" />
              <button 
                onClick={() => copyToClipboard(secret, setCopiedSecret)}
                className="bg-gray-700 hover:bg-gray-600 text-white px-4 transition-colors flex items-center justify-center w-14 border-y border-gray-700"
              >
                {copiedSecret ? <CheckCircle size={18} /> : <Clipboard size={18} />}
              </button>
              <button 
                onClick={handleRegenerate}
                disabled={isRegenerating}
                className="bg-amber-600 hover:bg-amber-500 disabled:bg-gray-700 text-white px-4 rounded-r transition-colors flex items-center gap-2 font-bold"
              >
                <RefreshCw size={16} className={isRegenerating ? 'animate-spin' : ''} /> {t('integrations.regenerateBtn', 'Regenerar')}
              </button>
            </div>
          </div>

          <div className="bg-indigo-950/30 border border-indigo-900/50 p-4 rounded-lg">
            <h4 className="font-bold text-indigo-400 flex items-center gap-2 mb-2"><Key size={16} /> {t('integrations.instructionsTitle', 'Instrucciones')}</h4>
            <ol className="list-decimal list-inside text-sm text-gray-300 space-y-2">
              <li>{t('integrations.inst1', 'Ve a Settings > Webhooks en Github o Bitbucket.')}</li>
              <li>{t('integrations.inst2', 'Pega la URL del Webhook. Selecciona Content type: application/json.')}</li>
              <li>{t('integrations.inst3', 'Pega el Secret Token. Selecciona solo eventos de Pull Request.')}</li>
              <li>{t('integrations.inst4', 'Añade [SPAR-12345678] al título de tu PR. Al mergearlo, la Quest se completará sola.')}</li>
            </ol>
          </div>
        </div>

        <div className="bg-gray-900/50 border border-gray-800 rounded-lg p-4">
          <h4 className="font-bold text-gray-300 mb-4">{t('integrations.history', 'Últimos eventos recibidos')}</h4>
          {mockEvents.length === 0 ? (
            <p className="text-sm text-gray-500 italic">{t('integrations.noEvents', 'Aún no se han recibido eventos.')}</p>
          ) : (
            <div className="space-y-3">
              {mockEvents.map(ev => (
                <div key={ev.id} className="flex flex-col p-3 bg-gray-950 border border-gray-800 rounded">
                  <div className="flex justify-between items-center mb-1">
                    <span className="font-bold text-white text-sm">{ev.type}</span>
                    <span className="text-xs text-gray-500">{ev.date}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    {ev.success ? (
                      <span className="px-2 py-0.5 bg-green-900/30 text-green-400 border border-green-800/50 rounded text-xs font-bold">200 OK</span>
                    ) : (
                      <span className="px-2 py-0.5 bg-red-900/30 text-red-400 border border-red-800/50 rounded text-xs font-bold">401 Error</span>
                    )}
                    {ev.error && <span className="text-xs text-red-400">{ev.error}</span>}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
