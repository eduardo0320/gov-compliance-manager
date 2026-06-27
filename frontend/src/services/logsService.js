import { apiFetch } from "./apiClient";

export async function obtenerLogs(filtros = {}) {
  const { pagina = 1, tamanoPagina = 20, fechaDesde, fechaHasta, tipoAccion, usuarioId } = filtros;
  const p = new URLSearchParams({ pagina: String(pagina), tamanoPagina: String(tamanoPagina) });
  if (fechaDesde)  p.append("fechaDesde",  fechaDesde);
  if (fechaHasta)  p.append("fechaHasta",  fechaHasta);
  if (tipoAccion)  p.append("tipoAccion",  tipoAccion);
  if (usuarioId)   p.append("usuarioId",   String(usuarioId));
  const res = await apiFetch(`/api/logs?${p}`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function obtenerFiltrosLogs() {
  const res = await apiFetch("/api/logs/filtros");
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function obtenerModulosLogs() {
  return (await obtenerFiltrosLogs())?.tiposAccion ?? [];
}

export async function obtenerLogsPorUsuario(usuarioId, pagina = 1, tamanoPagina = 50) {
  const p = new URLSearchParams({ pagina: String(pagina), tamanoPagina: String(tamanoPagina) });
  const res = await apiFetch(`/api/logs/usuario/${usuarioId}?${p}`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function obtenerEstadisticasLogs(fechaDesde = null, fechaHasta = null) {
  const p = new URLSearchParams();
  if (fechaDesde) p.append("fechaDesde", fechaDesde);
  if (fechaHasta) p.append("fechaHasta", fechaHasta);
  const res = await apiFetch(`/api/logs/estadisticas?${p}`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
