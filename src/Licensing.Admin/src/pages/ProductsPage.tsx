import { FormEvent, useEffect, useState } from 'react';
import { api } from '../api';

type Product = { id: string; code: string; name: string; description?: string };
type Plan = { id: string; code: string; name: string; defaultMaxActivations: number };
type Feature = { featureKey: string; name: string };

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [selected, setSelected] = useState('');
  const [plans, setPlans] = useState<Plan[]>([]);
  const [features, setFeatures] = useState<Feature[]>([]);
  const [productForm, setProductForm] = useState({ code: '', name: '' });
  const [planForm, setPlanForm] = useState({ code: '', name: '', maxActivations: 3 });
  const [featureForm, setFeatureForm] = useState({ key: '', name: '' });
  const [error, setError] = useState('');

  async function loadProducts() {
    setProducts(await api<Product[]>('/api/v1/admin/products'));
  }

  useEffect(() => { loadProducts().catch((e) => setError(e.message)); }, []);

  useEffect(() => {
    if (!selected) return;
    Promise.all([
      api<Plan[]>(`/api/v1/admin/products/${selected}/plans`),
      api<Feature[]>(`/api/v1/admin/products/${selected}/features`)
    ]).then(([p, f]) => { setPlans(p); setFeatures(f); });
  }, [selected]);

  async function createProduct(e: FormEvent) {
    e.preventDefault();
    await api('/api/v1/admin/products', { method: 'POST', body: JSON.stringify({ code: productForm.code, name: productForm.name }) });
    setProductForm({ code: '', name: '' });
    await loadProducts();
  }

  async function createPlan(e: FormEvent) {
    e.preventDefault();
    if (!selected) return;
    await api(`/api/v1/admin/products/${selected}/plans`, {
      method: 'POST',
      body: JSON.stringify({
        code: planForm.code,
        name: planForm.name,
        defaultMaxActivations: planForm.maxActivations
      })
    });
    setPlans(await api(`/api/v1/admin/products/${selected}/plans`));
  }

  async function createFeature(e: FormEvent) {
    e.preventDefault();
    if (!selected) return;
    await api(`/api/v1/admin/products/${selected}/features`, {
      method: 'POST',
      body: JSON.stringify({ featureKey: featureForm.key, name: featureForm.name })
    });
    setFeatures(await api(`/api/v1/admin/products/${selected}/features`));
  }

  return (
    <>
      <h2>محصولات و Planها</h2>
      {error && <p className="error">{error}</p>}
      <div className="card">
        <h3>محصول جدید</h3>
        <form onSubmit={createProduct}>
          <input placeholder="Code" value={productForm.code} onChange={(e) => setProductForm({ ...productForm, code: e.target.value })} />
          <input placeholder="Name" value={productForm.name} onChange={(e) => setProductForm({ ...productForm, name: e.target.value })} />
          <button type="submit">ایجاد محصول</button>
        </form>
      </div>
      <div className="card">
        <label>انتخاب محصول</label>
        <select value={selected} onChange={(e) => setSelected(e.target.value)}>
          <option value="">—</option>
          {products.map((p) => <option key={p.id} value={p.id}>{p.code} — {p.name}</option>)}
        </select>
      </div>
      {selected && (
        <>
          <div className="card">
            <h3>Plan جدید</h3>
            <form onSubmit={createPlan}>
              <input placeholder="Code" value={planForm.code} onChange={(e) => setPlanForm({ ...planForm, code: e.target.value })} />
              <input placeholder="Name" value={planForm.name} onChange={(e) => setPlanForm({ ...planForm, name: e.target.value })} />
              <input type="number" value={planForm.maxActivations} onChange={(e) => setPlanForm({ ...planForm, maxActivations: Number(e.target.value) })} />
              <button type="submit">ایجاد Plan</button>
            </form>
            <ul>{plans.map((p) => <li key={p.id}>{p.code} — {p.name} (max {p.defaultMaxActivations})</li>)}</ul>
          </div>
          <div className="card">
            <h3>Feature جدید</h3>
            <form onSubmit={createFeature}>
              <input placeholder="feature_key" value={featureForm.key} onChange={(e) => setFeatureForm({ ...featureForm, key: e.target.value })} />
              <input placeholder="Name" value={featureForm.name} onChange={(e) => setFeatureForm({ ...featureForm, name: e.target.value })} />
              <button type="submit">ایجاد Feature</button>
            </form>
            <ul>{features.map((f) => <li key={f.featureKey}>{f.featureKey} — {f.name}</li>)}</ul>
          </div>
        </>
      )}
    </>
  );
}
