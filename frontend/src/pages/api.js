// Nuevo endpoint: actividades agrupadas por dominio
export async function getActividadesPorDominio() {
  const res = await fetch(`${API_BASE}/api/actividades-por-dominio`, { credentials: "include" });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:5156";

async function parseApiError(res) {
  const fallback = `Error ${res.status}`;
  try {
    const data = await res.json();
    if (data?.error) return data.error;
    if (data?.mensaje) return data.mensaje;
    if (data?.title) {
      const details = data?.errors
        ? Object.values(data.errors).flat().join(" ")
        : "";
      return `${data.title}${details ? `: ${details}` : ""}`;
    }
  } catch {
    // Ignorar y usar texto plano o fallback.
  }

  try {
    const text = await res.text();
    return text || fallback;
  } catch {
    return fallback;
  }
}

export async function getDominios() {
  const res = await fetch(`${API_BASE}/api/dominios`, { credentials: "include" });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

// --- Subdominios por proceso (endpoint recomendado) ---
export async function getSubdominiosByProceso(procesoId) {
  if (!procesoId || procesoId === undefined || procesoId === null) {
    console.error('getSubdominiosByProceso: procesoId is undefined or null');
    return [];
  }

  try {
    const res = await fetch(`${API_BASE}/api/procesos/${procesoId}`, {
      credentials: "include",
    });
    if (!res.ok) {
      const errorText = await res.text();
      throw new Error(`Error ${res.status}: ${errorText}`);
    }
    const p = await res.json();
    return p.subdominios || [];
  } catch (error) {
    console.error(`Error cargando subdominios del proceso ${procesoId}:`, error);
    throw error;
  }
}

// --- Crear Proceso ---
export async function crearProceso(payload) {
  const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:5156";

  const res = await fetch(`${API_BASE}/api/procesos`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

// --- Obtener Proceso por ID ---
export async function getProcesoById(id) {
  const res = await fetch(`${API_BASE}/api/procesos/${id}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

// --- Editar Proceso ---
export async function editarProceso(id, payload) {
  const res = await fetch(`${API_BASE}/api/procesos/${id}`, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

// --- Busqueda (con normalización del resultado) ---
export async function buscarProcesosYActividades(query) {
  const res = await fetch(`${API_BASE}/api/procesos/search?q=${encodeURIComponent(query)}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();

  // Normalizar al formato que espera BarraSuperior:
  // { tipo, id, titulo, descripcion, dominioNombre, ruta }
  const arr = Array.isArray(data) ? data : (data.value || []);
  return arr.map(item => {
    // Extraer campos tolerando cualquier casing del backend
    const id = item.id ?? item.Id ?? item.idProceso ?? item.IdProceso;
    const tipo = item.tipo ?? item.Tipo ?? "Proceso";
    const titulo = item.titulo ?? item.Titulo
      ?? (item.codigo ? `${item.codigo} - ${item.nombre ?? ""}` : (item.nombre ?? ""));
    const descripcion = item.descripcion ?? item.Descripcion
      ?? item.marco_normativo ?? item.marcoNormativo ?? "";
    const dominioId = item.dominioId ?? item.DominioId
      ?? item.dominio?.id ?? item.dominio?.Id;
    const dominioNombre = item.dominioNombre ?? item.DominioNombre
      ?? item.dominio?.nombre ?? "";

    // Usar la ruta del backend si existe y es valida, sino construirla
    let ruta = item.ruta ?? item.Ruta ?? "";
    if (!ruta) {
      const tipoLower = tipo.toLowerCase();
      if (tipoLower === "actividad") {
        const subdominioId = item.subdominioId ?? item.SubdominioId;
        const actId = item.actividadId ?? item.ActividadId ?? id;
        ruta = (subdominioId && actId)
          ? `/subdominios/${subdominioId}/actividades/${actId}/editar`
          : dominioId ? `/processes/${dominioId}` : "/processes";
      } else {
        ruta = (dominioId && id)
          ? `/processes/${dominioId}/${id}`
          : "/processes";
      }
    }

    return { tipo, id, titulo, descripcion, dominioNombre, ruta };
  });
}

// --- Subdominio + actividades ---
export async function getSubdominio(id) {
  const res = await fetch(`${API_BASE}/api/subdominios/${id}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json(); // { idSubdominio, procesoId, practicasGobierno, indicadoresAsociados }
}

export async function getActividadesBySubdominio(id) {
  const res = await fetch(`${API_BASE}/api/subdominios/${id}/actividades`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();
  const arr = Array.isArray(data) ? data : (data.value || []);

  // Normalize each actividad to ensure consistent field names used by the frontend
  return arr.map(a => ({
    // idActividad may come as id, IdActividad, idActividad, or nested
    idActividad: a.idActividad ?? a.id ?? a.IdActividad ?? a.Id ?? null,
    nombre: a.nombre ?? a.Nombre ?? a.name ?? "",
    implementable: a.implementable ?? a.Implementable ?? "Sí",
    fechaCompromiso: a.fechaCompromiso ?? a.FechaCompromiso ?? a.fecha_compromiso ?? null,
    estadoImplementacion: a.estadoImplementacion ?? a.EstadoImplementacion ?? a.estado_implementacion ?? "Pendiente",
    porcentajeAvance: a.porcentajeAvance ?? a.PorcentajeAvance ?? a.porcentaje_avance ?? 0,
    funcionariosResponsablesId: a.funcionariosResponsablesId ?? a.FuncionariosResponsablesId ?? a.funcionarios_responsables_id ?? 1,
    fechaControl: a.fechaControl ?? a.FechaControl ?? a.fecha_control ?? null,
    documentos: a.documentos ?? a.Documentos ?? null,
    observaciones: a.observaciones ?? a.Observaciones ?? null,
    // keep original payload for any other fields
    _raw: a
  }));
}

export async function crearActividad(subdominioId, nombre) {
  const res = await fetch(`${API_BASE}/api/subdominios/${subdominioId}/actividades`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ nombre }),
  });
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();
  // Normalize response so callers can rely on idActividad
  const resp = data || {};
  const idActividad = resp.idActividad ?? resp.id ?? resp.IdActividad ?? resp.Id ?? null;
  return { ...resp, idActividad };
}

// --- Logs ---
export async function obtenerLogs(filtros = {}) {
  const {
    pagina = 1,
    tamanoPagina = 20,
    fechaDesde = null,
    fechaHasta = null,
    tipoAccion = null,
    usuarioId = null,
  } = filtros;

  const params = new URLSearchParams({
    pagina: pagina.toString(),
    tamanoPagina: tamanoPagina.toString(),
  });

  if (fechaDesde) params.append('fechaDesde', fechaDesde);
  if (fechaHasta) params.append('fechaHasta', fechaHasta);
  if (tipoAccion) params.append('tipoAccion', tipoAccion);
  if (usuarioId) params.append('usuarioId', usuarioId.toString());

  const res = await fetch(`${API_BASE}/api/logs?${params.toString()}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function obtenerFiltrosLogs() {
  const res = await fetch(`${API_BASE}/api/logs/filtros`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

// Compatibilidad con llamadas anteriores
export async function obtenerModulosLogs() {
  const data = await obtenerFiltrosLogs();
  return data?.tiposAccion ?? [];
}

// --- Nuevos endpoints de Logs ---
export async function obtenerLogsPorUsuario(usuarioId, pagina = 1, tamanoPagina = 50) {
  const params = new URLSearchParams({
    pagina: pagina.toString(),
    tamanoPagina: tamanoPagina.toString()
  });

  const res = await fetch(`${API_BASE}/api/logs/usuario/${usuarioId}?${params}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function obtenerEstadisticasLogs(fechaDesde = null, fechaHasta = null) {
  const params = new URLSearchParams();
  if (fechaDesde) params.append('fechaDesde', fechaDesde);
  if (fechaHasta) params.append('fechaHasta', fechaHasta);

  const res = await fetch(`${API_BASE}/api/logs/estadisticas?${params}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function getActividad(subdominioId, actividadId) {
  if (!subdominioId) throw new Error('getActividad: subdominioId is missing');
  if (!actividadId) throw new Error('getActividad: actividadId is missing');

  const res = await fetch(`${API_BASE}/api/subdominios/${subdominioId}/actividades/${actividadId}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function editarActividad(subdominioId, actividadId, payload) {
  const res = await fetch(`${API_BASE}/api/subdominios/${subdominioId}/actividades/${actividadId}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function getHistorialActividad(subdominioId, actividadId) {
  const res = await fetch(`${API_BASE}/api/subdominios/${subdominioId}/actividades/${actividadId}/historial`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

// Obtener información del usuario actual - ahora desde la respuesta del login guardada en localStorage
// (solo datos del usuario, NO el token que está en cookie HttpOnly)
export function getCurrentUserInfo() {
  const userStr = localStorage.getItem("usuario");
  if (!userStr) return null;

  try {
    return JSON.parse(userStr);
  } catch (error) {
    console.error('Error parseando usuario:', error);
    return null;
  }
}

// Versión async: intenta localStorage primero, si no hay datos consulta el backend
// y los guarda para futuras llamadas sincrónicas.
export async function getCurrentUserInfoAsync() {
  const cached = getCurrentUserInfo();
  if (cached) return cached;

  try {
    const res = await fetch(`${API_BASE}/api/auth/rol`, { credentials: 'include' });
    if (!res.ok) return null;
    const data = await res.json(); // { rol, usuario (nombre) }
    const userInfo = { nombre: data.usuario, rol: data.rol };
    localStorage.setItem('usuario', JSON.stringify(userInfo));
    return userInfo;
  } catch {
    return null;
  }
}


// Obtener mi perfil personal
export async function obtenerMiPerfil() {
  const res = await fetch(`${API_BASE}/api/usuarios/mi-perfil`, {
    credentials: "include",
    headers: { "Content-Type": "application/json" }
  });

  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ mensaje: 'Error desconocido' }));
    throw new Error(errorData.mensaje || `Error ${res.status}`);
  }

  return res.json();
}

// Actualizar mi información personal
export async function actualizarMiPerfil(datos) {
  const res = await fetch(`${API_BASE}/api/usuarios/mi-perfil`, {
    method: 'PUT',
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(datos)
  });

  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ mensaje: 'Error desconocido' }));
    throw new Error(errorData.mensaje || `Error ${res.status}`);
  }

  return res.json();
}

// Cambiar mi contraseña personal
export async function cambiarMiContrasena(datos) {
  const res = await fetch(`${API_BASE}/api/usuarios/mi-contrasena`, {
    method: 'PUT',
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(datos)
  });

  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ mensaje: 'Error desconocido' }));
    throw new Error(errorData.mensaje || `Error ${res.status}`);
  }

  return res.json();
}

// Cambiar contraseña del usuario autenticado (endpoint original - mantener compatibilidad)
export async function cambiarContrasena(datos) {
  const res = await fetch(`${API_BASE}/api/usuarios/cambiar-contrasena`, {
    method: 'PUT',
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(datos)
  });

  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ mensaje: 'Error desconocido' }));
    throw new Error(errorData.mensaje || `Error ${res.status}`);
  }

  return res.json();
}
export async function activarTwoFactor() {
  const res = await fetch(`${API_BASE}/api/auth/2fa/activar`, {
    method: 'POST',
    credentials: 'include',
  });

  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ mensaje: 'Error desconocido' }));
    throw new Error(errorData.mensaje || `Error ${res.status}`);
  }

  return res.json();
}

