export const IconDominio = ({ size = 16 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
    stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    <circle cx="12" cy="12" r="10"/>
    <line x1="2" y1="12" x2="22" y2="12"/>
    <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
  </svg>
);

export const IconProceso = ({ size = 16 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
    stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    <rect x="2" y="3" width="20" height="14" rx="2"/>
    <path d="M8 21h8M12 17v4"/>
  </svg>
);

export const IconSubdominio = ({ size = 16 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
    stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
    <polyline points="9 22 9 12 15 12 15 22"/>
  </svg>
);

export const IconActividad = ({ size = 16 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
    stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
    <polyline points="14 2 14 8 20 8"/>
    <line x1="9" y1="13" x2="15" y2="13"/>
    <line x1="9" y1="17" x2="13" y2="17"/>
  </svg>
);

/* ── Color por dominio COBIT ───────────────────────────────────── */
export const getDomainColor = (name = '') => {
  const n = typeof name === 'string' ? name : String(name ?? '');
  if (n.includes('EDM')) return '#e74c3c';
  if (n.includes('APO')) return '#3498db';
  if (n.includes('BAI')) return '#f39c12';
  if (n.includes('DSS')) return '#27ae60';
  if (n.includes('MEA')) return '#9b59b6';
  return '#6c757d';
};

/* ── Contenedor de ícono de dominio con color dinámico ─────────── */
export const DominioIconBox = ({ nombre = '', size = 30 }) => {
  const color = getDomainColor(nombre);
  return (
    <span style={{
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      width: size,
      height: size,
      borderRadius: 8,
      background: color + '18',
      border: `1px solid ${color}44`,
      color,
      flexShrink: 0,
    }}>
      <IconDominio size={size * 0.55} />
    </span>
  );
};

/* ── Contenedor de ícono de proceso (violeta) ──────────────────── */
export const ProcesoIconBox = ({ size = 28 }) => (
  <span className="tree-icon-box tree-icon-box--proceso" style={{ width: size, height: size }}>
    <IconProceso size={size * 0.55} />
  </span>
);

/* ── Contenedor de ícono de subdominio (ámbar) ─────────────────── */
export const SubdominioIconBox = ({ size = 26 }) => (
  <span className="tree-icon-box tree-icon-box--subdominio" style={{ width: size, height: size }}>
    <IconSubdominio size={size * 0.55} />
  </span>
);

/* ── Contenedor de ícono de actividad (verde) ──────────────────── */
export const ActividadIconBox = ({ size = 26 }) => (
  <span className="tree-icon-box tree-icon-box--actividad" style={{ width: size, height: size }}>
    <IconActividad size={size * 0.55} />
  </span>
);

/* ── Leyenda completa del árbol ────────────────────────────────── */
export const TreeLegend = () => (
  <div className="tree-legend">
    <span className="tree-legend-title">Referencia de niveles:</span>
    <div className="tree-legend-items">
      <span className="tree-legend-item tree-legend-dominio">
        <IconDominio size={14} /> Dominio
      </span>
      <span className="tree-legend-item tree-legend-proceso">
        <IconProceso size={14} /> Proceso
      </span>
      <span className="tree-legend-item tree-legend-subdominio">
        <IconSubdominio size={14} /> Práctica / Subdominio
      </span>
      <span className="tree-legend-item tree-legend-actividad">
        <IconActividad size={13} /> Actividad
      </span>
    </div>
  </div>
);