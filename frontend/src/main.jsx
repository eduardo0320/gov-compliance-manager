import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.jsx";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

// ── Estilos globales ─────────────────────────────────────────────
// Base & layout
import "./styles/base.css";             // Reset, variables CSS, botones, badges
import "./styles/layout.css";           // Barra superior, sidebar, contenido
import "./styles/globals.css";          // Estilos globales adicionales

// Páginas y secciones
import "./styles/forms.css";            // Login, formularios, cambio de contraseña
import "./styles/dashboard.css";        // Dashboard, stat cards, árbol de dominios
import "./styles/processes.css";        // Gestión de procesos
import "./styles/profile.css";          // Perfil de usuario
import "./styles/search.css";           // Búsqueda y resultados
import "./styles/Actividades.css";      // Actividades (incluye modal de nueva actividad)
import "./styles/MisActividades.css";   // Mis actividades asignadas
import "./styles/GestionUsuarios.css";  // Administración de usuarios
import "./styles/Logs.css";             // Visor de logs del sistema
import "./styles/reportes.css";         // Módulo de reportes
import "./styles/gantt-admin.css";      // Diagramas de Gantt (admin y personal)
import "./styles/documentos.css";       // Páginas de documentos

// Componentes
import "./styles/NotificationPanel.css"; // Panel de notificaciones

const queryClient = new QueryClient({
  defaultOptions: {
    queries:   { staleTime: 30_000, refetchOnWindowFocus: "always", refetchOnReconnect: true, retry: 1 },
    mutations: { retry: 0 },
  },
});

ReactDOM.createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </React.StrictMode>
);
