import React, { createContext, useContext, useState, useEffect } from "react";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [userRole, setUserRole]           = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [loading, setLoading]             = useState(true);

  useEffect(() => {
    // Llamar directamente sin el interceptor de apiClient para evitar
    // redirección durante el chequeo inicial (AppRoutes ya maneja eso)
    fetch(`${import.meta.env.VITE_API_BASE_URL || "http://localhost:5156"}/api/auth/rol`, {
      credentials: "include",
    })
      .then(res => {
        if (!res.ok) {
          localStorage.removeItem("usuario");
          sessionStorage.removeItem("sesionActiva");
          return null;
        }
        return res.json();
      })
      .then(data => {
        if (data) {
          setUserRole(data.rol);
          setIsAuthenticated(true);
          sessionStorage.setItem("sesionActiva", "1");
        }
      })
      .catch(() => {
        localStorage.removeItem("usuario");
        sessionStorage.removeItem("sesionActiva");
      })
      .finally(() => setLoading(false));
  }, []);

  const isAdmin  = userRole === "ADMIN"  || userRole === "SUPERADMIN";
  const isEditor = userRole === "EDITOR" || userRole === "EDITOR_DOMINIO";

  return (
    <AuthContext.Provider value={{ userRole, isAuthenticated, loading, isAdmin, isEditor }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth debe usarse dentro de <AuthProvider>");
  return ctx;
}