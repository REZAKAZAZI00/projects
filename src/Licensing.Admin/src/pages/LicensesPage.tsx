import { FormEvent, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';

type LicenseItem = {
  id: string;
  licenseKeyPrefix: string;
  customerName: string;
  productName: string;
  planName: string;
  status: string;
  expirationDateUtc: string;
  maxActivations: number;
  activeActivationCount: number;
};

type Paged<T> = { items: T[]; totalCount: number };

type Customer = { id: string; name: string };
type Product = { id: string; code: string; name: string };
type Plan = { id: string; code: string; name: string; defaultMaxActivations: number };

export default function LicensesPage() {
  const [items, setItems] = useState<LicenseItem[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [plans, setPlans] = useState<Plan[]>([]);
  const [filters, setFilters] = useState({ licenseKey: '', customerId: '', productId: '', status: '', expiringWithinDays: '' });
  const [createForm, setCreateForm] = useState({ customerId: '', productId: '', planId: '', maxActivations: 3, days: 365 });
  const [createdKey, setCreatedKey] = useState('');
  const [error, setError] = useState('');

  async function load() {
    const params = new URLSearchParams();
    if (filters.licenseKey) params.set('licenseKey', filters.licenseKey);
    if (filters.customerId) params.set('customerId', filters.customerId);
    if (filters.productId) params.set('productId', filters.productId);
    if (filters.status) params.set('status', filters.status);
    if (filters.expiringWithinDays) params.set('expiringWithinDays', filters.expiringWithinDays);
    const data = await api<Paged<LicenseItem>>(`/api/v1/licenses?${params.toString()}`);
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
    if (!createForm.productId) return;
    api<Plan[]>(`/api/v1/catalog/plans?productId=${createForm.productId}`).then(setPlans).catch(() => setPlans([]));
  }, [createForm.productId]);

  async function onFilter(e: FormEvent) {
    e.preventDefault();
    try { await load(); } catch (err) { setError(err instanceof Error ? err.message : 'Error'); }
  }

  async function onCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setCreatedKey('');
    const expiration = new Date();
    expiration.setUTCDate(expiration.getUTCDate() + Number(createForm.days));
    try {
      const result = await api<{ licenseId: string; licenseKey: string }>('/api/v1/licenses', {
        method: 'POST',
        body: JSON.stringify({
          customerId: createForm.customerId,
          productId: createForm.productId,
          planId: createForm.planId,
          maxActivations: Number(createForm.maxActivations),
          expirationDateUtc: expiration.toISOString(),
          activateImmediately: true
        })
      });
      setCreatedKey(result.licenseKey);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error');
    }
  }

  return (
    <>
      <h2>مدیریت لایسنس</h2>
      {error && <p className="error">{error}</p>}

      <div className="card">
        <h3>فیلتر</h3>
        <form className="filters" onSubmit={onFilter}>
          <input placeholder="License Key" value={filters.licenseKey} onChange={(e) => setFilters({ ...filters, licenseKey: e.target.value })} />
          <select value={filters.customerId} onChange={(e) => setFilters({ ...filters, customerId: e.target.value })}>
            <option value="">همه مشتری‌ها</option>
            {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <select value={filters.productId} onChange={(e) => setFilters({ ...filters, productId: e.target.value })}>
            <option value="">همه محصولات</option>
            {products.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          <select value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value })}>
            <option value="">همه وضعیت‌ها</option>
            {['Pending', 'Active', 'Suspended', 'Expired', 'Revoked'].map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <input placeholder="انقضا طی N روز" value={filters.expiringWithinDays} onChange={(e) => setFilters({ ...filters, expiringWithinDays: e.target.value })} />
          <button type="submit">اعمال</button>
        </form>
      </div>

      <div className="card">
        <h3>ایجاد لایسنس</h3>
        <form onSubmit={onCreate}>
          <select required value={createForm.customerId} onChange={(e) => setCreateForm({ ...createForm, customerId: e.target.value })}>
            <option value="">مشتری</option>
            {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <select required value={createForm.productId} onChange={(e) => setCreateForm({ ...createForm, productId: e.target.value, planId: '' })}>
            <option value="">محصول</option>
            {products.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          <select required value={createForm.planId} onChange={(e) => setCreateForm({ ...createForm, planId: e.target.value })}>
            <option value="">Plan</option>
            {plans.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          <input type="number" min={1} value={createForm.maxActivations} onChange={(e) => setCreateForm({ ...createForm, maxActivations: Number(e.target.value) })} />
          <input type="number" min={1} value={createForm.days} onChange={(e) => setCreateForm({ ...createForm, days: Number(e.target.value) })} />
          <button type="submit">ایجاد</button>
        </form>
        {createdKey && <p><strong>License Key:</strong> <code>{createdKey}</code></p>}
      </div>

      <div className="card">
        <table>
          <thead>
            <tr>
              <th>Prefix</th><th>مشتری</th><th>محصول</th><th>Plan</th><th>وضعیت</th><th>Activation</th><th>انقضا</th><th></th>
            </tr>
          </thead>
          <tbody>
            {items.map((l) => (
              <tr key={l.id}>
                <td>{l.licenseKeyPrefix}</td>
                <td>{l.customerName}</td>
                <td>{l.productName}</td>
                <td>{l.planName}</td>
                <td><span className="badge">{l.status}</span></td>
                <td>{l.activeActivationCount}/{l.maxActivations}</td>
                <td>{new Date(l.expirationDateUtc).toLocaleString()}</td>
                <td><Link to={`/licenses/${l.id}`}>جزئیات</Link></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
