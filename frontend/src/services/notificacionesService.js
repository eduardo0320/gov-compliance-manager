import { apiFetch } from "./apiClient";

export async function getNotificaciones(soloNoLeidas = false) {
  const res = await apiFetch(`/api/notificaciones${soloNoLeidas ? "?soloNoLeidas=true" : ""}`);
  if (!res.ok) throw new Error(`Error obteniendo notificaciones (${res.status})`);
  return res.json();
}

export async function marcarNotificacionLeida(id) {
  const res = await apiFetch(`/api/notificaciones/${id}/leer`, { method: "PUT" });
  if (!res.ok) throw new Error(`Error marcando notificación (${res.status})`);
  return res.json();
}

export async function marcarTodasNotificacionesLeidas() {
  const res = await apiFetch("/api/notificaciones/leer-todas", { method: "PUT" });
  if (!res.ok) throw new Error(`Error marcando todas las notificaciones (${res.status})`);
  return res.json();
}

export async function eliminarNotificacion(id) {
  const res = await apiFetch(`/api/notificaciones/${id}`, { method: "DELETE" });
  if (!res.ok) throw new Error(`Error eliminando notificación (${res.status})`);
  return res.ok;
}

export async function eliminarTodasNotificaciones() {
  const res = await apiFetch("/api/notificaciones/eliminar-todas", { method: "DELETE" });
  if (!res.ok) throw new Error(`Error eliminando todas las notificaciones (${res.status})`);
  return res.ok;
}

export async function getUsuariosSinActividades() {
  const res = await apiFetch("/api/notificaciones/usuarios-sin-actividades");
  if (!res.ok) throw new Error(`Error obteniendo usuarios sin actividades (${res.status})`);
  return res.json();
}
