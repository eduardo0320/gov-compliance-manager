import { apiFetch, parseApiError } from "./apiClient";

export async function getDocumentosActividad(subdominioId, actividadId) {
  const res = await apiFetch(`/api/subdominios/${subdominioId}/actividades/${actividadId}/documentos`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function getDocumento(id) {
  const res = await apiFetch(`/api/documentos/${id}`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function crearDocumento(formData) {
  const res = await apiFetch("/api/documentos", { method: "POST", body: formData });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

export async function eliminarDocumento(id) {
  const res = await apiFetch(`/api/documentos/${id}`, { method: "DELETE" });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

export async function cambiarEstadoDocumento(id, estado, comentario) {
  const res = await apiFetch(`/api/documentos/${id}/estado`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ estado, comentario }),
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

export async function subirNuevaVersion(documentoId, formData) {
  const res = await apiFetch(`/api/documentos/${documentoId}/versiones`, { method: "POST", body: formData });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

export async function descargarDocumento(documentoId, version = null) {
  const url = version != null
    ? `/api/documentos/${documentoId}/descargar?version=${version}`
    : `/api/documentos/${documentoId}/descargar`;
  const res = await apiFetch(url);
  if (!res.ok) throw new Error(`Error al descargar el archivo (${res.status})`);
  const ct = res.headers.get("content-type") ?? "";
  if (ct.includes("application/json")) { const d = await res.json(); window.open(d.url, "_blank", "noreferrer"); return; }
  const disposition = res.headers.get("content-disposition") ?? "";
  const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
  const filename = match ? match[1].replace(/['"]/g, "") : "documento";
  const blob = await res.blob();
  const blobUrl = window.URL.createObjectURL(blob);
  const a = Object.assign(document.createElement("a"), { href: blobUrl, download: filename });
  document.body.appendChild(a); a.click(); a.remove();
  window.URL.revokeObjectURL(blobUrl);
}

export async function actualizarDocumento(id, datos) {
  const res = await apiFetch(`/api/documentos/${id}`, {
    method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(datos),
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

export async function crearRelacionDocumento(documentoOrigenId, dto) {
  const res = await apiFetch(`/api/documentos/${documentoOrigenId}/relaciones`, {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(dto),
  });
  if (!res.ok) throw new Error(await parseApiError(res));
  return res.json();
}

export async function buscarDocumentos(filtros = {}) {
  const p = new URLSearchParams();
  if (filtros.nombre)           p.append("nombre",           filtros.nombre);
  if (filtros.estado)           p.append("estado",           filtros.estado);
  if (filtros.tipoDocumento)    p.append("tipoDocumento",    filtros.tipoDocumento);
  if (filtros.actividadId)      p.append("actividadId",      filtros.actividadId);
  if (filtros.vencimientoDesde) p.append("vencimientoDesde", filtros.vencimientoDesde);
  if (filtros.vencimientoHasta) p.append("vencimientoHasta", filtros.vencimientoHasta);
  if (filtros.codigoProceso)   p.append("codigoProceso",   filtros.codigoProceso);
  if (filtros.soloVencidos)     p.append("soloVencidos",     "true");
  if (filtros.limite)           p.append("limite",           filtros.limite);
  const res = await apiFetch(`/api/documentos/buscar?${p}`);
  if (!res.ok) throw new Error(`Error al buscar documentos (${res.status})`);
  return res.json();
}

export async function getAlertasVencimiento(dias = 30) {
  const res = await apiFetch(`/api/documentos/vencimientos?dias=${dias}`);
  if (!res.ok) throw new Error(`Error obteniendo alertas de vencimiento (${res.status})`);
  return res.json();
}

export async function getEstadisticasDocumentos() {
  const res = await apiFetch("/api/estadisticas/documentos");
  if (!res.ok) throw new Error(`Error obteniendo estadísticas (${res.status})`);
  return res.json();
}
