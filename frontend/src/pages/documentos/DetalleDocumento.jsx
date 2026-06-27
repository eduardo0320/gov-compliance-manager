import Toast, { useToast } from "../../components/ui/Toast";
import { useEffect, useState, useRef } from "react";
import { useNavigate, useParams, useLocation, Link } from "react-router-dom";
import {
  getDocumento,
  subirNuevaVersion,
  descargarDocumento,
  actualizarDocumento,
  crearRelacionDocumento,
  buscarDocumentos,
  getCurrentUserInfo,
} from "../../services";

// ── Helpers ───────────────────────────────────────────────────────────────────

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

const MAX_BYTES = 50 * 1024 * 1024;

function formatBytes(bytes) {
  if (!bytes) return "";
  if (bytes < 1024) return bytes + " B";
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB";
  return (bytes / (1024 * 1024)).toFixed(1) + " MB";
}

function formatFecha(fecha) {
  if (!fecha) return "—";
  return new Date(fecha).toLocaleDateString("es-ES", {
    day: "2-digit", month: "2-digit", year: "numeric",
  });
}

// ── Íconos SVG ────────────────────────────────────────────────────────────────

const IcoAtras = () => (
  <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.8">
    <polyline points="10,3 5,8 10,13" />
  </svg>
);
const IcoDescarga = () => (
  <svg width="14" height="14" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M3 14v3h14v-3M10 3v9M6 9l4 4 4-4" />
  </svg>
);
const IcoUpload = () => (
  <svg width="14" height="14" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M3 14v3h14v-3M10 14V5M6 9l4-4 4 4" />
  </svg>
);
const IcoEditar = () => (
  <svg width="14" height="14" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.8">
    <path d="M13 3l4 4-10 10H3v-4L13 3z" />
  </svg>
);
const IcoHistorial = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="1,4 1,10 7,10" />
    <path d="M3.51 15a9 9 0 1 0 .49-5.1L1 10" />
  </svg>
);
const IcoLink = () => (
  <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.8">
    <path d="M10 13a3 3 0 100-6 3 3 0 000 6z" />
    <path d="M6 6a3 3 0 10-4 0M18 6a3 3 0 10-4 0M6 14a3 3 0 10-4 0M18 14a3 3 0 10-4 0" />
  </svg>
);
const IcoSave = () => (
  <svg width="14" height="14" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M5 2H14L18 6V18A1 1 0 0117 19H3A1 1 0 012 18V3A1 1 0 013 2Z" />
    <polyline points="7,2 7,9 13,9 13,2" />
    <rect x="5" y="13" width="10" height="6" />
  </svg>
);
const IcoPlus = () => (
  <svg width="13" height="13" viewBox="0 0 14 14" fill="none" stroke="currentColor" strokeWidth="2">
    <line x1="7" y1="1" x2="7" y2="13" />
    <line x1="1" y1="7" x2="13" y2="7" />
  </svg>
);
const IcoLock = () => (
  <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2">
    <rect x="2" y="6" width="12" height="8" rx="1" />
    <path d="M5 6V4a3 3 0 016 0v2" />
  </svg>
);

// ── Componente ────────────────────────────────────────────────────────────────