export async function desactivarTwoFactor() {
  const res = await fetch(`${API_BASE}/api/auth/2fa/desactivar`, {
    method: 'POST',
    credentials: 'include',
  });

  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ mensaje: 'Error desconocido' }));
    throw new Error(errorData.mensaje || `Error ${res.status}`);
  }

  return res.json();
}

export async function confirmarTwoFactor(codigo) {
  if (!codigo || !codigo.cedula || !codigo.codigo) throw new Error('Cédula y código son requeridos');

  const res = await fetch(`${API_BASE}/api/auth/2fa/confirmar`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(codigo)
  });

  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ mensaje: 'Error desconocido' }));
    throw new Error(errorData.mensaje || `Error ${res.status}`);
  }

  return res.json();
}

// HU-009: Restablecer contraseña obligatoria
export async function restablecerContrasenaObligatoria(nuevaContrasena) {
  const res = await fetch(`${API_BASE}/api/usuarios/restablecer-contrasena-obligatoria`, {
    method: 'PUT',
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(nuevaContrasena)
  });

  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ mensaje: 'Error desconocido' }));
    throw new Error(errorData.mensaje || `Error ${res.status}`);
  }

  return res.json();
}

// --- Obtener Dominio por ID (normalizado) ---
export async function obtenerDominioPorId(dominioId) {
  const res = await fetch(`${API_BASE}/api/dominios/${dominioId}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();
  // Normalizar campos a id, codigo, nombre
  const id = data.id ?? data.id_Dominio ?? data.IdDominio ?? dominioId;
  const nombre = data.nombre ?? data.Nombre ?? "";
  let codigo = data.codigo ?? data.Codigo ?? "";

  return { id, codigo, nombre };
}

export async function getProcesosByDominio(dominioId) {
  const res = await fetch(`${API_BASE}/api/dominios/${dominioId}/procesos`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();

  const procesos = data.value || data;
  return procesos.map(p => ({
    ...p,
    idProceso: p.id ?? p.idProceso,
    codigo: p.codigo ?? p.code ?? "",
    nombre: p.nombre ?? "",
    marcoNormativo: p.marco_normativo ?? p.marcoNormativo ?? "",
    estadoImplementacion: p.estado_implementacion ?? p.estadoImplementacion ?? "Sí",
    porcentajeAvance: p.porcentaje_avance ?? p.porcentajeAvance ?? 0,
    fechaCreacion: p.fechaCreacion ?? p.fechaCreacion,
    fechaConclusionImplementacion:
      p.fecha_conclusion_implementacion ?? p.fechaConclusionImplementacion ?? null,
    prioridadImplementacion:
      p.prioridad_implementacion ?? p.prioridadImplementacion ?? null,
  }));
}


// ===== GESTIÓN DOCUMENTAL =====

// Obtener documentos de una actividad
export async function getDocumentosActividad(subdominioId, actividadId) {
  const res = await fetch(
    `${API_BASE}/api/subdominios/${subdominioId}/actividades/${actividadId}/documentos`,
    { credentials: "include" }
  );
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

// Obtener detalle de un documento (con versiones y relaciones)
export async function getDocumento(id) {
  const res = await fetch(`${API_BASE}/api/documentos/${id}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

// Crear documento (multipart/form-data)
export async function crearDocumento(formData) {
  const res = await fetch(`${API_BASE}/api/documentos`, {
    method: "POST",
    credentials: "include",
    body: formData,
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

// Eliminar documento (soft delete)
export async function eliminarDocumento(id) {
  const res = await fetch(`${API_BASE}/api/documentos/${id}`, {
    method: "DELETE",
    credentials: "include",
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

// Cambiar estado de un documento
export async function cambiarEstadoDocumento(id, estado, comentario) {
  const res = await fetch(`${API_BASE}/api/documentos/${id}/estado`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ estado, comentario }),
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

// Subir nueva versión de un documento (multipart/form-data)
export async function subirNuevaVersion(documentoId, formData) {
  const res = await fetch(`${API_BASE}/api/documentos/${documentoId}/versiones`, {
    method: "POST",
    credentials: "include",
    body: formData,
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

// Descargar una versión de un documento via fetch (respeta la cookie de sesión)
export async function descargarDocumento(documentoId, version = null) {
  const url = version != null
    ? `${API_BASE}/api/documentos/${documentoId}/descargar?version=${version}`
    : `${API_BASE}/api/documentos/${documentoId}/descargar`;

  const res = await fetch(url, { credentials: "include" });
  if (!res.ok) throw new Error(`Error al descargar el archivo (${res.status})`);

  // Si el backend devuelve JSON es porque es de tipo URL
  const contentType = res.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    const data = await res.json();
    window.open(data.url, "_blank", "noreferrer");
    return;
  }

  // Extraer nombre de archivo del header Content-Disposition
  const disposition = res.headers.get("content-disposition") ?? "";
  const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
  const filename = match ? match[1].replace(/['"]/g, "") : "documento";

  const blob = await res.blob();
  const blobUrl = window.URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = blobUrl;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  window.URL.revokeObjectURL(blobUrl);
}

// Actualizar metadatos de un documento (nombre, descripción, categoría, fechas, confidencialidad)
export async function actualizarDocumento(id, datos) {
  const res = await fetch(`${API_BASE}/api/documentos/${id}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(datos),
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

// Crear relación entre dos documentos
export async function crearRelacionDocumento(documentoOrigenId, dto) {
  const res = await fetch(`${API_BASE}/api/documentos/${documentoOrigenId}/relaciones`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(dto),
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

// ── Fase 5: Búsqueda y alertas ────────────────────────────────────────────────

/**
 * Busca documentos con filtros opcionales.
 * @param {Object} filtros - { nombre, estado, tipoDocumento, actividadId, vencimientoDesde, vencimientoHasta, soloVencidos, limite }
 */
export async function buscarDocumentos(filtros = {}) {
  const params = new URLSearchParams();
  if (filtros.nombre) params.append("nombre", filtros.nombre);
  if (filtros.estado) params.append("estado", filtros.estado);
  if (filtros.tipoDocumento) params.append("tipoDocumento", filtros.tipoDocumento);
  if (filtros.actividadId) params.append("actividadId", filtros.actividadId);
  if (filtros.vencimientoDesde) params.append("vencimientoDesde", filtros.vencimientoDesde);
  if (filtros.vencimientoHasta) params.append("vencimientoHasta", filtros.vencimientoHasta);
  if (filtros.soloVencidos) params.append("soloVencidos", "true");
  if (filtros.limite) params.append("limite", filtros.limite);

  const res = await fetch(`${API_BASE}/api/documentos/buscar?${params}`, { credentials: "include" });
  if (!res.ok) throw new Error(`Error al buscar documentos (${res.status})`);
  return res.json();
}

/**
 * Obtiene documentos vencidos y próximos a vencer.
 * @param {number} dias - Días hacia adelante para "próximos a vencer" (default 30)
 */
export async function getAlertasVencimiento(dias = 30) {
  const res = await fetch(`${API_BASE}/api/documentos/vencimientos?dias=${dias}`, { credentials: "include" });
  if (!res.ok) throw new Error(`Error obteniendo alertas de vencimiento (${res.status})`);
  return res.json();
}

export async function getEstadisticasDocumentos() {
  const res = await fetch(`${API_BASE}/api/estadisticas/documentos`, { credentials: "include" });
  if (!res.ok) throw new Error(`Error obteniendo estadísticas de documentos (${res.status})`);
  return res.json();
}

export async function getNotificaciones(soloNoLeidas = false) {
  const params = soloNoLeidas ? "?soloNoLeidas=true" : "";
  const res = await fetch(`${API_BASE}/api/notificaciones${params}`, { credentials: "include" });
  if (!res.ok) throw new Error(`Error obteniendo notificaciones (${res.status})`);
  return res.json(); // { notificaciones, noLeidas }
}

export async function marcarNotificacionLeida(id) {
  const res = await fetch(`${API_BASE}/api/notificaciones/${id}/leer`, {
    method: "PUT",
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Error marcando notificación (${res.status})`);
  return res.json();
}

export async function marcarTodasNotificacionesLeidas() {
  const res = await fetch(`${API_BASE}/api/notificaciones/leer-todas`, {
    method: "PUT",
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Error marcando todas las notificaciones (${res.status})`);
  return res.json();
}

export async function eliminarNotificacion(id) {
  const res = await fetch(`${API_BASE}/api/notificaciones/${id}`, {
    method: "DELETE",
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Error eliminando notificacion (${res.status})`);
  return res.ok;
}

export async function eliminarTodasNotificaciones() {
  const res = await fetch(`${API_BASE}/api/notificaciones/eliminar-todas`, {
    method: "DELETE",
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Error eliminando todas las notificaciones (${res.status})`);
  return res.ok;
}

export async function getUsuariosSinActividades() {
  const res = await fetch(`${API_BASE}/api/notificaciones/usuarios-sin-actividades`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Error obteniendo usuarios sin actividades (${res.status})`);
  return res.json();
}

export async function getMisActividades() {
  const res = await fetch(`${API_BASE}/api/actividades/mis-actividades`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Error obteniendo mis actividades (${res.status})`);
  return res.json();
}

export async function getActividadesEnRevision() {
  const res = await fetch(`${API_BASE}/api/actividades/en-revision`, {
    credentials: "include",
  });
  if (!res.ok)
    throw new Error(`Error obteniendo actividades en revisión (${res.status})`);
  return res.json();
}

export async function getReporteSeguimientoPendientes(filtros = {}) {
  const params = new URLSearchParams();

  if (typeof filtros.incluirPorcentajeSinActualizar === "boolean") {
    params.set("incluirPorcentajeSinActualizar", String(filtros.incluirPorcentajeSinActualizar));
  }

  if (typeof filtros.incluirEstadoIncompleto === "boolean") {
    params.set("incluirEstadoIncompleto", String(filtros.incluirEstadoIncompleto));
  }

  if (typeof filtros.incluirFechaControlSinAsignar === "boolean") {
    params.set("incluirFechaControlSinAsignar", String(filtros.incluirFechaControlSinAsignar));
  }

  const query = params.toString();
  const res = await fetch(
    `${API_BASE}/api/actividades/reporte-seguimiento${query ? `?${query}` : ""}`,
    {
      credentials: "include",
    },
  );

  if (!res.ok)
    throw new Error(`Error obteniendo reporte de seguimiento (${res.status})`);

  return res.json();
}

export async function getReporteActividadesEnRevisionPorUsuario() {
  const res = await fetch(`${API_BASE}/api/actividades/reporte-revision-por-usuario`, {
    credentials: "include",
  });

  if (!res.ok)
    throw new Error(`Error obteniendo reporte de actividades en revisión (${res.status})`);

  return res.json();
}

export async function enviarCorreosAlertas() {
  const res = await fetch(`${API_BASE}/api/actividades/enviar-alertas`, {
    method: "POST",
    credentials: "include",
  });

  if (!res.ok) {
    if (res.status === 403) {
      throw new Error("No tienes permisos para enviar correos de alerta");
    }

    const errorText = await res.text();
    throw new Error(`Error enviando correos de alerta (${res.status})${errorText ? `: ${errorText}` : ""}`);
  }

  const contentType = res.headers.get("content-type") || "";
  if (contentType.includes("application/json")) {
    return res.json();
  }

  const text = await res.text();
  return { mensaje: text || "Correos de alerta enviados exitosamente" };
}