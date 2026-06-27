import { apiFetch } from "./apiClient";

// ── Dominios ──────────────────────────────────────────────────────────────
export async function getDominios() {
  const res = await apiFetch("/api/dominios");
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function obtenerDominioPorId(dominioId) {
  const res = await apiFetch(`/api/dominios/${dominioId}`);
  if (!res.ok) throw new Error(await res.text());
  const d = await res.json();
  return {
    id:     d.id ?? d.id_Dominio ?? d.IdDominio ?? dominioId,
    nombre: d.nombre ?? d.Nombre ?? "",
    codigo: d.codigo ?? d.Codigo ?? "",
  };
}

export async function getProcesosByDominio(dominioId) {
  const res = await apiFetch(`/api/dominios/${dominioId}/procesos`);
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();
  return (data.value || data).map(p => ({
    ...p,
    idProceso:                    p.id ?? p.idProceso,
    codigo:                       p.codigo ?? p.code ?? "",
    nombre:                       p.nombre ?? "",
    marcoNormativo:               p.marco_normativo ?? p.marcoNormativo ?? "",
    estadoImplementacion:         p.estado_implementacion ?? p.estadoImplementacion ?? "Sí",
    porcentajeAvance:             p.porcentaje_avance ?? p.porcentajeAvance ?? 0,
    fechaConclusionImplementacion:p.fecha_conclusion_implementacion ?? p.fechaConclusionImplementacion ?? null,
    prioridadImplementacion:      p.prioridad_implementacion ?? p.prioridadImplementacion ?? null,
  }));
}

export async function getProcesos(dominioId) {
  const params = dominioId ? `?dominioId=${dominioId}` : "";
  const res = await apiFetch(`/api/procesos${params}`);
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();
  return (data.value || data).map(p => ({
    ...p,
    idProceso:                    p.id ?? p.idProceso,
    codigo:                       p.codigo ?? p.code ?? "",
    nombre:                       p.nombre ?? "",
    marcoNormativo:               p.marco_normativo ?? p.marcoNormativo ?? "",
    estadoImplementacion:         p.estado_implementacion ?? p.estadoImplementacion ?? "No definido",
    porcentajeAvance:             p.porcentaje_avance ?? p.porcentajeAvance ?? 0,
    fechaConclusionImplementacion:p.fecha_conclusion_implementacion ?? p.fechaConclusionImplementacion ?? null,
    prioridadImplementacion:      p.prioridad_implementacion ?? p.prioridadImplementacion ?? null,
    dominio: {
      id: p.dominio?.id ?? p.dominioId ?? null,
      nombre: p.dominio?.nombre ?? p.dominioNombre ?? "Sin dominio",
    }
  }));
}

// ── Procesos ──────────────────────────────────────────────────────────────
export async function getProcesoById(id) {
  const res = await apiFetch(`/api/procesos/${id}`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function crearProceso(payload) {
  const res = await apiFetch("/api/procesos", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function editarProceso(id, payload) {
  const res = await apiFetch(`/api/procesos/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function buscarProcesosYActividades(query) {
  const res = await apiFetch(`/api/procesos/search?q=${encodeURIComponent(query)}`);
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();
  return (Array.isArray(data) ? data : data.value || []).map(item => {
    const id    = item.id ?? item.Id ?? item.idProceso ?? item.IdProceso;
    const tipo  = item.tipo ?? item.Tipo ?? "Proceso";
    const titulo = item.titulo ?? item.Titulo ?? (item.codigo ? `${item.codigo} - ${item.nombre ?? ""}` : item.nombre ?? "");
    const descripcion = item.descripcion ?? item.Descripcion ?? item.marco_normativo ?? "";
    const dominioId   = item.dominioId ?? item.DominioId ?? item.dominio?.id;
    const dominioNombre = item.dominioNombre ?? item.DominioNombre ?? item.dominio?.nombre ?? "";
    let ruta = item.ruta ?? item.Ruta ?? "";
    if (!ruta) {
      ruta = tipo.toLowerCase() === "actividad"
        ? (item.subdominioId && (item.actividadId ?? id)) ? `/subdominios/${item.subdominioId}/actividades/${item.actividadId ?? id}/editar` : dominioId ? `/processes/${dominioId}` : "/processes"
        : (dominioId && id) ? `/processes/${dominioId}/${id}` : "/processes";
    }
    return { tipo, id, titulo, descripcion, dominioNombre, ruta };
  });
}

// ── Subdominios ───────────────────────────────────────────────────────────
export async function getSubdominiosByProceso(procesoId) {
  if (!procesoId) { console.error("getSubdominiosByProceso: procesoId es requerido"); return []; }
  const res = await apiFetch(`/api/procesos/${procesoId}`);
  if (!res.ok) throw new Error(`Error ${res.status}: ${await res.text()}`);
  const p = await res.json();
  return p.subdominios || [];
}

export async function getSubdominio(id) {
  const res = await apiFetch(`/api/subdominios/${id}`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
