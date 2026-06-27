import React, { useState, useEffect, useRef } from "react";
import { apiFetch } from "../services/apiClient";
import { useNavigate } from "react-router-dom";
import { crearActividad } from "../services";
import { DASHBOARD_CACHE_KEY, invalidarCacheDashboard } from "../utils/dashboardCache";


const IconDoc = () => (
  <svg width="22" height="22" viewBox="0 0 20 20" fill="none" stroke="#ffffff" strokeWidth="1.6">
    <rect x="3" y="2" width="14" height="16" rx="2" />
    <line x1="7" y1="7" x2="13" y2="7" />
    <line x1="7" y1="10" x2="13" y2="10" />
    <line x1="7" y1="13" x2="10" y2="13" />
  </svg>
);

const IconPlus = () => (
  <svg width="13" height="13" viewBox="0 0 14 14" fill="none" stroke="currentColor" strokeWidth="2">
    <line x1="7" y1="1" x2="7" y2="13" />
    <line x1="1" y1="7" x2="13" y2="7" />
  </svg>
);

const IconClose = () => (
  <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
    <line x1="4" y1="4" x2="16" y2="16" />
    <line x1="16" y1="4" x2="4" y2="16" />
  </svg>
);

const IconUser = () => (
  <svg width="13" height="13" viewBox="0 0 20 20" fill="none" stroke="#059669" strokeWidth="2">
    <circle cx="10" cy="7" r="4" />
    <path d="M2 18c0-4 3.6-7 8-7s8 3 8 7" />
  </svg>
);

const IconSearch = () => (
  <svg width="14" height="14" viewBox="0 0 20 20" fill="none" stroke="#9ca3af" strokeWidth="2">
    <circle cx="9" cy="9" r="6" />
    <line x1="13.5" y1="13.5" x2="18" y2="18" />
  </svg>
);

