import { useEffect, useState } from 'react';
import { api } from '../api';

type Audit = {
  id: string;
  actionType: string;
  licenseId?: string;
  actor?: string;
  ipAddress?: string;
  isSuspicious: boolean;
  createdAtUtc: string;
};

type Paged<T> = { items: T[] };

export default function AuditPage() {
  const [items, setItems] = useState<Audit[]>([]);

  useEffect(() => {
    api<Paged<Audit>>('/api/v1/audit-logs?pageSize=100').then((d) => setItems(d.items));
  }, []);

  return (
    <>
      <h2>Audit Log</h2>
      <div className="card">
        <table>
          <thead><tr><th>Action</th><th>License</th><th>Actor</th><th>IP</th><th>Suspicious</th><th>Time</th></tr></thead>
          <tbody>
            {items.map((a) => (
              <tr key={a.id}>
                <td>{a.actionType}</td>
                <td>{a.licenseId ?? '—'}</td>
                <td>{a.actor}</td>
                <td>{a.ipAddress}</td>
                <td>{a.isSuspicious ? 'Yes' : 'No'}</td>
                <td>{new Date(a.createdAtUtc).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
