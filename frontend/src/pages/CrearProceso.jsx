import Toast, { useToast } from "../components/ui/Toast";
import React, { useEffect, useState, useMemo } from "react";
import { useNavigate, useSearchParams, Link } from "react-router-dom";
import { crearProceso, obtenerDominioPorId } from "../services";
import { DASHBOARD_CACHE_KEY, invalidarCacheDashboard } from "../utils/dashboardCache";

const IconInfo = () => (
  <svg width="18" height="18" viewBox="0 0 20 20" fill="none" stroke="#1d4ed8" strokeWidth="1.6">
    <rect x="3" y="2" width="14" height="16" rx="2" />
    <line x1="7" y1="7" x2="13" y2="7" />
    <line x1="7" y1="10" x2="13" y2="10" />
    <line x1="7" y1="13" x2="10" y2="13" />
  </svg>
);

const IconSub = () => (
  <svg width="18" height="18" viewBox="0 0 20 20" fill="none" stroke="#7c3aed" strokeWidth="1.6">
    <path d="M4 4h12M4 8h8M4 12h10M4 16h6" strokeLinecap="round" />
  </svg>
);

const IconCalc = () => (
  <svg width="18" height="18" viewBox="0 0 20 20" fill="none" stroke="#6b7280" strokeWidth="1.6">
    <rect x="3" y="3" width="14" height="14" rx="2" />
    <line x1="7" y1="7" x2="13" y2="7" />
    <line x1="7" y1="10" x2="13" y2="10" />
    <line x1="7" y1="13" x2="13" y2="13" />
  </svg>
);

const IconSave = () => (
  <svg width="14" height="14" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M5 2H14L18 6V18A1 1 0 0117 19H3A1 1 0 012 18V3A1 1 0 013 2Z" />
    <polyline points="7,2 7,9 13,9 13,2" />
    <rect x="5" y="13" width="10" height="6" />
  </svg>
);

const IconPlus = () => (
  <svg width="13" height="13" viewBox="0 0 14 14" fill="none" stroke="currentColor" strokeWidth="2">
    <line x1="7" y1="1" x2="7" y2="13" />
    <line x1="1" y1="7" x2="13" y2="7" />
  </svg>
);

const IconTrash = () => (
  <svg width="13" height="13" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.8">
    <polyline points="3,6 17,6" />
    <path d="M8 6V4h4v2" />
    <rect x="5" y="6" width="10" height="12" rx="1" />
  </svg>
);