export default function ActividadFormulario({ subdominioId, onCancel }) {
  const navigate = useNavigate();
  const [nombre, setNombre] = useState("");

  const [busqueda, setBusqueda] = useState("");
  const [responsableSeleccionado, setResponsableSeleccionado] = useState(null);
  const [mostrarDropdown, setMostrarDropdown] = useState(false);
  const [usuarios, setUsuarios] = useState([]);
  const [loadingUsuarios, setLoadingUsuarios] = useState(true);
  const [dropdownPos, setDropdownPos] = useState({ top: 0, left: 0, width: 0 });

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [errorResponsable, setErrorResponsable] = useState("");

  const dropdownRef = useRef(null);
  const inputBusquedaRef = useRef(null);

  useEffect(() => {
    const cargarUsuarios = async () => {
      try {
        const res = await apiFetch(`/api/usuarios`);
        if (res.ok) {
          const data = await res.json();
          const lista = Array.isArray(data) ? data : (data.items ?? data.usuarios ?? []);
          setUsuarios(lista);
        }
      } catch {
        // silencioso
      } finally {
        setLoadingUsuarios(false);
      }
    };
    cargarUsuarios();
  }, []);

  useEffect(() => {
    const handler = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setMostrarDropdown(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  useEffect(() => {
    const handler = (e) => { if (e.key === "Escape" && onCancel) onCancel(); };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [onCancel]);

  const usuariosFiltrados = usuarios.filter((u) => {
    const texto = busqueda.toLowerCase().trim();
    if (!texto) return true;
    const nombre = (u.nombre_completo ?? u.nombre ?? "").toLowerCase();
    const cedula = (u.cedula ?? "").toLowerCase();
    return nombre.includes(texto) || cedula.includes(texto);
  });

  const seleccionarResponsable = (usuario) => {
    setResponsableSeleccionado(usuario);
    setBusqueda("");
    setMostrarDropdown(false);
    setErrorResponsable("");
  };

  const limpiarResponsable = () => {
    setResponsableSeleccionado(null);
    setBusqueda("");
    setMostrarDropdown(false);
  };

  const handleCreate = async () => {
    let valid = true;
    if (!nombre.trim()) { setError("El nombre de la actividad es requerido"); valid = false; }
    if (!responsableSeleccionado) { setErrorResponsable("Debe seleccionar un responsable"); valid = false; }
    if (!valid) return;

    setSaving(true);
    setError("");
    setErrorResponsable("");
    try {
      const resp = await crearActividad(subdominioId, nombre.trim(), responsableSeleccionado.id);
      const idAct = resp.idActividad ?? resp.id ?? resp.IdActividad ?? resp.Id ?? null;
      invalidarCacheDashboard();
      navigate(`/subdominios/${subdominioId}/actividades/${idAct}/editar`);
    } catch (e) {
      setError(e.message || "Error al crear la actividad");
    } finally {
      setSaving(false);
    }
  };

  const handleOverlayClick = (e) => {
    if (e.target === e.currentTarget && onCancel) onCancel();
  };

  const puedeCrear = nombre.trim() && responsableSeleccionado;

  return (
    <div className="act-modal-overlay" onClick={handleOverlayClick}>
      <div className="act-modal-box" role="dialog" aria-modal="true" aria-labelledby="act-modal-title">

        {/* Header */}
        <div className="act-modal-header">
          <div className="act-modal-header-icon act-modal-header-icon--spaced">
            <IconDoc />
          </div>
          <div className="act-modal-header-text">
            <h2 className="act-modal-title" id="act-modal-title">Nueva Actividad</h2>
            <p className="act-modal-subtitle act-modal-subtitle--spaced">Complete los datos para crear la actividad</p>
          </div>
          {onCancel && (
            <button type="button" className="act-modal-close-btn" onClick={onCancel} aria-label="Cerrar" disabled={saving}>
              <IconClose />
            </button>
          )}
        </div>

        <div className="act-modal-divider" />

        {/* Body */}
        <div className="act-modal-body">

          {/* Nombre */}
          <div className="act-modal-field">
            <label className="act-modal-label" htmlFor="act-nombre-input">
              <svg width="13" height="13" viewBox="0 0 20 20" fill="none" stroke="#059669" strokeWidth="2">
                <rect x="3" y="2" width="14" height="16" rx="2" />
                <line x1="7" y1="7" x2="13" y2="7" />
                <line x1="7" y1="10" x2="13" y2="10" />
              </svg>
              Nombre de la actividad <span className="act-modal-required">*</span>
            </label>
            <input
              id="act-nombre-input"
              className={"act-modal-input" + (error ? " act-modal-input--error" : "")}
              value={nombre}
              onChange={(e) => { setNombre(e.target.value); if (error) setError(""); }}
              placeholder="Ej. Definir política de copias de seguridad"
              disabled={saving}
              autoFocus
              maxLength={255}
            />
            {error && <span className="act-modal-field-error">{error}</span>}
          </div>

          {/* Responsable con búsqueda */}
          <div className="act-modal-field act-modal-field--mt" ref={dropdownRef}>
            <label className="act-modal-label" htmlFor="act-responsable-busqueda">
              <IconUser />
              Responsable <span className="act-modal-required">*</span>
            </label>

            {responsableSeleccionado ? (
              <div className="act-responsable-chip">
                <div>
                  <span className="act-responsable-chip-name">
                    {responsableSeleccionado.nombre_completo ?? responsableSeleccionado.nombre}
                  </span>
                  <span className="act-responsable-chip-cedula">
                    Cédula: {responsableSeleccionado.cedula}
                  </span>
                </div>
                <button
                  type="button"
                  className="act-responsable-chip-clear"
                  onClick={limpiarResponsable}
                  disabled={saving}
                  aria-label="Cambiar responsable"
                >
                  <IconClose />
                </button>
              </div>
            ) : (
              <div className="act-responsable-search-wrap">
                <span className="act-responsable-search-icon">
                  <IconSearch />
                </span>
                <input
                  id="act-responsable-busqueda"
                  ref={inputBusquedaRef}
                  className={"act-modal-input act-responsable-input" + (errorResponsable ? " act-modal-input--error" : "")}
                  value={busqueda}
                  onChange={(e) => { setBusqueda(e.target.value); setMostrarDropdown(true); if (errorResponsable) setErrorResponsable(""); }}
                  onFocus={() => {
                    if (inputBusquedaRef.current) {
                      const rect = inputBusquedaRef.current.getBoundingClientRect();
                      setDropdownPos({ top: rect.bottom + 4, left: rect.left, width: rect.width });
                    }
                    setMostrarDropdown(true);
                  }}
                  placeholder={loadingUsuarios ? "Cargando usuarios..." : "Buscar por nombre o cédula..."}
                  disabled={saving || loadingUsuarios}
                  autoComplete="off"
                />

                {mostrarDropdown && !loadingUsuarios && (
                  <div
                    className="act-responsable-dropdown"
                    style={{ top: dropdownPos.top, left: dropdownPos.left, width: dropdownPos.width }}
                  >
                    {usuariosFiltrados.length === 0 ? (
                      <div className="act-responsable-dropdown-empty">
                        No se encontraron usuarios
                      </div>
                    ) : (
                      usuariosFiltrados.map((u) => (
                        <div
                          key={u.id}
                          className="act-responsable-dropdown-item"
                          onClick={() => seleccionarResponsable(u)}
                        >
                          <div className="act-responsable-dropdown-item-name">
                            {u.nombre_completo ?? u.nombre}
                          </div>
                          <div className="act-responsable-dropdown-item-cedula">
                            Cédula: {u.cedula}
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                )}
              </div>
            )}

            {errorResponsable && <span className="act-modal-field-error">{errorResponsable}</span>}
          </div>

        </div>

        {/* Footer */}
        <div className="act-modal-footer">
          {onCancel && (
            <button type="button" className="act-modal-btn-cancel" onClick={onCancel} disabled={saving}>
              Cancelar
            </button>
          )}
          <button
            type="button"
            className="act-modal-btn-submit"
            onClick={handleCreate}
            disabled={saving || !puedeCrear}
          >
            <IconPlus />
            {saving ? "Creando..." : "Crear y editar"}
          </button>
        </div>

      </div>
    </div>
  );
}