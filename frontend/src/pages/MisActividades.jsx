import Toast, { useToast } from "../components/ui/Toast";
import { useEffect, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { getMisActividades } from "../services";
import { ActividadIconBox } from "../components/ui/TreeIcons";

function formatFecha(fecha) {
  if (!fecha) return "—";
  return new Date(fecha).toLocaleDateString("es-ES", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

function BadgeEstado({ estado }) {
  const claseMap = {
    "Pendiente":   "badge-estado--pendiente",
    "En Progreso": "badge-estado--en-progreso",
    "Implementado":"badge-estado--implementado",
    "Vencida":     "badge-estado--vencida",
  };
  const clase = claseMap[estado] ?? "badge-estado--default";
  return (
    <span className={`badge-estado ${clase}`}>
      {estado}
    </span>
  );
}

function BarraAvance({ porcentaje }) {
  const pct = Math.min(100, Math.max(0, Number(porcentaje) || 0));
  const color = pct === 100 ? "#198754" : pct >= 50 ? "#0d6efd" : "#ffc107";
  return (
    <div className="barra-avance-wrap">
      <div className="barra-avance-track">
        <div
          className="barra-avance-fill"
          style={{ width: `${pct}%`, background: color }}
        />
      </div>
      <span className="barra-avance-pct">{pct}%</span>
    </div>
  );
}

function TablaActividades({ actividades, navigate, emptyMsg }) {
  if (actividades.length === 0) {
    return (
      <div className="actividades-empty">
        <i className="fas fa-inbox"></i>
        {emptyMsg}
      </div>
    );
  }

  return (
    <div className="actividades-tabla-scroll">
      <table className="actividades-tabla">
        <thead>
          <tr>
            <th>Actividad</th>
            <th>Subdominio</th>
            <th>Estado</th>
            <th>Avance</th>
            <th>Fecha Compromiso</th>
            <th>Acción</th>
          </tr>
        </thead>
        <tbody>
          {actividades.map((a) => {
            const fechaCompromiso = a.fechaCompromiso
              ? new Date(a.fechaCompromiso)
              : null;
            const hoy = new Date();
            if (fechaCompromiso) fechaCompromiso.setHours(0, 0, 0, 0);
            hoy.setHours(0, 0, 0, 0);

            const vencida =
              !!fechaCompromiso &&
              fechaCompromiso < hoy &&
              a.estadoImplementacion !== "Implementado";

            const venceHoy =
              !!fechaCompromiso &&
              fechaCompromiso.getTime() === hoy.getTime() &&
              a.estadoImplementacion !== "Implementado";

            return (
              <tr
                key={a.idActividad}
                className={vencida ? "vencida" : venceHoy ? "vence-hoy" : ""}
              >
                <td>
                  <div className="nombre-actividad">{a.nombre}</div>
                  {vencida && (
                    <div className="vencida-badge">
                      <i className="fas fa-exclamation-triangle me-1"></i>Fecha
                      vencida
                    </div>
                  )}
                  {venceHoy && (
                    <div className="vence-hoy-badge">
                      <i className="fas fa-hourglass-half me-1"></i>Vence hoy
                    </div>
                  )}
                </td>
                <td className="col-subdominio">{a.subdominioNombre}</td>
                <td>
                  <BadgeEstado estado={a.estadoImplementacion} />
                </td>
                <td className="col-avance">
                  <BarraAvance porcentaje={a.porcentajeAvance} />
                </td>
                <td className={`col-fecha${vencida ? " fecha-vencida" : ""}`}>
                  {formatFecha(a.fechaCompromiso)}
                </td>
                <td>
                  <button
                    className="btn-accion-ver"
                    onClick={() =>
                      navigate(
                        `/subdominios/${a.subdominioId}/actividades/${a.idActividad}/editar`,
                        { state: { from: 'misActividades' } }
                      )
                    }
                  >
                    <i className="fas fa-edit me-1"></i>Ver
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export default function MisActividades() {
  const navigate = useNavigate();
  const [datos, setDatos] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const { toast: errorToast, showToast: showErrorToast, hideToast: hideErrorToast } = useToast();

  useEffect(() => {
    getMisActividades()
      .then(setDatos)
      .catch((e) => { const msg = e.message || "Error cargando actividades"; setError(msg); showErrorToast(msg, "error"); })
      .finally(() => setLoading(false));
  }, []);

  const pendientes = datos?.pendientes ?? [];
  const completadas = datos?.completadas ?? [];
  const vencidas = datos?.vencidas ?? [];

  return (
    <div className="mis-actividades-container">
      <nav className="act-breadcrumb" style={{ marginBottom: '0.75rem' }}>
        <Link to="/">Inicio</Link>
        <span className="sep">›</span>
        <span className="current">Mis Actividades</span>
      </nav>
      <div className="mis-actividades-header">
        <h2>
          <ActividadIconBox size={32} />
          Mis Actividades Asignadas
        </h2>
        <p>Actividades donde eres el funcionario responsable</p>
      </div>

      {loading ? (
        <div className="actividades-loading">
          <i className="fas fa-spinner fa-spin"></i>
          Cargando actividades...
        </div>
      ) : (
        <>
          {/* Tarjetas resumen */}
          <div className="mis-actividades-resumen">
            <div className="resumen-card resumen-card--completada">
              <i className="fas fa-check-circle resumen-icon"></i>
              <div className="resumen-numero">{completadas.length}</div>
              <div className="resumen-label">Completadas</div>
            </div>
            <div className="resumen-card resumen-card--pendiente">
              <i className="fas fa-clock resumen-icon"></i>
              <div className="resumen-numero">{pendientes.length}</div>
              <div className="resumen-label">Pendientes</div>
            </div>
            <div className="resumen-card resumen-card--vencida">
              <i className="fas fa-exclamation-triangle resumen-icon"></i>
              <div className="resumen-numero">{vencidas.length}</div>
              <div className="resumen-label">Vencidas</div>
            </div>
            <div className="resumen-card resumen-card--total">
              <i className="fas fa-list-check resumen-icon"></i>
              <div className="resumen-numero">
                {pendientes.length + completadas.length + vencidas.length}
              </div>
              <div className="resumen-label">Total asignadas</div>
            </div>
          </div>

          {/* Sección Completadas */}
          <div className="actividades-seccion">
            <div className="actividades-seccion-header actividades-seccion-header--completada">
              <i className="fas fa-check-double"></i>
              Completadas
              {completadas.length > 0 && (
                <span className="seccion-badge seccion-badge--completada">
                  {completadas.length}
                </span>
              )}
            </div>
            <TablaActividades
              actividades={completadas}
              navigate={navigate}
              emptyMsg="Todavía no tienes actividades completadas."
            />
          </div>

          {/* Sección Pendientes */}
          <div className="actividades-seccion">
            <div className="actividades-seccion-header actividades-seccion-header--pendiente">
              <i className="fas fa-hourglass-half"></i>
              Pendientes
              {pendientes.length > 0 && (
                <span className="seccion-badge seccion-badge--pendiente">
                  {pendientes.length}
                </span>
              )}
            </div>
            <TablaActividades
              actividades={pendientes}
              navigate={navigate}
              emptyMsg="¡No tienes actividades pendientes asignadas!"
            />
          </div>

          {/* Sección Vencidas */}
          <div className="actividades-seccion">
            <div className="actividades-seccion-header actividades-seccion-header--vencida">
              <i className="fas fa-exclamation-triangle"></i>
              Vencidas
              {vencidas.length > 0 && (
                <span className="seccion-badge seccion-badge--vencida">
                  {vencidas.length}
                </span>
              )}
            </div>
            <TablaActividades
              actividades={vencidas}
              navigate={navigate}
              emptyMsg="Todavía no tienes actividades vencidas."
            />
          </div>
        </>
      )}
      <Toast toast={errorToast} onClose={hideErrorToast} />
    </div>
  );
}