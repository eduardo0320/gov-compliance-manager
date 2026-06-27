import React, { useEffect, useState, useRef } from "react";
import jsPDF from "jspdf";
import html2canvas from "html2canvas";
import { exportarExcelGantt } from "../utils/excelExport";
import { useNavigate, Link } from "react-router-dom";
import { getMisActividades, getDominios, getActividadesPorDominio } from "../services";
import { DominioIconBox, getDomainColor } from "../components/ui/TreeIcons";


const GanttAdminView = ({ userRole: propUserRole }) => {
  // Fecha de hoy para filtros y semana actual
  const today = new Date(); today.setHours(0,0,0,0);
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actividades, setActividades] = useState([]);
  const [agrupadoPorDominio, setAgrupadoPorDominio] = useState(true);
  const [filtros, setFiltros] = useState({ dominio: "", estado: "" });
  const [userRole, setUserRole] = useState(propUserRole);
  const [dominios, setDominios] = useState([]);
  // Estado para navegación de semanas
  const [weekOffset, setWeekOffset] = useState(0);

  // Si cambia el prop, sincroniza el estado
  useEffect(() => {
    setUserRole(propUserRole);
  }, [propUserRole]);


  // Utilidad para convertir snake_case a camelCase
  function toCamelCase(str) {
    return str.replace(/_([a-z])/g, (_, c) => c.toUpperCase());
  }

  // Convierte recursivamente las claves de un objeto o array
  function keysToCamel(obj) {
    if (Array.isArray(obj)) {
      return obj.map(keysToCamel);
    } else if (obj && typeof obj === 'object') {
      return Object.fromEntries(
        Object.entries(obj).map(([k, v]) => [toCamelCase(k), keysToCamel(v)])
      );
    }
    return obj;
  }

  useEffect(() => {
    setLoading(true);
    setError("");
    getDominios().then(setDominios).catch(() => {});
    if (userRole === "ADMIN" || userRole === "SUPERADMIN") {
      getActividadesPorDominio()
        .then(data => {
          // data: [{ dominio, pendientes, vencidas, completadas }]
          // Unificar pendientes y vencidas para la tabla principal
          const grupos = (data || []).map(grupo => {
            let acts = [
              ...(grupo.pendientes || []),
              ...(grupo.vencidas || []),
              ...(grupo.completadas || [])
            ];
            return { ...grupo, actividades: acts };
          });
          setActividades(keysToCamel(grupos));
        })
        .catch(() => setError("Error cargando actividades"))
        .finally(() => setLoading(false));
    } else if (userRole === "EDITOR" || userRole === "EDITOR_DOMINIO") {
      getMisActividades()
        .then(data => {
          const acts = [
            ...(data.pendientes || []),
            ...(data.completadas || [])
          ];
          setActividades([{ dominio: "Mis actividades", actividades: keysToCamel(acts) }]);
        })
        .catch(() => setError("Error cargando actividades"))
        .finally(() => setLoading(false));
    } else {
      setError("No autorizado para ver esta página");
      setLoading(false);
    }
  }, [userRole]);

  // Mapeo de siglas a nombre completo COBIT
  const dominioNombresCOBIT = {
    'EDM': 'EDM - Evaluar, Dirigir y Monitorear',
    'APO': 'APO - Alinear, Planificar y Organizar',
    'BAI': 'BAI - Construir, Adquirir e Implementar',
    'DSS': 'DSS - Entregar, Dar Servicio y Soporte',
    'MEA': 'MEA - Monitorear, Evaluar y Valorar',
  };
  // Ya no se agrupa en frontend, el backend devuelve [{ dominio, actividades }]

  // Filtros y agrupación
  let grupos = [];
  if (agrupadoPorDominio) {
    grupos = (actividades || []).map(grupo => {
      let acts = grupo.actividades || [];
      if (filtros.estado === 'Pendiente') acts = acts.filter(act => act.estadoImplementacion === 'Pendiente');
      else if (filtros.estado === 'Completada') acts = acts.filter(act => act.estadoImplementacion === 'Implementado');
      else if (filtros.estado === 'Vencida') acts = acts.filter(act => {
        if (!act.fechaCompromiso) return false;
        const fecha = new Date(act.fechaCompromiso);
        return fecha < today && act.estadoImplementacion !== 'Implementado';
      });
      if (filtros.dominio && grupo.dominio?.nombre !== filtros.dominio && grupo.dominio !== filtros.dominio) return null;
      return { ...grupo, actividades: acts };
    }).filter(Boolean);
  } else {
    // Agrupación plana: todas las actividades en un solo grupo
    let acts = (actividades || []).flatMap(grupo => grupo.actividades || []);
    if (filtros.estado === 'Pendiente') acts = acts.filter(act => act.estadoImplementacion === 'Pendiente');
    else if (filtros.estado === 'Completada') acts = acts.filter(act => act.estadoImplementacion === 'Implementado');
    else if (filtros.estado === 'Vencida') acts = acts.filter(act => {
      if (!act.fechaCompromiso) return false;
      const fecha = new Date(act.fechaCompromiso);
      return fecha < today && act.estadoImplementacion !== 'Implementado';
    });
    if (filtros.dominio) acts = acts.filter(act => (act.subdominio?.dominio?.nombre || act.dominio?.nombre || act.dominio) === filtros.dominio);
    grupos = [{ dominio: "Todas las actividades", actividades: acts }];
  }

  // Helpers para semanas
  function getMonday(date) {
    const d = new Date(date);
    const day = d.getDay();
    const diff = (day === 0 ? -6 : 1 - day);
    d.setDate(d.getDate() + diff);
    d.setHours(0,0,0,0);
    return d;
  }
  // Semanas para el Gantt (9 semanas: 3 antes, actual, 5 después, desplazables)
  function getWeeks(num = 9, offset = 0) {
    const now = new Date();
    now.setHours(0,0,0,0);
    const monday = getMonday(now);
    monday.setDate(monday.getDate() - 21 + offset * 7); // 3 semanas antes + offset
    return Array.from({length: num}, (_, i) => {
      const d = new Date(monday);
      d.setDate(monday.getDate() + i * 7);
      return d;
    });
  }
  const weeks = getWeeks(9, weekOffset);
  // Etiquetas con rango de días de la semana
  const weekLabels = weeks.map(w => {
    const start = new Date(w);
    const end = new Date(w);
    end.setDate(end.getDate() + 6);
    return {
      label: `${start.toLocaleDateString('es-CR', { day: '2-digit', month: '2-digit' })} - ${end.toLocaleDateString('es-CR', { day: '2-digit', month: '2-digit' })}`,
      start,
      end
    };
  });
  // Índice de la semana actual
  const currentWeekIdx = weekLabels.findIndex(w => today >= w.start && today <= w.end);
  // Rango de fechas real de las actividades visibles
  const allVisibleActs = grupos.flatMap(g => g.actividades).filter(a => a && a.fechaCompromiso);
  let minFecha = null, maxFecha = null;
  if (allVisibleActs.length > 0) {
    minFecha = new Date(Math.min(...allVisibleActs.map(a => new Date(a.fechaCompromiso).getTime())));
    maxFecha = new Date(Math.max(...allVisibleActs.map(a => new Date(a.fechaCompromiso).getTime())));
  }
  const rangoFechasLabel = (minFecha && maxFecha)
    ? `${minFecha.toLocaleDateString('es-CR', { day: '2-digit', month: '2-digit', year: 'numeric' })} – ${maxFecha.toLocaleDateString('es-CR', { day: '2-digit', month: '2-digit', year: 'numeric' })}`
    : 'Sin actividades';

  // Filtrar actividades visibles SOLO dentro del rango de semanas
  const periodoStart = weekLabels[0].start;
  const periodoEnd = weekLabels[weekLabels.length-1].end;
  function isInPeriodo(act) {
    if (!act.fechaCompromiso) return false;
    const fecha = new Date(act.fechaCompromiso);
    return fecha >= periodoStart && fecha <= periodoEnd;
  }
  // Separar actividades visibles y vencidas fuera del periodo
  const allActs = grupos.flatMap(g => g.actividades);
  const visibles = allActs.filter(isInPeriodo);
  const vencidasFuera = allActs.filter(a => {
    if (!a.fechaCompromiso) return false;
    const fecha = new Date(a.fechaCompromiso);
    return fecha < periodoStart && a.estadoImplementacion !== 'Implementado';
  });
  const total = visibles.length;
  const completadas = visibles.filter(a => a.estadoImplementacion === "Implementado").length;
  const pendientes = visibles.filter(a => a.estadoImplementacion === "Pendiente").length;
  const vencidas = visibles.filter(a => {
    if (!a.fechaCompromiso) return false;
    const fecha = new Date(a.fechaCompromiso);
    return fecha < today && a.estadoImplementacion !== 'Implementado';
  }).length;

  const [showExportMenu, setShowExportMenu] = useState(false);
  const exportBtnRef = useRef();
  async function handleExport(format) {
    setShowExportMenu(false);
    const tableNode = document.querySelector(".gantt-admin-table");
    const fechaStr = new Date().toISOString().slice(0,10);
    if (!tableNode && format !== "excel") {
      alert("No se encontró la tabla para exportar.");
      return;
    }
    if (format === "pdf") {
      // Exportar tabla a PDF usando html2canvas + jsPDF
      const canvas = await html2canvas(tableNode, { scale: 2 });
      const imgData = canvas.toDataURL("image/png");
      const pdf = new jsPDF({ orientation: "landscape", unit: "pt", format: "a4" });
      const pageWidth = pdf.internal.pageSize.getWidth();
      const pageHeight = pdf.internal.pageSize.getHeight();
      const imgProps = canvas;
      const ratio = Math.min(pageWidth / canvas.width, pageHeight / canvas.height);
      const imgWidth = canvas.width * ratio;
      const imgHeight = canvas.height * ratio;
      pdf.addImage(imgData, "PNG", (pageWidth - imgWidth) / 2, 20, imgWidth, imgHeight, undefined, 'FAST');
      pdf.save(`gantt-admin-${fechaStr}.pdf`);
    } else if (format === "excel") {
      // Construir headers y filas para ExcelJS
      const headers = ["Dominio", "Actividad / Responsable", "Estado", "% Avance", "Fecha Compromiso", ...weekLabels.map(w => w.label)];
      const rows = [];
      const semanaActualIdx = weekLabels.findIndex(w => {
        const hoy = new Date(); hoy.setHours(0,0,0,0);
        return hoy >= w.start && hoy <= w.end;
      });
      (grupos || []).forEach(grupo => {
        const dominio = typeof grupo.dominio === 'object' ? grupo.dominio.nombre : grupo.dominio;
        (grupo.actividades || []).forEach(act => {
          let estado = "Pendiente";
          if (act.estadoImplementacion === "Implementado") estado = "Completada";
          else if (act.fechaCompromiso && new Date(act.fechaCompromiso) < today && act.estadoImplementacion !== "Implementado") estado = "Vencida";
          // Responsable: inicial y apellido
          let responsable = act.funcionariosResponsablesNombre || act.funcionarioResponsable || "";
          if (responsable) {
            const partes = responsable.trim().split(/\s+/);
            if (partes.length > 1) responsable = `${partes[0][0]}. ${partes[partes.length-1]}`;
            else responsable = partes[0][0] || "";
          }
          const row = [
            dominio,
            `${act.nombre}${responsable ? ` / ${responsable}` : ""}`,
            estado,
            act.porcentajeAvance + "%",
            act.fechaCompromiso ? new Date(act.fechaCompromiso).toLocaleDateString('es-CR') : ""
          ];
          weekLabels.forEach(w => {
            let valor = "";
            if (act.fechaCompromiso) {
              const fecha = new Date(act.fechaCompromiso);
              if (fecha >= w.start && fecha <= w.end) valor = (act.porcentajeAvance || "") + "%";
            }
            row.push(valor);
          });
          rows.push({ row, estado });
        });
      });
      await exportarExcelGantt({
        headers,
        rows,
        semanaActualIdx,
        sheetName: "Gantt",
        fileName: `gantt-admin-${fechaStr}`,
      });
    } else if (format === "img") {
      // Exportar tabla a imagen PNG usando html2canvas
      const canvas = await html2canvas(tableNode, { scale: 2 });
      const link = document.createElement("a");
      link.download = `gantt-admin-${fechaStr}.png`;
      link.href = canvas.toDataURL("image/png");
      link.click();
    } else {
      alert("Formato no soportado.");
    }
  }
  // Cerrar menú si se hace click fuera
  useEffect(() => {
    function handleClick(e) {
      if (showExportMenu && exportBtnRef.current && !exportBtnRef.current.contains(e.target)) {
        setShowExportMenu(false);
      }
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, [showExportMenu]);

  const [showVencidasModal, setShowVencidasModal] = useState(false);
  function handleGoActividad(act) {
    const subdominioId = act.subdominio?.id;
    const actividadId = act.id;
    if (subdominioId && actividadId) {
      navigate(`/subdominios/${subdominioId}/actividades/${actividadId}/editar`, { state: { from: 'ganttAdmin' } });
      setShowVencidasModal(false);
    }
  }

  if (loading) return <div className="gantt-loading"><i className="fas fa-spinner fa-spin"></i> Cargando actividades...</div>;
  if (error) return <div className="gantt-error"><i className="fas fa-exclamation-triangle" className="gantt-icon-mr"></i>{error}</div>;

  return (
    <div className="gantt-page">

      {/* Alerta: vencidas fuera del periodo */}
      {vencidasFuera.length > 0 && (
        <div className="gantt-alert-bar">
          <span><i className="fas fa-exclamation-circle" className="gantt-icon-mr"></i>Hay {vencidasFuera.length} actividades vencidas fuera del periodo visible.</span>
          <button className="gantt-alert-bar-btn" onClick={()=>setShowVencidasModal(true)}>Ver actividades vencidas</button>
        </div>
      )}

      {/* Modal: actividades vencidas */}
      {showVencidasModal && (
        <div className="gantt-modal-overlay">
          <div className="gantt-modal-content">
            <div className="gantt-modal-header">
              <div className="gantt-modal-header-icon">
                <i className="fas fa-exclamation-triangle"></i>
              </div>
              <div className="gantt-modal-title">
                <h5>Actividades vencidas</h5>
                <p>Fuera del periodo visible en el diagrama</p>
              </div>
              <button className="gantt-modal-close" onClick={()=>setShowVencidasModal(false)} aria-label="Cerrar">
                <i className="fas fa-times"></i>
              </button>
            </div>
            <div className="gantt-modal-divider" />
            <div className="gantt-modal-body">
              {vencidasFuera.map(act=>(
                <div key={act.id} className="gantt-overdue-item">
                  <div>
                    <div className="gantt-overdue-nombre">{act.nombre}</div>
                    <div className="gantt-overdue-fecha"><i className="fas fa-calendar-times" className="gantt-icon-mr-sm"></i>Fecha compromiso: {act.fechaCompromiso ? new Date(act.fechaCompromiso).toLocaleDateString('es-CR') : 'N/D'}</div>
                  </div>
                  <button className="gantt-overdue-ir-btn" onClick={()=>handleGoActividad(act)}><i className="fas fa-arrow-right"></i> Ir</button>
                </div>
              ))}
            </div>
            <div className="gantt-modal-footer">
              <button className="gantt-btn gantt-btn--outline" onClick={()=>setShowVencidasModal(false)}>Cerrar</button>
            </div>
          </div>
        </div>
      )}

      {/* Top bar: breadcrumb + título + nav */}
      <div className="gantt-top-bar">
        <div className="gantt-breadcrumb">
          <Link to="/">Inicio</Link><span className="sep">/</span>
          <span>Gestión de Proyectos</span><span className="sep">/</span>
          <span className="cur">Diagrama de Gantt</span>
        </div>
        <div className="gantt-title-row">
          <h1>Diagrama de Gantt General</h1>
        </div>
        <div className="gantt-nav-row">
          <button className="gantt-btn gantt-btn--outline" onClick={()=>setWeekOffset(w=>w-1)}>
            <i className="fas fa-arrow-left"></i> Semana anterior
          </button>
          <button className="gantt-btn gantt-btn--outline" onClick={()=>setWeekOffset(0)}>
            <i className="fa-regular fa-calendar-check"></i> Semana actual
          </button>
          <button className="gantt-btn gantt-btn--outline" onClick={()=>setWeekOffset(w=>w+1)}>
            Semana siguiente <i className="fas fa-arrow-right"></i>
          </button>
          <div className="gantt-spacer"></div>
          <div className="gantt-export-wrap" ref={exportBtnRef}>
            <button className="gantt-btn gantt-btn--secondary" onClick={()=>setShowExportMenu(v=>!v)}>
              <i className="fas fa-download"></i> Exportar <i className="fas fa-chevron-down" className="gantt-export-chevron"></i>
            </button>
            {showExportMenu && (
              <div className="gantt-export-menu">
                <button className="gantt-export-item" onClick={()=>handleExport("pdf")}><i className="fas fa-file-pdf"></i> Exportar PDF</button>
                <button className="gantt-export-item" onClick={()=>handleExport("excel")}><i className="fas fa-file-excel"></i> Exportar Excel</button>
                <button className="gantt-export-item" onClick={()=>handleExport("img")}><i className="fas fa-image"></i> Exportar Imagen</button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Stats bar */}
      <div className="gantt-stats-bar">
        <div className="gantt-stat-card gantt-stat-card--total">
          <div className="gantt-stat-value">{total}</div>
          <div className="gantt-stat-label">Total actividades</div>
        </div>
        <div className="gantt-stat-card gantt-stat-card--done">
          <div className="gantt-stat-value">{completadas}</div>
          <div className="gantt-stat-label">Completadas</div>
        </div>
        <div className="gantt-stat-card gantt-stat-card--pending">
          <div className="gantt-stat-value">{pendientes}</div>
          <div className="gantt-stat-label">Pendientes</div>
        </div>
        <div className="gantt-stat-card gantt-stat-card--overdue">
          <div className="gantt-stat-value">{vencidas}</div>
          <div className="gantt-stat-label">Vencidas</div>
        </div>
      </div>

      {/* Contenido principal */}
      <div className="gantt-content">
        <div className="gantt-filter-bar">
          <div className="gantt-legend-item"><div className="gantt-legend-dot gantt-legend-dot--pending"></div> Pendiente</div>
          <div className="gantt-legend-item"><div className="gantt-legend-dot gantt-legend-dot--done"></div> Completada</div>
          <div className="gantt-legend-item"><div className="gantt-legend-dot gantt-legend-dot--overdue"></div> Vencida</div>
          <div className="gantt-legend-sep"></div>
          <select className="gantt-filter-select" value={filtros.estado} onChange={e=>setFiltros(f=>({...f,estado:e.target.value}))}>
            <option value="">Todos los estados</option>
            <option value="Pendiente">Pendientes</option>
            <option value="Completada">Completadas</option>
            <option value="Vencida">Vencidas</option>
          </select>
          <select className="gantt-filter-select" value={filtros.dominio} onChange={e=>setFiltros(f=>({...f,dominio:e.target.value}))}>
            <option value="">Todos los dominios</option>
            {dominios.map(d=>(<option key={d.id||d.nombre} value={d.nombre}>{d.nombre}</option>))}
          </select>
          <div className="gantt-spacer"></div>
          <label className="gantt-group-toggle">
            <input type="checkbox" checked={agrupadoPorDominio} onChange={e=>setAgrupadoPorDominio(e.target.checked)} />
            Agrupar por dominio
          </label>
          <span className="gantt-range-label">Rango: {rangoFechasLabel}</span>
        </div>

        <div className="gantt-admin-wrapper">
          <div className="gantt-admin-scroll">
            <table className="gantt-admin-table">
              <thead>
                <tr>
                  <th className="task-col">Actividad / Responsable</th>
                  {weekLabels.map((w,i)=>(
                    <th key={i} className={"week-col" + (i===currentWeekIdx ? " week-col--current" : "")}>
                      {w.label}
                      {i===currentWeekIdx && <span className="gantt-week-current-badge">ACTUAL</span>}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {grupos.map(grupo => {
                  const domNombre = typeof grupo.dominio === 'object' ? grupo.dominio.nombre : grupo.dominio;
                  return (
                  <React.Fragment key={typeof grupo.dominio === 'object' ? grupo.dominio.id || JSON.stringify(grupo.dominio) : grupo.dominio}>
                    <tr className="group-row">
                      <td colSpan={1+weeks.length} className="group-label">
                        <DominioIconBox nombre={domNombre} size={22} />
                        <span className="gantt-group-label-text">{domNombre}</span>
                      </td>
                    </tr>
                    {grupo.actividades.filter(isInPeriodo).map(act => {
                      const subdominioId = act.subdominio?.id;
                      const actividadId = act.id;
                      let ruta = undefined;
                      if (subdominioId && actividadId) {
                        ruta = `/subdominios/${subdominioId}/actividades/${actividadId}/editar`;
                      }
                      // Determinar si la actividad está vencida
                      const fecha = act.fechaCompromiso ? new Date(act.fechaCompromiso) : null;
                      const esVencida = fecha && fecha < today && act.estadoImplementacion !== 'Implementado';
                      return (
                        <tr
                          key={actividadId}
                          className={
                            'gantt-admin-task-row' + (esVencida ? ' gantt-admin-task-row-vencida' : '')
                          }
                          onClick={ruta ? () => navigate(ruta, { state: { from: 'ganttAdmin' } }) : undefined}
                          style={ruta ? { cursor: 'pointer', background: '#f8fafd' } : {}}
                          title={`Actividad: ${act.nombre}\nResponsable: ${act.funcionariosResponsablesNombre || act.funcionarioResponsable || 'N/D'}\nEstado: ${act.estadoImplementacion}\nFecha compromiso: ${act.fechaCompromiso ? new Date(act.fechaCompromiso).toLocaleDateString('es-CR') : 'N/D'}`}
                          onMouseOver={ruta ? e => {
                            e.currentTarget.style.background = '#eaf3fa';
                            Array.from(e.currentTarget.children).forEach((td, idx) => {
                              if (weekLabels[idx-1] && idx > 0 && (idx-1) === currentWeekIdx) {
                                td.style.background = '#ffe6e6';
                              }
                            });
                          } : undefined}
                          onMouseOut={ruta ? e => {
                            e.currentTarget.style.background = '#f8fafd';
                            Array.from(e.currentTarget.children).forEach((td, idx) => {
                              if (weekLabels[idx-1] && idx > 0 && (idx-1) === currentWeekIdx) {
                                td.style.background = '#fff5f4';
                              } else if (idx > 0) {
                                td.style.background = '';
                              }
                            });
                          } : undefined}
                        >
                          <td className="task-col">
                            <div className="gantt-admin-task-row-content">
                              <div className="gantt-admin-task-name-line">
                                <span className="gantt-admin-task-name-text">{act.nombre}</span>
                              </div>
                              <div className="gantt-admin-task-meta">
                                {/* Responsable: inicial y apellido, de primero */}
                                {(() => {
                                  let responsable = act.funcionariosResponsablesNombre || act.funcionarioResponsable || "";
                                  if (responsable) {
                                    const partes = responsable.trim().split(/\s+/);
                                    if (partes.length > 1) responsable = `${partes[0][0]}. ${partes[partes.length-1]}`;
                                    else responsable = partes[0][0] || "";
                                  } else {
                                    responsable = "";
                                  }
                                  return responsable ? <span className="gantt-task-meta-responsable" title={act.funcionariosResponsablesNombre || act.funcionarioResponsable}>{responsable}</span> : null;
                                })()}
                                {/* Estado con color solo en el texto */}
                                {(() => {
                                  let estado = esVencida ? 'Vencida' : (act.estadoImplementacion === 'Implementado' ? 'Completada' : 'Pendiente');
                                  let color = '#f59f00';
                                  if (estado === 'Completada') color = '#28a745';
                                  else if (estado === 'Vencida') color = '#ec483b';
                                  const estadoClass = estado === 'Completada' ? 'gantt-task-meta-estado gantt-task-meta-estado--done' : estado === 'Vencida' ? 'gantt-task-meta-estado gantt-task-meta-estado--overdue' : 'gantt-task-meta-estado gantt-task-meta-estado--pending';
                                  return <span className={estadoClass}>{estado}</span>;
                                })()}
                                {act.fechaCompromiso && <span>{new Date(act.fechaCompromiso).toLocaleDateString('es-CR')}</span>}
                              </div>
                            </div>
                          </td>
                          {weekLabels.map((w,i)=>(
                            <td
                              key={i}
                              style={{background: i===currentWeekIdx ? '#fff5f4' : undefined}}
                            >
                              {(() => {
                                if (!act.fechaCompromiso) return null;
                                const fecha = new Date(act.fechaCompromiso);
                                const weekStart = w.start;
                                const weekEnd = w.end;
                                if (fecha >= weekStart && fecha <= weekEnd) {
                                  let barClass = 'gantt-admin-bar gantt-admin-bar-pendiente';
                                  if (esVencida) barClass = 'gantt-admin-bar gantt-admin-bar-vencida';
                                  else if (act.estadoImplementacion === 'Implementado') barClass = 'gantt-admin-bar gantt-admin-bar-completada';
                                  return <div className={barClass} title={`% Avance: ${act.porcentajeAvance}\nEstado: ${esVencida ? 'Vencida' : (act.estadoImplementacion === 'Implementado' ? 'Completada' : 'Pendiente')}`}>{act.porcentajeAvance}%</div>;
                                }
                                return null;
                              })()}
                            </td>
                          ))}
                        </tr>
                      );
                    })}
                  </React.Fragment>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

export default GanttAdminView;