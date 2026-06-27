/**
 * Toast.jsx — Componente de notificación tipo toast (banner inferior-derecho)
 * Basado en el estilo de Actividades.jsx: banner verde/rojo sólido con ícono y X
 *
 * Uso básico con el hook useToast:
 *   const { toast, showToast } = useToast();
 *   showToast("Guardado correctamente");          // success
 *   showToast("Ocurrió un error", "error");       // error
 *   showToast("Ten en cuenta que...", "info");    // info
 *   showToast("Sin cambios para guardar", "warning"); // warning
 *
 *   <Toast toast={toast} onClose={hideToast} />
 */

import { useState, useCallback, useRef } from "react";

const ICONS = {
  success: "fas fa-check",
  error:   "fas fa-times",
  warning: "fas fa-exclamation",
  info:    "fas fa-info",
};

/** El componente Toast visual */
export default function Toast({ toast, onClose }) {
  if (!toast?.visible) return null;

  return (
    <div className={`app-toast app-toast--${toast.tipo}`} role="alert" aria-live="assertive">
      <i className={ICONS[toast.tipo] ?? ICONS.success}></i>
      <span className="app-toast__msg">{toast.mensaje}</span>
      <button
        className="app-toast__close"
        onClick={onClose}
        aria-label="Cerrar notificación"
      >
        ×
      </button>
    </div>
  );
}

/** Hook para manejar el estado del toast */
export function useToast(duracion = 3500) {
  const [toast, setToast] = useState({ visible: false, mensaje: "", tipo: "success" });
  const timerRef = useRef(null);

  const showToast = useCallback((mensaje, tipo = "success") => {
    if (timerRef.current) clearTimeout(timerRef.current);
    setToast({ visible: true, mensaje, tipo });
    timerRef.current = setTimeout(
      () => setToast((t) => ({ ...t, visible: false })),
      duracion
    );
  }, [duracion]);

  const hideToast = useCallback(() => {
    if (timerRef.current) clearTimeout(timerRef.current);
    setToast((t) => ({ ...t, visible: false }));
  }, []);

  return { toast, showToast, hideToast };
}
