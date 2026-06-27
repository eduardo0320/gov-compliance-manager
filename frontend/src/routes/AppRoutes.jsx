import React from "react";
import ErrorBoundary from "../components/ui/ErrorBoundary";
import { Routes, Route, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { AdminRoute, EditorRoute } from "./ProtectedRoutes";
import Layout from "../layouts/Layout";

const Spinner = () => (
  <div className="app-spinner">
    <i className="fas fa-spinner fa-spin app-spinner__icon"></i>Cargando...
  </div>
);

const Dashboard            = React.lazy(() => import("../pages/Dashboard"));
const Profile              = React.lazy(() => import("../pages/Profile"));
const ProcessManagement    = React.lazy(() => import("../pages/ProcessManagement"));
const Login                = React.lazy(() => import("../pages/Login"));
const CambiarContrasena    = React.lazy(() => import("../pages/CambiarContrasena"));
const CrearProceso         = React.lazy(() => import("../pages/CrearProceso"));
const EditarProceso        = React.lazy(() => import("../pages/EditarProceso"));
const Logs                 = React.lazy(() => import("../pages/Logs"));
const Actividades          = React.lazy(() => import("../pages/Actividades"));
const MisActividades       = React.lazy(() => import("../pages/MisActividades"));
const ActividadesEnRevision = React.lazy(() => import("../pages/ActividadesEnRevision"));
const GanttAdminView       = React.lazy(() => import("../pages/GanttAdminView"));
const GanttPersonalView    = React.lazy(() => import("../pages/GanttPersonalView"));
const GestionUsuarios      = React.lazy(() => import("../pages/GestionUsuarios"));
const Reportes             = React.lazy(() => import("../pages/Reportes"));
const DetalleDocumento     = React.lazy(() => import("../pages/documentos/DetalleDocumento"));
const BuscarDocumentos     = React.lazy(() => import("../pages/documentos/BuscarDocumentos"));
const ExpiracionDocumentos = React.lazy(() => import("../pages/documentos/ExpiracionDocumentos"));
const NotFound             = React.lazy(() => import("../pages/NotFound"));

export default function AppRoutes() {
  const { isAuthenticated, loading, userRole } = useAuth();
  if (loading) return <Spinner />;

  return (
    <React.Suspense fallback={<Spinner />}>
      <Routes>
        {/* Públicas */}
        <Route path="/login"             element={<Login />} />
        <Route path="/cambiar-contrasena" element={<CambiarContrasena />} />

        {/* Protegidas */}
        <Route path="*" element={
          isAuthenticated
            ? <Layout>
                <ErrorBoundary>
                <React.Suspense fallback={<Spinner />}>
                  <Routes>
                    <Route path="/"                                                              element={<Dashboard />} />
                    <Route path="/profile"                                                       element={<Profile />} />
                    <Route path="/processes" element={<ProcessManagement />} />
                    <Route path="/processes/new"                                                 element={<CrearProceso />} />
                    <Route path="/processes/:dominioId"                                          element={<ProcessManagement />} />
                    <Route path="/processes/:dominioId/:procesoId"                               element={<ProcessManagement />} />
                    <Route path="/editar-proceso/:id"                                            element={<EditarProceso />} />
                    <Route path="/misActividades"                                                element={<MisActividades />} />
                    <Route path="/actividades-en-revision"                                       element={<AdminRoute><ActividadesEnRevision /></AdminRoute>} />
                    <Route path="/subdominios/:subdominioId/actividades/:actividadId/editar"     element={<Actividades />} />
                    <Route path="/documentos/buscar"                                             element={<BuscarDocumentos />} />
                    <Route path="/documentos/:documentoId"                                       element={<DetalleDocumento />} />
                    <Route path="/expiredDocuments"                                              element={<ExpiracionDocumentos rolUsuario={userRole} />} />
                    <Route path="/reportes"                                                 element={<AdminRoute><Reportes         rolUsuario={userRole} /></AdminRoute>} />
                    <Route path="/gantt"         element={<AdminRoute><GanttAdminView    userRole={userRole} /></AdminRoute>} />
                    <Route path="/gantt-personal" element={<EditorRoute><GanttPersonalView userRole={userRole} /></EditorRoute>} />
                    <Route path="/users"         element={<AdminRoute><GestionUsuarios   rolUsuario={userRole} /></AdminRoute>} />
                    <Route path="/logs"          element={<AdminRoute><Logs              rolUsuario={userRole} /></AdminRoute>} />
                    <Route path="*" element={<NotFound />} />
                  </Routes>
                </React.Suspense>
                </ErrorBoundary>
              </Layout>
            : <Navigate to="/login" replace />
        } />
      </Routes>
    </React.Suspense>
  );
}