type Props = {
  open: boolean;
  title: string;
  message: string;
  onCancel: () => void;
  onConfirm: () => void;
  confirmLabel?: string;
  danger?: boolean;
};

export default function ConfirmDialog({ open, title, message, onCancel, onConfirm, confirmLabel = 'تأیید', danger }: Props) {
  if (!open) return null;
  return (
    <div className="modal-backdrop">
      <div className="modal">
        <h3>{title}</h3>
        <p>{message}</p>
        <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
          <button className="secondary" onClick={onCancel}>انصراف</button>
          <button className={danger ? 'danger' : ''} onClick={onConfirm}>{confirmLabel}</button>
        </div>
      </div>
    </div>
  );
}
