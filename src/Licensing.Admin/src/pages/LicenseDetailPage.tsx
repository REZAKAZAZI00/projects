import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../api';
import ConfirmDialog from '../components/ConfirmDialog';

type LicenseDetail = {
  id: string;
  status: string;
  expirationDateUtc: string;
  maxActivations: number;
  activeActivationCount: number;
  customerName: string;
  productName: string;
  planName: string;
};

type Activation = { id: string; instanceIdentifier: string; status: string; activatedAtUtc: string };
type History = { fromStatus: string; toStatus: string; reason?: string; changedBy?: string; changedAtUtc: string };

export default function LicenseDetailPage() {
  const { id } = useParams();
  const [license, setLicense] = useState<LicenseDetail | null>(null);
  const [activations, setActivations] = useState<Activation[]>([]);
  const [history, setHistory] = useState<History[]>([]);
  const [confirm, setConfirm] = useState<{ action: string; activationId?: string } | null>(null);
  const [renewDate, setRenewDate] = useState('');
  const [error, setError] = useState('');

  async function reload() {
    if (!id) return;
    const [l, a, h] = await Promise.all([
      api<LicenseDetail>(`/api/v1/licenses/${id}`),
      api<Activation[]>(`/api/v1/licenses/${id}/activations`),
      api<History[]>(`/api/v1/licenses/${id}/history`)
    ]);
    setLicense(l);
    setActivations(a);
    setHistory(h);
  }

  useEffect(() => { reload().catch((e) => setError(e.message)); }, [id]);

  async function runAction(action: string, activationId?: string) {
    if (!id) return;
    setError('');
    try {
      if (action === 'suspend') await api(`/api/v1/licenses/${id}/suspend`, { method: 'POST', body: JSON.stringify({ reason: 'Admin action' }) });
      if (action === 'resume') await api(`/api/v1/licenses/${id}/resume`, { method: 'POST', body: JSON.stringify({ reason: 'Admin action' }) });
      if (action === 'revoke') await api(`/api/v1/licenses/${id}/revoke`, { method: 'POST', body: JSON.stringify({ reason: 'Admin action' }) });
      if (action === 'renew') await api(`/api/v1/licenses/${id}/renew`, { method: 'POST', body: JSON.stringify({ newExpirationDateUtc: new Date(renewDate).toISOString(), notes: 'Admin renew' }) });
      if (action === 'deactivate' && activationId) await api(`/api/v1/licenses/${id}/activations/${activationId}/deactivate`, { method: 'POST' });
      setConfirm(null);
      await reload();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error');
    }
  }

  if (!license) return <p>Loading...</p>;

  return (
    <>
      <h2>جزئیات لایسنس</h2>
      {error && <p className="error">{error}</p>}
      <div className="card">
        <p><strong>مشتری:</strong> {license.customerName}</p>
        <p><strong>محصول:</strong> {license.productName} / {license.planName}</p>
        <p><strong>وضعیت:</strong> {license.status}</p>
        <p><strong>Activation:</strong> {license.activeActivationCount}/{license.maxActivations}</p>
        <p><strong>انقضا:</strong> {new Date(license.expirationDateUtc).toLocaleString()}</p>
        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
          <button className="secondary" onClick={() => setConfirm({ action: 'suspend' })}>Suspend</button>
          <button className="secondary" onClick={() => setConfirm({ action: 'resume' })}>Resume</button>
          <button className="danger" onClick={() => setConfirm({ action: 'revoke' })}>Revoke</button>
        </div>
        <div style={{ marginTop: '0.75rem' }}>
          <input type="datetime-local" value={renewDate} onChange={(e) => setRenewDate(e.target.value)} />
          <button onClick={() => setConfirm({ action: 'renew' })}>Renew</button>
        </div>
      </div>

      <div className="card">
        <h3>Activationها</h3>
        <table>
          <thead><tr><th>Instance</th><th>وضعیت</th><th>زمان</th><th></th></tr></thead>
          <tbody>
            {activations.map((a) => (
              <tr key={a.id}>
                <td>{a.instanceIdentifier}</td>
                <td>{a.status}</td>
                <td>{new Date(a.activatedAtUtc).toLocaleString()}</td>
                <td>
                  {a.status === 'Active' && (
                    <button className="danger" onClick={() => setConfirm({ action: 'deactivate', activationId: a.id })}>Deactivate</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3>تاریخچه وضعیت</h3>
        <table>
          <thead><tr><th>From</th><th>To</th><th>Reason</th><th>By</th><th>At</th></tr></thead>
          <tbody>
            {history.map((h, i) => (
              <tr key={i}>
                <td>{h.fromStatus}</td><td>{h.toStatus}</td><td>{h.reason}</td><td>{h.changedBy}</td><td>{new Date(h.changedAtUtc).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ConfirmDialog
        open={!!confirm}
        title="تأیید عملیات"
        message="آیا از انجام این عملیات مطمئن هستید؟"
        danger={confirm?.action === 'revoke' || confirm?.action === 'deactivate'}
        onCancel={() => setConfirm(null)}
        onConfirm={() => confirm && runAction(confirm.action, confirm.activationId)}
      />
    </>
  );
}
