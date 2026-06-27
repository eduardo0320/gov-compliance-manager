import { useState, useCallback } from "react";

export default function ResultDialog({ dialog, onClose }) {
  if (!dialog?.visible) return null;

  const esError = dialog.tipo === "error";

  return (
    <div
      className="result-dialog-overlay"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="result-dialog-title"
    >
      <div className="result-dialog">
        <div className={`result-dialog-icon ${dialog.tipo}`}>
          <i className={esError ? "fas fa-times" : "fas fa-check"}></i>
        </div>
        <h3 className="result-dialog-title" id="result-dialog-title">
          {dialog.titulo}
        </h3>
        <p className="result-dialog-msg">{dialog.mensaje}</p>
        <div className="result-dialog-sep"></div>
        <button
          className={`result-dialog-btn ${dialog.tipo}`}
          onClick={onClose}
          autoFocus
        >
          Listo
        </button>
      </div>
    </div>
  );
}

export function useDialog() {
  const [dialog, setDialog] = useState({ visible: false, tipo: "success", titulo: "", mensaje: "" });

  const showDialog = useCallback((titulo, mensaje, tipo = "success") => {
    setDialog({ visible: true, tipo, titulo, mensaje });
  }, []);

  const closeDialog = useCallback(() => {
    setDialog((d) => ({ ...d, visible: false }));
  }, []);

  return { dialog, showDialog, closeDialog };
}

export function ConfirmDialog({ confirm, onClose }) {
  if (!confirm?.visible) return null;

  const handleConfirm = () => {
    onClose();
    confirm.onConfirm?.();
  };

  return (
    <div
      className="result-dialog-overlay"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-dialog-title"
    >
      <div className="result-dialog">
        <div className="result-dialog-icon confirm">
          <i className="fas fa-question"></i>
        </div>
        <h3 className="result-dialog-title" id="confirm-dialog-title">
          {confirm.titulo}
        </h3>
        {confirm.mensaje && (
          <p className="result-dialog-msg">{confirm.mensaje}</p>
        )}
        <div className="result-dialog-sep"></div>
        <div className="result-dialog-actions">
          <button className="result-dialog-btn-cancel" onClick={onClose}>
            Cancelar
          </button>
          <button className="result-dialog-btn success" onClick={handleConfirm} autoFocus>
            Confirmar
          </button>
        </div>
      </div>
    </div>
  );
}

export function useConfirm() {
  const [confirm, setConfirm] = useState({ visible: false, titulo: "", mensaje: "", onConfirm: null });

  const askConfirm = useCallback((titulo, mensaje, onConfirm) => {
    setConfirm({ visible: true, titulo, mensaje, onConfirm });
  }, []);

  const closeConfirm = useCallback(() => {
    setConfirm((c) => ({ ...c, visible: false, onConfirm: null }));
  }, []);

  return { confirm, askConfirm, closeConfirm };
}