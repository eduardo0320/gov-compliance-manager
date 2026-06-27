import React, { useState, useEffect, useMemo } from "react";
import { useNavigate, useParams, Link } from "react-router-dom";
import { editarProceso, getDominios, getProcesoById } from "../services";
import { DASHBOARD_CACHE_KEY, invalidarCacheDashboard } from "../utils/dashboardCache";
import Toast, { useToast } from "../components/ui/Toast";

function derivarCodigoDominio(nombre) {
  if (!nombre || typeof nombre !== "string") return "";
  const parts = nombre.split(" - ");
  if (parts.length > 1 && /^[A-Z0-9]{2,6}$/.test(parts[0].trim())) return parts[0].trim();
  const m = nombre.trim().match(/^[A-Z0-9]{2,6}\b/);
  return m ? m[0] : "";
}

const IconInfo = () => (
  <svg width="18" height="18" viewBox="0 0 20 20" fill="none" stroke="#1d4ed8" strokeWidth="1.6">
    <rect x="3" y="2" width="14" height="16" rx="2" />
    <line x1="7" y1="7" x2="13" y2="7" />
    <line x1="7" y1="10" x2="13" y2="10" />
    <line x1="7" y1="13" x2="10" y2="13" />
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

export default function EditarProceso() {
  const navigate = useNavigate();
  const { id } = useParams();

  const [dominios, setDominios] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const { toast, showToast, hideToast } = useToast();

  const [form, setForm] = useState({
    codigo: "",
    nombre: "",
    marcoNormativo: "",
    estadoImplementacion: "Sí",
    dominioId: 1,
    prioridadImplementacion: "",
  });

  const [porcentajeAvance, setPorcentajeAvance] = useState(0.0);
  const [fechaConclusion, setFechaConclusion] = useState("");

  useEffect(() => {
    const cargarDatos = async () => {
      try {
        setLoading(true);
        const [dominiosData, procesoData] = await Promise.all([
          getDominios(),
          getProcesoById(id),
        ]);
        setDominios(dominiosData);
        setForm({
          codigo: procesoData.codigo || "",
          nombre: procesoData.nombre || "",
          marcoNormativo: procesoData.marcoNormativo || procesoData.marco_normativo || "",
          estadoImplementacion: procesoData.estadoImplementacion || procesoData.estado_implementacion || "Sí",
          dominioId: procesoData.dominioId ?? procesoData.dominio?.id ?? 1,
          prioridadImplementacion: (() => {
            const v = procesoData.prioridadImplementacion ?? procesoData.prioridad_implementacion;
            return v === null || v === undefined || v === "" ? "" : String(v);
          })(),
        });
        setPorcentajeAvance(Number(procesoData.porcentajeAvance ?? procesoData.porcentaje_avance ?? 0));
        const fecha = procesoData.fechaConclusionImplementacion ?? procesoData.fecha_conclusion_implementacion ?? "";
        setFechaConclusion(fecha ? new Date(fecha).toLocaleString() : "");
      } catch (err) {
        setError("Error cargando los datos del proceso");
      } finally {
        setLoading(false);
      }
    };
    if (id) cargarDatos();
  }, [id]);

  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({ ...f, [name]: value }));
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    setError("");
    if (!form.codigo.trim() || !form.nombre.trim()) {
      setError("Código y Nombre son requeridos");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        codigo: form.codigo.trim(),
        nombre: form.nombre.trim(),
        marcoNormativo: form.marcoNormativo.trim(),
        estadoImplementacion: form.estadoImplementacion,
        dominioId: Number(form.dominioId),
        prioridadImplementacion:
          form.prioridadImplementacion === "" ? null : Number(form.prioridadImplementacion),
      };
      await editarProceso(id, payload);
      try { invalidarCacheDashboard(); } catch (e) {}
      showToast("Proceso actualizado correctamente.");
      setTimeout(() => navigate("/"), 1500);
    } catch (err) {
      setError(err.message || "Error guardando el proceso");
    } finally {
      setSaving(false);
    }
  };

  const dominioSeleccionado = useMemo(
    () => dominios.find((d) => (d.id ?? d.id_Dominio ?? d.IdDominio) === Number(form.dominioId)),
    [dominios, form.dominioId]
  );

  const tituloDominio = useMemo(() => {
    if (!dominioSeleccionado) return "";
    const nombre = dominioSeleccionado.nombre ?? dominioSeleccionado.Nombre ?? "";
    const codigo = dominioSeleccionado.codigo ?? derivarCodigoDominio(nombre);
    return codigo ? `${codigo} — ${nombre}` : nombre;
  }, [dominioSeleccionado]);

  if (loading) {
    return <div className="act-loading">Cargando proceso...</div>;
  }

  return (
    <div className="act-page">

      <nav className="act-breadcrumb">
        <Link to="/">Inicio</Link>
        <span className="sep">›</span>
        <Link to="/processes">Gestión de Procesos</Link>
        <span className="sep">›</span>
        <span className="current">Editar proceso</span>
      </nav>

      {/* Header */}
      <div className="act-header">
        <div className="act-header-left">
          <h1>Editar proceso</h1>
          <p>{tituloDominio || "Modifica la información del proceso y guarda los cambios."}</p>
        </div>
      </div>

      {error && <div className="act-alert danger" role="alert"><span>{error}</span></div>}
      <Toast toast={toast} onClose={hideToast} />

      <form onSubmit={onSubmit}>

        {/* Card: Información general */}
        <div className="act-card card-info">
          <div className="act-card-header">
            <div className="act-card-icon blue"><IconInfo /></div>
            <span className="act-card-title">Información general</span>
          </div>
          <div className="act-card-body">

            <div className="act-field">
              <label className="act-label">Dominio <span className="req">*</span></label>
              <select
                className="act-select"
                name="dominioId"
                value={form.dominioId}
                onChange={onChange}
                disabled={saving}
                required
              >
                {dominios.map((dominio) => {
                  const idDom   = dominio.id ?? dominio.id_Dominio ?? dominio.IdDominio;
                  const nombre  = dominio.nombre ?? dominio.Nombre ?? "";
                  const codigo  = dominio.codigo ?? derivarCodigoDominio(nombre);
                  const etiqueta = codigo ? `${codigo} - ${nombre}` : nombre;
                  return (
                    <option key={idDom} value={idDom}>{etiqueta}</option>
                  );
                })}
              </select>
            </div>

            <div className="act-grid2">
              <div className="act-field">
                <label className="act-label">Código <span className="req">*</span></label>
                <input
                  className="act-input"
                  name="codigo"
                  value={form.codigo}
                  onChange={onChange}
                  placeholder="Ej: APO01"
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
                  <option value="">(sin asignar)</option>
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
                placeholder="Nombre descriptivo del proceso"
                disabled={saving}
                required
              />
            </div>

            <div className="act-field">
              <label className="act-label">Marco normativo</label>
              <textarea
                className="act-textarea"
                name="marcoNormativo"
                value={form.marcoNormativo}
                onChange={onChange}
                placeholder="Marco normativo que rige el proceso"
                rows={3}
                disabled={saving}
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

        <div className="act-card card-obs">
          <div className="act-card-header">
            <div className="act-card-icon" className="doc-card-icon-gray"><IconCalc /></div>
            <div className="act-card-title-wrap">
              <span className="act-card-title">Campos Automáticos</span>
              <span className="act-card-subtitle">Se actualizan automáticamente</span>
            </div>
          </div>
          <div className="act-card-body">
            <div className="act-grid2">
              <div className="act-field">
                <label className="act-label">Porcentaje de avance</label>
                <input
                  className="act-input act-input--readonly"
                  value={`${Number.isFinite(porcentajeAvance) ? porcentajeAvance.toFixed(2) : "0.00"}%`}
                  readOnly
                  disabled
                />
                <span className="proc-hint">Se calcula a partir de las actividades.</span>
              </div>
              <div className="act-field">
                <label className="act-label">Fecha de conclusión</label>
                <input
                  className="act-input act-input--readonly"
                  value={fechaConclusion || ""}
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
            onClick={() => navigate("/")}
            disabled={saving}
          >
            Cancelar
          </button>
          <button type="submit" className="act-btn-save" disabled={saving}>
            <IconSave />
            {saving ? "Guardando..." : "Actualizar proceso"}
          </button>
        </div>

      </form>
    </div>
  );
}