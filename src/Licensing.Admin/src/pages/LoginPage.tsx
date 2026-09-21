import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getToken, login } from '../api';

export default function LoginPage() {
  const navigate = useNavigate();
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('ChangeMe!123');
  const [error, setError] = useState('');

  if (getToken()) {
    navigate('/');
    return null;
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await login(username, password);
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    }
  }

  return (
    <div className="login-wrap">
      <form className="card login-card" onSubmit={onSubmit}>
        <h2>ورود پنل مدیریت</h2>
        <label>نام کاربری</label>
        <input value={username} onChange={(e) => setUsername(e.target.value)} />
        <label>رمز عبور</label>
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
        {error && <p className="error">{error}</p>}
        <button type="submit">ورود</button>
      </form>
    </div>
  );
}
