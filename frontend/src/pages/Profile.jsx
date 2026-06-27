import React, { useState, useEffect } from "react";
import { useNavigate, Link } from "react-router-dom";
import CambiarContrasena from "./CambiarContrasena";
import { apiFetch } from "../services/apiClient";
import { obtenerMiPerfil, actualizarMiPerfil, activarTwoFactor, desactivarTwoFactor } from "../services";
import Toast, { useToast } from "../components/ui/Toast";
import { ConfirmDialog, useConfirm } from "../components/ui/ResultDialog";

const Profile = () => {
  const navigate = useNavigate();
  const [showCambiarContrasena, setShowCambiarContrasena] = useState(false);
  const { toast, showToast, hideToast } = useToast();
  const { confirm, askConfirm, closeConfirm } = useConfirm();

  const [userInfo, setUserInfo] = useState({
    nombre: "", correo_electronico: "", cedula: "", departamento: "",
    idRol: "", estado: true, fechaCreacion: "", fechaUltimaModificacion: "",
    ultimoAcceso: "", twoFactorEnabled: false,
  });
  const [editForm, setEditForm] = useState({ nombre: "", correo_electronico: "", departamento: "" });
  const [errors, setErrors]     = useState({});
  const [isLoading, setIsLoading] = useState(false);
  const [isEditing, setIsEditing] = useState(false);

  const formatearRol = (rol) => {
    const map = {
      SUPERADMIN: "Super Administrador", ADMIN: "Administrador", USER: "Usuario",
      SUPER_ADMIN: "Super Administrador", ADMINISTRATOR: "Administrador",
      STANDARD_USER: "Usuario Estándar", GUEST: "Invitado",
      MODERATOR: "Moderador", EDITOR: "Editor",
    };
    if (!rol) return "Sin rol asignado";
    if (rol.includes(" ")) return rol;
    return map[rol.toUpperCase()] || rol;
  };

  const obtenerIconoRol = (rol) => {
    const map = { SUPERADMIN: "fa-crown", ADMIN: "fa-user-shield", USER: "fa-user", EDITOR: "fa-user-edit" };
    if (!rol) return "fa-user-times";
    return map[rol.toUpperCase()] || "fa-user";
  };

  useEffect(() => { cargarMiPerfil(); }, []);

  const cargarMiPerfil = async () => {
    try {
      setIsLoading(true);
      const response = await obtenerMiPerfil();
      const data = response.perfil;
      setUserInfo({
        nombre: data.nombre || "", correo_electronico: data.correo_electronico || "",
        cedula: data.cedula || "", departamento: data.departamento || "",
        idRol: data.nombreRol || "", estado: data.estado,
        fechaCreacion: data.fechaCreacion || "", fechaUltimaModificacion: data.fechaUltimaModificacion || "",
        ultimoAcceso: data.ultimoAcceso || "", twoFactorEnabled: data.twoFactorEnabled || false,
      });
      setEditForm({ nombre: data.nombre || "", correo_electronico: data.correo_electronico || "", departamento: data.departamento || "" });
    } catch (error) {
      console.error("Error al cargar perfil:", error);
      showToast("Error al cargar la información del perfil", "error");
    } finally {
      setIsLoading(false);
    }
  };

  // ── 2FA ──────────────────────────────────────────────────────────
  const ejecutar2FA = async () => {
    const accion = userInfo.twoFactorEnabled ? "desactivar" : "activar";
    try {
      const response = await (userInfo.twoFactorEnabled ? desactivarTwoFactor() : activarTwoFactor());
      showToast(response?.mensaje || `2FA ${accion}do correctamente`);
      setUserInfo((prev) => ({ ...prev, twoFactorEnabled: !prev.twoFactorEnabled }));
    } catch (error) {
      showToast(`No se pudo ${accion} 2FA: ${error.message}`, "error");
    }
  };

  const handleConfigurarTwoFactor = () => {
    const accion = userInfo.twoFactorEnabled ? "desactivar" : "activar";
    askConfirm(
      `¿${accion.charAt(0).toUpperCase() + accion.slice(1)} verificación en dos pasos?`,
      `Esta acción va a ${accion} la autenticación de dos factores (2FA) en tu cuenta.`,
      ejecutar2FA
    );
  };

  // ── Editar perfil ────────────────────────────────────────────────
  const handleEdit = () => {
    if (!isEditing) {
      setEditForm({ nombre: userInfo.nombre, correo_electronico: userInfo.correo_electronico, departamento: userInfo.departamento });
      setErrors({});
    }
    setIsEditing(!isEditing);
  };

  const validateForm = () => {
    const e = {};
    if (!editForm.nombre?.trim()) e.nombre = "El nombre es requerido";
    else if (editForm.nombre.length < 2) e.nombre = "Mínimo 2 caracteres";
    else if (editForm.nombre.length > 100) e.nombre = "Máximo 100 caracteres";
    if (!editForm.correo_electronico?.trim()) e.correo_electronico = "El correo es requerido";
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(editForm.correo_electronico)) e.correo_electronico = "Formato inválido";
    else if (editForm.correo_electronico.length > 100) e.correo_electronico = "Máximo 100 caracteres";
    if (!editForm.departamento?.trim()) e.departamento = "El departamento es requerido";
    else if (editForm.departamento.length > 100) e.departamento = "Máximo 100 caracteres";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const ejecutarGuardado = async () => {
    try {
      setIsLoading(true);
      await actualizarMiPerfil(editForm);
      setUserInfo((prev) => ({ ...prev, nombre: editForm.nombre, correo_electronico: editForm.correo_electronico, departamento: editForm.departamento }));
      setIsEditing(false);
      showToast("Perfil actualizado correctamente");
    } catch (error) {
      showToast(error.message || "Error al actualizar el perfil", "error");
    } finally {
      setIsLoading(false);
    }
  };

  const handleSave = () => {
    if (!validateForm()) { showToast("Corregí los errores del formulario", "error"); return; }

    const cambios = [];
    if (editForm.nombre !== userInfo.nombre)
      cambios.push(`Nombre: "${userInfo.nombre}" → "${editForm.nombre}"`);
    if (editForm.correo_electronico !== userInfo.correo_electronico)
      cambios.push(`Correo: "${userInfo.correo_electronico}" → "${editForm.correo_electronico}"`);
    if (editForm.departamento !== userInfo.departamento)
      cambios.push(`Departamento: "${userInfo.departamento}" → "${editForm.departamento}"`);

    const mensaje = cambios.length === 0
      ? "No se detectaron cambios. ¿Guardás de todas formas?"
      : `Se guardarán los siguientes cambios:\n\n• ${cambios.join("\n• ")}`;

    askConfirm("¿Guardar cambios en el perfil?", mensaje, ejecutarGuardado);
  };

  const handleInputChange = (field, value) => {
    setEditForm((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: "" }));
  };

  const handleCancel = () => {
    setIsEditing(false);
    setErrors({});
    setEditForm({ nombre: userInfo.nombre, correo_electronico: userInfo.correo_electronico, departamento: userInfo.departamento });
  };

  const handleLogout = async () => {
    try { await apiFetch("/api/auth/logout", { method: "POST" }); }
    catch (error) { console.error("Error al cerrar sesión:", error); }
    finally { window.location.href = "/login"; }
  };

  return (
    <div className="profile-page content-header">
      <div className="breadcrumb"><Link to="/">Inicio</Link> / Mi Perfil</div>
      <div className="content-title">Mi Perfil</div>

      <div className="profile-content">
        <div className="profile-card">
          {isLoading && !userInfo.nombre ? (
            <div className="profile-loading">
              <i className="fas fa-spinner fa-spin profile-loading-icon"></i>
              <p className="profile-loading-text">Cargando información del perfil...</p>
            </div>
          ) : (
            <>
              <div className="profile-header">
                <div className="profile-avatar"><i className="fas fa-user-circle"></i></div>
                <div className="profile-basic">
                  <h2>{userInfo.nombre}</h2>
                  <p>{userInfo.departamento}</p>
                </div>
              </div>

              <div className="profile-details">
                {/* Información personal */}
                <div className="detail-section">
                  <h3>Información personal</h3>
                  <div className="detail-grid detail-grid-3col">
                    <div className={`detail-item ${isEditing ? "editable" : ""}`}>
                      <label>Nombre Completo</label>
                      {isEditing ? (
                        <div>
                          <input type="text" value={editForm.nombre}
                            onChange={(e) => handleInputChange("nombre", e.target.value)}
                            className={errors.nombre ? "error" : ""} disabled={isLoading}
                            placeholder="Ingrese su nombre completo" />
                          {errors.nombre && <span className="error-message">{errors.nombre}</span>}
                        </div>
                      ) : <span>{userInfo.nombre}</span>}
                    </div>
                    <div className="detail-item readonly">
                      <label>Cédula</label>
                      <span>{userInfo.cedula}</span>
                      {isEditing && <small className="detail-item-readonly-hint">Este campo no se puede modificar</small>}
                    </div>
                    <div className={`detail-item ${isEditing ? "editable" : ""}`}>
                      <label>Correo Electrónico</label>
                      {isEditing ? (
                        <div>
                          <input type="email" value={editForm.correo_electronico}
                            onChange={(e) => handleInputChange("correo_electronico", e.target.value)}
                            className={errors.correo_electronico ? "error" : ""} disabled={isLoading}
                            placeholder="usuario@ejemplo.com" />
                          {errors.correo_electronico && <span className="error-message">{errors.correo_electronico}</span>}
                        </div>
                      ) : <span>{userInfo.correo_electronico}</span>}
                    </div>
                  </div>
                </div>

                {/* Información de cuenta */}
                <div className="detail-section">
                  <h3>Información de la cuenta</h3>
                  <div className="detail-grid detail-grid-3col">
                    <div className="detail-item readonly">
                      <label>Rol</label>
                      <span>
                        <i className={`fas ${obtenerIconoRol(userInfo.idRol)} me-1`}></i>
                        {formatearRol(userInfo.idRol)}
                      </span>
                      {isEditing && <small className="detail-item-readonly-hint">Este campo no se puede modificar</small>}
                    </div>
                    <div className="detail-item">
                      <label>Estado</label>
                      <span>{userInfo.estado ? "Activo" : "Inactivo"}</span>
                    </div>
                    <div className="detail-item">
                      <label>Fecha de Creación</label>
                      <span>{userInfo.fechaCreacion ? new Date(userInfo.fechaCreacion).toLocaleString() : ""}</span>
                    </div>
                    <div className="detail-item">
                      <label>Último Acceso</label>
                      <span>{userInfo.ultimoAcceso ? new Date(userInfo.ultimoAcceso).toLocaleString() : ""}</span>
                    </div>
                  </div>
                </div>

                {/* Información laboral */}
                <div className="detail-section">
                  <h3>Información laboral</h3>
                  <div className="detail-grid">
                    <div className={`detail-item ${isEditing ? "editable" : ""}`}>
                      <label>Departamento</label>
                      {isEditing ? (
                        <div>
                          <input type="text" value={editForm.departamento}
                            onChange={(e) => handleInputChange("departamento", e.target.value)}
                            className={errors.departamento ? "error" : ""} disabled={isLoading}
                            placeholder="Ingrese su departamento" />
                          {errors.departamento && <span className="error-message">{errors.departamento}</span>}
                        </div>
                      ) : <span>{userInfo.departamento}</span>}
                    </div>
                  </div>
                </div>

                {/* Acciones */}
                <div className="detail-section">
                  <h3>Acciones</h3>
                  <div className="profile-actions">
                    {!isEditing ? (
                      <button className="btn btn-secondary" onClick={handleEdit} disabled={isLoading}>
                        <i className="fas fa-edit"></i> Editar Perfil
                      </button>
                    ) : (
                      <>
                        <button className="btn btn-primary" onClick={handleSave} disabled={isLoading}>
                          {isLoading
                            ? <><i className="fas fa-spinner fa-spin"></i> Guardando...</>
                            : <><i className="fas fa-save"></i> Guardar Cambios</>}
                        </button>
                        <button className="btn btn-danger" onClick={handleCancel} disabled={isLoading}>
                          <i className="fas fa-times"></i> Cancelar
                        </button>
                      </>
                    )}
                    <button className="btn btn-secondary" onClick={() => setShowCambiarContrasena(true)} disabled={isEditing}>
                      <i className="fas fa-key"></i> Cambiar Contraseña
                    </button>
                    <button
                      className={userInfo.twoFactorEnabled ? "btn btn-success" : "btn btn-secondary"}
                      onClick={handleConfigurarTwoFactor} disabled={isEditing}
                    >
                      <i className="fas fa-shield-alt"></i> {userInfo.twoFactorEnabled ? "Desactivar 2FA" : "Activar 2FA"}
                    </button>
                    <button className="btn btn-danger" onClick={handleLogout} disabled={isEditing}>
                      <i className="fas fa-sign-out-alt"></i> Cerrar sesión
                    </button>
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      </div>

      <CambiarContrasena isOpen={showCambiarContrasena} onClose={() => setShowCambiarContrasena(false)} />
      <Toast toast={toast} onClose={hideToast} />
      <ConfirmDialog confirm={confirm} onClose={closeConfirm} />
    </div>
  );
};

export default Profile;