export default function DetalleDocumento() {
  const { documentoId } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const userInfo = getCurrentUserInfo();
  const isAdmin = userInfo && (userInfo.rol === "ADMIN" || userInfo.rol === "SUPERADMIN");

  const [documento, setDocumento] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const { toast, showToast, hideToast } = useToast();

  const [mostrarNuevaVersion, setMostrarNuevaVersion] = useState(false);
  const [enviandoVersion, setEnviandoVersion] = useState(false);
  const [comentarioVersion, setComentarioVersion] = useState("");
  const [archivoVersion, setArchivoVersion] = useState(null);
  const [archivoVersionError, setArchivoVersionError] = useState("");
  const fileRef = useRef(null);
  const dropRef = useRef(null);
  const [dragging, setDragging] = useState(false);


  const [descargando, setDescargando] = useState(null);
  const [bloqueoForzadoPorError, setBloqueoForzadoPorError] = useState(false);

  const historialRef = useRef(null);

  const [mostrarEditar, setMostrarEditar] = useState(false);
  const [formEditar, setFormEditar] = useState({ nombre: "", descripcion: "", categoria: "", confidencialidad: "Interna", fechaVencimiento: "", fechaAlerta: "" });
  const [enviandoEditar, setEnviandoEditar] = useState(false);

  const [mostrarNuevaRelacion, setMostrarNuevaRelacion] = useState(false);
  const [formRelacion, setFormRelacion] = useState({ documentoDestinoId: null, nombreDestino: "", tipoRelacion: "Anexo", descripcion: "", orden: "" });
  const [enviandoRelacion, setEnviandoRelacion] = useState(false);
  const [busquedaRelacion, setBusquedaRelacion] = useState("");
  const [resultadosRelacion, setResultadosRelacion] = useState([]);
  const [buscandoRelacion, setBuscandoRelacion] = useState(false);

  const [tipoVersionamiento, setTipoVersionamiento] = useState("menor");
  const [fechaVencimiento, setFechaVencimiento] = useState("");

  // ── Carga ─────────────────────────────────────────────────────────────────

  const cargarDocumento = async () => {
    try {
      setLoading(true);
      const data = await getDocumento(documentoId);
      setDocumento(data);
      setBloqueoForzadoPorError(false);
    } catch (e) {
      setError(e.message || "Error cargando el documento");
    } finally {
      setLoading(false);
    }
  };

  const esErrorNoImplementable = (mensaje = "") =>
    String(mensaje).toLowerCase().includes("no implementable");

  useEffect(() => { if (documentoId) cargarDocumento(); }, [documentoId]);

  useEffect(() => {
    if (location.state?.scrollToHistorial && historialRef.current) {
      setTimeout(() => historialRef.current?.scrollIntoView({ behavior: "smooth", block: "start" }), 300);
    }
  }, [location.state, historialRef.current]);

  // ── Archivo nueva versión (drag & drop + input) ───────────────────────────

  const procesarArchivoVersion = (file) => {
    setArchivoVersionError("");
    if (!file) { setArchivoVersion(null); return; }
    const ext = "." + file.name.split(".").pop().toLowerCase();
    if (![".pdf", ".docx", ".doc", ".pptx", ".xlsx", ".xls"].includes(ext)) {
      setArchivoVersionError(`Solo se aceptan PDF, Word (.docx, .doc), PowerPoint (.pptx) y Excel (.xlsx, .xls). Tipo seleccionado: ${ext.toUpperCase()}.`);
      setArchivoVersion(null); return;
    }
    if (file.size > MAX_BYTES) {
      setArchivoVersionError("El archivo supera el tamaño máximo de 50 MB.");
      setArchivoVersion(null); return;
    }
    setArchivoVersion(file);
  };

  const onArchivoVersionChange = (e) => procesarArchivoVersion(e.target.files[0] ?? null);
  const onDragOver  = (e) => { e.preventDefault(); setDragging(true); };
  const onDragLeave = () => setDragging(false);
  const onDrop      = (e) => { e.preventDefault(); setDragging(false); procesarArchivoVersion(e.dataTransfer.files[0] ?? null); };

  // ── Submit versión ────────────────────────────────────────────────────────

  const onSubmitVersion = async (e) => {
    e.preventDefault();
    if (documento?.actividadImplementable === "No") {
      setError("La actividad esta marcada como no implementable. No se puede crear una nueva version.");
      return;
    }
    if (documento?.tipoDocumento !== "URL" && !archivoVersion) { setError("Debe seleccionar un archivo."); return; }
    setError(""); showToast("");
    setEnviandoVersion(true);
    try {
      const fd = new FormData();
      if (archivoVersion) fd.append("archivo", archivoVersion);
      if (comentarioVersion) fd.append("comentario", comentarioVersion);
      if (fechaVencimiento) fd.append("fechaVencimiento", fechaVencimiento);
      fd.append("tipoVersionamiento", tipoVersionamiento);
      const nuevaVersion = await subirNuevaVersion(documentoId, fd);
      setDocumento(prev => {
        if (!prev) return prev;

        const versionesActualizadas = [
          nuevaVersion,
          ...(prev.versiones ?? []).filter(v => v.id !== nuevaVersion.id),
        ].sort((a, b) => (b.numeroVersion ?? 0) - (a.numeroVersion ?? 0));

        return {
          ...prev,
          versiones: versionesActualizadas,
        };
      });
      showToast("Documento editado exitosamente.");
      cerrarNuevaVersion();
      await cargarDocumento();
    } catch (e) {
      const msg = e.message || "Error al subir la versión.";
      if (esErrorNoImplementable(msg)) setBloqueoForzadoPorError(true);
      setError(msg);
    } finally {
      setEnviandoVersion(false);
    }
  };

  const cerrarNuevaVersion = () => {
    setMostrarNuevaVersion(false);
    setComentarioVersion("");
    setFechaVencimiento("");
    setArchivoVersion(null);
    setArchivoVersionError("");
    setTipoVersionamiento("menor");
    setDragging(false);
    if (fileRef.current) fileRef.current.value = "";
  };

  // ── Descargar ─────────────────────────────────────────────────────────────

  const onDescargar = async (numeroVersion = null) => {
    setDescargando(numeroVersion ?? "actual");
    try { await descargarDocumento(documentoId, numeroVersion); }
    catch (e) { setError(e.message || "Error al descargar."); }
    finally { setDescargando(null); }
  };

  // ── Editar metadatos ──────────────────────────────────────────────────────

  const abrirEditar = () => {
    if (documento?.actividadImplementable === "No") {
      setError("La actividad esta marcada como no implementable. No se puede editar el documento.");
      return;
    }

    setFormEditar({
      nombre:           documento?.nombre ?? "",
      descripcion:      documento?.descripcion ?? "",
      categoria:        documento?.categoria ?? "",
      confidencialidad: documento?.confidencialidad ?? "Interna",
      fechaVencimiento: documento?.fechaVencimiento ? new Date(documento.fechaVencimiento).toISOString().split("T")[0] : "",
      fechaAlerta:      documento?.fechaAlerta      ? new Date(documento.fechaAlerta).toISOString().split("T")[0]      : "",
    });
    setMostrarEditar(!mostrarEditar);
    setMostrarNuevaVersion(false);
    setMostrarCambioEstado(false);
  };

  const onSubmitEditar = async (e) => {
    e.preventDefault();
    if (documento?.actividadImplementable === "No") {
      setError("La actividad esta marcada como no implementable. No se puede editar el documento.");
      return;
    }
    if (!formEditar.nombre.trim()) { setError("El nombre es requerido."); return; }
    setError(""); showToast("");
    setEnviandoEditar(true);
    try {
      await actualizarDocumento(documentoId, {
        nombre:           formEditar.nombre.trim(),
        descripcion:      formEditar.descripcion.trim() || null,
        categoria:        formEditar.categoria.trim() || null,
        confidencialidad: formEditar.confidencialidad,
        fechaVencimiento: formEditar.fechaVencimiento || null,
        fechaAlerta:      formEditar.fechaAlerta || null,
      });
      showToast("Documento editado exitosamente.");
      setMostrarEditar(false);
      await cargarDocumento();
    } catch (err) {
      const msg = err.message || "Error al actualizar el documento.";
      if (esErrorNoImplementable(msg)) setBloqueoForzadoPorError(true);
      setError(msg);
    } finally {
      setEnviandoEditar(false);
    }
  };

  // ── Google Drive ──────────────────────────────────────────────────────────

  const esGoogleDoc    = (url) => url && url.includes("docs.google.com/document");
  const esGoogleSheets = (url) => url && url.includes("docs.google.com/spreadsheets");
  const abrirEnGoogle  = (url) => {
    if (!url) return;
    window.open(url.replace(/\/view.*$/, "/edit").replace(/\/edit.*$/, "/edit"), "_blank", "noreferrer");
  };

  // ── Relación ──────────────────────────────────────────────────────────────

  const buscarDocDestino = async () => {
    if (!busquedaRelacion.trim()) return;
    setBuscandoRelacion(true);
    try {
      const results = await buscarDocumentos({ nombre: busquedaRelacion.trim(), limite: 5 });
      setResultadosRelacion((results ?? []).filter(r => r.id !== parseInt(documentoId)));
    } catch (err) { setError("Error al buscar documentos."); }
    finally { setBuscandoRelacion(false); }
  };

  const onSubmitRelacion = async (e) => {
    e.preventDefault();
    if (bloqueadoPorImplementable) { setError("No puede agregar relaciones. La actividad está marcada como no implementable."); return; }
    if (!formRelacion.documentoDestinoId) { setError("Seleccione un documento destino."); return; }
    setError(""); showToast("");
    setEnviandoRelacion(true);
    try {
      await crearRelacionDocumento(documentoId, {
        documentoDestinoId: formRelacion.documentoDestinoId,
        tipoRelacion:  formRelacion.tipoRelacion,
        descripcion:   formRelacion.descripcion.trim() || null,
        orden:         formRelacion.orden ? parseInt(formRelacion.orden) : null,
      });
      showToast("Relación creada correctamente.");
      setMostrarNuevaRelacion(false);
      setFormRelacion({ documentoDestinoId: null, nombreDestino: "", tipoRelacion: "Anexo", descripcion: "", orden: "" });
      setBusquedaRelacion("");
      setResultadosRelacion([]);
      await cargarDocumento();
    } catch (err) {
      setError(err.message || "Error al crear la relación.");
    } finally {
      setEnviandoRelacion(false);
    }
  };

  const bloqueadoPorImplementable =
    location.state?.bloqueadoPorImplementable === true ||
    documento?.bloqueadoEdicionPorImplementable === true ||
    documento?.actividadImplementable === "No" ||
    bloqueoForzadoPorError;

  useEffect(() => {
    if (!bloqueadoPorImplementable) return;
    setMostrarNuevaVersion(false);
    setMostrarEditar(false);
  }, [bloqueadoPorImplementable]);

  // ── Render ────────────────────────────────────────────────────────────────

  if (loading) return <div className="act-loading">Cargando...</div>;

  if (error && !documento) return (
    <div className="act-page">
      <div className="act-alert danger">{error}</div>
      <button className="act-btn-cancel" onClick={() => navigate(-1)}>Volver</button>
    </div>
  );

  if (!documento) return null;

  const versionActual = (documento.versiones ?? []).find(v => v.esVersionActual);
  const puedeSubirVersion = !["Obsoleto", "Archivado"].includes(documento.estado);

  const cerrarTodo = (abrirFn) => {
    setMostrarNuevaVersion(false);
    setMostrarEditar(false);
    abrirFn();
  };


  return (
    <div className="act-page">

      <nav className="act-breadcrumb">
        <Link to="/">Inicio</Link>
        <span className="sep">›</span>
        <Link to="/documentos/buscar">Documentos</Link>
        <span className="sep">›</span>
        <span className="current">Detalle de documento</span>
      </nav>

      {/* ── Header ── */}
      <div className="det-header">
        <button className="det-volver" onClick={() => navigate(-1)}>
          <IcoAtras /> Volver
        </button>
        <div className="det-acciones">
          {versionActual?.tipoAlmacenamiento === "Archivo" && (
            <button
              className="act-btn-save"
              disabled={descargando === "actual"}
              onClick={() => onDescargar()}
            >
              <IcoDescarga />
              {descargando === "actual" ? "Descargando..." : "Descargar"}
            </button>
          )}
          {versionActual?.tipoAlmacenamiento === "URL" && (
            <>
              <a href={versionActual.url} className="act-btn-save" target="_blank" rel="noreferrer">
                Abrir enlace
              </a>
              {esGoogleDoc(versionActual.url) && (
                <button className="act-btn-cancel" onClick={() => abrirEnGoogle(versionActual.url)}>
                  Editar en Docs
                </button>
              )}
              {esGoogleSheets(versionActual.url) && (
                <button className="act-btn-cancel" onClick={() => abrirEnGoogle(versionActual.url)}>
                  Editar en Sheets
                </button>
              )}
            </>
          )}
          {puedeSubirVersion && (
            <button
              className="act-btn-cancel"
              disabled={bloqueadoPorImplementable}
              onClick={() => cerrarTodo(() => setMostrarNuevaVersion(v => !v))}
            >
              <IcoUpload />
              {mostrarNuevaVersion ? "Cancelar" : "Nueva versión"}
            </button>
          )}
          <button className="act-btn-cancel" onClick={abrirEditar} disabled={bloqueadoPorImplementable}>
            <IcoEditar />
            {mostrarEditar ? "Cancelar" : "Editar"}
          </button>
        </div>
      </div>

      {bloqueadoPorImplementable && (
        <div className="act-alert warning" role="alert">
          <span>
            Esta actividad está marcada como no implementable. Los botones están bloqueados.
          </span>
        </div>
      )}

      {/* ── Nombre + badges ── */}
      <div className="det-nombre-wrap">
        <div className="det-tipo-icon">
          <i className={`${TIPO_ICON[documento.tipoDocumento] ?? "fas fa-file"}`}></i>
        </div>
        <div>
          <h1 className="det-nombre">{documento.nombre}</h1>
          <div className="det-badges">
            <span className={`badge ${ESTADO_BADGE[documento.estado] ?? ""}`}>
              {({ Borrador: "Borrador", En_Revision: "En Revisión", Aprobado: "Aprobado", Vigente: "Vigente", Obsoleto: "Obsoleto", Archivado: "Archivado" })[documento.estado] ?? documento.estado}
            </span>
            {documento.confidencialidad && (
              <span className="det-badge-conf">
                <IcoLock /> {documento.confidencialidad}
              </span>
            )}
            {documento.categoria && (
              <span className="badge badge-info">{documento.categoria}</span>
            )}
          </div>
        </div>
      </div>

      {error && <div className="act-alert danger">{error}</div>}
      
      {/* ── Descripción / metadatos ── */}
      {(documento.descripcion || documento.fechaVencimiento) && (
        <div className="act-card card-obs" className="doc-card-mb">
          <div className="act-card-body">
            <div className="det-meta-grid">
              {documento.descripcion && (
                <div className="det-meta-full">
                  <span className="det-meta-label">Descripción</span>
                  <span className="det-meta-val">{documento.descripcion}</span>
                </div>
              )}
              <div>
                <span className="det-meta-label">Creación</span>
                <span className="det-meta-val">{formatFecha(documento.fechaCreacion)}</span>
              </div>
              {documento.fechaVencimiento && (
                <div>
                  <span className="det-meta-label">Vencimiento</span>
                  <span className={`det-meta-val${new Date(documento.fechaVencimiento) < new Date() ? " det-meta-vencido" : ""}`}>
                    {formatFecha(documento.fechaVencimiento)}
                    {new Date(documento.fechaVencimiento) < new Date() && " ⚠️"}
                  </span>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ── Form: nueva versión ── */}
      {mostrarNuevaVersion && (
        <div className="act-card card-resp" className="doc-card-mb">
          <div className="act-card-header">
            <div className="act-card-icon purple"><IcoUpload /></div>
            <span className="act-card-title">Subir nueva versión</span>
          </div>
          <div className="act-card-body">
            <form onSubmit={onSubmitVersion}>
              <div className="docs-form-grid">
                {documento.tipoDocumento !== "URL" && (
                  <div className="docs-form-field docs-field-full">
                    <label className="act-label">Archivo <span className="req">*</span></label>
                    <div
                      ref={dropRef}
                      className={`docs-dropzone${dragging ? " docs-dropzone--active" : ""}${archivoVersion ? " docs-dropzone--filled" : ""}`}
                      onDragOver={onDragOver} onDragLeave={onDragLeave} onDrop={onDrop}
                      onClick={() => fileRef.current?.click()}
                    >
                      {archivoVersion ? (
                        <>
                          <i className="fas fa-check-circle docs-dropzone-icon docs-dropzone-icon--ok"></i>
                          <p className="docs-dropzone-main">{archivoVersion.name}</p>
                          <p className="docs-dropzone-sub">{formatBytes(archivoVersion.size)} — clic para cambiar</p>
                        </>
                      ) : (
                        <>
                          <i className="fas fa-cloud-upload-alt docs-dropzone-icon"></i>
                          <p className="docs-dropzone-main">Arrastra el archivo aquí</p>
                          <p className="docs-dropzone-sub">o <span className="docs-dropzone-link">selecciona desde tu equipo</span></p>
                          <p className="docs-dropzone-hint">PDF, Word, PowerPoint, Excel — máx. 50 MB</p>
                        </>
                      )}
                    </div>
                    <input ref={fileRef} type="file" accept=".pdf,.docx,.doc,.pptx,.xlsx,.xls" onChange={onArchivoVersionChange} className="doc-input-hidden" />
                    {archivoVersionError && <div className="docs-file-error"><i className="fas fa-exclamation-circle me-1"></i>{archivoVersionError}</div>}
                  </div>
                )}
                <div className="docs-form-field docs-field-sm">
                  <label className="act-label">Tipo de versión</label>
                  <select className="act-select" value={tipoVersionamiento} onChange={e => setTipoVersionamiento(e.target.value)}>
                    <option value="menor">Menor (1.0 → 1.1)</option>
                    <option value="mayor">Mayor (1.0 → 2.0)</option>
                  </select>
                </div>
                <div className="docs-form-field docs-field-sm">
                  <label className="act-label">Fecha de vencimiento <span className="req">*</span></label>
                  <input className="act-input" type="date" value={fechaVencimiento} onChange={e => setFechaVencimiento(e.target.value)} min={new Date().toISOString().split("T")[0]} required />
                </div>
                <div className="docs-form-field docs-field-lg">
                  <label className="act-label">Comentario de cambios</label>
                  <input className="act-input" value={comentarioVersion} onChange={e => setComentarioVersion(e.target.value)} placeholder="Descripción de los cambios" />
                </div>
              </div>
              <div className="docs-form-actions">
                <button type="submit" className="act-btn-save" disabled={enviandoVersion}>
                  {enviandoVersion ? <><i className="fas fa-spinner fa-spin me-1"></i>Guardando...</> : <><IcoSave /> Guardar versión</>}
                </button>
                <button type="button" className="act-btn-cancel" onClick={cerrarNuevaVersion} disabled={enviandoVersion}>Cancelar</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── Form: editar metadatos ── */}
      {mostrarEditar && (
        <div className="act-card card-obs" className="doc-card-mb">
          <div className="act-card-header">
            <div className="act-card-icon" className="doc-card-icon-gray"><IcoEditar /></div>
            <span className="act-card-title">Editar metadatos</span>
          </div>
          <div className="act-card-body">
            <form onSubmit={onSubmitEditar}>
              <div className="docs-form-grid">
                <div className="docs-form-field docs-field-lg">
                  <label className="act-label">Nombre <span className="req">*</span></label>
                  <input className="act-input" value={formEditar.nombre} onChange={e => setFormEditar(f => ({ ...f, nombre: e.target.value }))} maxLength={255} required />
                </div>
                <div className="docs-form-field docs-field-sm">
                  <label className="act-label">Categoría</label>
                  <input className="act-input" value={formEditar.categoria} onChange={e => setFormEditar(f => ({ ...f, categoria: e.target.value }))} placeholder="Ej: Normativa" maxLength={100} />
                </div>
                <div className="docs-form-field docs-field-full">
                  <label className="act-label">Descripción</label>
                  <textarea className="act-textarea" rows={2} value={formEditar.descripcion} onChange={e => setFormEditar(f => ({ ...f, descripcion: e.target.value }))} />
                </div>
                <div className="docs-form-field docs-field-sm">
                  <label className="act-label">Confidencialidad</label>
                  <select className="act-select" value={formEditar.confidencialidad} onChange={e => setFormEditar(f => ({ ...f, confidencialidad: e.target.value }))}>
                    <option value="Interna">Interna</option>
                    <option value="Publica">Pública</option>
                  </select>
                </div>
                <div className="docs-form-field docs-field-sm">
                  <label className="act-label">Fecha de vencimiento</label>
                  <input type="date" className="act-input" value={formEditar.fechaVencimiento} onChange={e => setFormEditar(f => ({ ...f, fechaVencimiento: e.target.value }))} />
                </div>
                <div className="docs-form-field docs-field-sm">
                  <label className="act-label">Fecha de alerta</label>
                  <input type="date" className="act-input" value={formEditar.fechaAlerta} onChange={e => setFormEditar(f => ({ ...f, fechaAlerta: e.target.value }))} />
                </div>
              </div>
              <div className="docs-form-actions" className="doc-form-actions-mt">
                <button type="submit" className="act-btn-save" disabled={enviandoEditar}>
                  {enviandoEditar ? <><i className="fas fa-spinner fa-spin me-1"></i>Guardando...</> : <><IcoSave /> Guardar cambios</>}
                </button>
                <button type="button" className="act-btn-cancel" onClick={() => setMostrarEditar(false)} disabled={enviandoEditar}>Cancelar</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── Historial de versiones ── */}
      <div className="act-card card-docs" ref={historialRef} className="doc-card-mb">
        <div className="act-card-header">
          <div className="act-card-icon amber"><IcoHistorial /></div>
          <span className="act-card-title">Historial de versiones</span>
        </div>
        <div className="act-card-body act-card-body--no-top">
          {(documento.versiones ?? []).length === 0 ? (
            <div className="docs-empty">Sin versiones registradas.</div>
          ) : (
            <div className="det-versiones">
              {(documento.versiones ?? []).map(v => (
                <div key={v.id} className={`det-version-row${v.esVersionActual ? " det-version-row--actual" : ""}`}>
                  <div className="det-version-ver">
                    <span className="det-version-num">v{v.versionTexto}</span>
                    {v.esVersionActual && <span className="det-version-badge-actual">actual</span>}
                  </div>
                  <div className="det-version-archivo">
                    {v.nombreArchivoOriginal ?? (v.url ? "Enlace URL" : "—")}
                  </div>
                  <div className="det-version-meta">
                    <span className="det-meta-label">Tamaño</span>
                    <span>{formatBytes(v.tamanoBytes) || "—"}</span>
                  </div>
                  <div className="det-version-meta">
                    <span className="det-meta-label">Subido por</span>
                    <span>{v.subidoPorNombre ?? "—"}</span>
                  </div>
                  <div className="det-version-meta">
                    <span className="det-meta-label">Fecha</span>
                    <span>{formatFecha(v.fechaSubida)}</span>
                  </div>
                  <div className="det-version-meta">
                    <span className="det-meta-label">Comentario</span>
                    <span>{v.comentario ?? "—"}</span>
                  </div>
                  <div className="det-version-meta">
                    <span className="det-meta-label">Vencimiento</span>
                    <span>{v.fechaVencimiento ? formatFecha(v.fechaVencimiento) : "—"}</span>
                  </div>
                  <div className="det-version-accion">
                    {v.tipoAlmacenamiento === "Archivo" ? (
                      <button
                        className="det-btn-dl"
                        title="Descargar esta versión"
                        disabled={descargando === v.numeroVersion}
                        onClick={() => onDescargar(v.numeroVersion)}
                      >
                        <i className={`fas fa-${descargando === v.numeroVersion ? "spinner fa-spin" : "download"}`}></i>
                      </button>
                    ) : v.url ? (
                      <a href={v.url} className="det-btn-dl" title="Abrir enlace" target="_blank" rel="noreferrer">
                        <i className="fas fa-external-link-alt"></i>
                      </a>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* ── Relaciones ── */}
      <div className="act-card card-obs">
        <div className="act-card-header">
          <div className="act-card-icon" className="doc-card-icon-gray"><IcoLink /></div>
          <div className="act-card-title-wrap">
            <span className="act-card-title">Relaciones</span>
          </div>
        </div>
        <div className="act-card-body">

          {!mostrarNuevaRelacion ? (
            <button
              className="proc-btn-agregar"
              onClick={() => setMostrarNuevaRelacion(true)}
              disabled={bloqueadoPorImplementable}
            >
              <IcoPlus /> Agregar relación
            </button>
          ) : (
            <form onSubmit={onSubmitRelacion}>
              <div className="docs-form-grid">
                <div className="docs-form-field docs-field-full">
                  <label className="act-label">Buscar documento destino</label>
                  <div className="doc-btn-row">
                    <input
                      className="act-input"
                      value={busquedaRelacion}
                      onChange={e => setBusquedaRelacion(e.target.value)}
                      onKeyDown={e => e.key === "Enter" && (e.preventDefault(), buscarDocDestino())}
                      placeholder="Buscar por nombre..."
                    />
                    <button type="button" className="act-btn-cancel" onClick={buscarDocDestino} disabled={buscandoRelacion}>
                      {buscandoRelacion ? <i className="fas fa-spinner fa-spin"></i> : <i className="fas fa-search"></i>}
                    </button>
                  </div>
                  {resultadosRelacion.length > 0 && (
                    <div className="act-dropdown">
                      {resultadosRelacion.map(r => (
                        <button
                          key={r.id}
                          type="button"
                          className="act-dropdown-item"
                          onClick={() => { setFormRelacion(f => ({ ...f, documentoDestinoId: r.id, nombreDestino: r.nombre })); setResultadosRelacion([]); setBusquedaRelacion(r.nombre); }}
                        >
                          <strong>{r.nombre}</strong> — {r.tipoDocumento} · v{r.versionActual?.versionTexto ?? "—"}
                        </button>
                      ))}
                    </div>
                  )}
                  {formRelacion.documentoDestinoId && (
                    <div className="docs-file-ok"><i className="fas fa-check-circle me-1"></i>Seleccionado: <strong>{formRelacion.nombreDestino}</strong></div>
                  )}
                </div>
                <div className="docs-form-field docs-field-sm">
                  <label className="act-label">Tipo de relación <span className="req">*</span></label>
                  <select className="act-select" value={formRelacion.tipoRelacion} onChange={e => setFormRelacion(f => ({ ...f, tipoRelacion: e.target.value }))}>
                    <option value="Anexo">Anexo</option>
                    <option value="Referencia">Referencia</option>
                    <option value="Dependencia">Dependencia</option>
                    <option value="Reemplaza">Reemplaza</option>
                    <option value="Relacionado">Relacionado</option>
                  </select>
                </div>
                <div className="docs-form-field docs-field-lg">
                  <label className="act-label">Descripción</label>
                  <input className="act-input" value={formRelacion.descripcion} onChange={e => setFormRelacion(f => ({ ...f, descripcion: e.target.value }))} placeholder="Descripción de la relación (opcional)" maxLength={500} />
                </div>
                <div className="docs-form-field docs-field-sm">
                  <label className="act-label">Orden (opcional)</label>
                  <input type="number" className="act-input" min="1" value={formRelacion.orden} onChange={e => setFormRelacion(f => ({ ...f, orden: e.target.value }))} placeholder="Ej: 1" />
                </div>
              </div>
              <div className="docs-form-actions" className="doc-form-actions-mt">
                <button type="submit" className="act-btn-save" disabled={enviandoRelacion || !formRelacion.documentoDestinoId}>
                  {enviandoRelacion ? <><i className="fas fa-spinner fa-spin me-1"></i>Guardando...</> : <><IcoPlus /> Crear relación</>}
                </button>
                <button
                  type="button"
                  className="act-btn-cancel"
                  onClick={() => { setMostrarNuevaRelacion(false); setFormRelacion({ documentoDestinoId: null, nombreDestino: "", tipoRelacion: "Anexo", descripcion: "", orden: "" }); setBusquedaRelacion(""); setResultadosRelacion([]); }}
                  disabled={enviandoRelacion}
                >Cancelar</button>
              </div>
            </form>
          )}

          {(documento.relaciones ?? []).length > 0 && (
            <div className="docs-table-wrap" className="doc-form-actions-mt">
              <div className="logs-table-container">
                <table className="logs-table">
                  <thead>
                    <tr>
                      <th>Tipo</th>
                      <th>Documento relacionado</th>
                      <th>Descripción</th>
                      <th>Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    {documento.relaciones.map(r => {
                      const esOrigen = r.documentoOrigenId === documento.id;
                      const idRel    = esOrigen ? r.documentoDestinoId  : r.documentoOrigenId;
                      const nomRel   = esOrigen ? r.documentoDestinoNombre : r.documentoOrigenNombre;
                      return (
                        <tr key={r.id}>
                          <td><span className="badge badge-info">{r.tipoRelacion}</span></td>
                          <td>
                            <button type="button" className="btn btn-link p-0" onClick={() => navigate(`/documentos/${idRel}`)}>
                              {nomRel}
                            </button>
                          </td>
                          <td>{r.descripcion ?? "—"}</td>
                          <td>
                            <button type="button" className="btn-icon" title="Ver documento" onClick={() => navigate(`/documentos/${idRel}`)}>
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
          )}
        </div>
      </div>

      <Toast toast={toast} onClose={hideToast} />
    </div>
  );
}