export default function CrearProceso() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const dominioId = Number(params.get("dominioId"));

  const [dominioInfo, setDominioInfo] = useState(null);
  const [loadingDominio, setLoadingDominio] = useState(true);

  const initialForm = {
    codigo: "",
    nombre: "",
    marcoNormativo: "",
    estadoImplementacion: "Sí",
    prioridadImplementacion: "",
    subdominios: [{ practicasGobierno: "", indicadoresAsociados: "" }],
  };

  const [form, setForm] = useState(initialForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const { toast, showToast, hideToast } = useToast();

  useEffect(() => {
    const fetchDominio = async () => {
      if (!dominioId) { setLoadingDominio(false); return; }
      try {
        const data = await obtenerDominioPorId(dominioId);
        setDominioInfo(data);
      } catch (err) {
        setDominioInfo(null);
      } finally {
        setLoadingDominio(false);
      }
    };
    fetchDominio();
  }, [dominioId]);

  const canSubmit = useMemo(() => {
    const codigoOk = form.codigo.trim().length > 0;
    const nombreOk = form.nombre.trim().length > 0;
    const marcoOk  = form.marcoNormativo.trim().length > 0;
    const prioridadOk =
      form.prioridadImplementacion === "" ||
      (Number(form.prioridadImplementacion) >= 1 && Number(form.prioridadImplementacion) <= 3);
    return codigoOk && nombreOk && marcoOk && prioridadOk && !saving;
  }, [form, saving]);

  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({ ...f, [name]: value }));
  };

  const onChangeSub = (idx, field, value) => {
    setForm((f) => {
      const next = [...f.subdominios];
      next[idx] = { ...next[idx], [field]: value };
      return { ...f, subdominios: next };
    });
  };

  const addSub = () => {
    setForm((f) => ({
      ...f,
      subdominios: [...f.subdominios, { practicasGobierno: "", indicadoresAsociados: "" }],
    }));
  };

  const removeSub = (idx) => {
    setForm((f) => ({ ...f, subdominios: f.subdominios.filter((_, i) => i !== idx) }));
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    if (!canSubmit) return;
    setSaving(true);
    setError("");
    showToast("");
    try {
      const payload = {
        dominioId,
        codigo: form.codigo.trim(),
        nombre: form.nombre.trim(),
        marcoNormativo: form.marcoNormativo.trim(),
        estadoImplementacion: form.estadoImplementacion,
        prioridadImplementacion:
          form.prioridadImplementacion === "" ? null : Number(form.prioridadImplementacion),
        subdominios: form.subdominios
          .filter((s) => s.practicasGobierno.trim())
          .map((s) => ({
            practicasGobierno: s.practicasGobierno.trim(),
            indicadoresAsociados: s.indicadoresAsociados.trim(),
          })),
      };
      const resp = await crearProceso(payload);
      const newId = resp?.id ?? resp?.idProceso ?? "(desconocido)";
      showToast(`Proceso creado correctamente (ID ${newId}).`);
      setForm(initialForm);
      try { invalidarCacheDashboard(); } catch (e) {}
      navigate("/");
    } catch (err) {
      setError(err?.message || "Error guardando el proceso");
    } finally {
      setSaving(false);
    }
  };

  const tituloDominio = useMemo(() => {
    if (loadingDominio) return "Cargando dominio...";
    if (!dominioInfo) return "Dominio desconocido";
    const codigo = dominioInfo?.codigo ?? "";
    const nombre = dominioInfo?.nombre ?? "";
    return `${codigo ? codigo + " — " : ""}${nombre}`;
  }, [dominioInfo, loadingDominio]);

  return (
    <div className="act-page">

      <nav className="act-breadcrumb">
        <Link to="/">Inicio</Link>
        <span className="sep">›</span>
        <Link to="/processes">Gestión de Procesos</Link>
        <span className="sep">›</span>
        <span className="current">Nuevo proceso</span>
      </nav>

      {/* Header */}
      <div className="act-header">
        <div className="act-header-left">
          <h1>Nuevo proceso</h1>
          <p>{tituloDominio}</p>
        </div>
      </div>

      {error   && <div className="act-alert danger"  role="alert"><span>{error}</span></div>}
      <Toast toast={toast} onClose={hideToast} />

      <form onSubmit={onSubmit}>

        {/* Card: Información general */}
        <div className="act-card card-info">
          <div className="act-card-header">
            <div className="act-card-icon blue"><IconInfo /></div>
            <span className="act-card-title">Información general</span>
          </div>
          <div className="act-card-body">

            <div className="act-grid2">
              <div className="act-field">
                <label className="act-label">Código <span className="req">*</span></label>
                <input
                  className="act-input"
                  name="codigo"
                  value={form.codigo}
                  onChange={onChange}
                  placeholder="EDM01, APO02..."
                  disabled={saving}
                  required
                />
              </div>
              <div className="act-field">
                <label className="act-label">Prioridad de implementación</label>
                <select
                  className="act-select"
                  name="prioridadImplementacion"
                  value={form.prioridadImplementacion}
                  onChange={onChange}
                  disabled={saving}
                >
                  <option value="">Sin asignar</option>
                  <option value="1">Etapa 1</option>
                  <option value="2">Etapa 2</option>
                  <option value="3">Etapa 3</option>
                </select>
              </div>
            </div>

            <div className="act-field">
              <label className="act-label">Nombre <span className="req">*</span></label>
              <input
                className="act-input"
                name="nombre"
                value={form.nombre}
                onChange={onChange}
                placeholder="Asegurar el marco de gobierno"
                disabled={saving}
                required
              />
            </div>

            <div className="act-field">
              <label className="act-label">Marco normativo <span className="req">*</span></label>
              <input
                className="act-input"
                name="marcoNormativo"
                value={form.marcoNormativo}
                onChange={onChange}
                placeholder="COBIT 2019, ISO/IEC 38500"
                disabled={saving}
                required
              />
            </div>

            <div className="act-field">
              <label className="act-label">Implementable</label>
              <div className="act-toggle-row">
                <label className="act-toggle">
                  <input
                    type="checkbox"
                    checked={form.estadoImplementacion === "Sí"}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, estadoImplementacion: e.target.checked ? "Sí" : "No" }))
                    }
                    disabled={saving}
                  />
                  <span className="act-toggle-track" />
                  <span className="act-toggle-thumb" />
                </label>
                <span className="act-toggle-label">
                  {form.estadoImplementacion === "Sí" ? "Sí" : "No"}
                </span>
              </div>
            </div>

          </div>
        </div>

        {/* Card: Prácticas de gobierno (subdominios) */}
        <div className="act-card card-resp">
          <div className="act-card-header">
            <div className="act-card-icon purple"><IconSub /></div>
            <div className="act-card-title-wrap">
              <span className="act-card-title">Prácticas de gobierno</span>
              <span className="act-card-subtitle">Subdominios del proceso</span>
            </div>
          </div>
          <div className="act-card-body">

            {form.subdominios.map((s, idx) => (
              <div key={idx} className="proc-subdominio">
                <div className="proc-subdominio-header">
                  <span className="proc-subdominio-num">Práctica {idx + 1}</span>
                  {form.subdominios.length > 1 && (
                    <button
                      type="button"
                      className="proc-btn-quitar"
                      onClick={() => removeSub(idx)}
                      disabled={saving}
                    >
                      <IconTrash /> Quitar
                    </button>
                  )}
                </div>

                <div className="act-field">
                  <label className="act-label">Prácticas de gobierno <span className="req">*</span></label>
                  <input
                    className="act-input"
                    value={s.practicasGobierno}
                    onChange={(e) => onChangeSub(idx, "practicasGobierno", e.target.value)}
                    placeholder="Evaluar el sistema de gobierno"
                    disabled={saving}
                    required
                  />
                </div>

                <div className="act-field">
                  <label className="act-label">Indicadores asociados</label>
                  <textarea
                    className="act-textarea"
                    rows={3}
                    value={s.indicadoresAsociados}
                    onChange={(e) => onChangeSub(idx, "indicadoresAsociados", e.target.value)}
                    placeholder={"a. Ciclo de vida real vs. objetivo para decisiones clave\nb. Frecuencia de revisiones independientes del gobierno"}
                    disabled={saving}
                  />
                </div>
              </div>
            ))}

            <button
              type="button"
              className="proc-btn-agregar"
              onClick={addSub}
              disabled={saving}
            >
              <IconPlus /> Agregar práctica
            </button>

          </div>
        </div>

        {/* Card: Campos calculados (solo lectura) */}
        <div className="act-card card-obs">
          <div className="act-card-header">
            <div className="act-card-icon" className="doc-card-icon-gray"><IconCalc /></div>
            <div className="act-card-title-wrap">
              <span className="act-card-title">Campos calculados</span>
              <span className="act-card-subtitle">Se actualizan automáticamente</span>
            </div>
          </div>
          <div className="act-card-body">
            <div className="act-grid2">
              <div className="act-field">
                <label className="act-label">Porcentaje de avance</label>
                <input className="act-input act-input--readonly" value="0.00%" readOnly disabled />
                <span className="proc-hint">Se calcula a partir de las actividades.</span>
              </div>
              <div className="act-field">
                <label className="act-label">Fecha de conclusión</label>
                <input
                  className="act-input act-input--readonly"
                  value=""
                  placeholder="Se establece al llegar a 100%"
                  readOnly
                  disabled
                />
              </div>
            </div>
          </div>
        </div>

        {/* Botones */}
        <div className="act-actions">
          <button
            type="button"
            className="act-btn-cancel"
            onClick={() => navigate(-1)}
            disabled={saving}
          >
            Cancelar
          </button>
          <button type="submit" className="act-btn-save" disabled={!canSubmit}>
            <IconSave />
            {saving ? "Guardando..." : "Guardar proceso"}
          </button>
        </div>

      </form>
    </div>
  );
}