import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getDominios, getProcesosByDominio } from '../../services';

const BarraSuperior = ({ onToggleNotifications, onToggleMobileMenu, notificationCount = 0 }) => {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery]     = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [showResults, setShowResults]     = useState(false);
  const [isSearching, setIsSearching]     = useState(false);
  // Cache de todos los procesos cargados al montar
  const allProcesosRef   = useRef(null);
  const searchRef        = useRef(null);
  const searchTimeoutRef = useRef(null);  

  // Cerrar al hacer clic fuera
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (searchRef.current && !searchRef.current.contains(e.target)) {
        setShowResults(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Precargar todos los procesos una sola vez
  const cargarTodosLosProcesos = async () => {
    if (allProcesosRef.current !== null) return allProcesosRef.current;
    try {
      const dominios = await getDominios();
      const todos = [];
      for (const dom of dominios) {
        try {
          const procs = await getProcesosByDominio(dom.id);
          procs.forEach(p => todos.push({
            tipo:         'Proceso',
            id:           p.idProceso ?? p.id,
            titulo:       p.codigo ? `${p.codigo} - ${p.nombre ?? ''}` : (p.nombre ?? ''),
            descripcion:  p.marcoNormativo ?? '',
            dominioNombre: dom.nombre ?? '',
            dominioId:    dom.id,
            codigo:       (p.codigo ?? '').toLowerCase(),
            nombreLower:  (p.nombre  ?? '').toLowerCase(),
            dominioLower: (dom.nombre ?? '').toLowerCase(),
          }));
        } catch (_) {}
      }
      allProcesosRef.current = todos;
      return todos;
    } catch (_) {
      allProcesosRef.current = [];
      return [];
    }
  };

  const handleSearch = async (query) => {
    const q = (query ?? '').trim();
    if (q.length < 2) {
      setSearchResults([]);
      setShowResults(false);
      return;
    }

    setIsSearching(true);
    try {
      const todos = await cargarTodosLosProcesos();
      const ql = q.toLowerCase();

      const filtrados = todos.filter(p =>
        p.codigo.includes(ql)       ||
        p.nombreLower.includes(ql)  ||
        p.dominioLower.includes(ql) ||
        p.titulo.toLowerCase().includes(ql)
      );

      // Construir ruta segura
      const safe = filtrados.map(p => ({
        tipo:          p.tipo,
        id:            p.id,
        titulo:        p.titulo,
        descripcion:   p.descripcion,
        dominioNombre: p.dominioNombre,
        ruta:          (p.dominioId && p.id)
                         ? `/processes/${p.dominioId}/${p.id}`
                         : '/processes',
      }));

      setSearchResults(safe);
      setShowResults(true);
    } catch (error) {
      console.error('Error en búsqueda:', error);
      setSearchResults([]);
      setShowResults(true);
    } finally {
      setIsSearching(false);
    }
  };

  const handleInputChange = (e) => {
    const value = e.target.value;
    setSearchQuery(value);
    clearTimeout(searchTimeoutRef.current);
    searchTimeoutRef.current = setTimeout(() => handleSearch(value), 300);
  };

  const handleResultClick = (result) => {
    setShowResults(false);
    setSearchQuery('');
    if (result.ruta) navigate(result.ruta);
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleSearch(searchQuery);
    } else if (e.key === 'Escape') {
      setShowResults(false);
    }
  };

  const handleProfileClick = () => navigate('/profile');

  return (
    <header className="barra-superior">
      <button className="mobile-menu-btn" onClick={onToggleMobileMenu}>
        <i className="fas fa-bars"></i>
      </button>

      <div className="logo">
        <img src="/images/MuniLogo_principal_letras_negras.png" alt="Logo Municipalidad de Palmares" />
      </div>

      <div className="search-container" ref={searchRef}>
        <input
          type="text"
          className="search-input"
          placeholder="Buscar por código, nombre o dominio..."
          value={searchQuery}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
        />
        <button className="search-btn" onClick={() => handleSearch(searchQuery)}>
          <i className={`fas ${isSearching ? 'fa-spinner fa-spin' : 'fa-search'}`}></i>
        </button>

        {showResults && (
          <div className="search-results">
            {searchResults.length === 0 ? (
              <div className="search-result-item no-results">
                <i className="fas fa-search"></i>
                <span>No se encontraron resultados</span>
              </div>
            ) : (
              searchResults.map((result, index) => (
                <div
                  key={`${result.tipo}-${result.id}-${index}`}
                  className="search-result-item"
                  onClick={() => handleResultClick(result)}
                >
                  <div className="result-icon">
                    <i className="fas fa-clipboard-list"></i>
                  </div>
                  <div className="result-content">
                    <div className="result-title">{result.titulo}</div>
                    {result.descripcion && (
                      <div className="result-description">{result.descripcion}</div>
                    )}
                    {result.dominioNombre && (
                      <div className="result-domain">
                        <i className="fas fa-cube"></i>
                        {' '}{result.dominioNombre}
                      </div>
                    )}
                  </div>
                  <div className="result-type">
                    <span className="type-badge proceso">Proceso</span>
                  </div>
                </div>
              ))
            )}
          </div>
        )}
      </div>

      <div className="bs-spacer" />

      <div className="bs-actions">
        <div className="bs-actions-left">
          <button
            className="bs-icon-btn"
            onClick={onToggleNotifications}
            aria-label="Notificaciones"
          >
            <i className="fas fa-bell" />
            {notificationCount > 0 && (
              <span className="bs-badge">{notificationCount}</span>
            )}
          </button>
        </div>
        <div className="bs-actions-right">
          <button
            className="bs-icon-btn"
            onClick={handleProfileClick}
            aria-label="Perfil"
          >
            <i className="fas fa-user" />
          </button>
        </div>
      </div>
    </header>
  );
};

export default BarraSuperior;