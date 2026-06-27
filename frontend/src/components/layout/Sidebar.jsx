import React, { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  getDominios,
  getProcesosByDominio,
  getSubdominiosByProceso,
  getActividadesBySubdominio,
} from '../../services';
import {
  DominioIconBox,
  ProcesoIconBox,
  SubdominioIconBox,
  ActividadIconBox,
} from '../ui/TreeIcons';

const Sidebar = ({ mobileOpen, onClose, isAdmin = false }) => {
  const navigate = useNavigate();
  const location = useLocation();

  const [dominios, setDominios] = useState([]);
  const [expandedDomains, setExpandedDomains] = useState({});
  const [expandedProcesses, setExpandedProcesses] = useState({});
  const [expandedSubs, setExpandedSubs] = useState({});
  const [procesosPorDominio, setProcesosPorDominio] = useState({});
  const [subdominiosPorProceso, setSubdominiosPorProceso] = useState({});
  const [actividadesPorSub, setActividadesPorSub] = useState({});

  useEffect(() => {
    (async () => {
      try {
        const data = await getDominios();
        setDominios(data);
      } catch (err) {
        console.error('Error cargando dominios:', err);
      }
    })();
  }, []);

  const toggleDomain = async (domain) => {
    const domainId = domain.id;
    setExpandedDomains(prev => ({ ...prev, [domainId]: !prev[domainId] }));
    if (!procesosPorDominio[domainId]) {
      try {
        const procesos = await getProcesosByDominio(domainId);
        setProcesosPorDominio(prev => ({ ...prev, [domainId]: procesos }));
      } catch (err) {
        console.error(`Error cargando procesos del dominio ${domainId}:`, err);
      }
    }
  };

  const toggleProcess = async (p) => {
    const idProceso = p.idProceso;
    setExpandedProcesses(prev => ({ ...prev, [idProceso]: !prev[idProceso] }));
    if (!subdominiosPorProceso[idProceso]) {
      try {
        const subs = await getSubdominiosByProceso(idProceso);
        setSubdominiosPorProceso(prev => ({ ...prev, [idProceso]: subs }));
      } catch (err) {
        console.error(`Error cargando subdominios del proceso ${idProceso}:`, err);
      }
    }
  };

  const toggleSub = async (sub) => {
    const subId = sub.idSubdominio;
    setExpandedSubs(prev => ({ ...prev, [subId]: !prev[subId] }));
    if (!actividadesPorSub[subId]) {
      try {
        const acts = await getActividadesBySubdominio(subId);
        setActividadesPorSub(prev => ({ ...prev, [subId]: acts }));
      } catch (err) {
        console.error(`Error cargando actividades del subdominio ${subId}:`, err);
      }
    }
  };

  const isActive = (path) => location.pathname === path;

  const navTo = (path) => {
    navigate(path);
    if (onClose) onClose();
  };

  return (
    <div className={`sidebar ${mobileOpen ? 'mobile-open' : ''}`}>
      <div className="sidebar-section">
        <div className="sidebar-title">Navegación</div>


        <div className={`menu-item  ${isActive('/') ? 'active' : ''}`} onClick={() => navTo('/')}>
          <i className="fas fa-file"></i><span>Panel de Control</span>
        </div>

        <div className={`menu-item ${isActive('/expiredDocuments') ? 'active' : ''}`} onClick={() => navTo('/expiredDocuments')}>
          <i className="fas fa-folder"></i><span>Verificar Documentos</span>
        </div>

        {isAdmin && (
          <>
            <div className={`menu-item ${isActive('/reportes') ? 'active' : ''}`} onClick={() => navTo('/reportes')}>
              <i className="fas fa-chart-bar"></i><span>Reportes</span>
            </div>
            <div className={`menu-item ${isActive('/gantt') ? 'active' : ''}`} onClick={() => navTo('/gantt')}>
              <i className="fas fa-chart-gantt"></i><span>Gantt General</span>
            </div>
            <div className={`menu-item ${isActive('/gantt-personal') ? 'active' : ''}`} onClick={() => navTo('/gantt-personal')}>
              <i className="fas fa-user-check"></i><span>Mi Gantt</span>
            </div>
          </>
        )}

        <div className={`menu-item ${isActive('/profile') ? 'active' : ''}`} onClick={() => navTo('/profile')}>
          <i className="fas fa-user"></i><span>Mi Perfil</span>
        </div>

        <div className={`menu-item ${isActive('/misActividades') ? 'active' : ''}`} onClick={() => navTo('/misActividades')}>
          <i className="fas fa-clipboard-check"></i><span>Actividades Asignadas</span>
        </div>

        {isAdmin && (
          <div
            className={`menu-item ${isActive('/actividades-en-revision') ? 'active' : ''}`}
            onClick={() => navigate('/actividades-en-revision')}
          >
            <i className="fas fa-user-shield"></i><span>Revisión de actividades</span>
          </div>
        )}

        {isAdmin && (
          <div className={`menu-item ${isActive('/users') ? 'active' : ''}`} onClick={() => navTo('/users')}>
            <i className="fas fa-users"></i><span>Gestión de Usuarios</span>
          </div>
        )}

        {isAdmin && (
          <div className={`menu-item ${isActive('/logs') ? 'active' : ''}`} onClick={() => navTo('/logs')}>
            <i className="fas fa-history"></i><span>Visor de Logs</span>
          </div>
        )}
      </div>

      {/* ── Árbol de Dominios COBIT ── */}
      <div className="sidebar-section">
        <div className="sidebar-title">Dominios COBIT</div>

        {dominios.map((domain) => (
          <div key={`dom-${domain.id}`}>
            {/* Dominio */}
            <div className="menu-item" onClick={() => toggleDomain(domain)} title={domain.nombre}>
              <i className={`fas fa-chevron-right expand-icon ${expandedDomains[domain.id] ? 'expanded' : ''}`}></i>
              <DominioIconBox nombre={domain.nombre} size={22} />
              <span className="sidebar-label">{domain.nombre}</span>
            </div>

            {expandedDomains[domain.id] && (
              <div className="domain-processes tree-connector">
                {(procesosPorDominio[domain.id] || []).map((p) => (
                  <div key={`proc-${p.idProceso}`}>
                    {/* Proceso */}
                    <div className="menu-item tree-item" onClick={() => toggleProcess(p)} title={`${p.codigo} - ${p.nombre}`}>
                      <i className={`fas fa-chevron-right expand-icon ${expandedProcesses[p.idProceso] ? 'expanded' : ''}`}></i>
                      <ProcesoIconBox size={20} />
                      <span className="sidebar-label">{p.codigo} - {p.nombre}</span>
                    </div>

                    {expandedProcesses[p.idProceso] && (subdominiosPorProceso[p.idProceso] || []).length > 0 && (
                      <div className="tree-connector">
                        {(subdominiosPorProceso[p.idProceso] || []).map((s) => (
                          <div key={`sub-${s.idSubdominio}`}>
                            {/* Subdominio */}
                            <div className="menu-item tree-item" onClick={() => toggleSub(s)} title={s.practicasGobierno}>
                              <i className={`fas fa-chevron-right expand-icon ${expandedSubs[s.idSubdominio] ? 'expanded' : ''}`}></i>
                              <SubdominioIconBox size={20} />
                              <span className="sidebar-label">{s.practicasGobierno}</span>
                            </div>

                            {expandedSubs[s.idSubdominio] && (
                              <div className="tree-connector">
                                <div className="process-activities">
                                  {(actividadesPorSub[s.idSubdominio] || []).length === 0 ? (
                                    <div className="empty-state small">
                                      <i className="fas fa-info-circle"></i>
                                      <span>No hay actividades.</span>
                                    </div>
                                  ) : (
                                    (actividadesPorSub[s.idSubdominio] || []).map((a) => (
                                      <div
                                        key={`act-${a.idActividad ?? a.id}`}
                                        className="sidebar-activity-item"
                                        onClick={() => navTo(`/subdominios/${s.idSubdominio}/actividades/${a.idActividad ?? a.id}/editar`)}
                                        title={a.nombre}
                                      >
                                        <ActividadIconBox size={18} />
                                        <span className="sidebar-label">{a.nombre}</span>
                                      </div>
                                    ))
                                  )}
                                </div>
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    )}

                    {expandedProcesses[p.idProceso] && (subdominiosPorProceso[p.idProceso] || []).length === 0 && (
                      <div className="empty-state">
                        <i className="fas fa-info-circle"></i>
                        <span>No hay subdominios en este proceso.</span>
                      </div>
                    )}
                  </div>
                ))}

                {(!procesosPorDominio[domain.id] || procesosPorDominio[domain.id].length === 0) && (
                  <div className="empty-state">
                    <i className="fas fa-info-circle"></i>
                    <span>No hay procesos en este dominio.</span>
                  </div>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

export default Sidebar;