import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Eye, EyeOff, Loader2, ShieldCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../useAuthStore';

export const RegisterForm = () => {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const setAuth = useAuthStore(state => state.setAuth);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);
    
    try {
      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, username, password })
      });
      
      let data: any = {};
      const text = await response.text();
      if (text) {
        try {
          data = JSON.parse(text);
        } catch {
          // Fallback if not json
        }
      }

      if (!response.ok) {
        throw new Error(data.error || 'Error al registrarse');
      }
      
      if (data.token) {
        setAuth(data.token, data.username || username, data.partyId, data.partyName, data.userId);
        navigate('/');
      } else {
        navigate('/login');
      }
    } catch (err: any) {
      setError(err.message || 'Error de conexión al forjar héroe');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center p-4">
      <div className="bg-gray-900 border border-gray-800 p-8 rounded-xl w-full max-w-md shadow-2xl">
        <div className="flex items-center justify-center gap-2 mb-2">
          <ShieldCheck className="text-amber-400" size={26} />
          <h2 className="text-3xl font-bold text-amber-500 text-center tracking-wider">Forja tu Héroe</h2>
        </div>
        <p className="text-gray-400 text-sm text-center mb-6">Crea tu identidad legendaria en el Reino de Sparnarok</p>

        {error && <div className="bg-red-500/10 border border-red-500 text-red-500 p-3 rounded mb-4 text-sm">{error}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-gray-400 text-sm font-bold mb-2">Email</label>
            <input 
              type="email" 
              required
              disabled={isLoading}
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="tu.heroe@sparnarok.com"
              className="w-full bg-gray-950 border border-gray-700 text-white rounded p-3 focus:outline-none focus:border-amber-500 transition-colors disabled:opacity-50"
            />
          </div>
          <div>
            <label className="block text-gray-400 text-sm font-bold mb-2">Nombre de Héroe (Username)</label>
            <input 
              type="text" 
              required
              disabled={isLoading}
              value={username}
              onChange={e => setUsername(e.target.value)}
              placeholder="Ej: ThorinDev, LadyBinary"
              className="w-full bg-gray-950 border border-gray-700 text-white rounded p-3 focus:outline-none focus:border-amber-500 transition-colors disabled:opacity-50"
            />
          </div>
          <div>
            <label className="block text-gray-400 text-sm font-bold mb-2">Password</label>
            <div className="relative">
              <input 
                type={showPassword ? "text" : "password"} 
                required
                minLength={6}
                disabled={isLoading}
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="Mínimo 6 caracteres"
                className="w-full bg-gray-950 border border-gray-700 text-white rounded p-3 pr-10 focus:outline-none focus:border-amber-500 transition-colors disabled:opacity-50"
              />
              <button
                type="button"
                tabIndex={-1}
                disabled={isLoading}
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-3.5 text-gray-400 hover:text-amber-500 transition-colors"
                aria-label={showPassword ? "Ocultar contraseña" : "Ver contraseña"}
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>
          <button 
            type="submit" 
            disabled={isLoading}
            className="w-full bg-amber-500 hover:bg-amber-600 disabled:bg-amber-600/50 disabled:cursor-not-allowed text-gray-900 font-bold py-3 rounded transition-colors flex items-center justify-center gap-2 shadow-lg shadow-amber-500/20"
          >
            {isLoading ? (
              <>
                <Loader2 size={18} className="animate-spin text-gray-950" />
                <span>{t('auth.register.loading', 'Forjando Héroe...')}</span>
              </>
            ) : (
              <span>{t('auth.register.submit', 'Crear Héroe')}</span>
            )}
          </button>
        </form>
        <p className="text-gray-500 text-center mt-6 text-sm">
          ¿Ya eres miembro del gremio? <Link to="/login" className="text-amber-500 hover:underline font-semibold">Inicia sesión</Link>
        </p>
      </div>
    </div>
  );
};
