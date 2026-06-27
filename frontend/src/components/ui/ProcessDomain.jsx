import React, { useEffect, useState } from "react";
import { apiFetch } from "../../services/apiClient";
import { useParams, useNavigate, Link } from "react-router-dom";
import { getProcesosByDominio } from '../../services';
import {
  IconDominio, IconProceso, IconSubdominio,
  DominioIconBox, TreeLegend,
} from "./TreeIcons";


const ProcessDomain = () => {
  const { dominioId } = useParams();
  const navigate = useNavigate();

  const [domain, setDomain] = useState(null);
  const [procesos, setProcesos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  useEffect(() => {
    let mounted = true;
    const fetchDomainAndProcesses = async () => {
      setLoading(true);
      setErr("");
      try {
        const dominioRes = await apiFetch(`/api/dominios/${dominioId}`, {
          credentials: "include",
        });
        if (!dominioRes.ok) throw new Error(`HTTP ${dominioRes.status}`);
        const dominioData = await dominioRes.json();

        const procesosData = await getProcesosByDominio(dominioId);

        if (!mounted) return;
        setDomain(dominioData ?? null);
        setProcesos(Array.isArray(procesosData) ? procesosData : []);
      } catch (e) {
        console.error("Error cargando dominio o procesos:", e);
        if (mounted) setErr("No se pudo cargar el dominio o sus procesos.");
      } finally {
        if (mounted) setLoading(false);
      }
    };
    fetchDomainAndProcesses();
    return () => { mounted = false; };
  }, [dominioId]);

  const handleProcessClick = (processId) => {
    navigate(`/processes/${dominioId}/${processId}`);
  };

  if (loading) return <div>Cargando...</div>;
  if (err) return <div>{err}</div>;
  if (!domain) return <div>No se encontró el dominio</div>;

  const totalSubdominios = procesos.reduce(
    (total, p) => total + (p.subdominios?.length || 0),
    0
  );

  return (
    <>
      <div className="content-header">
        <div className="breadcrumb">
          <Link to="/">Inicio</Link> / <Link to="/processes">Gestión de Procesos</Link> / {domain.nombre}
        </div>
        <div className="content-title">
          <span className="tree-level-icon tree-level-icon--dominio"><IconDominio /></span>
          {domain.nombre}
        </div>
        <div className="action-buttons">
          <button className="btn btn-secondary">
            <i className="fas fa-download"></i> Exportar Dominio
          </button>
        </div>
      </div>

      {/* Floating Action Button for Adding Process */}
      <button className="fab-button" title="Nuevo Proceso">
        <i className="fas fa-plus"></i>
      </button>

      <div className="processes-view">
        <div className="domain-overview">
          <div className="overview-card">
            <h3>Descripción del Dominio</h3>
            <p>{domain.descripcion || "Sin descripción"}</p>
          </div>

          <div className="overview-stats">
            <div className="stat-item">
              <div className="stat-number">{procesos.length}</div>
              <div className="stat-label">Procesos</div>
            </div>
            <div className="stat-item">
              <div className="stat-number">{totalSubdominios}</div>
              <div className="stat-label">Subdominios</div>
            </div>
          </div>
        </div>

        <div className="domain-section expanded">
          <div className="domain-section-legend">
            <TreeLegend />
          </div>
          <div className="domain-content">
            <ul className="process-list">
              {procesos.map((proceso) => (
                <li
                  key={proceso.idProceso}
                  className="process-item"
                  onClick={() => handleProcessClick(proceso.idProceso)}
                >
                  {/* Inline Action Buttons */}
                  <div className="process-actions">
                    <button 
                      className="action-btn edit-btn" 
                      onClick={(e) => {
                        e.stopPropagation();
                      }}
                      title="Editar proceso"
                    >
                      <i className="fas fa-edit"></i>
                    </button>
                    <button 
                      className="action-btn delete-btn" 
                      onClick={(e) => {
                        e.stopPropagation();
                      }}
                      title="Eliminar proceso"
                    >
                      <i className="fas fa-trash"></i>
                    </button>
                  </div>

                  <div className="process-header">
                    <span className="process-level-icon process-level-icon--proceso"><IconProceso /></span>
                    <div className="process-code">{proceso.codigo}</div>
                    <div className="process-name">{proceso.nombre}</div>
                  </div>
                  <div className="process-description">
                    {proceso.marcoNormativo}
                  </div>

                  {!!proceso.subdominios?.length && (
                    <div className="activities-section">
                      <div className="activities-header">
                        <span className="process-level-icon process-level-icon--subdominio"><IconSubdominio /></span>
                        Subdominios ({proceso.subdominios.length})
                      </div>
                      <div className="activity-list">
                        {proceso.subdominios.map((s) => (
                          <span key={s.idSubdominio} className="activity-tag">
                            <span className="activity-tag-icon"><IconSubdominio /></span>
                            {s.practicasGobierno}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </>
  );
};

export default ProcessDomain;