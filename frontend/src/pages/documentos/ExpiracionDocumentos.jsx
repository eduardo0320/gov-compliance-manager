import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { getAlertasVencimiento } from "../../services";

const normalizarFecha = (valor) => {
  if (!valor) return null;
  const fecha = new Date(valor);
  return Number.isNaN(fecha.getTime()) ? null : fecha;
};

const inicioDelDia = (valor = new Date()) =>
  new Date(valor.getFullYear(), valor.getMonth(), valor.getDate());

const diasHasta = (fecha, desde) => {
  if (!fecha) return null;
  return Math.floor((inicioDelDia(fecha) - inicioDelDia(desde)) / (1000 * 60 * 60 * 24));
};

const fechaTexto = (valor) => {
  const fecha = normalizarFecha(valor);
  if (!fecha) return 'Sin fecha';
  return fecha.toLocaleDateString('es-CR', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const escaparCsv = (valor) => {
  const texto = (valor ?? '').toString();
  if (texto.includes(';') || texto.includes('"') || texto.includes('\n')) {
    return `"${texto.replace(/"/g, '""')}"`;
  }
  return texto;
};

const descargarCsv = (nombreArchivo, filas) => {
  const csv = filas
    .map((fila) => fila.map((columna) => escaparCsv(columna)).join(';'))
    .join('\r\n');
  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement('a');
  enlace.href = url;
  enlace.download = nombreArchivo;
  document.body.appendChild(enlace);
  enlace.click();
  document.body.removeChild(enlace);
  URL.revokeObjectURL(url);
};

const ItemDocumento = ({ documento, tipo, onOpen }) => {
  const dias = tipo === 'vencidos' ? documento.diasVencido : documento.diasRestantes;

  return (
    <div className="document-item doc-item--clickable" onClick={() => onOpen(documento.id)}>
      <i className="fas fa-file-alt"></i>
      <div className="doc-item-text">
        <div className="doc-item-nombre">{documento.nombre}</div>
        <div className="doc-item-meta">Dominio: {documento.dominioNombre || 'Sin dominio'}</div>
        <div className="doc-item-meta">Actividad: {documento.actividadNombre || 'Sin actividad'}</div>
        <div className="doc-item-fecha">Vence: {fechaTexto(documento.fechaVencimiento)}</div>
      </div>
      <div className="doc-item-badge-col">
        {tipo === 'vencidos' ? (
          <span className="badge badge-danger">{dias} día(s) vencido</span>
        ) : (
          <span className="badge badge-warning">{dias} día(s)</span>
        )}
      </div>
    </div>
  );
};

const Seccion = ({ titulo, icono, documentos, tipo, vacioTexto, onOpen }) => {
  const esVencidos = tipo === 'vencidos';

  const exportarCategoria = () => {
    const filas = [['categoria', 'id', 'nombre', 'dominio', 'actividad', 'estado', 'fechaVencimiento', 'dias']];
    documentos.forEach((doc) => {
      filas.push([
        esVencidos ? 'Vencido' : 'Proximo a vencer',
        doc.id, doc.nombre,
        doc.dominioNombre || '', doc.actividadNombre || '',
        doc.estado,
        fechaTexto(doc.fechaVencimiento),
        esVencidos ? doc.diasVencido : doc.diasRestantes
      ]);
    });
    const fecha = new Date().toISOString().slice(0, 10);
    const nombre = esVencidos
      ? `documentos-vencidos-${fecha}.csv`
      : `documentos-proximos-a-vencer-${fecha}.csv`;
    descargarCsv(nombre, filas);
  };

  return (
    <div className="doc-seccion-wrapper">

      <div className={esVencidos ? "doc-seccion-card--danger" : "doc-seccion-card--warning"}>
        <div className="doc-card-content">
          {documentos.length === 0 ? (
            <div className="empty-state">
              <i className="fas fa-info-circle"></i>
              <span>{vacioTexto}</span>
            </div>
          ) : (
            <div className="actividades-tabla-scroll documents-list">
              <table className="actividades-tabla">
                <thead>
                  <tr>
                    <th>Documento</th>
                    <th>Dominio</th>
                    <th>Actividad</th>
                    <th>Estado</th>
                    <th>Vencimiento</th>
                    <th>Días</th>
                    <th>Acción</th>
                  </tr>
                </thead>
                <tbody>
                  {documentos.map((doc) => {
                    const dias = esVencidos ? doc.diasVencido : doc.diasRestantes;
                    return (
                      <tr key={`${tipo}-${doc.id}`} className="doc-row">
                        <td className="doc-name">{doc.nombre}</td>
                        <td className="doc-domain">{doc.dominioNombre || 'Sin dominio'}</td>
                        <td className="doc-activity">{doc.actividadNombre || 'Sin actividad'}</td>
                        <td className="doc-state">{doc.estado}</td>
                        <td className={`doc-date`}>{fechaTexto(doc.fechaVencimiento)}</td>
                        <td className="doc-days">{dias ?? '—'}</td>
                        <td>
                          <button className="btn-accion-ver" onClick={() => onOpen(doc.id)}>
                            <i className="fas fa-edit me-1"></i>Ver
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

const ExpiracionDocumentos = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [vencidos, setVencidos] = useState([]);
  const [proximos, setProximos] = useState([]);

  const onOpenDocumento = (idDocumento) => {
    if (!idDocumento) return;
    navigate(`/documentos/${idDocumento}`);
  };

  useEffect(() => {
    const cargarDatos = async () => {
      setLoading(true);
      setError('');
      try {
        const alertas = await getAlertasVencimiento(7);
        setVencidos(Array.isArray(alertas?.vencidos) ? alertas.vencidos : []);
        setProximos(Array.isArray(alertas?.proximosAVencer) ? alertas.proximosAVencer : []);
      } catch (err) {
        console.error('Error cargando expiración de documentos:', err);
        setError('No se pudieron cargar los documentos.');
      } finally {
        setLoading(false);
      }
    };
    cargarDatos();
  }, []);

  const resumen = useMemo(
    () => ({ vencidos: vencidos.length, proximos: proximos.length }),
    [vencidos.length, proximos.length]
  );

  const exportAll = () => {
    const filas = [['categoria', 'id', 'nombre', 'dominio', 'actividad', 'estado', 'fechaVencimiento', 'dias']];
    proximos.forEach((doc) => {
      filas.push([
        'Proximo a vencer',
        doc.id, doc.nombre,
        doc.dominioNombre || '', doc.actividadNombre || '',
        doc.estado,
        fechaTexto(doc.fechaVencimiento),
        doc.diasRestantes
      ]);
    });
    vencidos.forEach((doc) => {
      filas.push([
        'Vencido',
        doc.id, doc.nombre,
        doc.dominioNombre || '', doc.actividadNombre || '',
        doc.estado,
        fechaTexto(doc.fechaVencimiento),
        doc.diasVencido
      ]);
    });
    const fecha = new Date().toISOString().slice(0, 10);
    descargarCsv(`documentos-expiracion-${fecha}.csv`, filas);
  };

  const exportProximos = () => {
    const filas = [['categoria', 'id', 'nombre', 'dominio', 'actividad', 'estado', 'fechaVencimiento', 'dias']];
    proximos.forEach((doc) => {
      filas.push([
        'Proximo a vencer',
        doc.id, doc.nombre,
        doc.dominioNombre || '', doc.actividadNombre || '',
        doc.estado,
        fechaTexto(doc.fechaVencimiento),
        doc.diasRestantes
      ]);
    });
    const fecha = new Date().toISOString().slice(0, 10);
    descargarCsv(`documentos-proximos-${fecha}.csv`, filas);
  };

  const exportVencidos = () => {
    const filas = [['categoria', 'id', 'nombre', 'dominio', 'actividad', 'estado', 'fechaVencimiento', 'dias']];
    vencidos.forEach((doc) => {
      filas.push([
        'Vencido',
        doc.id, doc.nombre,
        doc.dominioNombre || '', doc.actividadNombre || '',
        doc.estado,
        fechaTexto(doc.fechaVencimiento),
        doc.diasVencido
      ]);
    });
    const fecha = new Date().toISOString().slice(0, 10);
    descargarCsv(`documentos-vencidos-${fecha}.csv`, filas);
  };

  if (loading) {
    return (
      <div className="content-header">
        <div className="content-title">
          <i className="fas fa-spinner fa-spin doc-loading-icon"></i>
          Cargando documentos...
        </div>
      </div>
    );
  }

  return (
    <div className="mis-actividades-container">
      <nav className="act-breadcrumb" style={{ marginBottom: '0.75rem' }}>
        <Link to="/">Inicio</Link>
        <span className="sep">›</span>
        <span className="current">Verificar Documentos</span>
      </nav>
      <div className="mis-actividades-header">
        <h2>
          <i className="fas fa-folder icon-tasks" style={{ fontSize: 28 }}></i>
          Verificar Documentos
        </h2>
        <p>Clasificación por estado de vencimiento</p>
      </div>

      {error && (
        <div className="empty-state doc-error-mb">
          <i className="fas fa-exclamation-triangle"></i>
          <span>{error}</span>
        </div>
      )}

      <div className="mis-actividades-resumen">
        <div className="resumen-card resumen-card--pendiente">
          <i className="fas fa-clock resumen-icon"></i>
          <div className="resumen-numero">{resumen.proximos}</div>
          <div className="resumen-label">Próximos a vencer (7d)</div>
        </div>
        <div className="resumen-card resumen-card--vencida">
          <i className="fas fa-exclamation-triangle resumen-icon"></i>
          <div className="resumen-numero">{resumen.vencidos}</div>
          <div className="resumen-label">Vencidos</div>
        </div>
        <div className="resumen-card resumen-card--total">
          <i className="fas fa-list-check resumen-icon"></i>
          <div className="resumen-numero">{resumen.proximos + resumen.vencidos}</div>
          <div className="resumen-label">Total</div>
        </div>
      </div>

      <div className="actividades-seccion">
        <div className="actividades-seccion-header actividades-seccion-header--pendiente">
          <div className="actividades-seccion-header-content">
            <i className="fas fa-exclamation-circle"></i>
            <span>Próximos a vencer (en 7 días)</span>
            {proximos.length > 0 && <span className="seccion-badge seccion-badge--pendiente">{proximos.length}</span>}
          </div>
          <button
            type="button"
            className="doc-export-btn--warning doc-export-btn-inline"
            onClick={exportProximos}
            disabled={proximos.length === 0}
            aria-label="Exportar próximos a vencer"
          >
            <i className="fas fa-file-csv"></i>
            <span>Exportar</span>
          </button>
        </div>
        <div className="doc-section-body">
          <Seccion
            documentos={proximos}
            tipo="proximos"
            vacioTexto="No hay documentos próximos a vencer."
            onOpen={onOpenDocumento}
          />
        </div>
      </div>

      <div className="actividades-seccion">
        <div className="actividades-seccion-header actividades-seccion-header--vencida">
          <div className="actividades-seccion-header-content">
            <i className="fas fa-times-circle"></i>
            <span>Vencidos</span>
            {vencidos.length > 0 && <span className="seccion-badge seccion-badge--vencida">{vencidos.length}</span>}
          </div>
          <button
            type="button"
            className="doc-export-btn--danger doc-export-btn-inline"
            onClick={exportVencidos}
            disabled={vencidos.length === 0}
            aria-label="Exportar vencidos"
          >
            <i className="fas fa-file-csv"></i>
            <span>Exportar</span>
          </button>
        </div>
        <div className="doc-section-body">
          <Seccion
            titulo=""
            icono="fas fa-times-circle"
            documentos={vencidos}
            tipo="vencidos"
            vacioTexto="No hay documentos vencidos."
            onOpen={onOpenDocumento}
          />
        </div>
      </div>
    </div>
  );
};

export default ExpiracionDocumentos;