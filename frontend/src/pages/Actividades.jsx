import Toast, { useToast } from "../components/ui/Toast";
import { useEffect, useState, useRef, useCallback, useMemo } from "react";
import { apiFetch } from "../services/apiClient";
import { useNavigate, useParams, useLocation, Link } from "react-router-dom";
import {
  getActividad,
  editarActividad,
  getCurrentUserInfo,
  getHistorialActividad,
  getDocumentosActividad,
} from "../services";
import { DASHBOARD_CACHE_KEY, invalidarCacheDashboard } from "../utils/dashboardCache";
import DocumentosActividad from "./documentos/DocumentosActividad";
import { ActividadIconBox, IconDominio, IconProceso, IconSubdominio, IconActividad } from "../components/ui/TreeIcons";

const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:5156";

/* ── Icon components ── */
const IconDoc = () => (
  <svg
    width="18"
    height="18"
    viewBox="0 0 20 20"
    fill="none"
    stroke="#1d4ed8"
    strokeWidth="1.6"
  >
    <rect x="3" y="2" width="14" height="16" rx="2" />
    <line x1="7" y1="7" x2="13" y2="7" />
    <line x1="7" y1="10" x2="13" y2="10" />
    <line x1="7" y1="13" x2="10" y2="13" />
  </svg>
);
const IconUsers = () => (
  <svg
    width="18"
    height="18"
    viewBox="0 0 20 20"
    fill="none"
    stroke="#7c3aed"
    strokeWidth="1.6"
  >
    <circle cx="10" cy="6" r="3" />
    <circle cx="4" cy="8" r="2.2" />
    <circle cx="16" cy="8" r="2.2" />
    <path d="M1 17c0-2.5 2-4 5-4M14 13c3 0 5 1.5 5 4M7 13s1.2-.5 3-.5 3 .5 3 .5c0 0 1.5 1 1.5 4H5.5C5.5 14 7 13 7 13z" />
  </svg>
);
const IconFolder = () => (
  <svg
    width="18"
    height="18"
    viewBox="0 0 20 20"
    fill="none"
    stroke="#d97706"
    strokeWidth="1.6"
  >
    <path d="M2 6a2 2 0 012-2h4l2 2h6a2 2 0 012 2v7a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
  </svg>
);
const IconCalendar = () => (
  <svg
    width="14"
    height="14"
    viewBox="0 0 20 20"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.6"
  >
    <rect x="3" y="4" width="14" height="13" rx="1.5" />
    <line x1="7" y1="2" x2="7" y2="6" />
    <line x1="13" y1="2" x2="13" y2="6" />
    <line x1="3" y1="9" x2="17" y2="9" />
  </svg>
);
const IconCheck = () => (
  <svg
    width="16"
    height="16"
    viewBox="0 0 20 20"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
  >
    <polyline points="4,10 8,14 16,6" />
  </svg>
);
const IconHistory = () => (
  <svg
    width="15"
    height="15"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <polyline points="1,4 1,10 7,10" />
    <path d="M3.51 15a9 9 0 1 0 .49-5.1L1 10" />
    <polyline points="12,7 12,12 15,14" />
  </svg>
);
const IconLock = () => (
  <svg
    width="15"
    height="15"
    viewBox="0 0 20 20"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.6"
  >
    <rect x="4" y="9" width="12" height="9" rx="1.5" />
    <path d="M7 9V6a3 3 0 016 0v3" />
  </svg>
);
const IconPlus = () => (
  <svg
    width="13"
    height="13"
    viewBox="0 0 14 14"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
  >
    <line x1="7" y1="1" x2="7" y2="13" />
    <line x1="1" y1="7" x2="13" y2="7" />
  </svg>
);
const IconSave = () => (
  <svg
    width="14"
    height="14"
    viewBox="0 0 20 20"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
  >
    <path d="M5 2H14L18 6V18A1 1 0 0117 19H3A1 1 0 012 18V3A1 1 0 013 2Z" />
    <polyline points="7,2 7,9 13,9 13,2" />
    <rect x="5" y="13" width="10" height="6" />
  </svg>
);
const IconBar = () => (
  <svg
    width="14"
    height="14"
    viewBox="0 0 20 20"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.6"
  >
    <rect x="2" y="12" width="3" height="6" rx="1" />
    <rect x="8" y="8" width="3" height="10" rx="1" />
    <rect x="14" y="4" width="3" height="14" rx="1" />
  </svg>
);

/* ── Helpers ── */
const getRolFromUserInfo = (i) =>
  String(i?.rol ?? i?.nombreRol ?? "").trim().toUpperCase();
const getIdFromUserInfo = (i) =>
  i?.idUsuario ?? i?.id_usuario ?? i?.Id_Usuario ?? i?.id ?? i?.Id ?? null;
const getNombreUsuario = (u) =>
  u?.nombre_completo || u?.nombre || u?.nombreCompleto || "";

const normalizarEstado = (estado) =>
  String(estado || "").trim().toLowerCase().replace(/_/g, " ");

const esEstadoEnRevision = (estado) => {
  const v = normalizarEstado(estado);
  return v === "en revisión" || v === "en revision";
};

const esEstadoEnProgreso = (estado) =>
  normalizarEstado(estado) === "en progreso";

const esEstadoImplementado = (estado) =>
  normalizarEstado(estado) === "implementado";

const obtenerNombreDocumento = (doc) =>
  (
    doc?.versionActual?.nombreArchivoOriginal ||
    doc?.VersionActual?.nombreArchivoOriginal ||
    ""
  ).trim() || (doc?.nombre || doc?.Nombre || "").trim();

const esDocumentoPrincipal = (doc) =>
  String(doc?.rolEnActividad ?? doc?.rolDocumento ?? doc?.RolEnActividad ?? "")
    .trim()
    .toLowerCase() === "principal";

