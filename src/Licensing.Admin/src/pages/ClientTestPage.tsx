import { FormEvent, useState } from 'react';

export default function ClientTestPage() {
  const [form, setForm] = useState({
    licenseKey: '',
    productCode: 'PRODUCT-A',
    instanceId: 'server-1',
    productVersion: '1.0.0'
  });
  const [result, setResult] = useState('');

  async function call(path: string) {
    const res = await fetch(`/api/v1/client/licenses/${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        licenseKey: form.licenseKey,
        productCode: form.productCode,
        instanceIdentifier: form.instanceId,
        productVersion: form.productVersion
      })
    });
    const text = await res.text();
    setResult(text);
  }

  function onValidate(e: FormEvent) {
    e.preventDefault();
    call('validate');
  }

  return (
    <>
      <h2>تست Client API</h2>
      <div className="card">
        <form onSubmit={onValidate}>
          <input placeholder="License Key" value={form.licenseKey} onChange={(e) => setForm({ ...form, licenseKey: e.target.value })} />
          <input placeholder="Product Code" value={form.productCode} onChange={(e) => setForm({ ...form, productCode: e.target.value })} />
          <input placeholder="Instance ID" value={form.instanceId} onChange={(e) => setForm({ ...form, instanceId: e.target.value })} />
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button type="button" onClick={() => call('activate')}>Activate</button>
            <button type="button" onClick={() => call('deactivate')}>Deactivate</button>
            <button type="submit">Validate</button>
          </div>
        </form>
        <pre style={{ whiteSpace: 'pre-wrap' }}>{result}</pre>
      </div>
    </>
  );
}
