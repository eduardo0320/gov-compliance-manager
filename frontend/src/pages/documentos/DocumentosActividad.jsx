import Toast, { useToast } from "../../components/ui/Toast";
import { ConfirmDialog, useConfirm } from "../../components/ui/ResultDialog";
import { useEffect, useState, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import {
  getDocumentosActividad,
  crearDocumento,
  eliminarDocumento,
  descargarDocumento,
  subirNuevaVersion,
  getCurrentUserInfo,
} from "../../services";

// ── Helpers ──────────────────────────────────────────────────────────────────

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
const EXTENSIONES_PERMITIDAS = [".pdf", ".docx", ".doc", ".pptx", ".xlsx", ".xls"];

const EXT_A_TIPO = {
  ".pdf":  "PDF",
  ".docx": "DOCX",
  ".doc":  "DOC",
  ".pptx": "PPTX",
  ".xlsx": "XLSX",
  ".xls":  "XLS",
};

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

const FORM_VACÍO = {
  nombre: "",
  descripcion: "",
  tipoDocumento: "PDF",
  rolDocumento: "Anexo",
  categoria: "",
  url: "",
  comentarioVersion: "",
  fechaVencimiento: "",
  confidencialidad: "Interna",
};

// ── Componente ────────────────────────────────────────────────────────────────

export default function DocumentosActividad({
  subdominioId,
  actividadId,
  onDocumentosChange,
  bloqueoEdicion = false,
  bloqueadoPorImplementable = false,
  readOnly = false,
}) {
  const navigate = useNavigate();
  const location = useLocation();
  const userInfo = getCurrentUserInfo();
  const isAdmin = userInfo && (userInfo.rol === "ADMIN" || userInfo.rol === "SUPERADMIN");

  const [documentos, setDocumentos] = useState([]);
  const [loading, setLoading] = useState(true);
  const { toast, showToast, hideToast } = useToast();
  const { confirm: confirmEliminar, askConfirm, closeConfirm } = useConfirm();
  const [error, setError] = useState("");

  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [form, setForm] = useState(FORM_VACÍO);
  const [archivo, setArchivo] = useState(null);
  const [archivoError, setArchivoError] = useState("");
  const fileInputRef = useRef(null);
  const dropZoneRef = useRef(null);
  const [dragging, setDragging] = useState(false);

  const [modoNuevaVersion, setModoNuevaVersion] = useState(false);
  const [docDestino, setDocDestino] = useState(null);
  const [sugerenciaDoc, setSugerenciaDoc] = useState(null);

  const [descargando, setDescargando] = useState(null);
  // "archivo" | "url" — controla si el formulario muestra subida de archivo o campo URL
  const [modoEntrada, setModoEntrada] = useState("archivo");
  const [descargandoUrl, setDescargandoUrl] = useState(false);
  const edicionBloqueada = Boolean(bloqueoEdicion || bloqueadoPorImplementable || readOnly);

  // ── Carga ────────────────────────────────────────────────────────────────

  const cargarDocumentos = async () => {
    try {
      setLoading(true);
      const data = await getDocumentosActividad(subdominioId, actividadId);
      setDocumentos(Array.isArray(data) ? data : []);
    } catch (e) {
      setError("Error cargando documentos: " + (e.message || "Error desconocido"));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (subdominioId && actividadId) cargarDocumentos();
  }, [subdominioId, actividadId, location.key]);

  useEffect(() => {
    const recargarAlVolver = () => {
      if (document.visibilityState === "visible" && subdominioId && actividadId) {
        cargarDocumentos();
      }
    };
    window.addEventListener("focus", recargarAlVolver);
    document.addEventListener("visibilitychange", recargarAlVolver);
    return () => {
      window.removeEventListener("focus", recargarAlVolver);
      document.removeEventListener("visibilitychange", recargarAlVolver);
    };
  }, [subdominioId, actividadId]);

  useEffect(() => {
    if (typeof onDocumentosChange === "function") {
      onDocumentosChange(documentos);
    }
  }, [documentos, onDocumentosChange]);

  useEffect(() => {
    if (edicionBloqueada && mostrarFormulario) {
      cancelarFormulario();
    }
  }, [edicionBloqueada, mostrarFormulario]);

  // ── Archivo (input + drag & drop) ────────────────────────────────────────

  const procesarArchivo = (file) => {
    setArchivoError("");
    if (!file) { setArchivo(null); return; }

    const ext = "." + file.name.split(".").pop().toLowerCase();
    if (!EXTENSIONES_PERMITIDAS.includes(ext)) {
      setArchivoError(
        `Solo se aceptan PDF, Word (.docx, .doc), PowerPoint (.pptx) y Excel (.xlsx, .xls). ` +
        `El archivo seleccionado es de tipo ${ext.toUpperCase()}.`
      );
      setArchivo(null);
      return;
    }
    if (file.size > MAX_BYTES) {
      setArchivoError("El archivo supera el tamaño máximo de 50 MB. Intente comprimir el documento.");
      setArchivo(null);
      return;
    }
    setArchivo(file);

    const nombreSinExt = file.name.replace(/\.[^.]+$/, "");
    const tipoDetectado = EXT_A_TIPO[ext] ?? "OTRO";
    setForm(f => ({
      ...f,
      nombre: f.nombre.trim() ? f.nombre : nombreSinExt,
      tipoDocumento: tipoDetectado,
    }));
  };

  const onArchivoChange = (e) => {
    procesarArchivo(e.target.files[0] ?? null);
  };

  const onDragOver = (e) => {
    e.preventDefault();
    setDragging(true);
  };
  const onDragLeave = () => setDragging(false);
  const onDrop = (e) => {
    e.preventDefault();
    setDragging(false);
    procesarArchivo(e.dataTransfer.files[0] ?? null);
  };

  // ── Detección de nombre similar ─────────────────────────────

  const onNombreChange = (valor) => {
    setForm(f => ({ ...f, nombre: valor }));
    if (!modoNuevaVersion) {
      const v = valor.trim().toLowerCase();
      if (v.length >= 3) {
        const encontrado = documentos.find(d =>
          d.nombre.toLowerCase().includes(v) || v.includes(d.nombre.toLowerCase())
        );
        setSugerenciaDoc(encontrado ?? null);
      } else {
        setSugerenciaDoc(null);
      }
    }
  };

  const activarModoNuevaVersion = (doc) => {
    setSugerenciaDoc(null);
    setModoNuevaVersion(true);
    setDocDestino(doc);
    setForm(f => ({ ...f, nombre: doc.nombre, comentarioVersion: "" }));
  };

  // ── Submit ─────────────────────────────────────────────────────────────────

  const onSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (edicionBloqueada) {
      setError("La edición de documentos está bloqueada para esta actividad.");
      return;
    }

    if (modoNuevaVersion) {
      if (!form.fechaVencimiento) { setError("La fecha de vencimiento es obligatoria."); return; }
      if (docDestino?.tipoDocumento !== "URL" && !archivo) {
        setError("Debe seleccionar un archivo."); return;
      }
      if (docDestino?.tipoDocumento === "URL" && !form.url.trim()) {
        setError("Debe ingresar una URL."); return;
      }
      setEnviando(true);
      try {
        const fd = new FormData();
        if (archivo) fd.append("archivo", archivo);
        if (form.url.trim()) fd.append("url", form.url.trim());
        if (form.comentarioVersion.trim()) fd.append("comentario", form.comentarioVersion.trim());
        if (form.fechaVencimiento) fd.append("fechaVencimiento", form.fechaVencimiento);
        fd.append("tipoVersionamiento", "menor");
        await subirNuevaVersion(docDestino.id, fd);
        cancelarFormulario();
        await cargarDocumentos();
      } catch (err) {
        setError(err.message || "Error al subir la nueva versión.");
      } finally {
        setEnviando(false);
      }
      return;
    }

    if (!form.nombre.trim()) { setError("El nombre es requerido."); return; }
    if (!form.fechaVencimiento) { setError("La fecha de vencimiento es obligatoria."); return; }
    if (form.tipoDocumento !== "URL" && !archivo) {
      setError("Debe seleccionar un archivo."); return;
    }
    if (form.tipoDocumento === "URL" && !form.url.trim()) {
      setError("Debe ingresar una URL."); return;
    }

    setEnviando(true);
    try {
      const fd = new FormData();
      fd.append("nombre",           form.nombre.trim());
      fd.append("descripcion",      form.descripcion.trim());
      fd.append("tipoDocumento",    form.tipoDocumento);
      fd.append("rolEnActividad",   form.rolDocumento);
      fd.append("actividadId",      actividadId);
      fd.append("confidencialidad", form.confidencialidad);
      if (form.categoria)         fd.append("categoria",        form.categoria.trim());
      if (form.fechaVencimiento)  fd.append("fechaVencimiento",  form.fechaVencimiento);
      if (form.comentarioVersion) fd.append("comentarioVersion", form.comentarioVersion);
      if (form.tipoDocumento === "URL") fd.append("url", form.url.trim());
      else if (archivo)           fd.append("archivo", archivo);

      await crearDocumento(fd);
      showToast("El documento fue cargado correctamente.");
      cancelarFormulario();
      await cargarDocumentos();
    } catch (err) {
      setError(err.message || "Error al crear el documento.");
    } finally {
      setEnviando(false);
    }
  };

  const cancelarFormulario = () => {
    setMostrarFormulario(false);
    setModoNuevaVersion(false);
    setDocDestino(null);
    setSugerenciaDoc(null);
    setForm(FORM_VACÍO);
    setArchivo(null);
    setArchivoError("");
    setDragging(false);
    setModoEntrada("archivo");
    setDescargandoUrl(false);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  useEffect(() => {
    if (edicionBloqueada && mostrarFormulario) {
      cancelarFormulario();
    }
  }, [edicionBloqueada, mostrarFormulario]);

  const onEliminar = async (doc) => {
    if (edicionBloqueada) return;
    askConfirm(
      "¿Eliminar documento?",
      `¿Seguro que querés eliminar "${doc.nombre}"? Esta acción no se puede deshacer.`,
      async () => {
        setError("");
        try {
          await eliminarDocumento(doc.id);
          showToast("Documento eliminado correctamente.");
          await cargarDocumentos();
        } catch (e) {
          setError(e.message || "Error al eliminar el documento.");
        }
      }
    );
  };

  const onDescargar = async (doc) => {
    setDescargando(doc.id);
    try {
      await descargarDocumento(doc.id);
    } catch (e) {
      setError(e.message || "Error al descargar el archivo.");
    } finally {
      setDescargando(null);
    }
  };

  // ── Render ────────────────────────────────────────────────────────────────

  const esURL = (modoNuevaVersion ? docDestino?.tipoDocumento : form.tipoDocumento) === "URL";

  return (
    <div className="docs-section">

      {/* ── Botón agregar documento ── */}
      <div className="docs-add-bar">
        <button
          type="button"
          className={`act-btn-doc-nuevo${mostrarFormulario ? " act-btn-doc-nuevo--cancel" : ""}`}
          disabled={edicionBloqueada}
          onClick={() => {
            if (edicionBloqueada) {
              setError("La edición de documentos está bloqueada para esta actividad.");
              return;
            }
            if (mostrarFormulario) {
              cancelarFormulario();
            } else {
              const tienePrincipal = documentos.some(d => d.rolEnActividad === "Principal");
              setForm({ ...FORM_VACÍO, rolDocumento: tienePrincipal ? "Anexo" : "Principal" });
              setMostrarFormulario(true);
              setError("");
            }
          }}
        >
          <i className={`fas fa-${mostrarFormulario ? "times" : "plus"} me-1`}></i>
          {edicionBloqueada ? "Edición bloqueada" : mostrarFormulario ? "Cancelar" : "Nuevo Documento"}
        </button>
      </div>

      {error && <div className="act-alert danger">{error}</div>}
      

      {/* ── Formulario inline ── */}
      {mostrarFormulario && (
        <div className={`docs-form-panel${modoNuevaVersion ? " docs-form-panel--version" : ""}`}>

          {/* Encabezado */}
          {modoNuevaVersion ? (
            <div className="docs-version-banner">
              <i className="fas fa-code-branch me-2"></i>
              Nueva versión de: <strong>{docDestino?.nombre}</strong>
              <span className="docs-version-badge ms-2">
                v{docDestino?.versionActual?.versionTexto ?? "1.0"} actual
              </span>
              <button
                type="button"
                className="docs-cancel-version"
                onClick={() => { setModoNuevaVersion(false); setDocDestino(null); setSugerenciaDoc(null); }}
              >
                <i className="fas fa-times me-1"></i>Cancelar modo versión
              </button>
            </div>
          ) : (
            <div className="docs-form-header">
              <div className="docs-form-header-icon">
                <i className="fas fa-file-upload"></i>
              </div>
              <div>
                <p className="docs-form-header-title">Nuevo documento</p>
                <p className="docs-form-header-sub">Completa los campos para adjuntar el documento.</p>
              </div>
            </div>
          )}

          <form onSubmit={onSubmit}>
            <div className="docs-form-grid">

              {/* ── 1. Toggle Archivo / URL ── */}
              {!modoNuevaVersion && (
                <div className="docs-form-field docs-field-full">
                  <div className="docs-modo-tabs">
                    <button
                      type="button"
                      className={`docs-modo-tab${modoEntrada === "archivo" ? " docs-modo-tab--active" : ""}`}
                      onClick={() => {
                        setModoEntrada("archivo");
                        setForm(f => ({ ...f, tipoDocumento: "PDF", url: "" }));
                        setArchivo(null);
                        setArchivoError("");
                      }}
                    >
                      <i className="fas fa-upload me-1"></i> Subir archivo
                    </button>
                    <button
                      type="button"
                      className={`docs-modo-tab${modoEntrada === "url" ? " docs-modo-tab--active" : ""}`}
                      onClick={() => {
                        setModoEntrada("url");
                        setForm(f => ({ ...f, tipoDocumento: "URL", url: "" }));
                        setArchivo(null);
                        setArchivoError("");
                      }}
                    >
                      <i className="fas fa-link me-1"></i> Agregar por URL
                    </button>
                  </div>
                </div>
              )}

              {/* ── 1b. Campo URL o Dropzone según modo ── */}
              {(modoEntrada === "url" && !modoNuevaVersion) ? (
                <div className="docs-form-field docs-field-full">
                  <label className="act-label">
                    URL del documento <span className="req">*</span>
                  </label>
                  <input
                    className="act-input"
                    type="url"
                    value={form.url}
                    onChange={e => setForm(f => ({ ...f, url: e.target.value }))}
                    placeholder="https://..."
                  />
                  <p className="docs-url-hint">
                    <i className="fas fa-info-circle me-1"></i>
                    El sistema intentará descargar y guardar el archivo. Si no es posible (requiere inicio de sesión, etc.) se guardará solo el enlace.
                  </p>
                </div>
              ) : (!modoNuevaVersion || esURL) ? (
                esURL ? (
                  <div className="docs-form-field docs-field-full">
                    <label className="act-label">
                      URL <span className="req">*</span>
                    </label>
                    <input
                      className="act-input"
                      type="url"
                      value={form.url}
                      onChange={e => setForm(f => ({ ...f, url: e.target.value }))}
                      placeholder="https://..."
                    />
                  </div>
                ) : (
                <div className="docs-form-field docs-field-full">
                  <label className="act-label">
                    Archivo <span className="req">*</span>
                  </label>

                  {/* Zona drag & drop */}
                  <div
                    ref={dropZoneRef}
                    className={`docs-dropzone${dragging ? " docs-dropzone--active" : ""}${archivo ? " docs-dropzone--filled" : ""}`}
                    onDragOver={onDragOver}
                    onDragLeave={onDragLeave}
                    onDrop={onDrop}
                    onClick={() => fileInputRef.current?.click()}
                  >
                    {archivo ? (
                      <>
                        <i className="fas fa-check-circle docs-dropzone-icon docs-dropzone-icon--ok"></i>
                        <p className="docs-dropzone-main">{archivo.name}</p>
                        <p className="docs-dropzone-sub">{formatBytes(archivo.size)} — clic para cambiar</p>
                      </>
                    ) : (
                      <>
                        <i className="fas fa-cloud-upload-alt docs-dropzone-icon"></i>
                        <p className="docs-dropzone-main">Arrastra tu archivo aquí</p>
                        <p className="docs-dropzone-sub">
                          o <span className="docs-dropzone-link">selecciona desde tu equipo</span>
                        </p>
                        <p className="docs-dropzone-hint">PDF, Word, PowerPoint, Excel — máx. 50 MB</p>
                      </>
                    )}
                  </div>

                  {/* Input oculto */}
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".pdf,.docx,.doc,.pptx,.xlsx,.xls"
                    onChange={onArchivoChange}
                    className="doc-input-hidden"
                  />

                  {archivoError && (
                    <div className="docs-file-error">
                      <i className="fas fa-exclamation-circle me-1"></i>
                      {archivoError}
                    </div>
                  )}
                </div>
                )
              ) : null}

              {/* ── 2. Campos del documento (solo modo nuevo) ── */}
              {!modoNuevaVersion && (
                <>
                  {/* Nombre + Tipo */}
                  <div className="docs-form-field docs-field-full">
                    <label className="act-label">
                      Nombre <span className="req">*</span>
                    </label>
                    <input
                      className="act-input"
                      value={form.nombre}
                      onChange={e => onNombreChange(e.target.value)}
                      maxLength={255}
                      placeholder="Ej: Manual de Procedimientos"
                    />
                    {sugerenciaDoc && (
                      <div className="docs-sugerencia">
                        <i className="fas fa-lightbulb docs-sugerencia-icon"></i>
                        <span>
                          Ya existe: <strong>{sugerenciaDoc.nombre}</strong>{" "}
                          (v{sugerenciaDoc.versionActual?.versionTexto ?? "—"})
                        </span>
                        <button
                          type="button"
                          className="docs-btn-version"
                          onClick={() => activarModoNuevaVersion(sugerenciaDoc)}
                        >
                          <i className="fas fa-code-branch me-1"></i>
                          Subir como nueva versión
                        </button>
                      </div>
                    )}
                  </div>

                  {/* Descripción */}
                  <div className="docs-form-field docs-field-full">
                    <label className="act-label">Descripción</label>
                    <input
                      className="act-input"
                      value={form.descripcion}
                      onChange={e => setForm(f => ({ ...f, descripcion: e.target.value }))}
                      placeholder="Descripción opcional del documento"
                    />
                  </div>


                  {/* Rol + Confidencialidad */}
                  <div className="docs-form-field docs-field-sm">
                    <label className="act-label">Rol del documento</label>
                    <select
                      className="act-select"
                      value={form.rolDocumento}
                      onChange={e => setForm(f => ({ ...f, rolDocumento: e.target.value }))}
                    >
                      <option value="Principal">Principal (único por actividad)</option>
                      <option value="Anexo">Anexo / Referencia</option>
                    </select>
                  </div>

                  <div className="docs-form-field docs-field-sm">
                    <label className="act-label">Confidencialidad</label>
                    <select
                      className="act-select"
                      value={form.confidencialidad}
                      onChange={e => setForm(f => ({ ...f, confidencialidad: e.target.value }))}
                    >
                      <option value="Interna">Interna</option>
                      <option value="Publica">Pública</option>
                    </select>
                  </div>



                  {/* Categoría + Fecha vencimiento + Comentario */}
                  <div className="docs-form-field docs-field-sm">
                    <label className="act-label">Categoría</label>
                    <input
                      className="act-input"
                      value={form.categoria}
                      onChange={e => setForm(f => ({ ...f, categoria: e.target.value }))}
                      placeholder="Ej: Normativa, Técnico"
                      maxLength={100}
                    />
                  </div>

                  <div className="docs-form-field docs-field-sm">
                    <label className="act-label">Fecha de vencimiento (si aplica) <span className="req">*</span></label>
                    <input
                      className="act-input"
                      type="date"
                      value={form.fechaVencimiento}
                      onChange={e => setForm(f => ({ ...f, fechaVencimiento: e.target.value }))}
                      min={new Date().toISOString().split("T")[0]}
                      required
                    />
                  </div>

                  <div className="docs-form-field docs-field-sm">
                    <label className="act-label">Comentario (versión 1)</label>
                    <input
                      className="act-input"
                      value={form.comentarioVersion}
                      onChange={e => setForm(f => ({ ...f, comentarioVersion: e.target.value }))}
                      placeholder="Nota sobre esta versión inicial"
                      disabled={edicionBloqueada}
                    />
                  </div>
                </>
              )}

              {/* Campos en modo nueva versión */}
              {modoNuevaVersion && (
                <>
                  <div className="docs-form-field docs-field-sm">
                    <label className="act-label">Fecha de vencimiento <span className="req">*</span></label>
                    <input
                      className="act-input"
                      type="date"
                      value={form.fechaVencimiento}
                      onChange={e => setForm(f => ({ ...f, fechaVencimiento: e.target.value }))}
                      min={new Date().toISOString().split("T")[0]}
                      required
                    />
                  </div>

                  <div className="docs-form-field docs-field-full">
                    <label className="act-label">Descripción del cambio</label>
                    <input
                      className="act-input"
                      value={form.comentarioVersion}
                      onChange={e => setForm(f => ({ ...f, comentarioVersion: e.target.value }))}
                      placeholder="¿Qué cambió en esta versión?"
                      disabled={edicionBloqueada}
                    />
                  </div>
                </>
              )}

            </div>

            <div className="docs-form-actions">
              <button
                type="submit"
                className={`act-btn-save${modoNuevaVersion ? " act-btn-save--warning" : ""}`}
                disabled={enviando}
              >
                {enviando
                  ? <><i className="fas fa-spinner fa-spin me-1"></i>Guardando...</>
                  : modoNuevaVersion
                    ? <><i className="fas fa-code-branch me-1"></i>Guardar nueva versión</>
                    : <><i className="fas fa-upload me-1"></i>Guardar Documento</>
                }
              </button>
              <button
                type="button"
                className="act-btn-cancel"
                onClick={cancelarFormulario}
                disabled={enviando}
              >
                Cancelar
              </button>
            </div>
          </form>
        </div>
      )}

      {/* ── Tabla de documentos ── */}
      {!mostrarFormulario && (
        loading ? (
          <>
            <div className="docs-loading">
              <i className="fas fa-spinner fa-spin me-2"></i>Cargando documentos...
            </div>
            {(() => {
            const principal = documentos.find(d => d.rolEnActividad === "Principal");
            return principal ? (
              <DocTable
                docs={[principal]}
                navigate={navigate}
                descargando={descargando}
                onDescargar={onDescargar}
                onEliminar={onEliminar}
                isAdmin={isAdmin}
                activarModoNuevaVersion={activarModoNuevaVersion}
                setMostrarFormulario={setMostrarFormulario}
                bloqueoEdicion={edicionBloqueada}
              />
            ) : (
              <div className="docs-empty-row">
                <i className="fas fa-circle-notch me-1"></i>Sin documento principal
              </div>
            );
          })()}

          <div className="docs-section-label docs-section-label--mt">
            <i className="fas fa-paperclip me-1"></i>Anexos y referencias
          </div>
          {(() => {
            const anexos = documentos.filter(d => d.rolEnActividad !== "Principal");
            return anexos.length > 0 ? (
              <DocTable
                docs={anexos}
                navigate={navigate}
                descargando={descargando}
                onDescargar={onDescargar}
                onEliminar={onEliminar}
                isAdmin={isAdmin}
                activarModoNuevaVersion={activarModoNuevaVersion}
                setMostrarFormulario={setMostrarFormulario}
                bloqueoEdicion={edicionBloqueada}
              />
            ) : (
              <div className="docs-empty-row">
                <i className="fas fa-circle-notch me-1"></i>Sin anexos
              </div>
            );
          })()}
        </>
        ) : documentos.length === 0 ? (
          <div className="docs-empty">
            <i className="fas fa-folder-open docs-empty-icon"></i>
            No hay documentos asociados a esta actividad.
          </div>
        ) : (
          <>
            <div className="docs-section-label">
              <i className="fas fa-star me-1 docs-star-icon"></i>Documento principal
            </div>
            {(() => {
              const principal = documentos.find(d => d.rolEnActividad === "Principal");
              return principal ? (
                <DocTable
                  docs={[principal]}
                  navigate={navigate}
                  descargando={descargando}
                  onDescargar={onDescargar}
                  onEliminar={onEliminar}
                  isAdmin={isAdmin}
                  activarModoNuevaVersion={activarModoNuevaVersion}
                  setMostrarFormulario={setMostrarFormulario}
                  bloqueoEdicion={edicionBloqueada}
                />
              ) : (
                <div className="docs-empty-row">
                  <i className="fas fa-circle-notch me-1"></i>Sin documento principal
                </div>
              );
            })()}

            <div className="docs-section-label docs-section-label--mt">
              <i className="fas fa-paperclip me-1"></i>Anexos y referencias
            </div>
            {(() => {
              const anexos = documentos.filter(d => d.rolEnActividad !== "Principal");
              return anexos.length > 0 ? (
                <DocTable
                  docs={anexos}
                  navigate={navigate}
                  descargando={descargando}
                  onDescargar={onDescargar}
                  onEliminar={onEliminar}
                  isAdmin={isAdmin}
                  activarModoNuevaVersion={activarModoNuevaVersion}
                  setMostrarFormulario={setMostrarFormulario}
                  bloqueoEdicion={edicionBloqueada}
                />
              ) : (
                <div className="docs-empty-row">
                  <i className="fas fa-circle-notch me-1"></i>Sin anexos
                </div>
              );
            })()}
          </>
        )
      )}
      <Toast toast={toast} onClose={hideToast} />
      <ConfirmDialog confirm={confirmEliminar} onClose={closeConfirm} />
    </div>
  );
}

// ── Sub-componente tabla ──────────────────────────────────────────────────────

function DocTable({ docs, navigate, descargando, onDescargar, onEliminar, isAdmin, activarModoNuevaVersion, setMostrarFormulario, bloqueoEdicion }) {
  return (
    <div className="logs-table-container docs-table-wrap">
      <table className="logs-table">
        <thead>
          <tr>
            <th>Documento</th>
            <th>Tipo</th>
            <th>Estado</th>
            <th>Versión</th>
            <th>Vencimiento</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          {docs.map(doc => {
            const estaVencido = doc.fechaVencimiento && new Date(doc.fechaVencimiento) < new Date();
            const tieneArchivo = doc.versionActual?.tipoAlmacenamiento === "Archivo";
            const tieneUrl    = doc.versionActual?.tipoAlmacenamiento === "URL";
            const numVersion  = doc.versionActual?.numeroVersion ?? 0;
            const nombreMostrado = (doc.versionActual?.nombreArchivoOriginal || "").trim() || doc.nombre;

            return (
              <tr key={doc.id}>
                <td>
                  <div className="doc-nombre-text">{nombreMostrado}</div>
                  {doc.descripcion && (
                    <div className="doc-desc-text">{doc.descripcion}</div>
                  )}
                </td>
                <td>
                  <i className={`${TIPO_ICON[doc.tipoDocumento] ?? "fas fa-file"} me-1`}></i>
                  {doc.tipoDocumento}
                </td>
                <td>
                  <span className={`badge ${ESTADO_BADGE[doc.estado] ?? ""}`}>
                    {doc.estado?.replace("_", " ")}
                  </span>
                </td>
                <td>
                  {doc.versionActual ? (
                    <span
                      title={numVersion > 1 ? `${numVersion} versiones` : "Versión inicial"}
                      className={numVersion > 1 ? "doc-version-text-bold" : "doc-version-text-normal"}
                    >
                      v{doc.versionActual.versionTexto}
                      {numVersion > 1 && (
                        <span className="doc-version-badge">
                          ({numVersion})
                        </span>
                      )}
                    </span>
                  ) : (
                    <span className="doc-version-none">—</span>
                  )}
                </td>
                <td>
                  <span style={estaVencido ? { color: "#dc3545" } : {}}>
                    {formatFecha(doc.fechaVencimiento)}
                    {estaVencido && " ⚠️"}
                  </span>
                </td>
                <td>
                  <div className="d-flex gap-1">
                    <button
                      type="button"
                      className="btn-icon"
                      title="Ver detalle"
                      onClick={() =>
                        navigate(`/documentos/${doc.id}`, {
                          state: { bloqueoEdicion, bloqueadoPorImplementable: bloqueoEdicion },
                        })
                      }
                    >
                      <i className="fas fa-eye"></i>
                    </button>
                    <button
                      type="button"
                      className="btn-icon"
                      title={numVersion > 1 ? `Ver historial (${numVersion} versiones)` : "Historial de versiones"}
                      onClick={() =>
                        navigate(`/documentos/${doc.id}`, {
                          state: { scrollToHistorial: true, bloqueoEdicion, bloqueadoPorImplementable: bloqueoEdicion },
                        })
                      }
                      style={numVersion > 1 ? { color: "#2980b9" } : {}}
                    >
                      <i className="fas fa-history"></i>
                      {numVersion > 1 && (
                        <span className="doc-version-count-sup">
                          {numVersion}
                        </span>
                      )}
                    </button>
                    {tieneArchivo && (
                      <button type="button" className="btn-icon" title="Descargar versión actual" disabled={descargando === doc.id} onClick={() => onDescargar(doc)}>
                        <i className={`fas fa-${descargando === doc.id ? "spinner fa-spin" : "download"}`}></i>
                      </button>
                    )}
                    {!bloqueoEdicion && (tieneArchivo || tieneUrl) && (
                      <button
                        type="button"
                        className="btn-icon doc-link-gdrive"
                        title={`Editar ${doc.tipoDocumento} — sube una nueva versión editada`}
                        disabled={bloqueoEdicion}
                        onClick={() => { setMostrarFormulario(true); activarModoNuevaVersion(doc); }}
                      >
                        <i className="fas fa-edit"></i>
                      </button>
                    )}
                    {tieneUrl && (
                      <>
                        <a href={doc.versionActual.url} className="btn-icon doc-link-inline" title="Abrir enlace" target="_blank" rel="noreferrer">
                          <i className="fas fa-external-link-alt"></i>
                        </a>
                        {doc.versionActual.url?.includes("docs.google.com/document") && (
                          <a href={doc.versionActual.url.replace(/\/view.*$/, "/edit").replace(/\/edit.*$/, "/edit")} className="btn-icon doc-link-inline doc-link-gdocs" title="Editar en Google Docs" target="_blank" rel="noreferrer">
                            <i className="fas fa-edit"></i>
                          </a>
                        )}
                        {doc.versionActual.url?.includes("docs.google.com/spreadsheets") && (
                          <a href={doc.versionActual.url.replace(/\/view.*$/, "/edit").replace(/\/edit.*$/, "/edit")} className="btn-icon doc-link-inline doc-link-gsheets" title="Editar en Google Sheets" target="_blank" rel="noreferrer">
                            <i className="fas fa-edit"></i>
                          </a>
                        )}
                      </>
                    )}
                    {isAdmin && !bloqueoEdicion && (
                      <button type="button" className="btn-icon delete-btn" title="Eliminar documento" onClick={() => onEliminar(doc)}>
                        <i className="fas fa-trash"></i>
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}