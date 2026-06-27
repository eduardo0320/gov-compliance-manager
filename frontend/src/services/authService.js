import { apiFetch } from "./apiClient";

export function getCurrentUserInfo() {
  try { return JSON.parse(localStorage.getItem("usuario")); } catch { return null; }
}

export async function getCurrentUserInfoAsync() {
  const cached = getCurrentUserInfo();
  if (cached) return cached;
  try {
    const res = await apiFetch("/api/auth/rol");
    if (!res.ok) return null;
    const data = await res.json();
    const userInfo = { nombre: data.usuario, rol: data.rol };
    localStorage.setItem("usuario", JSON.stringify(userInfo));
    return userInfo;
  } catch { return null; }
}

export async function obtenerRolUsuario() {
  const res = await apiFetch("/api/auth/rol");
  if (!res.ok) return null;
  return res.json();
}

export async function activarTwoFactor() {
  const res = await apiFetch("/api/auth/2fa/activar", { method: "POST" });
  if (!res.ok) { const d = await res.json().catch(() => ({})); throw new Error(d.mensaje || `Error ${res.status}`); }
  return res.json();
}

export async function desactivarTwoFactor() {
  const res = await apiFetch("/api/auth/2fa/desactivar", { method: "POST" });
  if (!res.ok) { const d = await res.json().catch(() => ({})); throw new Error(d.mensaje || `Error ${res.status}`); }
  return res.json();
}

export async function confirmarTwoFactor(codigo) {
  if (!codigo?.cedula || !codigo?.codigo) throw new Error("Cédula y código son requeridos");
  const res = await apiFetch("/api/auth/2fa/confirmar", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(codigo),
  });
  if (!res.ok) { const d = await res.json().catch(() => ({})); throw new Error(d.mensaje || `Error ${res.status}`); }
  return res.json();
}
