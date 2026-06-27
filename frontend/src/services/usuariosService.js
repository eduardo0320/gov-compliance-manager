import { apiFetch } from "./apiClient";

const throwApiError = async (res) => {
  const d = await res.json().catch(() => ({ mensaje: "Error desconocido" }));
  throw new Error(d.mensaje || `Error ${res.status}`);
};

export async function obtenerMiPerfil() {
  const res = await apiFetch("/api/usuarios/mi-perfil", { headers: { "Content-Type": "application/json" } });
  if (!res.ok) await throwApiError(res);
  return res.json();
}

export async function actualizarMiPerfil(datos) {
  const res = await apiFetch("/api/usuarios/mi-perfil", {
    method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(datos),
  });
  if (!res.ok) await throwApiError(res);
  return res.json();
}

export async function cambiarMiContrasena(datos) {
  const res = await apiFetch("/api/usuarios/mi-contrasena", {
    method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(datos),
  });
  if (!res.ok) await throwApiError(res);
  return res.json();
}

// Compatibilidad con llamadas anteriores
export async function cambiarContrasena(datos) {
  const res = await apiFetch("/api/usuarios/cambiar-contrasena", {
    method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(datos),
  });
  if (!res.ok) await throwApiError(res);
  return res.json();
}

export async function restablecerContrasenaObligatoria(nuevaContrasena) {
  const res = await apiFetch("/api/usuarios/restablecer-contrasena-obligatoria", {
    method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(nuevaContrasena),
  });
  if (!res.ok) await throwApiError(res);
  return res.json();
}
