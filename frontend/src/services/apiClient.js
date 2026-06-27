const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:5156";
export { API_BASE };

export async function parseApiError(res) {
  const fallback = `Error ${res.status}`;
  try {
    const data = await res.json();
    if (data?.error)  return data.error;
    if (data?.mensaje) return data.mensaje;
    if (data?.title) {
      const details = data?.errors ? Object.values(data.errors).flat().join(" ") : "";
      return `${data.title}${details ? `: ${details}` : ""}`;
    }
  } catch { /* noop */ }
  try { return (await res.text()) || fallback; } catch { return fallback; }
}

export async function apiFetch(url, options = {}) {
  const res = await fetch(`${API_BASE}${url}`, { credentials: "include", ...options });

  // Si el servidor rechaza por sesión expirada, limpiar y redirigir al login
  if (res.status === 401 && !url.includes("/api/auth/")) {
    localStorage.removeItem("usuario");
    sessionStorage.removeItem("sesionActiva");
    window.location.href = "/login";
  }

  return res;
}