const calcularVencimientoDocumentoPrincipal = (documentosActividad = []) => {
  const principal = documentosActividad.find(esDocumentoPrincipal);
  if (!principal?.fechaVencimiento) return false;

  const vencimiento = new Date(principal.fechaVencimiento);
  if (Number.isNaN(vencimiento.getTime())) return false;

  const hoy = new Date();
  hoy.setHours(0, 0, 0, 0);
  vencimiento.setHours(0, 0, 0, 0);
  return vencimiento < hoy;
};



export default function Actividades() {
  const navigate = useNavigate();
  const location = useLocation();
  const { subdominioId, actividadId } = useParams();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showHistorial, setShowHistorial] = useState(false);
  const [loadingHistorial, setLoadingHistorial] = useState(false);
  const [errorHistorial, setErrorHistorial] = useState("");
  const [historial, setHistorial] = useState([]);
  const [puedeEditar, setPuedeEditar] = useState(false);
  const [isAdmin, setIsAdmin] = useState(false);
  const [tieneDocumentosVencidos, setTieneDocumentosVencidos] = useState(false);
  const [guardandoRevision, setGuardandoRevision] = useState(false);
  const [guardandoAceptacion, setGuardandoAceptacion] = useState(false);
  const [originalForm, setOriginalForm] = useState({});
  const documentosInicializadosRef = useRef(false);
  const documentosSnapshotRef = useRef("");

  // ── Árbol de ubicación ──
  const [arbolInfo, setArbolInfo] = useState(null); // { dominio, proceso, subdominio }

  const [form, setForm] = useState({
    nombre: "",
    implementable: "Sí",
    fechaCompromiso: "",
    estadoImplementacion: "Pendiente",
    porcentajeAvance: 0,
    funcionariosResponsablesId: "",
    fechaControl: "",
    documentos: "",
    observaciones: "",
  });

  const enRevision = esEstadoEnRevision(form.estadoImplementacion);
  const actividadImplementada = esEstadoImplementado(form.estadoImplementacion);
  const actividadAceptada =
    actividadImplementada && Number(form.porcentajeAvance || 0) >= 100;
  const puedeEnviarRevision = esEstadoEnProgreso(form.estadoImplementacion);

  const [usuarios, setUsuarios] = useState([]);
  const [loadingUsuarios, setLoadingUsuarios] = useState(true);
  const [mostrarDropdown, setMostrarDropdown] = useState(false);
  const [busquedaUsuario, setBusquedaUsuario] = useState("");
  const dropdownRef = useRef(null);

  const { toast, showToast: mostrarToast, hideToast } = useToast();

  /* ── Documentos callback ── */
  const actualizarDocumentosForm = useCallback((documentosActividad = []) => {
    const normalizados = documentosActividad.map((doc) => ({
      id: doc.id ?? doc.idDocumento ?? 0,
      nombre: obtenerNombreDocumento(doc),
      rol:
        doc.rolEnActividad ?? doc.rolDocumento ?? doc.RolEnActividad ?? "Anexo",
    }));
    const serializado = JSON.stringify(normalizados);
    const principalVencido = calcularVencimientoDocumentoPrincipal(documentosActividad);

    setTieneDocumentosVencidos(principalVencido);
    if (!documentosInicializadosRef.current) {
      documentosInicializadosRef.current = true;
      documentosSnapshotRef.current = serializado;
      setOriginalForm((f) => {
        const next = {
          ...f,
          documentos: serializado,
        };
        return next;
      });
      setForm((f) => ({
        ...f,
        documentos: serializado,
      }));
      return;
    }

    if (documentosSnapshotRef.current === serializado) return;

    documentosSnapshotRef.current = serializado;

    setForm((f) => {
      const next = {
        ...f,
        documentos: serializado,
      };
      if (f.estadoImplementacion === "Implementado") {
        return {
          ...next,
          estadoImplementacion: "En Progreso",
        };
      }
      return next;
    });
  }, []);

  /* ── Sync nombre del responsable en el input de búsqueda ── */
  useEffect(() => {
    if (!form.funcionariosResponsablesId) {
      setBusquedaUsuario("");
      return;
    }
    if (!usuarios.length) return;
    const encontrado = usuarios.find(
      (u) =>
        String(u.idUsuario ?? u.id ?? u.cedula) ===
        String(form.funcionariosResponsablesId),
    );
    if (encontrado) setBusquedaUsuario(getNombreUsuario(encontrado));
  }, [form.funcionariosResponsablesId, usuarios]);

  /* ── Usuarios filtrados ── */
  const terminoBusquedaUsuario = busquedaUsuario.trim().toLowerCase();
  const responsableActual = usuarios.find(
    (u) =>
      String(u.idUsuario ?? u.id ?? u.cedula) ===
      String(form.funcionariosResponsablesId),
  );
  const nombreResponsableActual = getNombreUsuario(responsableActual).toLowerCase();

  const usuariosFiltrados = usuarios.filter((u) => {
    if (!terminoBusquedaUsuario) return true;
    if (nombreResponsableActual && terminoBusquedaUsuario === nombreResponsableActual)
      return true;
    const texto =
      `${getNombreUsuario(u)} ${u.cedula || ""} ${u.departamento || ""}`.toLowerCase();
    return texto.includes(terminoBusquedaUsuario);
  });

  /* ── Load usuarios ── */
  // Admins: carga lista completa para poder cambiar el responsable.
  // Editores: solo necesitan mostrar el nombre del responsable actual;
  //           intentamos /api/usuarios/{id} y, como último recurso, mi-perfil.
  useEffect(() => {
    (async () => {
      try {
        setLoadingUsuarios(true);
        const res = await apiFetch(`/api/usuarios`, {
          credentials: "include",
        });
        if (res.ok) {
          const data = await res.json();
          const lista = data?.usuarios || data || [];
          setUsuarios(
            lista.filter((u) => u.activo !== false && u.estado !== "Inactivo"),
          );
        }
        // Si 403 u otro error la lista queda [], se resuelve en el effect de abajo
      } catch (e) {
        console.error("Error cargando usuarios:", e);
      } finally {
        setLoadingUsuarios(false);
      }
    })();
  }, []);

  /* ── Fallback: cargar solo el responsable cuando la lista completa no está disponible ── */
  // Se ejecuta cuando ya cargó la actividad (form.funcionariosResponsablesId disponible)
  // y la lista de usuarios quedó vacía (editor sin acceso a /api/usuarios).
  useEffect(() => {
    if (loadingUsuarios) return; // esperar a que termine la carga principal
    if (usuarios.length > 0) return; // ya tenemos la lista, no hace falta
    if (!form.funcionariosResponsablesId) return; // todavía no sabemos quién es el responsable

    (async () => {
      const responsableId = form.funcionariosResponsablesId;

      // Intento 1: endpoint individual del usuario responsable
      try {
        const r = await apiFetch(`/api/usuarios/${responsableId}`, {
          credentials: "include",
        });
        if (r.ok) {
          const u = await r.json();
          if (u && (u.nombre_completo || u.nombre || u.nombreCompleto)) {
            setUsuarios([u]);
            return;
          }
        }
      } catch {
        /* ignorar */
      }

      // Intento 2: mi-perfil (funciona si el editor ES el responsable)
      try {
        const r = await apiFetch(`/api/usuarios/mi-perfil`, {
          credentials: "include",
        });
        if (r.ok) {
          const u = await r.json();
          const miId =
            u?.idUsuario ?? u?.id_usuario ?? u?.Id_Usuario ?? u?.id ?? u?.Id;
          if (miId != null && String(miId) === String(responsableId)) {
            setUsuarios([u]);
            return;
          }
        }
      } catch {
        /* ignorar */
      }

      // Intento 3: usar los datos guardados en localStorage (nombre puede estar disponible)
      const info = getCurrentUserInfo();
      const miId = getIdFromUserInfo(info);
      if (info && miId != null && String(miId) === String(responsableId)) {
        // Construir un objeto compatible con getNombreUsuario
        const nombreLocal =
          info.nombre_completo || info.nombreCompleto || info.nombre || "";
        if (nombreLocal) {
          setUsuarios([{ idUsuario: miId, nombre_completo: nombreLocal }]);
        }
      }
    })();
  }, [loadingUsuarios, usuarios.length, form.funcionariosResponsablesId]);

  /* ── Load actividad ── */
  useEffect(() => {
    (async () => {
      try {
        documentosInicializadosRef.current = false;
        documentosSnapshotRef.current = "";
        if (!subdominioId || !actividadId) {
          setError("ID de subdominio o actividad faltante en la ruta.");
          setLoading(false);
          return;
        }
        const a = await getActividad(subdominioId, actividadId);
        const formData = {
          nombre: a.nombre ?? "",
          implementable: a.implementable ?? "Sí",
          fechaCompromiso: a.fechaCompromiso
            ? a.fechaCompromiso.substring(0, 10)
            : "",
          estadoImplementacion: a.estadoImplementacion ?? "Pendiente",
          porcentajeAvance: a.porcentajeAvance ?? 0,
          funcionariosResponsablesId: a.funcionariosResponsablesId ?? "",
          fechaControl: a.fechaControl ? a.fechaControl.substring(0, 10) : "",
          documentos: a.documentos ?? "",
          observaciones: a.observaciones ?? "",
        };
        setForm(formData);
        setTieneDocumentosVencidos(Boolean(a.tieneDocumentosVencidos));
        setOriginalForm(formData);

        const info = getCurrentUserInfo();
        const rol = getRolFromUserInfo(info);
        const esAdmin = rol === "ADMIN" || rol === "SUPERADMIN";
        const idActual = getIdFromUserInfo(info);
        const esResponsable =
          idActual != null &&
          a.funcionariosResponsablesId != null &&
          Number(idActual) === Number(a.funcionariosResponsablesId);

        setIsAdmin(esAdmin);
        setPuedeEditar(esAdmin || esResponsable);
      } catch (e) {
        setError(e.message || "Error cargando la actividad");
      } finally {
        setLoading(false);
      }
    })();
  }, [subdominioId, actividadId]);

  /* ── Cargar árbol de ubicación (dominio › proceso › subdominio) ── */
  useEffect(() => {
    if (!subdominioId || !actividadId) return;
    (async () => {
      try {
        const a = await getActividad(subdominioId, actividadId);
        setArbolInfo({
          dominio:    a.dominio    ? { id: a.dominio.id, nombre: a.dominio.nombre ?? "" } : null,
          proceso:    a.proceso    ? { id: a.proceso.id, codigo: a.proceso.codigo ?? "", nombre: a.proceso.nombre ?? "" } : null,
          subdominio: { nombre: a.subdominio?.practicasGobierno ?? "" },
        });
      } catch (e) {
        console.error("Error cargando árbol de ubicación:", e);
      }
    })();
  }, [subdominioId, actividadId]);

  /* ── Close dropdown on outside click ── */
  useEffect(() => {
    const handler = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target))
        setMostrarDropdown(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  /* ── Open historial from navigation state ── */
  useEffect(() => {
    if (location.state?.openHistorial && subdominioId && actividadId)
      abrirHistorial();
  }, [location.state, subdominioId, actividadId]);

  /* ── Form handlers ── */
  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({
      ...f,
      [name]: value,
      ...(originalForm.estadoImplementacion === "Implementado" &&
      f.estadoImplementacion === "Implementado"
        ? { estadoImplementacion: "En Progreso" }
        : {}),
    }));
  };
  const onChangeNumber = (e) => {
    const { name, value } = e.target;
    const n = Number(value);
    setForm((f) => ({
      ...f,
      [name]: isNaN(n) ? 0 : n,
      ...(originalForm.estadoImplementacion === "Implementado" &&
      f.estadoImplementacion === "Implementado"
        ? { estadoImplementacion: "En Progreso" }
        : {}),
    }));
  };

  /* ── Submit ── */
  const onSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (enRevision) {
      mostrarToast("La actividad está en revisión. Cancela la revisión para volver a editarla.", "warning");
      return;
    }

    const info = getCurrentUserInfo();
    const rol = getRolFromUserInfo(info);
    const id = getIdFromUserInfo(info);
    const resId = Number(form.funcionariosResponsablesId);
    const puede =
      rol === "ADMIN" ||
      rol === "SUPERADMIN" ||
      (id != null && Number(id) === resId);

    if (!puede) {
      mostrarToast("No tienes permisos para editar esta actividad.", "error");
      return;
    }
    if (!form.funcionariosResponsablesId && form.implementable !== "No") {
      mostrarToast("Debe asignar un funcionario responsable antes de guardar.", "warning");
      return;
    }

    const payload = {};

    if (isAdmin && form.nombre !== originalForm.nombre) {
      payload.nombre = form.nombre;
    }

    if (isAdmin && form.implementable !== originalForm.implementable) {
      payload.implementable = form.implementable;
      // Si se marca como "No Implementable", limpiar el responsable
      if (form.implementable === "No") {
        payload.funcionariosResponsablesId = null;
      }
    }

    if (!bloqueoPorImplementable) {
      if (form.fechaCompromiso !== originalForm.fechaCompromiso) {
        payload.fechaCompromiso = form.fechaCompromiso || null;
      }

      if (form.estadoImplementacion !== originalForm.estadoImplementacion) {
        payload.estadoImplementacion = form.estadoImplementacion;
      }

      if (form.porcentajeAvance !== originalForm.porcentajeAvance) {
        payload.porcentajeAvance = form.porcentajeAvance;
      }

      if (
        String(form.funcionariosResponsablesId) !==
        String(originalForm.funcionariosResponsablesId)
      ) {
        payload.funcionariosResponsablesId = Number(
          form.funcionariosResponsablesId,
        );
      }

      if (form.fechaControl !== originalForm.fechaControl) {
        payload.fechaControl = form.fechaControl || null;
      }

      if (form.documentos !== originalForm.documentos) {
        payload.documentos = form.documentos || null;
      }
    }

    if (form.estadoImplementacion === "Implementado") {
      payload.porcentajeAvance = 100;
    }

    if (form.observaciones !== originalForm.observaciones) {
      payload.observaciones = form.observaciones || null;
    }

    const changed = Object.keys(payload).length > 0;

    if (!changed) {
      mostrarToast("No hay cambios para guardar.", "info");
      return;
    }

    try {
      await editarActividad(subdominioId, actividadId, payload);
      // Construir el estado final que refleja exactamente lo que quedó en el servidor.
      // Si se marcó como "No implementable", el backend limpia el responsable — debemos
      // reflejarlo localmente para que el display se actualice sin recargar la página.
      const formFinal = {
        ...form,
        funcionariosResponsablesId:
          form.implementable === "No" ? "" : form.funcionariosResponsablesId,
      };
      setForm(formFinal);
      setOriginalForm(formFinal);
      invalidarCacheDashboard();
      mostrarToast("Actividad actualizada correctamente");
    } catch (e) {
      mostrarToast(e.message || "Error guardando la actividad", "error");
    }
  };

  const onToggleRevision = async () => {
    setError("");

    if (!subdominioId || !actividadId) {
      mostrarToast("ID de subdominio o actividad faltante en la ruta.", "error");
      return;
    }

    if (!puedeEditar) {
      mostrarToast("No tienes permisos para cambiar el estado de revisión.", "error");
      return;
    }

    if (!enRevision && !puedeEnviarRevision) {
      mostrarToast('Solo se puede enviar a revisión cuando el estado está en "En Progreso".', "warning");
      return;
    }

    const estadoDestino = enRevision ? "En Progreso" : "En Revisión";

    try {
      setGuardandoRevision(true);
      await editarActividad(subdominioId, actividadId, {
        estadoImplementacion: estadoDestino,
      });

      setForm((f) => ({ ...f, estadoImplementacion: estadoDestino }));
      setOriginalForm((f) => ({ ...f, estadoImplementacion: estadoDestino }));
      invalidarCacheDashboard();

      mostrarToast(
        estadoDestino === "En Revisión"
          ? "Actividad enviada a revisión."
          : "Revisión cancelada. La actividad volvió a En Progreso.",
      );
    } catch (e) {
      mostrarToast(e.message || "Error actualizando el estado de revisión", "error");
    } finally {
      setGuardandoRevision(false);
    }
  };

  const onAceptarActividad = async () => {
    setError("");

    if (!isAdmin) {
      mostrarToast("Solo administradores y superadministradores pueden aceptar actividades.", "error");
      return;
    }

    if (!subdominioId || !actividadId) {
      mostrarToast("ID de subdominio o actividad faltante en la ruta.", "error");
      return;
    }

    if (actividadAceptada) {
      mostrarToast("La actividad ya está implementada.", "info");
      return;
    }

    try {
      setGuardandoAceptacion(true);
      await editarActividad(subdominioId, actividadId, {
        estadoImplementacion: "Implementado",
        porcentajeAvance: 100,
      });

      setForm((f) => ({
        ...f,
        estadoImplementacion: "Implementado",
        porcentajeAvance: 100,
      }));
      setOriginalForm((f) => ({
        ...f,
        estadoImplementacion: "Implementado",
        porcentajeAvance: 100,
      }));
      invalidarCacheDashboard();
      mostrarToast("Actividad aceptada y marcada como implementada.");
    } catch (e) {
      mostrarToast(e.message || "Error al aceptar la actividad", "error");
    } finally {
      setGuardandoAceptacion(false);
    }
  };

  /* ── Historial ── */
  const abrirHistorial = async () => {
    setLoadingHistorial(true);
    setErrorHistorial("");
    try {
      const data = await getHistorialActividad(subdominioId, actividadId);
      setHistorial(Array.isArray(data) ? data : []);
      setShowHistorial(true);
    } catch (e) {
      setErrorHistorial(e.message || "Error cargando historial");
      setShowHistorial(true);
    } finally {
      setLoadingHistorial(false);
    }
  };

  /* ── Helpers ── */
  const formatearFecha = (fecha) => {
    if (!fecha) return "-";
    return new Date(fecha).toLocaleDateString("es-CR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
  };

  /* Recalcula cuando cambia el responsable o la lista de usuarios */
  const funcionarioSeleccionado = useMemo(() => {
    if (!form.funcionariosResponsablesId || !usuarios.length) return null;
    return (
      usuarios.find(
        (u) =>
          String(u.idUsuario ?? u.id ?? u.cedula) ===
          String(form.funcionariosResponsablesId),
      ) ?? null
    );
  }, [form.funcionariosResponsablesId, usuarios]);

  const obtenerListaDocumentos = (d) => {
    if (!d) return [];
    if (Array.isArray(d)) return d;
    if (typeof d === "object") return [d];
    if (typeof d === "string") {
      try {
        const p = JSON.parse(d);
        return Array.isArray(p) ? p : [p];
      } catch {
        return d
          .split(/[,;\n\r]/)
          .map((x) => x.trim())
          .filter(Boolean)
          .map((nombre) => ({ nombre, rol: "Anexo" }));
      }
    }
    return [];
  };

  const normalizarNombreDoc = (doc) =>
    typeof doc === "string" ? doc : (doc?.nombre ?? doc?.Nombre ?? "");
  const normalizarIdDoc = (doc) => {
    if (!doc || typeof doc !== "object") return 0;
    const n = Number(doc.id ?? doc.Id ?? 0);
    return Number.isFinite(n) ? n : 0;
  };

  const clasificarDocumentos = (documentos) => {
    const docs = obtenerListaDocumentos(documentos);
    const principal = [];
    const anexos = [];
    docs.forEach((doc) => {
      const rol =
        typeof doc === "object" ? (doc.rol ?? doc.Rol ?? "Anexo") : "Anexo";
      const entry = {
        id: normalizarIdDoc(doc),
        nombre: normalizarNombreDoc(doc),
      };
      if (!entry.nombre) return;
      if (String(rol).toLowerCase() === "principal") principal.push(entry);
      else anexos.push(entry);
    });
    const byId = (a, b) => {
      const ai = Number(a.id || 0),
        bi = Number(b.id || 0);
      if (ai > 0 && bi > 0) return bi - ai;
      if (ai > 0) return -1;
      if (bi > 0) return 1;
      return 0;
    };
    return { principal: principal.sort(byId), anexos: anexos.sort(byId) };
  };

  const renderDocumentoItem = (doc, key) =>
    doc.id > 0 ? (
      <li key={key}>
        <button
          type="button"
          className="btn btn-link p-0 align-baseline"
          onClick={() =>
            navigate(`/documentos/${doc.id}`, {
              state: { bloqueoEdicion, bloqueadoPorImplementable: bloqueoEdicion },
            })
          }
        >
          {doc.nombre}
        </button>
      </li>
    ) : (
      <li key={key}>{doc.nombre}</li>
    );

  /*
   * These two values are genuinely dynamic (change on every keystroke / slider move)
   * so they must remain inline — moving them to CSS would require CSS custom properties
   * set via JS anyway, which is equivalent.
   */
  const sliderStyle = {
    background: `linear-gradient(to right, #059669 ${actividadImplementada ? 100 : form.porcentajeAvance}%, #e5e7eb ${actividadImplementada ? 100 : form.porcentajeAvance}%)`,
  };
  const estadoDotColor =
    form.estadoImplementacion === "Implementado" ||
    form.estadoImplementacion === "En Progreso"
      ? "#059669"
      : "#f59e0b";
  // Usar form.implementable (estado actual del toggle) para bloqueo inmediato en la UI.
  // Esto evita que el admin pueda interactuar con documentos/campos luego de marcar "No"
  // antes de guardar. El backend también valida esto como segunda línea de defensa.
  const bloqueoPorImplementable = form.implementable === "No";
  const bloqueoEdicion = !puedeEditar || enRevision || bloqueoPorImplementable;

  if (loading) return <div className="act-loading">Cargando...</div>;

  return (
    <div className="act-page">
      {/* ── Breadcrumb ── */}
      <nav className="act-breadcrumb">
        {(() => {
          const from = location.state?.from;
          if (from === 'dashboard') {
            return <><Link to="/">Inicio</Link><span className="sep">›</span><span className="current">Editar actividad</span></>;
          }
          if (from === 'ganttPersonal') {
            return <><Link to="/">Inicio</Link><span className="sep">›</span><Link to="/gantt-personal">Mi Diagrama de Gantt</Link><span className="sep">›</span><span className="current">Editar actividad</span></>;
          }
          if (from === 'ganttAdmin') {
            return <><Link to="/">Inicio</Link><span className="sep">›</span><Link to="/gantt">Diagrama de Gantt</Link><span className="sep">›</span><span className="current">Editar actividad</span></>;
          }
          return <><Link to="/">Inicio</Link><span className="sep">›</span><Link to="/misActividades">Mis Actividades</Link><span className="sep">›</span><span className="current">Editar actividad</span></>;
        })()}
      </nav>

      {/* ── Árbol de ubicación ── */}
      {arbolInfo && (
        <div className="act-ubicacion-arbol">
          {arbolInfo.dominio && (
            <Link
              className="act-arbol-nodo act-arbol-nodo--link"
              to={`/processes/${arbolInfo.dominio.id}`}
              title="Ver dominio en el árbol"
            >
              <span className="act-arbol-icon act-arbol-icon--dominio"><IconDominio size={13} /></span>
              <span className="act-arbol-label">{arbolInfo.dominio.nombre}</span>
            </Link>
          )}
          {arbolInfo.proceso && (
            <>
              <span className="act-arbol-sep">›</span>
              <Link
                className="act-arbol-nodo act-arbol-nodo--link"
                to={`/processes/${arbolInfo.dominio?.id}/${arbolInfo.proceso.id}`}
                title="Ver proceso en el árbol"
              >
                <span className="act-arbol-icon act-arbol-icon--proceso"><IconProceso size={13} /></span>
                <span className="act-arbol-label">{arbolInfo.proceso.codigo ? `${arbolInfo.proceso.codigo} — ` : ""}{arbolInfo.proceso.nombre}</span>
              </Link>
            </>
          )}
          {arbolInfo.subdominio?.nombre && (
            <>
              <span className="act-arbol-sep">›</span>
              <span className="act-arbol-nodo">
                <span className="act-arbol-icon act-arbol-icon--subdominio"><IconSubdominio size={13} /></span>
                <span className="act-arbol-label">{arbolInfo.subdominio.nombre}</span>
              </span>
            </>
          )}
          <span className="act-arbol-sep">›</span>
          <span className="act-arbol-nodo act-arbol-nodo--current">
            <span className="act-arbol-icon act-arbol-icon--actividad"><IconActividad size={13} /></span>
            <span className="act-arbol-label">{form.nombre || "Actividad"}</span>
          </span>
        </div>
      )}

      {/* ── Header ── */}
      <div className="act-header">
        <div className="act-header-left">
          <div className="act-edit-header-row">
            <ActividadIconBox size={36} />
            <h1 className="act-edit-title">Editar Actividad</h1>
          </div>
          <p>Modifica la información de la actividad y guarda los cambios.</p>
        </div>
        <button className="act-hist-btn" type="button" onClick={abrirHistorial}>
          <IconHistory />
          Ver historial de versiones
        </button>
      </div>

      {/* ── Alerts ── */}
      {error && (
        <div className="act-alert danger" role="alert">
          <span>{error}</span>
        </div>
      )}
      {!loading && !bloqueoPorImplementable && !puedeEditar && (
        <div className="act-alert warning" role="alert">
          <IconLock />
          <span>
            Esta actividad está asignada a otro funcionario. Solo el responsable
            asignado, administradores y superadministradores pueden editar esta
            información.
          </span>
        </div>
      )}

      <form id="actividad-form" onSubmit={onSubmit}>
        {/* ── Card: Información general ── */}
        <div className="act-card card-info">
          <div className="act-card-header">
            <div className="act-card-icon blue">
              <IconDoc />
            </div>
            <span className="act-card-title">Información general</span>
          </div>
          <div className="act-card-body">
            {/* Nombre */}
            <div className={`act-field${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
              <label className="act-label">
                Nombre de la actividad <span className="req">*</span>
                {!isAdmin && (
                  <span className="hint">
                    (Solo administradores pueden editar el nombre)
                  </span>
                )}
              </label>
              <input
                className={`act-input${!isAdmin || bloqueoEdicion ? " act-input--readonly" : ""}`}
                name="nombre"
                value={form.nombre}
                onChange={onChange}
                readOnly={!isAdmin || bloqueoEdicion}
                maxLength={255}
              />
            </div>

            {/* Implementable + Estado */}
            <div className="act-grid2">
              <div className={`act-field${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
                <label className="act-label">Implementable</label>
                <div className="act-toggle-row">
                  <label className="act-toggle">
                    <input
                      type="checkbox"
                      checked={form.implementable === "Sí"}
                      onChange={(e) => {
                        if (!isAdmin || enRevision) return;
                        setForm((f) => ({
                          ...f,
                          implementable: e.target.checked ? "Sí" : "No",
                        }));
                      }}
                      disabled={!isAdmin || enRevision}
                    />
                    <span className="act-toggle-track" />
                    <span className="act-toggle-thumb" />
                  </label>
                  <span className="act-toggle-label">
                    {form.implementable === "Sí" ? "Sí" : "No"}
                  </span>
                </div>
              </div>

              <div className={`act-field${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
                <label className="act-label">Estado de implementación</label>
                <div className="act-select-wrap">
                  {/* color is runtime-computed → stays inline */}
                  <span
                    className="act-status-dot"
                    style={{ background: estadoDotColor }}
                  />
                  <select
                    className="act-select"
                    name="estadoImplementacion"
                    value={form.estadoImplementacion}
                    onChange={onChange}
                    disabled={true}
                  >
                    <option>Pendiente</option>
                    <option>En Progreso</option>
                    <option>En Revisión</option>
                    <option>Implementado</option>
                  </select>
                </div>
              </div>
            </div>

            {/* Fechas */}
            <div className="act-grid2">
              <div className={`act-field${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
                <label className="act-label">Fecha compromiso</label>
                <div className="act-date-wrap">
                  <IconCalendar />
                  <input
                    type="date"
                    className="act-input"
                    name="fechaCompromiso"
                    value={form.fechaCompromiso}
                    onChange={onChange}
                    disabled={bloqueoEdicion}
                  />
                </div>
              </div>
              <div className={`act-field${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
                <label className="act-label">Fecha control</label>
                <div className="act-date-wrap">
                  <IconCalendar />
                  <input
                    type="date"
                    className="act-input"
                    name="fechaControl"
                    value={form.fechaControl}
                    onChange={onChange}
                    disabled={bloqueoEdicion}
                  />
                </div>
              </div>
            </div>

            {/* Porcentaje de avance — slider */}
            <div className={`act-field${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
              <label className="act-label">Porcentaje de avance</label>
              <div className="act-slider-wrap">
                <div className="act-slider-val">
                  <IconBar />
                  <span>{form.porcentajeAvance}</span>
                  <span className="pct-sign">%</span>
                </div>
                {/* background gradient is runtime-computed → stays inline */}
                <input
                  type="range"
                  className="act-range"
                  min="0"
                  max="100"
                  step="1"
                  name="porcentajeAvance"
                  value={form.porcentajeAvance}
                  onChange={onChangeNumber}
                  disabled={bloqueoEdicion}
                  style={sliderStyle}
                />
              </div>
            </div>
          </div>
        </div>

        {/* ── Card: Responsable ── */}
        <div className="act-card card-resp">
          <div className="act-card-header">
            <div className="act-card-icon purple">
              <IconUsers />
            </div>
            <span className="act-card-title">Responsable</span>
          </div>
          <div className={`act-card-body${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
            <div className={`act-field${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
              <label className="act-label">
                Funcionario responsable <span className="req">*</span>
              </label>

              {loadingUsuarios ? (
                <div className="act-input act-input--loading">
                  Cargando usuarios…
                </div>
              ) : (
                <div className="act-dropdown-wrap" ref={dropdownRef}>
                  <input
                    type="text"
                    className={`act-input${!isAdmin || bloqueoEdicion ? " act-input--readonly" : ""}`}
                    placeholder="Buscar por nombre, cédula o departamento…"
                    value={busquedaUsuario}
                    onChange={(e) => {
                      if (!isAdmin || bloqueoEdicion) return;
                      setBusquedaUsuario(e.target.value);
                      setMostrarDropdown(true);
                    }}
                    onFocus={() => {
                      if (isAdmin && !bloqueoEdicion) setMostrarDropdown(true);
                    }}
                    readOnly={!isAdmin || bloqueoEdicion}
                  />
                  {isAdmin &&
                    !bloqueoEdicion &&
                    mostrarDropdown &&
                    usuariosFiltrados.length > 0 && (
                      <div className="act-dropdown">
                        {usuariosFiltrados.map((u) => {
                          const uid = u.idUsuario ?? u.id ?? u.cedula;
                          const nombre = getNombreUsuario(u);
                          return (
                            <button
                              key={uid}
                              type="button"
                              className="act-dropdown-item"
                              onClick={() => {
                                setForm((f) => ({
                                  ...f,
                                  funcionariosResponsablesId: uid,
                                }));
                                setBusquedaUsuario(nombre);
                                setMostrarDropdown(false);
                              }}
                            >
                              <strong>{nombre}</strong> — {u.cedula || ""}
                              {u.departamento && ` (${u.departamento})`}
                            </button>
                          );
                        })}
                      </div>
                    )}
                  {funcionarioSeleccionado && (
                    <div className="act-resp-confirm">
                      <IconCheck />
                      <span>
                        Responsable asignado:{" "}
                        <strong>
                          {getNombreUsuario(funcionarioSeleccionado)}
                        </strong>
                        {funcionarioSeleccionado.departamento && (
                          <span className="dept">
                            ({funcionarioSeleccionado.departamento})
                          </span>
                        )}
                      </span>
                    </div>
                  )}
                  {!funcionarioSeleccionado && form.implementable === "No" && (
                    <div className="act-resp-confirm">
                      <span>
                        <strong>Sin responsable</strong>
                      </span>
                    </div>
                  )}
                </div>
              )}

              {!form.funcionariosResponsablesId && form.implementable !== "No" && (
                <div className="act-alert warning act-alert--sm">
                  Cada actividad debe tener al menos un responsable asignado
                  antes de ser guardada.
                </div>
              )}
            </div>
          </div>
        </div>

        {/* ── Card: Observaciones ── */}
        <div className="act-card card-obs">
          <div className="act-card-header">
            <span className="act-card-title">Observaciones</span>
          </div>
          <div className="act-card-body">
            <textarea
              className={`act-textarea${tieneDocumentosVencidos ? ' act-critical' : ''}`}
              rows={3}
              name="observaciones"
              value={form.observaciones}
              onChange={onChange}
              disabled={bloqueoEdicion}
              placeholder="Agrega observaciones sobre esta actividad…"
            />
          </div>
        </div>
      </form>

      {/* ── Card: Documentos (outside form) ── */}
      {!loading && subdominioId && actividadId && (
        <div className={`act-card card-docs${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
          <div className="act-card-header">
            <div className="act-card-icon amber">
              <IconFolder />
            </div>
            <div className="act-card-title-wrap">
              <div className="act-card-title">Documentos</div>
              <div className="act-card-subtitle">
                Gestiona los documentos relacionados con esta actividad
              </div>
            </div>
          </div>
          <div className={`act-card-body act-card-body--no-top${tieneDocumentosVencidos ? ' act-critical' : ''}`}>
            <DocumentosActividad
              subdominioId={subdominioId}
              actividadId={actividadId}
              onDocumentosChange={actualizarDocumentosForm}
              bloqueoEdicion={bloqueoEdicion}
            />
          </div>
        </div>
      )}

      {/* ── Action buttons (prettier version) ── */}
      <div className="act-actions-container">
        <div className="act-actions-box">
          <button
            type="button"
            className="act-action-btn act-action-btn--back"
            onClick={() => navigate(-1)}
            title="Volver a la página anterior"
          >
            ← Volver
          </button>

          {puedeEditar && (
            <div className="act-actions-group">
              {!enRevision && (
                <button
                  type="submit"
                  form="actividad-form"
                  className="act-action-btn act-action-btn--save"
                  disabled={guardandoAceptacion}
                  title="Guardar cambios realizados"
                >
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6">
                    <path d="M14 3H6a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h8l4-4V5a2 2 0 0 0-2-2z" />
                    <polyline points="14 3 14 8 19 8" />
                  </svg>
                  Guardar cambios
                </button>
              )}

              {puedeEnviarRevision && !enRevision && (
                <button
                  type="button"
                  className="act-action-btn act-action-btn--info"
                  onClick={onToggleRevision}
                  disabled={guardandoRevision || guardandoAceptacion}
                  title="Enviar esta actividad para revisión"
                >
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6">
                    <path d="M10 2l6 3v7c0 5-6 8-6 8s-6-3-6-8V5l6-3z" />
                  </svg>
                  {guardandoRevision ? "Procesando..." : "Enviar a revisión"}
                </button>
              )}

              {enRevision && (
                <button
                  type="button"
                  className="act-action-btn act-action-btn--danger"
                  onClick={onToggleRevision}
                  disabled={guardandoRevision || guardandoAceptacion}
                  title="Cancelar la solicitud de revisión"
                >
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6">
                    <line x1="4" y1="12" x2="16" y2="12" />
                  </svg>
                  {guardandoRevision ? "Procesando..." : "Cancelar revisión"}
                </button>
              )}

              {isAdmin && enRevision && (
                <button
                  type="button"
                  className="act-action-btn act-action-btn--success"
                  onClick={onAceptarActividad}
                  disabled={guardandoAceptacion || guardandoRevision || actividadAceptada}
                  title={
                    actividadAceptada
                      ? "La actividad ya está implementada."
                      : "Acepta y marca la actividad como implementada al 100%."
                  }
                >
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2">
                    <polyline points="4 10 8 14 16 6" />
                  </svg>
                  {guardandoAceptacion ? "Aceptando..." : "Aceptar actividad"}
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      <Toast toast={toast} onClose={hideToast} />

      {/* ── Historial modal ── */}
      {showHistorial && (
        <div className="act-overlay" onClick={() => setShowHistorial(false)}>
          <div className="act-overlay-inner">
            <div className="act-modal" onClick={(e) => e.stopPropagation()}>
              <div className="act-modal-header">
                <h5>Versiones anteriores</h5>
                <button
                  className="act-modal-close"
                  type="button"
                  onClick={() => setShowHistorial(false)}
                >
                  ✕
                </button>
              </div>
              <div className="act-modal-body">
                {loadingHistorial && <p>Cargando historial…</p>}
                {errorHistorial && (
                  <div className="act-alert danger">{errorHistorial}</div>
                )}
                {!loadingHistorial &&
                  !errorHistorial &&
                  historial.length === 0 && (
                    <div className="act-alert info">
                      Esta actividad no tiene versiones anteriores registradas.
                    </div>
                  )}
                {!loadingHistorial &&
                  !errorHistorial &&
                  historial.map((item) => {
                    const docs = clasificarDocumentos(
                      item.datosAnteriores?.documentos,
                    );
                    return (
                      <div
                        key={item.idHistorialActividad}
                        className="act-version-card"
                      >
                        <div className="act-version-top">
                          <span className="act-version-badge">
                            Versión {item.version}
                          </span>
                          <span className="act-version-date">
                            {formatearFecha(item.fechaRegistro)}
                          </span>
                        </div>
                        <div className="act-version-grid">
                          <div>
                            <strong>Nombre:</strong>{" "}
                            {item.datosAnteriores?.nombre || "-"}
                          </div>
                          <div>
                            <strong>Implementable:</strong>{" "}
                            {item.datosAnteriores?.implementable || "-"}
                          </div>
                          <div>
                            <strong>Estado:</strong>{" "}
                            {item.datosAnteriores?.estadoImplementacion || "-"}
                          </div>
                          <div>
                            <strong>Avance:</strong>{" "}
                            {item.datosAnteriores?.porcentajeAvance ?? "-"}%
                          </div>
                          <div>
                            <strong>Fecha compromiso:</strong>{" "}
                            {formatearFecha(
                              item.datosAnteriores?.fechaCompromiso,
                            )}
                          </div>
                          <div>
                            <strong>Fecha control:</strong>{" "}
                            {formatearFecha(item.datosAnteriores?.fechaControl)}
                          </div>
                          <div className="full">
                            <strong>Observaciones:</strong>{" "}
                            {item.datosAnteriores?.observaciones || "-"}
                          </div>
                          <div className="full">
                            <strong>Modificado por:</strong>{" "}
                            {item.usuarioModificacionNombre || "-"}
                          </div>
                          {(docs.principal.length > 0 ||
                            docs.anexos.length > 0) && (
                            <div className="full">
                              <strong>Documento principal:</strong>
                              {docs.principal.length === 0 ? (
                                <span className="act-muted">
                                  {" "}
                                  Sin documento principal.
                                </span>
                              ) : (
                                <ul className="act-doc-list">
                                  {docs.principal.map((d, i) =>
                                    renderDocumentoItem(
                                      d,
                                      `${item.idHistorialActividad}-p-${i}`,
                                    ),
                                  )}
                                </ul>
                              )}
                              <strong>Anexos:</strong>
                              {docs.anexos.length === 0 ? (
                                <span className="act-muted"> Sin anexos.</span>
                              ) : (
                                <ul className="act-doc-list">
                                  {docs.anexos.map((d, i) =>
                                    renderDocumentoItem(
                                      d,
                                      `${item.idHistorialActividad}-a-${i}`,
                                    ),
                                  )}
                                </ul>
                              )}
                            </div>
                          )}
                        </div>
                      </div>
                    );
                  })}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}