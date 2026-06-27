import { apiFetch } from "./apiClient";

export async function getActividadesPorDominio() {
  const res = await apiFetch("/api/actividades-por-dominio");
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function getActividadesBySubdominio(id) {
  const res = await apiFetch(`/api/subdominios/${id}/actividades`);
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json();
  return (Array.isArray(data) ? data : data.value || []).map(a => ({
    idActividad:              a.idActividad ?? a.id ?? a.IdActividad ?? a.Id ?? null,
    nombre:                   a.nombre ?? a.Nombre ?? a.name ?? "",
    implementable:            a.implementable ?? a.Implementable ?? "Sí",
    fechaCompromiso:          a.fechaCompromiso ?? a.FechaCompromiso ?? null,
    estadoImplementacion:     a.estadoImplementacion ?? a.EstadoImplementacion ?? "Pendiente",
    porcentajeAvance:         a.porcentajeAvance ?? a.PorcentajeAvance ?? 0,
    funcionariosResponsablesId: a.funcionariosResponsablesId ?? a.FuncionariosResponsablesId ?? 1,
    fechaControl:             a.fechaControl ?? a.FechaControl ?? null,
    documentos:               a.documentos ?? a.Documentos ?? null,
    observaciones:            a.observaciones ?? a.Observaciones ?? null,
    _raw: a,
  }));
}

export async function getActividad(subdominioId, actividadId) {
  if (!subdominioId) throw new Error("subdominioId es requerido");
  if (!actividadId)  throw new Error("actividadId es requerido");
  const res = await apiFetch(`/api/subdominios/${subdominioId}/actividades/${actividadId}`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function crearActividad(subdominioId, nombre, funcionariosResponsablesId) {
  const res = await apiFetch(`/api/subdominios/${subdominioId}/actividades`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ nombre, funcionariosResponsablesId }),
  });
  if (!res.ok) throw new Error(await res.text());
  const data = await res.json() || {};
  return { ...data, idActividad: data.idActividad ?? data.id ?? data.IdActividad ?? null };
}

export async function editarActividad(subdominioId, actividadId, payload) {
  const res = await apiFetch(`/api/subdominios/${subdominioId}/actividades/${actividadId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function getHistorialActividad(subdominioId, actividadId) {
  const res = await apiFetch(`/api/subdominios/${subdominioId}/actividades/${actividadId}/historial`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function getMisActividades() {
  const res = await apiFetch("/api/actividades/mis-actividades");
  if (!res.ok) throw new Error(`Error obteniendo mis actividades (${res.status})`);
  return res.json();
}

export async function enviarCorreosAlertas() {
  const res = await apiFetch("/api/actividades/enviar-alertas", { method: "POST" });
  if (!res.ok) {
    if (res.status === 403) throw new Error("No tienes permisos para enviar correos de alerta");
    const text = await res.text();
    throw new Error(`Error enviando correos de alerta (${res.status})${text ? `: ${text}` : ""}`);
  }
  const ct = res.headers.get("content-type") || "";
  if (ct.includes("application/json")) return res.json();
  return { mensaje: (await res.text()) || "Correos de alerta enviados exitosamente" };
}
