import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../useAuthStore';

export const LoginForm = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const setAuth = useAuthStore(state => state.setAuth);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      
      const data = await response.json();
      if (!response.ok) throw new Error(data.error || 'Error al iniciar sesión');
      
      setAuth(data.token, data.username);
      navigate('/');
    } catch (err: any) {
      setError(err.message);
    }
  };

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center p-4">
      <div className="bg-gray-900 border border-gray-800 p-8 rounded-xl w-full max-w-md shadow-2xl">
        <h2 className="text-3xl font-bold text-amber-500 mb-6 text-center">Sparnarok Login</h2>
        {error && <div className="bg-red-500/10 border border-red-500 text-red-500 p-3 rounded mb-4 text-sm">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-gray-400 text-sm font-bold mb-2">Email</label>
            <input 
              type="email" 
              required
              value={email}
              onChange={e => setEmail(e.target.value)}
              className="w-full bg-gray-950 border border-gray-700 text-white rounded p-3 focus:outline-none focus:border-amber-500 transition-colors"
            />
          </div>
          <div>
            <label className="block text-gray-400 text-sm font-bold mb-2">Password</label>
            <input 
              type="password" 
              required
              value={password}
              onChange={e => setPassword(e.target.value)}
              className="w-full bg-gray-950 border border-gray-700 text-white rounded p-3 focus:outline-none focus:border-amber-500 transition-colors"
            />
          </div>
          <button type="submit" className="w-full bg-amber-500 text-gray-900 font-bold py-3 rounded hover:bg-amber-600 transition-colors">
            Entrar a la Party
          </button>
        </form>
        <p className="text-gray-500 text-center mt-6 text-sm">
          ¿No tienes una cuenta? <Link to="/register" className="text-amber-500 hover:underline">Regístrate aquí</Link>
        </p>
      </div>
    </div>
  );
};
