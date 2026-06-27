import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import ActividadFormulario from './ActividadFormulario';
import { DASHBOARD_CACHE_KEY, invalidarCacheDashboard } from "../utils/dashboardCache";
import {
  IconDominio, IconProceso, IconSubdominio, IconActividad,
  DominioIconBox, ProcesoIconBox, SubdominioIconBox, ActividadIconBox,
  getDomainColor, TreeLegend,
} from "../components/ui/TreeIcons";
import { obtenerRolUsuario, getAlertasVencimiento } from "../services";
import { apiFetch } from "../services/apiClient";

const fmtDateTime = (v) => {
  if (!v) return '—';
  const d = new Date(v);
  return isNaN(d.getTime()) ? '—' : d.toLocaleString();
};

const fmtPrioridad = (n) => {
  if (n === 1 || n === '1') return 'Etapa 1';
  if (n === 2 || n === '2') return 'Etapa 2';
  if (n === 3 || n === '3') return 'Etapa 3';
  return '—';
};

const fmtImplementable = (val) =>
  (val ?? '').toString().toLowerCase() === 'no' ? 'No' : 'Sí';

const CACHE_TTL = 5 * 60 * 1000;

function readCache() {
  try {
    const raw = localStorage.getItem(DASHBOARD_CACHE_KEY);
    if (!raw) return null;
    const { ts, data } = JSON.parse(raw);
    if (Date.now() - ts > CACHE_TTL) return null;
    return data;
  } catch { return null; }
}

function writeCache(data) {
  try { localStorage.setItem(DASHBOARD_CACHE_KEY, JSON.stringify({ ts: Date.now(), data })); } catch {}
}

