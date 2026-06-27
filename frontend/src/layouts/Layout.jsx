import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import BarraSuperior from '../components/layout/BarraSuperior';
import Sidebar from '../components/layout/Sidebar';
import NotificationPanel from '../components/layout/NotificationPanel';
import { apiFetch } from '../services/apiClient';
import { getNotificaciones, getMisActividades, getUsuariosSinActividades, getCurrentUserInfoAsync } from '../services';

const HEADER_H       = 64;
const INACTIVIDAD_MS = 5 * 60 * 1000;   // 5 min → cerrar sesión
const AVISO_MS       = 4 * 60 * 1000;   // 4 min → mostrar aviso
const TICK_MS        = 5 * 1000;         // revisar cada 5 seg (funciona aunque la pestaña esté oculta)
const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:5156";

const Layout = ({ children }) => {
  const navigate = useNavigate();
  const [showNotifications, setShowNotifications]   = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen]         = useState(false);
  const location = useLocation();
  const [mostrarAviso, setMostrarAviso]             = useState(false);
  const [segundosRestantes, setSegundosRestantes]   = useState(60);

  const ultimaActividad      = useRef(Date.now());
  const timerCuentaRegresiva = useRef(null);
  const tickInterval         = useRef(null);
  const avisoActivo          = useRef(false);

  const [noLeidas, setNoLeidas] = useState(0);
  const [isAdmin, setIsAdmin] = useState(false);

  // ── Cerrar sesión ──────────────────────────────────────────────────────
  const cerrarSesion = useCallback(async () => {
    setMostrarAviso(false);
    avisoActivo.current = false;
    clearInterval(tickInterval.current);
    clearInterval(timerCuentaRegresiva.current);
    sessionStorage.removeItem('sesionActiva');
    try {
      await apiFetch('/api/auth/logout', { method: 'POST' });
    } catch (_) {}
    localStorage.removeItem('usuario');
    window.location.href = '/login';
  }, []);

  // ── Cargar número de notificaciones no leídas ─────────────────────────
  const cargarNotificaciones = async () => {
    try {
      const userInfo = await getCurrentUserInfoAsync();
      const adminCheck = !!(userInfo && (userInfo.rol === 'ADMIN' || userInfo.rol === 'SUPERADMIN'));
      setIsAdmin(adminCheck);

      const promises = [
        getNotificaciones(false).catch(() => ({ notificaciones: [], noLeidas: 0 })),
        getMisActividades().catch(() => null),
      ];
      if (adminCheck) {
        promises.push(getUsuariosSinActividades().catch(() => ({ usuarios: [] })));
      }

      const [data, misActs, sinActsData] = await Promise.all(promises);

      const todasNotifs = data.notificaciones ?? [];
      let count = todasNotifs.filter(n => !n.leida).length;

      if (misActs) {
        const tienePendientes = (misActs.pendientes ?? []).length > 0;
        const tieneCompletadas = (misActs.completadas ?? []).length > 0;
        const tieneNotifAsignacion = todasNotifs.some(
          n => !n.leida && (n.tipo === 'asignacion' || (n.titulo && n.titulo.toLowerCase().includes('asign')))
        );
        if (!tienePendientes && !tieneCompletadas && !tieneNotifAsignacion) {
          count += 1;
        }
      }

      if (adminCheck && sinActsData && (sinActsData.usuarios ?? []).length > 0) {
        count += 1;
      }

      setNoLeidas(count);
    } catch (error) {
      console.error('Error cargando notificaciones:', error);
      setNoLeidas(0);
    }
  };

  const handleToggleNotifications = () => {
    setShowNotifications(v => !v);
    if (!showNotifications) {
      cargarNotificaciones();
    }
  };

  const registrarActividad = useCallback(() => {
    ultimaActividad.current = Date.now();
    if (avisoActivo.current) {
      setMostrarAviso(false);
      avisoActivo.current = false;
      clearInterval(timerCuentaRegresiva.current);
    }
  }, []);

  useEffect(() => {
    cargarNotificaciones();

    const interval = setInterval(cargarNotificaciones, 30000);

    return () => clearInterval(interval);
  }, []);

  // ── Tick principal: se ejecuta cada TICK_MS aunque la pestaña esté oculta
  useEffect(() => {
    // Marcar sesión activa en sessionStorage (se borra al cerrar el navegador)
    sessionStorage.setItem('sesionActiva', '1');

    tickInterval.current = setInterval(() => {
      const ahora = Date.now();
      const inactivo = ahora - ultimaActividad.current;

      if (inactivo >= INACTIVIDAD_MS) {
        // Tiempo agotado → cerrar sesión inmediatamente
        cerrarSesion();
        return;
      }

      if (inactivo >= AVISO_MS && !avisoActivo.current) {
        // Mostrar aviso con cuenta regresiva
        avisoActivo.current = true;
        const segsIniciales = Math.round((INACTIVIDAD_MS - inactivo) / 1000);
        setSegundosRestantes(segsIniciales > 0 ? segsIniciales : 1);
        setMostrarAviso(true);

        timerCuentaRegresiva.current = setInterval(() => {
          setSegundosRestantes(prev => {
            if (prev <= 1) {
              clearInterval(timerCuentaRegresiva.current);
              return 0;
            }
            return prev - 1;
          });
        }, 1000);
      }
    }, TICK_MS);

    // Escuchar actividad del usuario
    const eventos = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'click'];
    eventos.forEach(e => window.addEventListener(e, registrarActividad));

    // Cerrar sesión cuando se cierra la pestaña/navegador
    const handleBeforeUnload = () => {
      sessionStorage.removeItem('sesionActiva');
      // Logout síncrono (keepalive para que se envíe aunque se cierre la página)
      try {
        navigator.sendBeacon(`${API_BASE}/api/auth/logout`);
      } catch (_) {}
    };
    window.addEventListener('beforeunload', handleBeforeUnload);

    return () => {
      clearInterval(tickInterval.current);
      clearInterval(timerCuentaRegresiva.current);
      eventos.forEach(e => window.removeEventListener(e, registrarActividad));
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
  }, [cerrarSesion, registrarActividad]);


  // Cerrar sidebar al navegar en móvil
  React.useEffect(() => {
    setMobileMenuOpen(false);
  }, [location.pathname]);

  return (
    <div className="app-layout">
      <BarraSuperior
        onToggleNotifications={handleToggleNotifications}
        onToggleMobileMenu={() => setMobileMenuOpen(v => !v)}
        notificationCount={noLeidas}
      />
      <NotificationPanel 
        show={showNotifications} 
        onClose={() => setShowNotifications(false)}
        onNavigate={navigate}
        onNotificationsUpdated={cargarNotificaciones}
      />

      {mobileMenuOpen && (
        <div className="sidebar-overlay" onClick={() => setMobileMenuOpen(false)} aria-hidden="true" />
      )}
      <div className="main-container">
        <Sidebar mobileOpen={mobileMenuOpen} onClose={() => setMobileMenuOpen(false)} isAdmin={isAdmin} />
        <div className="content layout-content">
          {children}
        </div>
      </div>

      {/* Modal de aviso de inactividad */}
      {mostrarAviso && (
        <div className="idle-overlay">
          <div className="idle-modal">
            <div className="idle-modal-icon">⏱️</div>
            <h3>¿Seguís ahí?</h3>
            <p>Tu sesión se cerrará por inactividad en</p>
            <div className={`idle-countdown ${segundosRestantes <= 10 ? 'urgente' : ''}`}>
              {segundosRestantes}s
            </div>
            <div className="idle-modal-actions">
              <button className="idle-btn-continuar" onClick={registrarActividad}>
                Seguir conectado
              </button>
              <button className="idle-btn-salir" onClick={cerrarSesion}>
                Cerrar sesión
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Layout;