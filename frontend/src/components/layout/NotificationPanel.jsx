import React, { useEffect, useRef, useState } from "react";
import { apiFetch } from "../../services/apiClient";
import { useNavigate } from "react-router-dom";
import {
  getAlertasVencimiento,
  getNotificaciones,
  marcarNotificacionLeida,
  marcarTodasNotificacionesLeidas,
  eliminarNotificacion,
  eliminarTodasNotificaciones,
  getUsuariosSinActividades,
  getMisActividades,
} from '../../services';
import { getCurrentUserInfoAsync } from '../../services';
import "../../styles/NotificationPanel.css";


const IconTrash = () => (
  <svg width="13" height="13" viewBox="0 0 20 20" fill="none" stroke="currentColor"
    strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3,6 5,6 17,6" />
    <path d="M8 6V4a1 1 0 011-1h2a1 1 0 011 1v2" />
    <path d="M5 6l1 11a1 1 0 001 1h6a1 1 0 001-1l1-11" />
    <line x1="10" y1="11" x2="10" y2="15" />
    <line x1="8"  y1="11" x2="8.3" y2="15" />
    <line x1="12" y1="11" x2="11.7" y2="15" />
  </svg>
);

const NotificationPanel = ({ show, onClose, onNavigate, onNotificationsUpdated }) => {
  const panelRef  = useRef(null);
  const [alertas,              setAlertas]              = useState(null);
  const [notifs,               setNotifs]               = useState([]);
  const [usuariosSinActs,      setUsuariosSinActs]      = useState([]);
  const [sinActividadesPropias,setSinActividadesPropias]= useState(false);
  const [loading,              setLoading]              = useState(false);
  const [marcando,             setMarcando]             = useState(false);
  const [eliminando,           setEliminando]           = useState(null);
  const [eliminandoTodas,      setEliminandoTodas]      = useState(false);
  const [listaExpandida,       setListaExpandida]       = useState(false);
  const [isAdmin,              setIsAdmin]              = useState(false);

  /* ── Carga de datos ── */
  useEffect(() => {
    if (!show) return;
    setLoading(true);
    setListaExpandida(false);

    const cargar = async () => {
      const userInfo   = await getCurrentUserInfoAsync();
      const adminCheck = !!(userInfo && (userInfo.rol === "ADMIN" || userInfo.rol === "SUPERADMIN"));
      setIsAdmin(adminCheck);

      const promises = [
        getAlertasVencimiento(7).catch(() => null),
        getNotificaciones(false).catch(() => ({ notificaciones: [], noLeidas: 0 })),
        getMisActividades().catch(() => null),
      ];
      if (adminCheck) {
        promises.push(getUsuariosSinActividades().catch(() => ({ usuarios: [] })));
      }

      const [alertasData, notifsData, misActs, sinActsData] = await Promise.all(promises);
      setAlertas(alertasData);

      const rawNotifs = notifsData?.notificaciones ?? [];
      const seen = new Set();
      const notifsSinDup = rawNotifs.filter((n) => {
        if (seen.has(n.id)) return false;
        seen.add(n.id);
        return true;
      });
      setNotifs(notifsSinDup);

      if (misActs) {
        const tienePendientes  = (misActs.pendientes  ?? []).length > 0;
        const tieneCompletadas = (misActs.completadas ?? []).length > 0;
        const tieneNotifAsign  = notifsSinDup.some(
          (n) => !n.leida && (n.tipo === "asignacion" || n.titulo?.toLowerCase().includes("asign"))
        );
        setSinActividadesPropias(!tienePendientes && !tieneCompletadas && !tieneNotifAsign);
      }

      if (sinActsData) {
        const lista = sinActsData.usuarios ?? [];
        setUsuariosSinActs(lista);
        if (adminCheck && lista.length > 0) {
          apiFetch(`/api/notificaciones/notificar-editores-sin-actividades`, {
            method: "POST", credentials: "include",
          }).catch(() => {});
        }
      }
    };

    cargar().finally(() => setLoading(false));
  }, [show]);

  /* ── Click fuera cierra el panel ── */
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (panelRef.current && !panelRef.current.contains(e.target)) onClose();
    };
    if (show) document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [show, onClose]);

  /* ── Handlers ── */
  const handleNavigation    = (path) => { if (onNavigate) onNavigate(path); onClose(); };
  const handleMarcarLeida   = async (id) => {
    try {
      await marcarNotificacionLeida(id);
      setNotifs((prev) => prev.map((n) => (n.id === id ? { ...n, leida: true } : n)));
      onNotificationsUpdated?.();
    } catch (e) { console.error(e); }
  };
  const handleMarcarTodas   = async () => {
    setMarcando(true);
    try {
      await marcarTodasNotificacionesLeidas();
      setNotifs((prev) => prev.map((n) => ({ ...n, leida: true })));
      onNotificationsUpdated?.();
    } catch (e) { console.error(e); }
    finally { setMarcando(false); }
  };
  const handleEliminar      = async (e, id) => {
    e.stopPropagation();
    setEliminando(id);
    try {
      await eliminarNotificacion(id);
      setNotifs((prev) => prev.filter((n) => n.id !== id));
      onNotificationsUpdated?.();
    } catch (e) { console.error(e); }
    finally { setEliminando(null); }
  };
  const handleEliminarTodas = async () => {
    setEliminandoTodas(true);
    try {
      await eliminarTodasNotificaciones();
      setNotifs([]);
      onNotificationsUpdated?.();
    } catch (e) { console.error(e); }
    finally { setEliminandoTodas(false); }
  };

  /* ── Datos derivados ── */
  const noLeidasCount = notifs.filter((n) => !n.leida).length;

  const sistemaNotifs = [];
  if (alertas) {
    if (alertas.totalVencidos > 0)
      sistemaNotifs.push({
        id: "vencidos",
        title: `${alertas.totalVencidos} ${alertas.totalVencidos === 1 ? "documento vencido" : "documentos vencidos"}`,
        message: "Haz clic para revisar los documentos vencidos",
        type: "danger",
        action: () => handleNavigation("/expiredDocuments"),
        icon: "fas fa-exclamation-triangle",
      });
    if (alertas.totalProximosAVencer > 0)
      sistemaNotifs.push({
        id: "proximos",
        title: `${alertas.totalProximosAVencer} ${alertas.totalProximosAVencer === 1 ? "documento próximo a vencer" : "documentos próximos a vencer"}`,
        message: "Vencen en los próximos 7 días",
        type: "warning",
        action: () => handleNavigation("/expiredDocuments"),
        icon: "fas fa-clock",
      });
  }
  if (isAdmin && usuariosSinActs.length > 0) sistemaNotifs.push({ id: "sin-actividades-admin" });
  if (sinActividadesPropias)
    sistemaNotifs.push({
      id: "sin-actividades-propias",
      title: "No tenés actividades asignadas",
      message: "Actualmente no se te ha asignado ninguna actividad pendiente.",
      type: "info",
      icon: "fas fa-inbox",
    });

  const tieneAlgo = sistemaNotifs.length > 0 || notifs.length > 0;

  /* ── Mapa de colores por tipo ── */
  const typeClass = {
    danger:  "notif-item--danger",
    warning: "notif-item--warning",
    info:    "notif-item--info",
    success: "notif-item--success",
  };

  /* ── Sub-componente: editores sin actividades ── */
  const EditoresSinActsCard = () => {
    const MAX_VISIBLE  = 4;
    const total        = usuariosSinActs.length;
    const necesitaMore = total > MAX_VISIBLE;
    const visibles     = listaExpandida ? usuariosSinActs : usuariosSinActs.slice(0, MAX_VISIBLE);

    return (
      <div className="notif-editors-card">
        <div className="notif-editors-card-header"
          onClick={() => handleNavigation("/users?sinActividades=true")}>
          <i className="fas fa-user-slash notif-editors-card-icon"></i>
          <div>
            <div className="notif-editors-card-title">
              {total === 1 ? "1 usuario editor sin actividades asignadas"
                           : `${total} usuarios editores sin actividades asignadas`}
            </div>
            <div className="notif-editors-card-sub">
              {total === 1 ? "Hay un editor sin ninguna actividad asignada."
                           : `Hay ${total} editores sin ninguna actividad asignada.`}
            </div>
          </div>
        </div>

        <div className="notif-editors-divider">
          {visibles.map((u) => (
            <div key={u.id} className="notif-editors-user">
              <div className="notif-editors-avatar">
                <i className="fas fa-user"></i>
              </div>
              <span className="notif-editors-name">{u.nombre}</span>
              {u.departamento && (
                <span className="notif-editors-dept">{u.departamento}</span>
              )}
            </div>
          ))}

          {necesitaMore && (
            <button className="notif-editors-toggle"
              onClick={(e) => { e.stopPropagation(); setListaExpandida((v) => !v); }}>
              <i className={`fas fa-chevron-${listaExpandida ? "up" : "down"}`}></i>
              {listaExpandida ? "Ver menos" : `Ver ${total - MAX_VISIBLE} más`}
            </button>
          )}
        </div>

        <div className="notif-editors-footer">
          <button className="notif-editors-link"
            onClick={() => handleNavigation("/users?sinActividades=true")}>
            Ir a gestión de usuarios
          </button>
        </div>
      </div>
    );
  };

  /* ── Render ── */
  return (
    <div ref={panelRef} className={`notification-panel ${show ? "show" : ""}`}>

      {/* Header */}
      <div className="notification-header">
        <span className="notif-title">
          <i className="fas fa-bell"></i>
          Notificaciones
        </span>
        <div className="notif-header-btns">
          {noLeidasCount > 0 && (
            <button className="header-btn mark" onClick={handleMarcarTodas} disabled={marcando}>
              {marcando ? "Marcando..." : "Marcar como leídas"}
            </button>
          )}
          {notifs.length > 0 && (
            <button className="header-btn delete" onClick={handleEliminarTodas} disabled={eliminandoTodas}>
              {eliminandoTodas ? "..." : "Borrar todo"}
            </button>
          )}
        </div>
      </div>

      {/* Lista */}
      <div className="notification-list">

        {loading ? (
          <div className="notif-loading">
            <i className="fas fa-spinner fa-spin"></i> Cargando...
          </div>

        ) : !tieneAlgo ? (
          <div className="notif-item notif-item--info notif-item--static">
            <div className="notif-item-inner">
              <i className="fas fa-check-circle notif-item-icon"></i>
              <div className="notif-item-text">
                <div className="notif-item-title">Sin notificaciones</div>
                <div className="notif-item-msg">No hay alertas en este momento</div>
              </div>
            </div>
          </div>

        ) : (
          <>
            {/* Mis notificaciones */}
            {notifs.length > 0 && (
              <>
                <div className="notif-section-label">Mis notificaciones</div>
                {notifs.map((n) => {
                  const estaEliminando = eliminando === n.id;
                  const cls = [
                    "notif-item",
                    typeClass[n.tipo] ?? "notif-item--info",
                    n.leida          ? "notif-item--read"     : "",
                    estaEliminando   ? "notif-item--deleting" : "",
                    n.urlDestino     ? "notif-item--clickable": "notif-item--static",
                  ].filter(Boolean).join(" ");

                  return (
                    <div key={n.id} className={cls}
                      onClick={() => {
                        if (!n.leida) handleMarcarLeida(n.id);
                        if (n.urlDestino) handleNavigation(n.urlDestino);
                      }}>

                      {!n.leida && <span className="notif-unread-dot" />}

                      <button className="notif-trash-btn"
                        onClick={(e) => handleEliminar(e, n.id)}
                        disabled={estaEliminando}
                        title="Eliminar notificación">
                        {estaEliminando
                          ? <i className="fas fa-spinner fa-spin" className="notif-spinner-sm"></i>
                          : <IconTrash />}
                      </button>

                      <div className="notif-item-inner">
                        <i className="fas fa-bell notif-item-icon"></i>
                        <div className="notif-item-text">
                          <div className="notif-item-title">{n.titulo}</div>
                          <div className="notif-item-msg">{n.mensaje}</div>
                          <div className="notif-item-date">
                            {new Date(n.fechaCreacion).toLocaleDateString("es-ES", {
                              day: "2-digit", month: "2-digit", year: "numeric",
                              hour: "2-digit", minute: "2-digit",
                            })}
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </>
            )}

            {/* Sistema */}
            {sistemaNotifs.length > 0 && (
              <>
                <div className={`notif-section-label ${notifs.length > 0 ? "notif-section-label--mt" : ""}`}>
                  Sistema
                </div>
                {sistemaNotifs.map((notif) => {
                  if (notif.id === "sin-actividades-admin") {
                    return <EditoresSinActsCard key="sin-actividades-admin" />;
                  }
                  const cls = [
                    "notif-item",
                    typeClass[notif.type] ?? "notif-item--info",
                    notif.action ? "notif-item--clickable" : "notif-item--static",
                  ].join(" ");
                  return (
                    <div key={notif.id} className={cls} onClick={notif.action}>
                      <div className="notif-item-inner">
                        <i className={`${notif.icon} notif-item-icon`}></i>
                        <div className="notif-item-text">
                          <div className="notif-item-title">{notif.title}</div>
                          <div className="notif-item-msg">{notif.message}</div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default NotificationPanel;