const Dashboard = () => {
  const navigate = useNavigate();
  const [dominios, setDominios] = useState([]);
  const [stats, setStats] = useState({ totalDominios:0, totalProcesos:0, totalSubdominios:0, totalActividades:0, procesosImplementados:0, procesosPendientes:0 });
  const [loading, setLoading] = useState(true);
  const [isAdmin, setIsAdmin] = useState(false);
  const [alertasVencimiento, setAlertasVencimiento] = useState(null);
  const [expandedDomains, setExpandedDomains] = useState({});
  const [expandedProcesses, setExpandedProcesses] = useState({});
  const [expandedSubs, setExpandedSubs] = useState({});
  const [creatingMode, setCreatingMode] = useState(null);
  const [creatingParentId, setCreatingParentId] = useState(null);

  useEffect(() => {
    obtenerRolUsuario()
      .then(d => { if (d) setIsAdmin(d.rol === 'ADMIN' || d.rol === 'SUPERADMIN'); })
      .catch(() => {});
  }, []);

  useEffect(() => {
    getAlertasVencimiento(30)
      .then(d => { if (d) setAlertasVencimiento(d); })
      .catch(() => {});
  }, []);

  const cargarArbol = useCallback(async (forzar = false) => {
    if (!forzar) {
      const cached = readCache();
      if (cached) {
        setDominios(cached.dominios);
        setStats(cached.stats);
        setLoading(false);
        return;
      }
    }
    setLoading(true);
    try {
      const res = await apiFetch('/api/dashboard/arbol-completo');
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const arbol = await res.json();

      let totalProcesos = 0, totalSubdominios = 0, totalActividades = 0;
      for (const dom of arbol) {
        totalProcesos += dom.procesos?.length ?? 0;
        for (const proc of dom.procesos ?? []) {
          totalSubdominios += proc.subdominios?.length ?? 0;
          for (const sub of proc.subdominios ?? []) {
            totalActividades += sub.actividades?.length ?? 0;
          }
        }
      }

      const nuevoStats = {
        totalDominios: arbol.length, totalProcesos, totalSubdominios, totalActividades,
        procesosImplementados: Math.floor(totalProcesos * 0.7),
        procesosPendientes: Math.ceil(totalProcesos * 0.3)
      };

      setDominios(arbol);
      setStats(nuevoStats);
      writeCache({ dominios: arbol, stats: nuevoStats });
    } catch (err) {
      console.error('Error cargando arbol:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { cargarArbol(); }, [cargarArbol]);

  const toggleDomain  = (id) => setExpandedDomains(p  => ({ ...p, [id]: !p[id] }));
  const toggleProcess = (id) => setExpandedProcesses(p => ({ ...p, [id]: !p[id] }));
  const toggleSub     = (id) => setExpandedSubs(p      => ({ ...p, [id]: !p[id] }));
  const startCreating  = (mode, parentId) => { setCreatingMode(mode); setCreatingParentId(parentId); };
  const cancelCreating = () => { setCreatingMode(null); setCreatingParentId(null); };

  return (
    <div className="content">
      <div className="content-header">
        <div className="breadcrumb"><Link to="/">Inicio</Link></div>
        <div className="content-title">Panel de Control - Sistema de Normas MICITT</div>
      </div>
      <div className="dashboard-content">
        <div className="welcome-banner">
          <div className="welcome-content">
            <h2>Sistema de Gestión de Normas COBIT</h2>
            <p>Gestiona y supervisa los procesos de gobierno y gestión de TI basados en el marco COBIT 2019</p>
          </div>
          <div className="welcome-icon"><i className="fas fa-shield-alt"></i></div>
        </div>

        <div className="stats-grid">
          <div className="stat-card">
            <div className="stat-icon stat-icon--purple"><i className="fas fa-layer-group"></i></div>
            <div className="stat-info">
              <div className="stat-number stat-number--purple">{stats.totalDominios}</div>
              <div className="stat-label">Dominios COBIT</div>
            </div>
          </div>
          <div className="stat-card">
            <div className="stat-icon stat-icon--blue"><i className="fas fa-project-diagram"></i></div>
            <div className="stat-info">
              <div className="stat-number stat-number--blue">{stats.totalProcesos}</div>
              <div className="stat-label">Procesos Totales</div>
            </div>
          </div>
          <div className="stat-card">
            <div className="stat-icon stat-icon--green"><i className="fas fa-check-circle"></i></div>
            <div className="stat-info">
              <div className="stat-number stat-number--green">{stats.procesosImplementados}</div>
              <div className="stat-label">Implementados</div>
            </div>
          </div>
          <div className="stat-card">
            <div className="stat-icon stat-icon--red"><i className="fas fa-clock"></i></div>
            <div className="stat-info">
              <div className="stat-number stat-number--red">{stats.procesosPendientes}</div>
              <div className="stat-label">Pendientes</div>
            </div>
          </div>
        </div>

        <div className="directory-tree">
          <div className="tree-header">
            <h3>Estructura Completa del Sistema</h3>
            <TreeLegend />
          </div>

          {loading ? (
            <div className="loading-state">
              <i className="fas fa-spinner fa-spin"></i>
              <span>Cargando estructura...</span>
            </div>
          ) : (
            <div className="tree-container">
              {dominios.map((domain) => {
                const domId = domain.id;
                const domNombre = domain.nombre ?? '';
                return (
                  <div key={domId} className="tree-domain">
                    <div className="tree-item domain-item">
                      <div className="tree-content" onClick={() => toggleDomain(domId)}>
                        <div className="tree-toggle">
                          <i className={`fas ${expandedDomains[domId] ? 'fa-chevron-down' : 'fa-chevron-right'}`}></i>
                        </div>
                        <div className="tree-icon tree-icon--dominio" style={{ color: getDomainColor(domNombre), background: getDomainColor(domNombre) + '18', border: `1px solid ${getDomainColor(domNombre)}44` }}>
                          <IconDominio size={16} />
                        </div>
                        <div className="tree-label">
                          <span className="tree-name">{domNombre}</span>
                          <span className="tree-count">{domain.procesos?.length ?? 0} procesos</span>
                        </div>
                      </div>
                      {isAdmin && (
                        <div className="tree-actions">
                          <button className="tree-action-btn create-btn" title="Crear Proceso"
                            onClick={(e) => { e.stopPropagation(); navigate(`/processes/new?dominioId=${domId}`); }}>
                            <i className="fas fa-plus"></i>
                          </button>
                        </div>
                      )}
                    </div>

                    {expandedDomains[domId] && (
                      <div className="tree-children">
                        {domain.procesos?.map((proceso) => (
                          <div key={proceso.idProceso} className="tree-process">
                            <div className="tree-item process-item">
                              <div className="tree-content" onClick={() => toggleProcess(proceso.idProceso)}>
                                <div className="tree-toggle">
                                  <i className={`fas ${expandedProcesses[proceso.idProceso] ? 'fa-chevron-down' : 'fa-chevron-right'}`}></i>
                                </div>
                                <div className="tree-icon tree-icon--proceso">
                                  <IconProceso size={16} />
                                </div>
                                <div className="process-card">
                                  <div className="process-header">
                                    <span className="pill pill-code">{proceso.codigo}</span>
                                    <h4 className="process-title">{proceso.nombre}</h4>
                                  </div>
                                  <div className="process-meta">
                                    <div className="meta-item">
                                      <i className="fas fa-book" />
                                      <span className="meta-label">Marco normativo</span>
                                      <span className="meta-value">{proceso.marcoNormativo || '—'}</span>
                                    </div>
                                    <div className="meta-item">
                                      <i className="fas fa-traffic-light" />
                                      <span className="meta-label">Implementable</span>
                                      <span className={`badge ${fmtImplementable(proceso.estadoImplementacion)==='No' ? 'badge-danger' : 'badge-success'}`}>
                                        {fmtImplementable(proceso.estadoImplementacion)}
                                      </span>
                                    </div>
                                    <div className="meta-item">
                                      <i className="fas fa-flag-checkered" />
                                      <span className="meta-label">Fecha de culminación</span>
                                      <span className="meta-value">{fmtDateTime(proceso.fechaConclusionImplementacion)}</span>
                                    </div>
                                    <div className="meta-item">
                                      <i className="fas fa-sort-amount-up" />
                                      <span className="meta-label">Prioridad</span>
                                      <span className={`chip ${
                                        proceso.prioridadImplementacion===1||proceso.prioridadImplementacion==='1' ? 'chip-high'
                                        : proceso.prioridadImplementacion===2||proceso.prioridadImplementacion==='2' ? 'chip-medium'
                                        : proceso.prioridadImplementacion===3||proceso.prioridadImplementacion==='3' ? 'chip-low'
                                        : 'chip-none'
                                      }`}>
                                        {fmtPrioridad(proceso.prioridadImplementacion)}
                                      </span>
                                    </div>
                                    <div className="process-progress">
                                      <div className="progress-track">
                                        <div className="progress-fill" style={{ width:`${Number(proceso.porcentajeAvance||0)}%` }} />
                                      </div>
                                      <span className="progress-label">{Number(proceso.porcentajeAvance||0).toFixed(2)}%</span>
                                    </div>
                                  </div>
                                </div>
                              </div>
                              {isAdmin && (
                                <div className="tree-actions">
                                  <button className="tree-action-btn edit-btn" title="Editar Proceso"
                                    onClick={(e) => { e.stopPropagation(); navigate(`/editar-proceso/${proceso.idProceso}`); }}>
                                    <i className="fas fa-edit"></i>
                                  </button>
                                </div>
                              )}
                            </div>

                            {expandedProcesses[proceso.idProceso] && (
                              <div className="tree-children">
                                {proceso.subdominios?.map((subdominio) => (
                                  <div key={subdominio.idSubdominio} className="tree-subdomain">
                                    <div className="tree-item subdomain-item">
                                      <div className="tree-content" onClick={() => toggleSub(subdominio.idSubdominio)}>
                                        <div className="tree-toggle">
                                          <i className={`fas ${expandedSubs[subdominio.idSubdominio] ? 'fa-chevron-down' : 'fa-chevron-right'}`}></i>
                                        </div>
                                        <div className="tree-icon tree-icon--subdominio">
                                          <IconSubdominio size={14} />
                                        </div>
                                        <div className="tree-label">
                                          <span className="tree-name">{subdominio.practicasGobierno}</span>
                                          <span className="tree-count">{subdominio.actividades?.length ?? 0} actividades</span>
                                        </div>
                                      </div>
                                      {isAdmin && (
                                        <div className="tree-actions">
                                          <button className="tree-action-btn create-btn" title="Crear Actividad"
                                            onClick={(e) => { e.stopPropagation(); if (!expandedSubs[subdominio.idSubdominio]) toggleSub(subdominio.idSubdominio); startCreating('actividad', subdominio.idSubdominio); }}>
                                            <i className="fas fa-plus"></i>
                                          </button>
                                        </div>
                                      )}
                                    </div>

                                    {creatingMode === 'actividad' && creatingParentId === subdominio.idSubdominio && (
                                      <ActividadFormulario
                                        subdominioId={subdominio.idSubdominio}
                                        onCancel={cancelCreating}
                                        onSuccess={() => { cancelCreating(); cargarArbol(true); }}
                                      />
                                    )}

                                    {expandedSubs[subdominio.idSubdominio] && (
                                      <div className="tree-children">
                                        {subdominio.actividades?.map((actividad) => (
                                          <div key={actividad.idActividad} className="tree-activity">
                                            <div className="tree-item activity-item tree-item--clickable"
                                              onClick={() => navigate(`/subdominios/${subdominio.idSubdominio}/actividades/${actividad.idActividad}/editar`, { state: { from: 'dashboard' } })}>
                                              <div className="tree-content">
                                                <div className="tree-icon tree-icon--actividad">
                                                  <IconActividad size={13} />
                                                </div>
                                                <div className="tree-label">
                                                  <span className="tree-name">{actividad.nombre}</span>
                                                  <span className="tree-status" data-status={actividad.estadoImplementacion}>
                                                    {actividad.estadoImplementacion}
                                                  </span>
                                                </div>
                                              </div>
                                              <div className="tree-actions"></div>
                                            </div>
                                          </div>
                                        ))}
                                      </div>
                                    )}
                                  </div>
                                ))}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;