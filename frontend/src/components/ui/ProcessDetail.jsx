import { useEffect, useState } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import {
  getSubdominiosByProceso,
  getActividadesBySubdominio,
  getProcesoById
} from '../../services';
import ActividadFormulario from '../../pages/ActividadFormulario';
import {
  IconSubdominio, IconActividad, TreeLegend,
} from "./TreeIcons";

export default function ProcessDetail() {
  const { dominioId, procesoId } = useParams();
  const navigate = useNavigate();

  const [proceso, setProceso]                         = useState(null);
  const [subs, setSubs]                               = useState([]);
  const [loading, setLoading]                         = useState(true);
  const [expandedSubs, setExpandedSubs]               = useState({});
  const [actividadesPorSub, setActividadesPorSub]     = useState({});
  const [creatingMode, setCreatingMode]               = useState(null);
  const [creatingParentId, setCreatingParentId]       = useState(null);

  useEffect(() => {
    (async () => {
      try {
        const [procesoData, subsData] = await Promise.all([
          getProcesoById(procesoId),
          getSubdominiosByProceso(procesoId)
        ]);
        setProceso(procesoData);
        setSubs(subsData);
      } catch (err) {
        console.error("Error cargando proceso o subdominios:", err);
      } finally {
        setLoading(false);
      }
    })();
  }, [procesoId]);

  const toggleSub = async (sub) => {
    const id = sub.idSubdominio;
    setExpandedSubs(prev => ({ ...prev, [id]: !prev[id] }));
    if (!actividadesPorSub[id]) {
      try {
        const acts = await getActividadesBySubdominio(id);
        setActividadesPorSub(prev => ({ ...prev, [id]: acts }));
      } catch (err) {
        console.error(`Error cargando actividades del subdominio ${id}:`, err);
      }
    }
  };

  const startCreating = (parentId) => {
    setCreatingMode('actividad');
    setCreatingParentId(parentId);
  };

  const cancelCreating = () => {
    setCreatingMode(null);
    setCreatingParentId(null);
  };

  if (loading) return (
    <div className="loading-state">
      <i className="fas fa-spinner fa-spin"></i>
      <span>Cargando...</span>
    </div>
  );

  const nombreProceso = proceso?.nombre ?? `Proceso #${procesoId}`;
  const codigoProceso = proceso?.codigo ?? '';

  return (
    <div>
      <div className="content-header">
        <div className="breadcrumb">
          <Link to="/">Inicio</Link> /&nbsp;
          <span
            className="pd-link-danger"
            onClick={() => navigate('/processes')}
          >
            Gestión de Procesos
          </span>
          {dominioId && (
            <>
              &nbsp;/&nbsp;
              <span
                className="pd-link-danger"
                onClick={() => navigate(`/processes/${dominioId}`)}
              >
                Dominio
              </span>
            </>
          )}
          &nbsp;/ {codigoProceso} {nombreProceso}
        </div>
        <div className="content-title">
          {codigoProceso && (
            <span className="pill pill-code" className="pill-mr">
              {codigoProceso}
            </span>
          )}
          {nombreProceso}
        </div>
      </div>

      <div className="dashboard-content">
        <div className="directory-tree">
          <div className="tree-header">
            <h3>Subdominios del Proceso</h3>
            <p>
              {subs.length} subdominio{subs.length !== 1 ? 's' : ''}{' '}
              encontrado{subs.length !== 1 ? 's' : ''}
            </p>
            <TreeLegend />
          </div>

          {subs.length === 0 ? (
            <div className="loading-state">
              <i className="fas fa-info-circle"></i>
              <span>Este proceso no tiene subdominios.</span>
            </div>
          ) : (
            <div className="tree-container">
              {subs.map((sub) => (
                <div key={sub.idSubdominio} className="tree-subdomain">
                  {/* ── Subdominio ── */}
                  <div className="tree-item subdomain-item">
                    <div className="tree-content" onClick={() => toggleSub(sub)}>
                      <div className="tree-toggle">
                        <i
                          className={`fas ${
                            expandedSubs[sub.idSubdominio]
                              ? 'fa-chevron-down'
                              : 'fa-chevron-right'
                          }`}
                        ></i>
                      </div>
                      <div className="tree-icon tree-icon--subdominio">
                        <IconSubdominio />
                      </div>
                      <div className="tree-label">
                        <span className="tree-name">{sub.practicasGobierno}</span>
                        <span className="tree-count">
                          {actividadesPorSub[sub.idSubdominio]?.length ?? '•'} actividades
                        </span>
                      </div>
                    </div>
                    <div className="tree-actions">
                      <button
                        className="tree-action-btn create-btn"
                        onClick={(e) => {
                          e.stopPropagation();
                          if (!expandedSubs[sub.idSubdominio]) toggleSub(sub);
                          startCreating(sub.idSubdominio);
                        }}
                        title="Crear Actividad"
                      >
                        <i className="fas fa-plus"></i>
                      </button>
                    </div>
                  </div>

                  {creatingMode === 'actividad' &&
                    creatingParentId === sub.idSubdominio && (
                      <ActividadFormulario
                        subdominioId={sub.idSubdominio}
                        onCancel={cancelCreating}
                      />
                    )}

                  {expandedSubs[sub.idSubdominio] && (
                    <div className="tree-children">
                      {/* espacio reservado — modal ya se muestra arriba */}

                      {/* Lista de actividades */}
                      {(actividadesPorSub[sub.idSubdominio] ?? []).length === 0 ? (
                        <div
                          className="tree-item"
                          className="pd-empty-cell"
                        >
                          <i className="fas fa-info-circle" className="pd-icon-mr"></i>
                          No hay actividades
                        </div>
                      ) : (
                        actividadesPorSub[sub.idSubdominio].map((actividad) => (
                          <div key={actividad.idActividad} className="tree-activity">
                            <div
                              className="tree-item activity-item"
                              onClick={() =>
                                navigate(
                                  `/subdominios/${sub.idSubdominio}/actividades/${
                                    actividad.idActividad ?? actividad.id
                                  }/editar`
                                )
                              }
                              className="pd-clickable"
                            >
                              <div className="tree-content">
                                <div className="tree-icon tree-icon--actividad">
                                  <IconActividad />
                                </div>
                                <div className="tree-label">
                                  <span className="tree-name">{actividad.nombre}</span>
                                  <span
                                    className="tree-status"
                                    data-status={actividad.estadoImplementacion}
                                  >
                                    {actividad.estadoImplementacion}
                                  </span>
                                </div>
                              </div>
                              <div className="tree-actions">
                                <button
                                  className="tree-action-btn"
                                  title="Versiones anteriores"
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    navigate(
                                      `/subdominios/${sub.idSubdominio}/actividades/${
                                        actividad.idActividad ?? actividad.id
                                      }/editar`,
                                      { state: { openHistorial: true } }
                                    );
                                  }}
                                >
                                  <i className="fas fa-history"></i>
                                </button>
                              </div>
                            </div>
                          </div>
                        ))
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}