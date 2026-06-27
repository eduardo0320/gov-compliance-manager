import { useState, useEffect, useCallback } from "react";
import { useNavigate, useSearchParams, Link } from "react-router-dom";
import { buscarDocumentos } from "../../services";

const ESTADO_BADGE = {
  Borrador:    "badge-secondary",
  En_Revision: "badge-warning",
  Aprobado:    "badge-info",
  Vigente:     "badge-success",
  Obsoleto:    "badge-danger",
  Archivado:   "",
};

const TIPO_ICON = {
  PDF:  "fas fa-file-pdf",
  DOCX: "fas fa-file-word",
  DOC:  "fas fa-file-word",
  PPTX: "fas fa-file-powerpoint",
  XLSX: "fas fa-file-excel",
  XLS:  "fas fa-file-excel",
  URL:  "fas fa-link",
  OTRO: "fas fa-file",
};

function formatFecha(fecha) {
  if (!fecha) return "—";
  return new Date(fecha).toLocaleDateString("es-ES", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function esVencido(fecha) {
  if (!fecha) return false;
  return new Date(fecha) < new Date();
}

const FILTROS_VACÍOS = {
  nombre: "", estado: "", tipoDocumento: "",
  vencimientoDesde: "", vencimientoHasta: "",
  soloVencidos: false, limite: 100,
};

const IcoBuscar = () => (
  <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.8">
    <circle cx="9" cy="9" r="6" /><line x1="13.5" y1="13.5" x2="18" y2="18" />
  </svg>
);

export default function BuscarDocumentos() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const initFiltros = () => ({ ...FILTROS_VACÍOS, soloVencidos: searchParams.get("soloVencidos") === "true" });

  const [filtros,    setFiltros]    = useState(initFiltros);
  const [resultados, setResultados] = useState([]);
  const [cargando,   setCargando]   = useState(false);
  const [error,      setError]      = useState("");
  const [buscado,    setBuscado]    = useState(false);

  useEffect(() => {
    if (searchParams.get("soloVencidos") === "true") {
      ejecutarBusqueda({ ...FILTROS_VACÍOS, soloVencidos: true, limite: 100 });
    }
  }, []);

  const ejecutarBusqueda = useCallback(async (f = filtros) => {
    setCargando(true); setError(""); setBuscado(true);
    try {
      const data = await buscarDocumentos(f);
      setResultados(data ?? []);
    } catch (err) {
      setError(err.message ?? "Error al buscar documentos");
      setResultados([]);
    } finally {
      setCargando(false);
    }
  }, [filtros]);

  const handleSubmit = (e) => { e.preventDefault(); ejecutarBusqueda(filtros); };
  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFiltros(prev => ({ ...prev, [name]: type === "checkbox" ? checked : value }));
  };
  const handleLimpiar = () => { setFiltros(FILTROS_VACÍOS); setResultados([]); setBuscado(false); setError(""); };

  return (
    <div className="act-page">

      <nav className="act-breadcrumb">
        <Link to="/">Inicio</Link>
        <span className="sep">›</span>
        <span className="current">Buscar documentos</span>
      </nav>

      {/* Header */}
      <div className="act-header">
        <div className="act-header-left">
          <h1>Buscar documentos</h1>
          <p>Filtrá por nombre, estado, tipo o fechas de vencimiento.</p>
        </div>
      </div>

      {/* Card de filtros */}
      <div className="act-card card-info">
        <div className="act-card-header">
          <div className="act-card-icon blue"><IcoBuscar /></div>
          <span className="act-card-title">Filtros de búsqueda</span>
        </div>
        <div className="act-card-body">
          <form onSubmit={handleSubmit}>
            <div className="docs-form-grid">
              <div className="docs-form-field docs-field-full">
                <label className="act-label">Nombre del documento</label>
                <input
                  className="act-input"
                  type="text"
                  name="nombre"
                  value={filtros.nombre}
                  onChange={handleChange}
                  placeholder="Buscar por nombre..."
                  autoComplete="off"
                />
              </div>
              <div className="docs-form-field docs-field-sm">
                <label className="act-label">Estado</label>
                <select className="act-select" name="estado" value={filtros.estado} onChange={handleChange}>
                  <option value="">Todos</option>
                  <option value="Borrador">Borrador</option>
                  <option value="En_Revision">En Revisión</option>
                  <option value="Aprobado">Aprobado</option>
                  <option value="Vigente">Vigente</option>
                  <option value="Obsoleto">Obsoleto</option>
                  <option value="Archivado">Archivado</option>
                </select>
              </div>
              <div className="docs-form-field docs-field-sm">
                <label className="act-label">Tipo</label>
                <select className="act-select" name="tipoDocumento" value={filtros.tipoDocumento} onChange={handleChange}>
                  <option value="">Todos</option>
                  <option value="PDF">PDF</option>
                  <option value="DOCX">DOCX</option>
                  <option value="DOC">DOC</option>
                  <option value="PPTX">PPTX</option>
                  <option value="XLSX">XLSX</option>
                  <option value="XLS">XLS</option>
                  <option value="URL">URL / Enlace</option>
                  <option value="OTRO">Otro</option>
                </select>
              </div>
              <div className="docs-form-field docs-field-sm">
                <label className="act-label">Vence desde</label>
                <input className="act-input" type="date" name="vencimientoDesde" value={filtros.vencimientoDesde} onChange={handleChange} />
              </div>
              <div className="docs-form-field docs-field-sm">
                <label className="act-label">Vence hasta</label>
                <input className="act-input" type="date" name="vencimientoHasta" value={filtros.vencimientoHasta} onChange={handleChange} />
              </div>
            </div>

            <div className="det-filtros-footer">
              <label className="det-check-label">
                <input
                  type="checkbox"
                  name="soloVencidos"
                  checked={filtros.soloVencidos}
                  onChange={handleChange}
                  className="doc-check-mr"
                />
                Mostrar solo documentos vencidos
              </label>
              <div className="doc-filtros-footer-gap">
                <button type="button" className="act-btn-cancel" onClick={handleLimpiar}>Limpiar</button>
                <button type="submit" className="act-btn-save" disabled={cargando}>
                  <IcoBuscar />
                  {cargando ? "Buscando..." : "Buscar"}
                </button>
              </div>
            </div>
          </form>
        </div>
      </div>

      {error && <div className="act-alert danger">{error}</div>}

      {/* Resultados */}
      {buscado && !cargando && !error && (
        <>
          <p className="det-meta-label doc-results-label">
            {resultados.length === 0
              ? "No se encontraron documentos con los filtros indicados."
              : <>Se {resultados.length === 1 ? "encontró" : "encontraron"} <strong>{resultados.length}</strong> {resultados.length === 1 ? "documento" : "documentos"}.</>
            }
          </p>

          {resultados.length > 0 ? (
            <div className="act-card">
              <div className="act-card-body act-card-body--no-top">
                <div className="doc-table-container logs-table-container">
                  <table className="logs-table doc-table-min">
                    <thead>
                      <tr>
                        <th className="doc-th-icon"></th>
                        <th>Nombre</th>
                        <th>Estado</th>
                        <th>Versión</th>
                        <th>Vencimiento</th>
                        <th>Actividad</th>
                        <th className="doc-th-actions">Acciones</th>
                      </tr>
                    </thead>
                    <tbody>
                      {resultados.map(doc => {
                        const vencido = esVencido(doc.fechaVencimiento);
                        const versionTexto = typeof doc.versionActual === "string"
                          ? doc.versionActual
                          : (doc.versionActual?.versionTexto ?? "—");
                        const actividadTexto =
                          doc.actividadNombre ?? doc.actividad?.nombre ??
                          (doc.actividadId ? `Actividad #${doc.actividadId}` : "—");
                        return (
                          <tr key={doc.id}>
                            <td className="doc-td-center">
                              <i className={`${TIPO_ICON[doc.tipoDocumento] ?? "fas fa-file"} doc-type-icon`}></i>
                            </td>
                            <td className="doc-td-nombre">
                              <span className="doc-td-nombre-text" title={doc.nombre}>{doc.nombre}</span>
                              {doc.descripcion && (
                                <small className="doc-td-desc">
                                  {doc.descripcion.length > 60 ? doc.descripcion.slice(0, 60) + "…" : doc.descripcion}
                                </small>
                              )}
                            </td>
                            <td>
                              <span className={`badge ${ESTADO_BADGE[doc.estado] ?? "badge-secondary"}`}>
                                {doc.estado?.replace("_", " ") ?? "—"}
                              </span>
                            </td>
                            <td className="doc-td-version">{versionTexto}</td>
                            <td>
                              {doc.fechaVencimiento ? (
                                <span className={vencido ? "doc-fecha-vencida" : "doc-fecha-ok"}>
                                  {vencido && <i className="fas fa-exclamation-triangle doc-fecha-vencida-icon"></i>}
                                  {formatFecha(doc.fechaVencimiento)}
                                </span>
                              ) : <span className="doc-fecha-none">—</span>}
                            </td>
                            <td className="doc-td-meta">
                              <span className="doc-td-meta-text">{actividadTexto}</span>
                            </td>
                            <td className="doc-td-center">
                              <button className="btn-icon" title="Ver detalle" onClick={() => navigate(`/documentos/${doc.id}`)}>
                                <i className="fas fa-eye"></i>
                              </button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          ) : (
            <div className="docs-empty">
              <i className="fas fa-search docs-empty-icon"></i>
              Sin resultados — probá ajustando los filtros.
            </div>
          )}
        </>
      )}

      {!buscado && !cargando && (
        <div className="docs-empty">
          <i className="fas fa-folder-open docs-empty-icon"></i>
          Ingresá los criterios de búsqueda para comenzar.
        </div>
      )}
    </div>
  );
}