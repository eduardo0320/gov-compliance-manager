import { useState } from "react";
import { apiFetch } from "../services/apiClient";

/** Decodifica el payload del JWT y devuelve exp en milisegundos.
 *  Maneja base64url y padding para evitar errores de atob.
 */
const getExpMsFromJwt = (token) => {
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return null;
    let payloadB64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    // padding
    while (payloadB64.length % 4 !== 0) payloadB64 += "=";
    const json = JSON.parse(atob(payloadB64));
    if (typeof json.exp === "number") return json.exp * 1000; // exp en segundos -> ms
  } catch {}
  return null;
};

export default function Login() {
  const [cedula, setCedula] = useState("");
  const [contrasena, setContrasena] = useState("");
  const [showRecovery, setShowRecovery] = useState(false);
  const [step, setStep] = useState(1);
  // Step 1
  const [recoveryCedula, setRecoveryCedula] = useState("");
  // Step 2
  const [codigo, setCodigo] = useState("");
  const [newPass, setNewPass] = useState("");
  const [newPass2, setNewPass2] = useState("");
  const [loginError, setLoginError] = useState("");
  const [recoveryMsg, setRecoveryMsg] = useState("");
  const [isTwoFactorStep, setIsTwoFactorStep] = useState(false);
  const [twoFactorCodigo, setTwoFactorCodigo] = useState("");
  const [twoFactorMsg, setTwoFactorMsg] = useState("");
  const [twoFactorCedula, setTwoFactorCedula] = useState("");

  const handleLogin = async (e) => {
    e.preventDefault();
    setLoginError("");
    
    try {
      const response = await apiFetch(`/api/auth/login`, { 
        method: "POST",
        credentials: "include", 
        headers: { 
          "Content-Type": "application/json" 
        },
        body: JSON.stringify({ cedula, contrasena }),
      });

      if (!response.ok) {
        let errorMessage = "Cédula o contraseña incorrecta.";
        try {
          const errorData = await response.json(); // ✅ Mejor parsear JSON
          errorMessage = errorData.message || errorMessage;
        } catch {
          // Si no es JSON, usar mensaje por defecto
        }
        setLoginError(errorMessage);
        return;
      }

      const data = await response.json();
      
      // HU-009: Verificar si se requiere cambio de contraseña
      if (data.mensaje === "CAMBIO_CONTRASENA_REQUERIDO" || data.message === "CAMBIO_CONTRASENA_REQUERIDO") {  
        sessionStorage.setItem('sesionActiva', '1');
        window.location.href = "/cambiar-contrasena";
        return;
      }

      // Marcar sesión activa (para detectar cierre de navegador en Layout)
      sessionStorage.setItem('sesionActiva', '1');
      
      // HU-2FA: Verificación de segundo factor requerida
      if (data.mensaje === "2FA_REQUERIDO") {
        setIsTwoFactorStep(true);
        setTwoFactorCedula(cedula);
        setTwoFactorMsg("Se ha enviado un código de 2 dígitos al correo registrado. Ingréselo para continuar.");
        return;
      }

      // Marcar sesión activa (para detectar cierre de navegador en Layout)
      sessionStorage.setItem('sesionActiva', '1');
      // Guardar info del usuario en localStorage para que getCurrentUserInfo() funcione
      if (data.user) {
        // Normalizar el campo rol (el backend devuelve 'nombreRol')
        const userToSave = { ...data.user, rol: data.user.rol ?? data.user.nombreRol ?? '' };
        localStorage.setItem('usuario', JSON.stringify(userToSave));
      }
      // Redirigir al dashboard
      window.location.href = "/";
      
    } catch (error) {
      console.error("Error de autenticación:", error);
      setLoginError("Error de conexión con el servidor.");
    }
  };

  const handleTwoFactorConfirm = async (e) => {
    e.preventDefault();
    setTwoFactorMsg("");

    if (!twoFactorCodigo.trim()) {
      setTwoFactorMsg("❌ Por favor ingrese el código que le llegó al correo.");
      return;
    }

    try {
      const res = await apiFetch(`/api/auth/2fa/confirmar`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ cedula: twoFactorCedula, codigo: twoFactorCodigo }),
      });

      const payload = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(payload.mensaje || "Código 2FA inválido");

      setTwoFactorMsg("✅ 2FA verificado. Redirigiendo...");
      // Guardar info del usuario en localStorage
      if (payload.user) {
        const userToSave = { ...payload.user, rol: payload.user.rol ?? payload.user.nombreRol ?? '' };
        localStorage.setItem('usuario', JSON.stringify(userToSave));
      }
      setTimeout(() => {
        window.location.href = "/";
      }, 1200);
    } catch (err) {
      setTwoFactorMsg(`❌ ${err.message}`);
    }
  };

  const handleRecoveryRequest = async (e) => {
    e.preventDefault();
    setRecoveryMsg("");
    
    if (!recoveryCedula.trim()) {
      setRecoveryMsg("❌ Por favor ingrese su cédula.");
      return;
    }

    try {
      const res = await apiFetch(`/api/auth/recuperacion/solicitar`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ cedula: recoveryCedula }),
      });
      const payload = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(payload.mensaje || "No se pudo procesar la solicitud.");
      setRecoveryMsg(`✅ ${payload.mensaje}`);
      setStep(2);
    } catch (err) {
      setRecoveryMsg(`❌ ${err.message}`);
    }
  };




  const handleRecoveryConfirm = async (e) => {
    e.preventDefault();
    setRecoveryMsg("");

    if (newPass !== newPass2) {
      setRecoveryMsg("❌ Las contraseñas no coinciden.");
      return;
    }
    const policy = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;
    if (!policy.test(newPass)) {
      setRecoveryMsg("❌ La nueva contraseña no cumple con los requisitos (mínimo 8 caracteres, mayúscula, minúscula, número y símbolo).");
      return;
    }

    try {
      const res = await apiFetch(`/api/auth/recuperacion/confirmar`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          cedula: recoveryCedula,
          codigo,
          nuevaContrasena: newPass,
        }),
      });
      const payload = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(payload.mensaje || "No se pudo cambiar la contraseña.");
      setRecoveryMsg(`✅ ${payload.mensaje}`);
      setTimeout(() => {
        setShowRecovery(false);
        setStep(1);
        setRecoveryCedula("");
        setCodigo(""); setNewPass(""); setNewPass2("");
        setRecoveryMsg("");
      }, 2000);
    } catch (err) {
      setRecoveryMsg(`❌ ${err.message}`);
    }
  };

  return (
    <div className="login-container">
      {!showRecovery ? (
        isTwoFactorStep ? (
          <div className="login-box">
            <img src="/images/logo_miccit.png" alt="Logo_login" className="login-logo" />
            <h2>Verificación en dos pasos (2FA)</h2>
            <form className="login-form" onSubmit={handleTwoFactorConfirm}>
              <input
                type="text"
                value={twoFactorCodigo}
                onChange={e => setTwoFactorCodigo(e.target.value)}
                placeholder="Código 2D enviado al correo"
                required
              />
              <button type="submit">Validar código</button>
            </form>
            <div className="twofactor-msg">{twoFactorMsg}</div>
            <div className="login-links">
              <a href="#" onClick={(e) => { e.preventDefault(); setIsTwoFactorStep(false); setLoginError(''); setTwoFactorMsg(''); setCedula(''); setContrasena(''); }}>Volver</a>
            </div>
          </div>
        ) : (
          <div className="login-box">
            <img src="/images/logo_miccit.png" alt="Logo_dashboard" className="login-logo" />
            <h2>Iniciar Sesión</h2>
            <form className="login-form" onSubmit={handleLogin}>
              <input type="text" value={cedula} onChange={e => setCedula(e.target.value)} placeholder="Cédula" />
              <input type="password" value={contrasena} onChange={e => setContrasena(e.target.value)} placeholder="Contraseña" />
              <button type="submit">Ingresar</button>
            </form>
            <div className="login-links">
              <a href="#" onClick={(e) => { e.preventDefault(); setShowRecovery(true); setStep(1); }}>¿Olvidó su contraseña?</a>
            </div>

            {loginError && <div className="login-error">{loginError}</div>}
          </div>
        )
      ) : (
        <div className="login-box">
          <img src="/images/logo_miccit.png" alt="Logo_dashboard" className="login-logo" />
          <h2>Recuperar Contraseña</h2>

          {step === 1 ? (
            <form className="login-form" onSubmit={handleRecoveryRequest}>
              <input 
                type="text" 
                value={recoveryCedula} 
                onChange={e => setRecoveryCedula(e.target.value)} 
                placeholder="Ingrese su cédula" 
                required 
              />
              <p className="login-hint">
                Se enviará un código de verificación al correo electrónico registrado en el sistema.
              </p>
              <button type="submit">Enviar código</button>
            </form>
          ) : (
            <form className="login-form" onSubmit={handleRecoveryConfirm}>
              <input type="text" value={codigo} onChange={e => setCodigo(e.target.value)} placeholder="Código recibido por correo" required />
              <input type="password" value={newPass} onChange={e => setNewPass(e.target.value)} placeholder="Nueva contraseña" required />
              <input type="password" value={newPass2} onChange={e => setNewPass2(e.target.value)} placeholder="Confirmar contraseña" required />
              <button type="submit">Cambiar contraseña</button>
            </form>
          )}

          <div className="login-links">
            <a href="#" onClick={(e) => {e.preventDefault(); setShowRecovery(false);}}>Volver al inicio de sesión</a>
          </div>
          {recoveryMsg && <div className="login-success">{recoveryMsg}</div>}
        </div>
      )}
    </div>
  );
}