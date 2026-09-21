import { FormEvent, useEffect, useState } from 'react';
import { api } from '../api';
import ConfirmDialog from '../components/ConfirmDialog';

type Sub = {
  id: string;
  customerName: string;
  productName: string;
  planName: string;
  status: string;
  expirationDateUtc: string;
  currentLicenseId?: string;
};

type Paged<T> = { items: T[] };
type Customer = { id: string; name: string };
type Product = { id: string; name: string };
type Plan = { id: string; name: string; defaultMaxActivations: number };

export default function SubscriptionsPage() {
  const [items, setItems] = useState<Sub[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [plans, setPlans] = useState<Plan[]>([]);
  const [form, setForm] = useState({ customerId: '', productId: '', planId: '', days: 365 });
  const [cancelId, setCancelId] = useState<string | null>(null);
  const [error, setError] = useState('');

  async function load() {
    const data = await api<Paged<Sub>>('/api/v1/subscriptions');
    setItems(data.items);
  }

  useEffect(() => {
    (async () => {
      const [c, p] = await Promise.all([
        api<Customer[]>('/api/v1/customers'),
        api<Product[]>('/api/v1/catalog/products')
      ]);
      setCustomers(c);
      setProducts(p);
      await load();
    })().catch((e) => setError(e.message));
  }, []);

  useEffect(() => {
    if (!form.productId) return;
    api<Plan[]>(`/api/v1/catalog/plans?productId=${form.productId}`).then(setPlans);
  }, [form.productId]);

  async function onCreate(e: FormEvent) {
    e.preventDefault();
    const start = new Date();
    const end = new Date();
    end.setUTCDate(end.getUTCDate() + Number(form.days));
    await api('/api/v1/subscriptions', {
      method: 'POST',
      body: JSON.stringify({
        customerId: form.customerId,
        productId: form.productId,
        planId: form.planId,
        startDateUtc: start.toISOString(),
        expirationDateUtc: end.toISOString(),
        createLicense: true
      })
    });
    await load();
  }

  return (
    <>
      <h2>اشتراک‌ها</h2>
      {error && <p className="error">{error}</p>}
      <div className="card">
        <h3>ایجاد Subscription</h3>
        <form onSubmit={onCreate}>
          <select required value={form.customerId} onChange={(e) => setForm({ ...form, customerId: e.target.value })}>
            <option value="">مشتری</option>
            {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <select required value={form.productId} onChange={(e) => setForm({ ...form, productId: e.target.value, planId: '' })}>
            <option value="">محصول</option>
            {products.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          <select required value={form.planId} onChange={(e) => setForm({ ...form, planId: e.target.value })}>
            <option value="">Plan</option>
            {plans.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          <button type="submit">ایجاد</button>
        </form>
      </div>
      <div className="card">
        <table>
          <thead><tr><th>مشتری</th><th>محصول</th><th>Plan</th><th>وضعیت</th><th>انقضا</th><th></th></tr></thead>
          <tbody>
            {items.map((s) => (
              <tr key={s.id}>
                <td>{s.customerName}</td><td>{s.productName}</td><td>{s.planName}</td><td>{s.status}</td>
                <td>{new Date(s.expirationDateUtc).toLocaleString()}</td>
                <td><button className="danger" onClick={() => setCancelId(s.id)}>Cancel</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <ConfirmDialog
        open={!!cancelId}
        title="لغو اشتراک"
        message="Subscription و License مرتبط لغو/Revoke می‌شود. ادامه می‌دهید؟"
        danger
        onCancel={() => setCancelId(null)}
        onConfirm={async () => {
          if (!cancelId) return;
          await api(`/api/v1/subscriptions/${cancelId}/cancel`, { method: 'POST', body: JSON.stringify({ reason: 'Admin cancel' }) });
          setCancelId(null);
          await load();
        }}
      />
    </>
  );
}
