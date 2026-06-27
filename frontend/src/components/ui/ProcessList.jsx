import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getDominios, getProcesosByDominio } from '../../services';

const ProcessList = () => {
  const navigate = useNavigate();
  const [dominios, setDominios] = useState([]);
  const [allProcesos, setAllProcesos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filtroTexto, setFiltroTexto] = useState('');
  const [filtroDominio, setFiltroDominio] = useState('');

  useEffect(() => {
    cargarDatos();
  }, []);

  const cargarDatos = async () => {
    try {
      setLoading(true);
      const dominiosData = await getDominios();
      setDominios(dominiosData);

      // Cargar todos los procesos de todos los dominios
      const todosLosProcesos = [];
      for (const dominio of dominiosData) {
        try {
          const procesos = await getProcesosByDominio(dominio.id);
          procesos.forEach(proceso => {
            todosLosProcesos.push({
              ...proceso,
              dominioNombre: dominio.nombre
            });
          });
        } catch (err) {
          console.error(`Error cargando procesos del dominio ${dominio.id}:`, err);
        }
      }
      setAllProcesos(todosLosProcesos);
    } catch (err) {
      console.error('Error cargando datos:', err);
    } finally {
      setLoading(false);
    }
  };

  const procesosFiltrados = allProcesos.filter(proceso => {
    const matchTexto = !filtroTexto || 
      proceso.codigo.toLowerCase().includes(filtroTexto.toLowerCase()) ||
      proceso.nombre.toLowerCase().includes(filtroTexto.toLowerCase());
    
    const matchDominio = !filtroDominio || proceso.dominioId === parseInt(filtroDominio);
    
    return matchTexto && matchDominio;
  });

  if (loading) {
    return (
      <div className="process-list-container">
        <div className="loading">Cargando procesos...</div>
      </div>
    );
  }

  return (
    <div className="process-list-container">
      <div className="list-header">
        <h3>Lista de Procesos</h3>
        
        <div className="list-filters">
          <input
            type="text"
            placeholder="Buscar por código o nombre..."
            value={filtroTexto}
            onChange={(e) => setFiltroTexto(e.target.value)}
            className="filter-input"
          />
          
          <select
            value={filtroDominio}
            onChange={(e) => setFiltroDominio(e.target.value)}
            className="filter-select"
          >
            <option value="">Todos los dominios</option>
            {dominios.map(dominio => (
              <option key={dominio.id} value={dominio.id}>
                {dominio.nombre}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="process-grid">
        {procesosFiltrados.length === 0 ? (
          <div className="empty-state">
            <i className="fas fa-search"></i>
            <p>No se encontraron procesos con los filtros aplicados</p>
          </div>
        ) : (
          procesosFiltrados.map(proceso => (
            <div key={proceso.idProceso} className="process-card">
              <div className="process-card-header">
                <div className="process-code">{proceso.codigo}</div>
                <div className="process-actions-card">
                  <button 
                    className="btn-icon edit-btn"
                    onClick={() => navigate(`/editar-proceso/${proceso.idProceso}`)}
                    title="Editar proceso"
                  >
                    <i className="fas fa-edit"></i>
                  </button>
                </div>
              </div>
              
              <div className="process-card-content">
                <h4 className="process-title">{proceso.nombre}</h4>
                <p className="process-domain">{proceso.dominioNombre}</p>
                <p className="process-framework">{proceso.marcoNormativo}</p>
                
                <div className="process-status">
                  <span className={`status-badge ${proceso.estadoImplementacion === 'Sí' ? 'implemented' : 'not-implemented'}`}>
                    {proceso.estadoImplementacion === 'Sí' ? 'Implementado' : 'No Implementado'}
                  </span>
                </div>
              </div>
              
              <div className="process-card-footer">
                <div className="progress-info">
                  <span>Progreso: {proceso.porcentajeAvance}%</span>
                  <div className="progress-bar">
                    <div 
                      className="progress-fill" 
                      style={{width: `${proceso.porcentajeAvance}%`}}
                    ></div>
                  </div>
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default ProcessList;