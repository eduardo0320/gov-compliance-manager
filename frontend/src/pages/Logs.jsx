import Toast, { useToast } from "../components/ui/Toast";
import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { obtenerFiltrosLogs, obtenerLogs } from '../services';

const Logs = ({ rolUsuario }) => {
  if (rolUsuario !== 'ADMIN' && rolUsuario !== 'SUPERADMIN') {
    return (
      <div className="logs-denied">
        <div className="logs-denied-icon">
          <i className="fas fa-lock"></i>
        </div>
        <h2>Acceso Denegado</h2>
        <p>
          No tiene permisos para acceder a esta página.
          Solo los administradores pueden ver los logs del sistema.
        </p>
      </div>
    );
  }

  const [logs, setLogs] = useState([]);
  const [tiposAccion, setTiposAccion] = useState([]);
  const [usuarios, setUsuarios] = useState([]);
  const [filtros, setFiltros] = useState({
    fechaDesde: '',
    fechaHasta: '',
    tipoAccion: '',
    usuarioId: ''
  });
  const [paginaActual, setPaginaActual] = useState(1);
  const [totalPaginas, setTotalPaginas] = useState(1);
  const [totalRegistros, setTotalRegistros] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const { toast: errorToast, showToast: showErrorToast, hideToast: hideErrorToast } = useToast();

  const tamanoPagina = 20;

  useEffect(() => { cargarCatalogos(); }, []);
  useEffect(() => { cargarLogs(); }, [paginaActual, filtros]);

  const cargarCatalogos = async () => {
    try {
      const data = await obtenerFiltrosLogs();
      setTiposAccion(data?.tiposAccion ?? []);
      setUsuarios(data?.usuarios ?? []);
    } catch (err) {
      console.error('Error cargando filtros de logs:', err);
    }
  };

  const cargarLogs = async () => {
    try {
      setLoading(true);
      setError('');
      const data = await obtenerLogs({
        pagina: paginaActual,
        tamanoPagina,
        fechaDesde: filtros.fechaDesde || null,
        fechaHasta: filtros.fechaHasta || null,
        tipoAccion: filtros.tipoAccion || null,
        usuarioId: filtros.usuarioId ? Number(filtros.usuarioId) : null
      });
      const logsDesc = [...(data?.logs ?? [])].sort(
        (a, b) => new Date(b.fechaEvento) - new Date(a.fechaEvento)
      );
      setLogs(logsDesc);
      setTotalPaginas(data?.totalPaginas ?? 1);
      setTotalRegistros(data?.totalRegistros ?? 0);
    } catch (err) {
      console.error('Error cargando logs:', err);
      setError('Error cargando los logs del sistema'); showErrorToast('Error cargando los logs del sistema', 'error');
      setLogs([]);
      setTotalPaginas(1);
      setTotalRegistros(0);
    } finally {
      setLoading(false);
    }
  };

  const handleFiltroChange = (e) => {
    const { name, value } = e.target;
    setFiltros((prev) => ({ ...prev, [name]: value }));
    setPaginaActual(1);
  };

  const limpiarFiltros = () => {
    setFiltros({ fechaDesde: '', fechaHasta: '', tipoAccion: '', usuarioId: '' });
    setPaginaActual(1);
  };

  const formatearFecha = (fecha) =>
    new Date(fecha).toLocaleString('es-ES', {
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', second: '2-digit'
    });

  const getBadgeClass = (tipoEvento) => {
    const tipo = (tipoEvento || '').toLowerCase();
    if (tipo === 'creación') return 'log-badge log-badge--success';
    if (tipo === 'modificación') return 'log-badge log-badge--warning';
    if (['eliminación','desactivación','intentosfallidoslogin','error'].includes(tipo)) return 'log-badge log-badge--danger';
    if (['login','logout','activación'].includes(tipo)) return 'log-badge log-badge--info';
    return 'log-badge log-badge--secondary';
  };

  if (loading && logs.length === 0) {
    return (
      <div className="logs-loading">
        <i className="fas fa-spinner fa-spin"></i> Cargando logs...
      </div>
    );
  }

  return (
    <div className="logs-page">

      {/* Header */}
      <div className="logs-header">
        <div className="logs-header-left">
          <div className="logs-breadcrumb">
            <span><Link to="/">Inicio</Link></span>
            <span className="sep">/</span>
            <span className="cur">Logs</span>
          </div>
          <h1>Visor de Logs / Auditoría</h1>
          <p>Registro de eventos y actividad del sistema</p>
        </div>
      </div>

      {/* Filter card */}
      <div className="logs-filter-card">
        <div className="logs-filter-card-header">
          <div className="logs-filter-card-icon">
            <i className="fas fa-filter"></i>
          </div>
          <div>
            <div className="logs-filter-card-title">Filtros de búsqueda</div>
            <div className="logs-filter-card-subtitle">Filtra por fecha, tipo de acción o usuario</div>
          </div>
        </div>
        <div className="logs-filter-card-body">
          <div className="logs-filters-grid">
            <div className="logs-filter-field">
              <label className="logs-filter-label" htmlFor="fechaDesde">Fecha desde</label>
              <input
                id="fechaDesde"
                name="fechaDesde"
                type="date"
                value={filtros.fechaDesde}
                onChange={handleFiltroChange}
                className="logs-filter-input"
              />
            </div>

            <div className="logs-filter-field">
              <label className="logs-filter-label" htmlFor="fechaHasta">Fecha hasta</label>
              <input
                id="fechaHasta"
                name="fechaHasta"
                type="date"
                value={filtros.fechaHasta}
                onChange={handleFiltroChange}
                className="logs-filter-input"
              />
            </div>

            <div className="logs-filter-field">
              <label className="logs-filter-label" htmlFor="tipoAccion">Tipo de acción</label>
              <select
                id="tipoAccion"
                name="tipoAccion"
                value={filtros.tipoAccion}
                onChange={handleFiltroChange}
                className="logs-filter-select"
              >
                <option value="">Todas</option>
                {tiposAccion.map((tipo) => (
                  <option key={tipo} value={tipo}>{tipo}</option>
                ))}
              </select>
            </div>

            <div className="logs-filter-field">
              <label className="logs-filter-label" htmlFor="usuarioId">Usuario</label>
              <select
                id="usuarioId"
                name="usuarioId"
                value={filtros.usuarioId}
                onChange={handleFiltroChange}
                className="logs-filter-select"
              >
                <option value="">Todos</option>
                {usuarios.map((usuario) => (
                  <option key={usuario.idUsuario} value={usuario.idUsuario}>
                    {usuario.nombreUsuario}
                  </option>
                ))}
              </select>
            </div>

            <div className="logs-filter-field logs-filter-field-actions">
              <label className="logs-filter-label">&nbsp;</label>
              <button className="logs-btn-clear" onClick={limpiarFiltros}>
                <i className="fas fa-times-circle"></i> Limpiar filtros
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Hint */}
      <div className="logs-order-hint">
        <i className="fas fa-sort-amount-down"></i> Mostrando los registros más recientes primero
      </div>

      {/* Error banner */}

      {/* Table card */}
      <div className="logs-table-card">
        <div className="logs-table-container">
          <table className="logs-table">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Usuario</th>
                <th>Tipo de Evento</th>
                <th>Módulo</th>
                <th>Descripción</th>
                <th>IP</th>
              </tr>
            </thead>
            <tbody>
              {logs.length === 0 ? (
                <tr>
                  <td colSpan="6">
                    <div className="logs-no-data">
                      <span className="logs-no-data-icon"><i className="fas fa-inbox"></i></span>
                      {loading ? 'Cargando logs...' : 'No hay logs para los filtros seleccionados'}
                    </div>
                  </td>
                </tr>
              ) : (
                logs.map((log) => (
                  <tr key={log.idAuditoria}>
                    <td className="log-fecha">{formatearFecha(log.fechaEvento)}</td>
                    <td className="log-usuario">{log.nombreUsuario || 'Sistema'}</td>
                    <td className="log-tipo">
                      <span className={getBadgeClass(log.tipoEvento)}>
                        {log.tipoEvento}
                      </span>
                    </td>
                    <td className="log-modulo">{log.modulo || '—'}</td>
                    <td className="log-descripcion">{log.descripcion}</td>
                    <td className="log-ip">{log.direccionIp || '—'}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="logs-pagination">
          <button
            className="logs-page-btn"
            onClick={() => setPaginaActual((prev) => Math.max(prev - 1, 1))}
            disabled={paginaActual === 1 || loading}
          >
            <i className="fas fa-chevron-left"></i> Anterior
          </button>
          <span className="logs-page-info">
            Página {paginaActual} de {totalPaginas} &nbsp;·&nbsp; {totalRegistros} registros
          </span>
          <button
            className="logs-page-btn"
            onClick={() => setPaginaActual((prev) => prev + 1)}
            disabled={loading || paginaActual >= totalPaginas}
          >
            Siguiente <i className="fas fa-chevron-right"></i>
          </button>
        </div>
      </div>

      <Toast toast={errorToast} onClose={hideErrorToast} />
    </div>
  );
};

export default Logs;