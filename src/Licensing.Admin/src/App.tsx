import { Navigate, NavLink, Route, Routes, useNavigate } from 'react-router-dom';
import { clearToken, getToken } from './api';
import LoginPage from './pages/LoginPage';
import LicensesPage from './pages/LicensesPage';
import LicenseDetailPage from './pages/LicenseDetailPage';
import SubscriptionsPage from './pages/SubscriptionsPage';
import ProductsPage from './pages/ProductsPage';
import AuditPage from './pages/AuditPage';
import ClientTestPage from './pages/ClientTestPage';

function Shell({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate();
  return (
    <div className="layout">
      <aside className="sidebar">
        <h3 style={{ marginTop: 0 }}>Licensing Admin</h3>
        <NavLink to="/">لایسنس‌ها</NavLink>
        <NavLink to="/subscriptions">اشتراک‌ها</NavLink>
        <NavLink to="/products">محصولات</NavLink>
        <NavLink to="/audit">Audit Log</NavLink>
        <NavLink to="/client-test">تست Client API</NavLink>
        <button
          className="secondary"
          style={{ marginTop: '1rem', width: '100%' }}
          onClick={() => { clearToken(); navigate('/login'); }}
        >
          خروج
        </button>
      </aside>
      <main className="content">{children}</main>
    </div>
  );
}

function PrivateRoute({ children }: { children: React.ReactNode }) {
  if (!getToken()) return <Navigate to="/login" replace />;
  return <Shell>{children}</Shell>;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/" element={<PrivateRoute><LicensesPage /></PrivateRoute>} />
      <Route path="/licenses/:id" element={<PrivateRoute><LicenseDetailPage /></PrivateRoute>} />
      <Route path="/subscriptions" element={<PrivateRoute><SubscriptionsPage /></PrivateRoute>} />
      <Route path="/products" element={<PrivateRoute><ProductsPage /></PrivateRoute>} />
      <Route path="/audit" element={<PrivateRoute><AuditPage /></PrivateRoute>} />
      <Route path="/client-test" element={<PrivateRoute><ClientTestPage /></PrivateRoute>} />
    </Routes>
  );
}
