import React, { useState } from 'react';
import { cambiarContrasena, restablecerContrasenaObligatoria } from '../services';

const validarRequisitos = (contrasena) => ({
  tieneMinimo8:   contrasena.length >= 8,
  tieneMayuscula: /[A-Z]/.test(contrasena),
  tieneMinuscula: /[a-z]/.test(contrasena),
  tieneNumero:    /[0-9]/.test(contrasena),
  tieneSimbolo:   /[@$!%*?&]/.test(contrasena),
});

// ── Modo página: primer login con contraseña temporal ──────────────────────
function PaginaCambiarContrasena() {
  const [formData, setFormData] = useState({ nuevaContrasena: '', confirmarContrasena: '' });
  const [mensaje, setMensaje] = useState({ texto: '', tipo: '' });
  const [mostrar, setMostrar] = useState({ nueva: false, confirmar: false });
  const [cargando, setCargando] = useState(false);

  const handleChange = (e) => setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
  const toggleMostrar = (campo) => setMostrar(prev => ({ ...prev, [campo]: !prev[campo] }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nuevaContrasena || !formData.confirmarContrasena) {
      setMensaje({ texto: 'Todos los campos son requeridos.', tipo: 'error' }); return;
    }
    if (formData.nuevaContrasena !== formData.confirmarContrasena) {
      setMensaje({ texto: 'Las contraseñas no coinciden.', tipo: 'error' }); return;
    }
    if (!Object.values(validarRequisitos(formData.nuevaContrasena)).every(Boolean)) {
      setMensaje({ texto: 'La contraseña no cumple con todos los requisitos.', tipo: 'error' }); return;
    }
    setCargando(true);
    setMensaje({ texto: '', tipo: '' });
    try {
      await restablecerContrasenaObligatoria(formData.nuevaContrasena);
      setMensaje({ texto: '¡Contraseña establecida correctamente! Redirigiendo...', tipo: 'success' });
      setTimeout(() => { window.location.href = '/'; }, 1500);
    } catch (err) {
      setMensaje({ texto: err.message || 'Error al cambiar la contraseña.', tipo: 'error' });
    } finally {
      setCargando(false);
    }
  };

  const req = validarRequisitos(formData.nuevaContrasena);

  return (
    <div className="login-container">
      <div className="login-box" style={{ maxWidth: 420 }}>
        <img src="/images/logo_miccit.png" alt="Logo" className="login-logo" />
        <h2 style={{ marginBottom: 6 }}>Cambio de contraseña obligatorio</h2>
        <p style={{ fontSize: 13, color: '#666', marginBottom: 20, textAlign: 'center' }}>
          Por seguridad debe establecer una contraseña personal antes de continuar.
        </p>

        {mensaje.texto && (
          <div className={`act-alert ${mensaje.tipo === 'success' ? 'success' : 'danger'}`} style={{ marginBottom: 14 }}>
            {mensaje.texto}
          </div>
        )}

        <form className="login-form" onSubmit={handleSubmit} style={{ gap: 14 }}>
          <div className="password-form-group">
            <label className="password-form-label">Nueva contraseña</label>
            <div className="password-input-container">
              <input type={mostrar.nueva ? 'text' : 'password'} name="nuevaContrasena"
                value={formData.nuevaContrasena} onChange={handleChange}
                className="password-form-input" placeholder="Nueva contraseña" autoComplete="new-password" />
              <span className="eye-toggle-button" onClick={() => toggleMostrar('nueva')}>
                <i className={`fas ${mostrar.nueva ? 'fa-eye-slash' : 'fa-eye'}`}></i>
              </span>
            </div>
            {formData.nuevaContrasena && (
              <div className="password-requirements" style={{ marginTop: 8 }}>
                <p className="requirements-title">Requisitos:</p>
                <div className="requirements-list">
                  {[
                    [req.tieneMinimo8,   'Mínimo 8 caracteres'],
                    [req.tieneMayuscula, 'Al menos una mayúscula'],
                    [req.tieneMinuscula, 'Al menos una minúscula'],
                    [req.tieneNumero,    'Al menos un número'],
                    [req.tieneSimbolo,   'Al menos un carácter especial (@$!%*?&)'],
                  ].map(([ok, label]) => (
                    <div key={label} className={`requirement-item ${ok ? 'valid' : 'invalid'}`}>
                      <i className={`fas ${ok ? 'fa-check' : 'fa-times'}`}></i>
                      <span>{label}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          <div className="password-form-group">
            <label className="password-form-label">Confirmar nueva contraseña</label>
            <div className="password-input-container">
              <input type={mostrar.confirmar ? 'text' : 'password'} name="confirmarContrasena"
                value={formData.confirmarContrasena} onChange={handleChange}
                className="password-form-input" placeholder="Repetir contraseña" autoComplete="new-password" />
              <span className="eye-toggle-button" onClick={() => toggleMostrar('confirmar')}>
                <i className={`fas ${mostrar.confirmar ? 'fa-eye-slash' : 'fa-eye'}`}></i>
              </span>
            </div>
            {formData.confirmarContrasena && (
              <div className={`password-confirm-msg ${formData.nuevaContrasena === formData.confirmarContrasena ? 'password-confirm-msg--ok' : 'password-confirm-msg--err'}`}>
                <i className={`fas ${formData.nuevaContrasena === formData.confirmarContrasena ? 'fa-check' : 'fa-times'}`}></i>
                {formData.nuevaContrasena === formData.confirmarContrasena ? ' Las contraseñas coinciden' : ' Las contraseñas no coinciden'}
              </div>
            )}
          </div>

          <button type="submit" disabled={cargando} style={{ marginTop: 4 }}>
            {cargando ? 'Cambiando...' : 'Establecer nueva contraseña'}
          </button>
        </form>
      </div>
    </div>
  );
}

// ── Modo modal: desde el perfil ────────────────────────────────────────────
function ModalCambiarContrasena({ isOpen, onClose }) {
  const [formData, setFormData] = useState({
    contrasenaActual: '',
    nuevaContrasena: '',
    confirmarContrasena: '',
  });
  const [mensaje, setMensaje] = useState({ texto: '', tipo: '' });
  const [mostrar, setMostrar] = useState({ actual: false, nueva: false, confirmar: false });
  const [cargando, setCargando] = useState(false);

  const handleChange = (e) => setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
  const toggleMostrar = (campo) => setMostrar(prev => ({ ...prev, [campo]: !prev[campo] }));

  const mostrarMensaje = (texto, tipo) => {
    setMensaje({ texto, tipo });
    setTimeout(() => setMensaje({ texto: '', tipo: '' }), 5000);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.contrasenaActual || !formData.nuevaContrasena || !formData.confirmarContrasena) {
      mostrarMensaje('Todos los campos son requeridos.', 'error'); return;
    }
    setCargando(true);
    try {
      const data = await cambiarContrasena({
        ContrasenaActual:    formData.contrasenaActual,
        NuevaContrasena:     formData.nuevaContrasena,
        ConfirmarContrasena: formData.confirmarContrasena,
      });
      mostrarMensaje(data?.mensaje || 'Contraseña cambiada exitosamente.', 'success');
      setFormData({ contrasenaActual: '', nuevaContrasena: '', confirmarContrasena: '' });
    } catch (err) {
      mostrarMensaje(err.message || 'Error al cambiar la contraseña.', 'error');
    } finally {
      setCargando(false);
    }
  };

  if (!isOpen) return null;

  const req = validarRequisitos(formData.nuevaContrasena);

  return (
    <div className="password-modal-overlay">
      <div className="password-modal">
        <div className="password-modal-header">
          <h2 className="password-modal-title">Cambiar Contraseña</h2>
        </div>
        <div className="password-modal-body">
          {mensaje.texto && (
            <div className={`act-alert ${mensaje.tipo === 'success' ? 'success' : 'danger'}`}>
              {mensaje.texto}
            </div>
          )}
          <form onSubmit={handleSubmit}>
            <div className="password-form-group">
              <label className="password-form-label">Contraseña actual</label>
              <div className="password-input-container">
                <input type={mostrar.actual ? 'text' : 'password'} name="contrasenaActual"
                  value={formData.contrasenaActual} onChange={handleChange} className="password-form-input" />
                <span className="eye-toggle-button" onClick={() => toggleMostrar('actual')}>
                  <i className={`fas ${mostrar.actual ? 'fa-eye-slash' : 'fa-eye'}`}></i>
                </span>
              </div>
            </div>

            <div className="password-form-group">
              <label className="password-form-label">Nueva contraseña</label>
              <div className="password-input-container">
                <input type={mostrar.nueva ? 'text' : 'password'} name="nuevaContrasena"
                  value={formData.nuevaContrasena} onChange={handleChange} className="password-form-input" />
                <span className="eye-toggle-button" onClick={() => toggleMostrar('nueva')}>
                  <i className={`fas ${mostrar.nueva ? 'fa-eye-slash' : 'fa-eye'}`}></i>
                </span>
              </div>
              <div className="password-requirements">
                <p className="requirements-title">Requisitos de contraseña:</p>
                <div className="requirements-list">
                  {[
                    [req.tieneMinimo8,   'Mínimo 8 caracteres'],
                    [req.tieneMayuscula, 'Al menos una letra mayúscula'],
                    [req.tieneMinuscula, 'Al menos una letra minúscula'],
                    [req.tieneNumero,    'Al menos un número'],
                    [req.tieneSimbolo,   'Al menos un carácter especial (@$!%*?&)'],
                  ].map(([ok, label]) => (
                    <div key={label} className={`requirement-item ${ok ? 'valid' : 'invalid'}`}>
                      <i className={`fas ${ok ? 'fa-check' : 'fa-times'}`}></i>
                      <span>{label}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="password-form-group">
              <label className="password-form-label">Confirmar nueva contraseña</label>
              <div className="password-input-container">
                <input type={mostrar.confirmar ? 'text' : 'password'} name="confirmarContrasena"
                  value={formData.confirmarContrasena} onChange={handleChange} className="password-form-input" />
                <span className="eye-toggle-button" onClick={() => toggleMostrar('confirmar')}>
                  <i className={`fas ${mostrar.confirmar ? 'fa-eye-slash' : 'fa-eye'}`}></i>
                </span>
              </div>
              {formData.confirmarContrasena && (
                <div className={`password-confirm-msg ${formData.nuevaContrasena === formData.confirmarContrasena ? 'password-confirm-msg--ok' : 'password-confirm-msg--err'}`}>
                  <i className={`fas ${formData.nuevaContrasena === formData.confirmarContrasena ? 'fa-check' : 'fa-times'}`}></i>
                  {formData.nuevaContrasena === formData.confirmarContrasena ? ' Las contraseñas coinciden' : ' Las contraseñas no coinciden'}
                </div>
              )}
            </div>

            <div className="password-form-actions">
              <button type="button" onClick={onClose} className="password-btn password-btn-secondary">Cancelar</button>
              <button type="submit" disabled={cargando} className="password-btn password-btn-primary">
                {cargando ? 'Cambiando...' : 'Cambiar Contraseña'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

// Exportación única: detecta el modo según las props recibidas
const CambiarContrasena = (props) => {
  if ('isOpen' in props) return <ModalCambiarContrasena {...props} />;
  return <PaginaCambiarContrasena />;
};

export default CambiarContrasena;