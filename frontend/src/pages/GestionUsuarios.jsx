import React, { useEffect, useState, useCallback, useRef } from "react";
import { Link } from "react-router-dom";
import Toast, { useToast } from "../components/ui/Toast";
import ResultDialog, { useDialog } from "../components/ui/ResultDialog";
import { ConfirmDialog, useConfirm } from "../components/ui/ResultDialog";
import { apiFetch } from "../services/apiClient";
import jsPDF from "jspdf";
import autoTable from "jspdf-autotable";


const GestionUsuarios = ({ rolUsuario }) => {
  if (rolUsuario !== "ADMIN" && rolUsuario !== "SUPERADMIN") {
    return (
      <div className="acceso-denegado">
        <i className="fas fa-lock acceso-denegado__icon"></i>
        <h2 className="acceso-denegado__titulo">Acceso Denegado</h2>
        <p className="acceso-denegado__desc">
          No tiene permisos para acceder a esta página. <br />
          Solo los administradores pueden gestionar usuarios.
        </p>
      </div>
    );
  }

  const [usuarios, setUsuarios] = useState([]);
  const [cantidad, setCantidad] = useState(0);
  const [cargandoUsuarios, setCargandoUsuarios] = useState(true);



  const { toast, showToast, hideToast } = useToast();
  const { dialog, showDialog, closeDialog } = useDialog();
  const { confirm: confirmDialog, askConfirm, closeConfirm } = useConfirm();
  const temporizadorSalidaRef = useRef(null);

  const [tipoFiltro, setTipoFiltro] = useState("nombre");
  const [valorFiltro, setValorFiltro] = useState("");
  const [filtrosAplicados, setFiltrosAplicados] = useState({
    nombre: "", cedula: "", rol: "", estado: "", departamento: "", fechaCreacion: ""
  });

  const [mostrarModalEdicion, setMostrarModalEdicion] = useState(false);
  const [usuarioEditando, setUsuarioEditando] = useState(null);
  const [formEdicion, setFormEdicion] = useState({
    cedula: "", nombre: "", correo_electronico: "", departamento: "", idRol: ""
  });
  // Errores de campo para el formulario de edición
  const [erroresEdicion, setErroresEdicion] = useState({});

  const [mostrarModalRegistro, setMostrarModalRegistro] = useState(false);
  const [roles, setRoles] = useState([]);
  const [form, setForm] = useState({
    nombre: "", cedula: "", correo_electronico: "", departamento: "", idRol: "",
  });
  // Errores de campo para el formulario de registro
  const [erroresRegistro, setErroresRegistro] = useState({});
  const [submittingRegistro, setSubmittingRegistro] = useState(false);

  /* ─── utilidades ─── */

  const formatearFecha = (fecha) => {
    if (!fecha) return <span className="fecha-vacia">Sin fecha</span>;
    const date = new Date(fecha);
    if (isNaN(date.getTime())) return <span className="fecha-vacia">Sin fecha</span>;
    return (
      <div className="fecha-bloque">
        <div>{date.toLocaleDateString("es-ES", { day: "2-digit", month: "2-digit", year: "numeric" })}</div>
        <div className="fecha-hora">{date.toLocaleTimeString("es-ES", { hour: "2-digit", minute: "2-digit" })}</div>
      </div>
    );
  };

  function validarCedulaCR(cedula) {
    return /^\d{9}$/.test(cedula);
  }

  /* ─── carga de roles ─── */

  useEffect(() => {
    if (rolUsuario !== "ADMIN" && rolUsuario !== "SUPERADMIN") return;
    (async () => {
      try {
        const res = await apiFetch(`/api/roles`);
        if (!res.ok) { setRoles([]); return; }
        const data = await res.json();
        setRoles(Array.isArray(data) ? data : []);
      } catch { setRoles([]); }
    })();
  }, [rolUsuario]);

  /* ─── carga de usuarios ─── */

  const cargarUsuarios = useCallback(() => {
    if (rolUsuario !== "ADMIN" && rolUsuario !== "SUPERADMIN") return;
    setCargandoUsuarios(true);
    apiFetch(`/api/usuarios/filtrar`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        ...filtrosAplicados,
        fechaCreacion: filtrosAplicados.fechaCreacion || null,
      }),
    })
      .then((res) => res.json())
      .then((data) => {
        setUsuarios(data.usuarios || []);
        setCantidad(data.cantidad || 0);
        setCargandoUsuarios(false);
      })
      .catch(() => setCargandoUsuarios(false));
  }, [rolUsuario, filtrosAplicados]);

  useEffect(() => { cargarUsuarios(); }, [cargarUsuarios]);

  /* ─── toast (solo para errores no asociados a un campo) ─── */

  const mostrarMensajeTemporal = (texto, esError = false) => showToast(texto, esError ? "error" : "success");

  /* ─── filtros ─── */

  const aplicarFiltro = () => {
    const nuevosFiltros = { nombre: "", cedula: "", rol: "", estado: "", departamento: "", fechaCreacion: "" };
    nuevosFiltros[tipoFiltro] = valorFiltro;
    setFiltrosAplicados(nuevosFiltros);
  };

  const limpiarFiltros = () => {
    setValorFiltro("");
    setTipoFiltro("nombre");
    setFiltrosAplicados({ nombre: "", cedula: "", rol: "", estado: "", departamento: "", fechaCreacion: "" });
  };

  /* ─── refresco ─── */

  const refrescarUsuarios = async () => {
    setCargandoUsuarios(true);
    try {
      const lista = await apiFetch(`/api/usuarios`).then((r) => r.json());
      setUsuarios(lista?.usuarios || []);
    } finally { setCargandoUsuarios(false); }
  };

  /* ─── validación de registro ─── */

  const validarFormRegistro = () => {
    const errores = {};
    if (form.nombre.trim().split(/\s+/).length < 3)
      errores.nombre = "Ingrese nombre y dos apellidos.";
    if (!validarCedulaCR(form.cedula))
      errores.cedula = "Debe tener exactamente 9 dígitos, sin guiones.";
    if (!/^[\w.+-]+@[\w-]+(\.[\w-]+)+$/.test(form.correo_electronico.trim()))
      errores.correo_electronico = "Correo electrónico inválido.";
    if (!form.departamento.trim())
      errores.departamento = "El departamento es obligatorio.";
    if (!form.idRol)
      errores.idRol = "Seleccione un rol.";
    return errores;
  };

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    // Limpiar el error del campo al escribir
    if (erroresRegistro[e.target.name])
      setErroresRegistro({ ...erroresRegistro, [e.target.name]: "" });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const errores = validarFormRegistro();
    if (Object.keys(errores).length > 0) { setErroresRegistro(errores); return; }
    if (submittingRegistro) return;
    setErroresRegistro({});
    setSubmittingRegistro(true);
    try {
      const res = await apiFetch(`/api/usuarios/registrar`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });
      const contentType = res.headers.get("content-type") || "";
      let payload;
      if (contentType.includes("application/json")) payload = await res.json();
      else { const text = await res.text(); throw new Error(text || "Respuesta no JSON del servidor"); }
      if (!res.ok) throw new Error(payload.mensaje || "Error en el registro");
      setForm({ nombre: "", cedula: "", correo_electronico: "", departamento: "", idRol: "" });
      setMostrarModalRegistro(false);
      showDialog("¡Usuario registrado!", payload.mensaje || "El usuario ha sido registrado exitosamente.");
      refrescarUsuarios();
    } catch (err) {
      mostrarMensajeTemporal("❌ " + err.message, true);
    } finally {
      setSubmittingRegistro(false);
    }
  };

  /* ─── estado de usuario ─── */

  const cambiarEstado = async (cedula, nombre, estadoActual) => {
    const advertencia = estadoActual === "Activo"
      ? `¿Está seguro de que desea desactivar a este usuario?\nEsta acción bloqueará su acceso.`
      : `¿Está seguro de que desea activar a este usuario?`;
    askConfirm(
      estadoActual === "Activo" ? "¿Desactivar usuario?" : "¿Activar usuario?",
      `${advertencia}

Usuario: ${nombre}`,
      async () => {
        try {
          const respuesta = await apiFetch(`/api/usuarios/${cedula}/estado`, {
            method: "PUT", credentials: "include", headers: { "Content-Type": "application/json" },
          });
          if (!respuesta.ok) {
            const errData = await respuesta.json().catch(() => ({}));
            mostrarMensajeTemporal("❌ " + (errData.mensaje || "No se puede cambiar el estado del usuario actual."), true);
            return;
          }
          const data = await respuesta.json();
          mostrarMensajeTemporal(data.mensaje || "Estado cambiado exitosamente", false);
          cargarUsuarios();
        } catch {
          mostrarMensajeTemporal("No se pudo cambiar el estado del usuario.", true);
        }
      }
    );
    return;
  };

  /* ─── edición ─── */

  const abrirModalEdicion = (usuario) => {
    setUsuarioEditando(usuario);
    setFormEdicion({
      cedula: usuario.cedula || "",
      nombre: usuario.nombre_completo || "",
      correo_electronico: usuario.correo_electronico || "",
      departamento: usuario.departamento || "",
      idRol: usuario.idRol || "",
    });
    setErroresEdicion({});
    setMostrarModalEdicion(true);
  };

  const cerrarModalEdicion = () => {
    setMostrarModalEdicion(false);
    setUsuarioEditando(null);
    setFormEdicion({ cedula: "", nombre: "", correo_electronico: "", departamento: "", idRol: "" });
    setErroresEdicion({});
  };

  const handleEdicionChange = (e) => {
    setFormEdicion({ ...formEdicion, [e.target.name]: e.target.value });
    if (erroresEdicion[e.target.name])
      setErroresEdicion({ ...erroresEdicion, [e.target.name]: "" });
  };

  const validarFormEdicion = () => {
    const errores = {};
    if (!validarCedulaCR(formEdicion.cedula))
      errores.cedula = "Debe tener exactamente 9 dígitos, sin guiones.";
    if (formEdicion.nombre.trim().split(/\s+/).length < 3)
      errores.nombre = "Ingrese nombre y dos apellidos.";
    if (!/^[\w.+-]+@[\w-]+(\.[\w-]+)+$/.test(formEdicion.correo_electronico.trim()))
      errores.correo_electronico = "Correo electrónico inválido.";
    if (!formEdicion.departamento.trim())
      errores.departamento = "El departamento es obligatorio.";
    if (!formEdicion.idRol)
      errores.idRol = "Seleccione un rol.";
    return errores;
  };

  const editarInformacion = async (e) => {
    e.preventDefault();
    if (!usuarioEditando) return;
    const errores = validarFormEdicion();
    if (Object.keys(errores).length > 0) { setErroresEdicion(errores); return; }
    setErroresEdicion({});
    try {
      const usuarioActualizado = {
        cedula: formEdicion.cedula.trim(),
        nombre: formEdicion.nombre.trim(),
        correo_electronico: formEdicion.correo_electronico.trim(),
        departamento: formEdicion.departamento.trim(),
        idRol: formEdicion.idRol,
      };
      const res = await apiFetch(`/api/usuarios/editarUsuario/${usuarioEditando.cedula}`, {
        method: "PUT", credentials: "include", headers: { "Content-Type": "application/json" },
        body: JSON.stringify(usuarioActualizado),
      });
      if (!res.ok) { const errorData = await res.json(); throw new Error(errorData.mensaje || "Error al editar información"); }
      const data = await res.json();
      mostrarMensajeTemporal("✅ " + (data.mensaje || "Usuario actualizado exitosamente"), false);
      setUsuarios((prev) => prev.map((u) => u.cedula === usuarioEditando.cedula
        ? { ...u, cedula: usuarioActualizado.cedula, nombre_completo: usuarioActualizado.nombre, correo_electronico: usuarioActualizado.correo_electronico, departamento: usuarioActualizado.departamento, idRol: usuarioActualizado.idRol }
        : u
      ));
      cerrarModalEdicion();
      await refrescarUsuarios();
    } catch (error) {
      mostrarMensajeTemporal("❌ " + error.message, true);
    }
  };

  /* ─── exportar PDF ─── */

  const exportarPDF = () => {
    const doc = new jsPDF("landscape");
    const logoImg = new Image();
    logoImg.onload = function () {
      doc.addImage(logoImg, "PNG", 14, 10, 40, 20);
      autoTable(doc, {
        startY: 40,
        margin: { left: 25, right: 25, top: 40, bottom: 20 },
        tableWidth: "auto",
        head: [["Nombre Completo", "Cédula", "Correo", "Departamento", "Rol", "Estado", "Fecha Creación"]],
        body: usuarios.map((u) => [
          u.nombre_completo, u.cedula, u.correo_electronico, u.departamento, u.rol_asignado, u.estado,
          u.fechaCreacion ? new Date(u.fechaCreacion).toLocaleDateString("es-ES") : "Sin fecha",
        ]),
        styles: { fontSize: 8, cellPadding: 3, halign: "center", valign: "middle" },
        headStyles: { fillColor: [41, 128, 185], textColor: [255, 255, 255], fontStyle: "bold", fontSize: 8, halign: "center" },
        columnStyles: {
          0: { cellWidth: 50, halign: "left" }, 1: { cellWidth: 25, halign: "center" },
          2: { cellWidth: 65, halign: "left" }, 3: { cellWidth: 30, halign: "center" },
          4: { cellWidth: 30, halign: "center" }, 5: { cellWidth: 25, halign: "center" },
          6: { cellWidth: 30, halign: "center" },
        },
        theme: "grid",
        didDrawPage: (data) => {
          const currentPage = doc.internal.getCurrentPageInfo().pageNumber;
          const pageCount = doc.internal.getNumberOfPages();
          doc.setFontSize(10);
          doc.text(`Página ${currentPage} de ${pageCount}`, doc.internal.pageSize.width - 35, doc.internal.pageSize.height - 5);
        },
      });
      doc.save("usuarios_registrados.pdf");
    };
    logoImg.src = "/images/logo_miccit.png";
  };

  /* ─── render ─── */

  return (
    <div className="gu-wrapper">

      <nav className="act-breadcrumb" style={{ marginBottom: '0.75rem' }}>
        <Link to="/">Inicio</Link>
        <span className="sep">›</span>
        <span className="current">Gestión de Usuarios</span>
      </nav>

      {/* Filtros */}
      <div className="gu-filtros">
        <div className="gu-filtros__fila">
          <div className="gu-filtros__grupo">
            <label className="gu-filtros__label">Filtrar por:</label>
            <select
              className="gu-filtros__select"
              value={tipoFiltro}
              onChange={(e) => { setTipoFiltro(e.target.value); setValorFiltro(""); }}
            >
              <option value="nombre">Nombre</option>
              <option value="cedula">Cédula</option>
              <option value="rol">Rol</option>
              <option value="estado">Estado</option>
              <option value="departamento">Departamento</option>
              <option value="fechaCreacion">Fecha de Creación</option>
            </select>
          </div>

          <div className="gu-filtros__grupo gu-filtros__grupo--grow">
            <label className="gu-filtros__label">Buscar:</label>
            {tipoFiltro === "rol" ? (
              <select className="gu-filtros__select" value={valorFiltro} onChange={(e) => setValorFiltro(e.target.value)}>
                <option value="">Seleccione un rol</option>
                <option value="ADMIN">ADMIN</option>
                <option value="SUPERADMIN">SUPERADMIN</option>
                <option value="EDITOR">EDITOR</option>
              </select>
            ) : tipoFiltro === "estado" ? (
              <select className="gu-filtros__select" value={valorFiltro} onChange={(e) => setValorFiltro(e.target.value)}>
                <option value="">Seleccione un estado</option>
                <option value="Activo">Activo</option>
                <option value="Inactivo">Inactivo</option>
              </select>
            ) : tipoFiltro === "fechaCreacion" ? (
              <input className="gu-filtros__input" type="date" value={valorFiltro} onChange={(e) => setValorFiltro(e.target.value)} />
            ) : (
              <input
                className="gu-filtros__input"
                type="text"
                value={valorFiltro}
                onChange={(e) => setValorFiltro(e.target.value)}
                onKeyPress={(e) => e.key === "Enter" && aplicarFiltro()}
                placeholder={
                  tipoFiltro === "nombre" ? "Ingrese el nombre" :
                  tipoFiltro === "cedula" ? "Ingrese la cédula" :
                  tipoFiltro === "departamento" ? "Ingrese el departamento" : "Ingrese el valor"
                }
              />
            )}
          </div>

          <div className="gu-filtros__acciones">
            <button className="gu-btn gu-btn--buscar" onClick={aplicarFiltro}>
              <i className="fas fa-search"></i> Buscar
            </button>
            <button className="gu-btn gu-btn--limpiar" onClick={limpiarFiltros}>
              <i className="fas fa-times"></i> Limpiar
            </button>
          </div>
        </div>

        {Object.values(filtrosAplicados).some((v) => v) && (
          <div className="gu-filtros__indicador">
            <i className="fas fa-filter"></i>
            <strong>Filtro activo:</strong>{" "}
            {{ nombre: "Nombre", cedula: "Cédula", rol: "Rol", estado: "Estado", departamento: "Departamento", fechaCreacion: "Fecha" }[tipoFiltro]}
            {" "}= "{filtrosAplicados[tipoFiltro] || valorFiltro}"
          </div>
        )}
      </div>

      {/* Tabla */}
      <div className="gu-tabla-wrapper">
        <div className="gu-tabla__contador">
          <span>Cantidad de usuarios: <strong>{cantidad}</strong></span>
        </div>

        <div className="gu-tabla__scroll">
          <table className="gu-tabla">
            <colgroup>
              <col className="gu-col-1" />
              <col className="gu-col-2" />
              <col className="gu-col-3" />
              <col className="gu-col-4" />
              <col className="gu-col-5" />
              <col className="gu-col-6" />
              <col className="gu-col-4" />
              <col className="gu-col-2" />
            </colgroup>
            <thead className="gu-tabla__thead">
              <tr>
                <th className="gu-tabla__th">Nombre Completo</th>
                <th className="gu-tabla__th">Cédula</th>
                <th className="gu-tabla__th">Correo Electrónico</th>
                <th className="gu-tabla__th">Departamento</th>
                <th className="gu-tabla__th">Rol Asignado</th>
                <th className="gu-tabla__th">Estado</th>
                <th className="gu-tabla__th">Fecha Creación</th>
                <th className="gu-tabla__th gu-tabla__th--center">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {cargandoUsuarios ? (
                <tr><td colSpan={8} className="gu-tabla__msg">Cargando usuarios...</td></tr>
              ) : usuarios.length === 0 ? (
                <tr><td colSpan={8} className="gu-tabla__msg">No hay usuarios registrados.</td></tr>
              ) : (
                usuarios.map((u, i) => (
                  <tr key={i} className={`gu-tabla__tr ${i % 2 === 0 ? "gu-tabla__tr--par" : ""}`}>
                    <td className="gu-tabla__td gu-tabla__td--wrap">{u.nombre_completo}</td>
                    <td className="gu-tabla__td gu-tabla__td--nowrap">{u.cedula}</td>
                    <td className="gu-tabla__td gu-tabla__td--wrap">{u.correo_electronico}</td>
                    <td className="gu-tabla__td gu-tabla__td--wrap">{u.departamento}</td>
                    <td className="gu-tabla__td gu-tabla__td--wrap">{u.rol_asignado}</td>
                    <td className="gu-tabla__td gu-tabla__td--nowrap">{u.estado}</td>
                    <td className="gu-tabla__td gu-tabla__td--nowrap">{formatearFecha(u.fechaCreacion)}</td>
                    <td className="gu-tabla__td gu-tabla__td--acciones">
                      <button
                        className={`gu-btn-accion ${u.estado === "Activo" ? "gu-btn-accion--desactivar" : "gu-btn-accion--activar"}`}
                        onClick={() => cambiarEstado(u.cedula, u.nombre_completo, u.estado)}
                      >
                        {u.estado === "Activo" ? "Desactivar" : "Activar"}
                      </button>
                      <button className="gu-btn-accion gu-btn-accion--editar" onClick={() => abrirModalEdicion(u)}>
                        Editar
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="gu-tabla__pie">
          <button className="gu-btn gu-btn--registrar" onClick={() => setMostrarModalRegistro(true)}>
            <i className="fas fa-user-plus"></i> Registrar Nuevo Usuario
          </button>
          <button className="gu-btn gu-btn--pdf" onClick={exportarPDF}>
            <i className="fas fa-file-pdf"></i> Exportar a PDF
          </button>
        </div>
      </div>

      {/* ── Modal de Registro ── */}
      {mostrarModalRegistro && (
        <div className="reg-overlay">
          <div className="reg-modal">
            <div className="reg-header">
              <div className="reg-header-icon"><i className="fas fa-user-plus"></i></div>
              <div>
                <h2 className="reg-title">Nuevo Usuario</h2>
                <p className="reg-subtitle">Complete los datos para registrar la cuenta</p>
              </div>
              <button className="reg-close" onClick={() => { setMostrarModalRegistro(false); setForm({ nombre: "", cedula: "", correo_electronico: "", departamento: "", idRol: "" }); setErroresRegistro({}); }}>
                <i className="fas fa-times"></i>
              </button>
            </div>
            <div className="reg-divider"></div>

            <form onSubmit={handleSubmit} className="reg-body" noValidate>
              {/* Nombre */}
              <div className="reg-field reg-field-full">
                <label className="reg-label">
                  <i className="fas fa-id-card reg-label-icon"></i>
                  Nombre completo <span className="reg-required">*</span>
                </label>
                <input
                  name="nombre"
                  value={form.nombre}
                  onChange={handleChange}
                  placeholder="(ej: Juan Carlos Rodríguez Mora)"
                  className={`reg-input${erroresRegistro.nombre ? " reg-input--error" : ""}`}
                  autoComplete="off"
                />
                {erroresRegistro.nombre && <span className="reg-campo-error">{erroresRegistro.nombre}</span>}
              </div>

              {/* Cédula */}
              <div className="reg-field">
                <label className="reg-label">
                  <i className="fas fa-fingerprint reg-label-icon"></i>
                  Cédula <span className="reg-required">*</span>
                </label>
                <input
                  name="cedula"
                  value={form.cedula}
                  onChange={handleChange}
                  placeholder="9 dígitos sin espacios"
                  className={`reg-input${erroresRegistro.cedula ? " reg-input--error" : ""}`}
                  autoComplete="off"
                />
                {erroresRegistro.cedula && <span className="reg-campo-error">{erroresRegistro.cedula}</span>}
              </div>

              {/* Correo */}
              <div className="reg-field">
                <label className="reg-label">
                  <i className="fas fa-envelope reg-label-icon"></i>
                  Correo electrónico <span className="reg-required">*</span>
                </label>
                <input
                  name="correo_electronico"
                  value={form.correo_electronico}
                  onChange={handleChange}
                  placeholder="correo@dominio.com"
                  type="email"
                  className={`reg-input${erroresRegistro.correo_electronico ? " reg-input--error" : ""}`}
                  autoComplete="off"
                />
                {erroresRegistro.correo_electronico && <span className="reg-campo-error">{erroresRegistro.correo_electronico}</span>}
              </div>

              {/* Departamento */}
              <div className="reg-field">
                <label className="reg-label">
                  <i className="fas fa-building reg-label-icon"></i>
                  Departamento <span className="reg-required">*</span>
                </label>
                <input
                  name="departamento"
                  value={form.departamento}
                  onChange={handleChange}
                  placeholder="Nombre del departamento"
                  className={`reg-input${erroresRegistro.departamento ? " reg-input--error" : ""}`}
                  autoComplete="off"
                />
                {erroresRegistro.departamento && <span className="reg-campo-error">{erroresRegistro.departamento}</span>}
              </div>

              {/* Rol */}
              <div className="reg-field">
                <label className="reg-label">
                  <i className="fas fa-shield-alt reg-label-icon"></i>
                  Rol <span className="reg-required">*</span>
                </label>
                <select
                  name="idRol"
                  value={form.idRol}
                  onChange={handleChange}
                  className={`reg-input reg-select${erroresRegistro.idRol ? " reg-input--error" : ""}`}
                >
                  <option value="">Seleccione un rol</option>
                  {roles.map((r) => (
                    <option key={r.idRol} value={r.idRol}>{r.nombre}</option>
                  ))}
                </select>
                {erroresRegistro.idRol && <span className="reg-campo-error">{erroresRegistro.idRol}</span>}
              </div>

              <div className="reg-footer">
                <button
                  type="button"
                  className="reg-btn-cancel"
                  onClick={() => { setMostrarModalRegistro(false); setForm({ nombre: "", cedula: "", correo_electronico: "", departamento: "", idRol: "" }); setErroresRegistro({}); }}
                >
                  Cancelar
                </button>
                <button type="submit" className="reg-btn-submit" disabled={submittingRegistro}>
                  <i className="fas fa-check"></i> {submittingRegistro ? "Registrando..." : "Registrar usuario"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── Modal de Edición ── */}
      {mostrarModalEdicion && (
        <div className="ed-overlay">
          <div className="ed-modal">
            <div className="ed-header">
              <h2 className="ed-titulo">Editar Usuario</h2>
              <button className="ed-cerrar" onClick={cerrarModalEdicion}>×</button>
            </div>

            <form onSubmit={editarInformacion} className="ed-form" noValidate>
              {/* Cédula */}
              <div className="ed-campo">
                <label className="ed-label">Cédula *</label>
                <input
                  type="text"
                  name="cedula"
                  value={formEdicion.cedula}
                  onChange={handleEdicionChange}
                  placeholder="9 dígitos sin guiones ni espacios"
                  maxLength="9"
                  className={`ed-input${erroresEdicion.cedula ? " ed-input--error" : ""}`}
                />
                {erroresEdicion.cedula && <span className="ed-campo-error">{erroresEdicion.cedula}</span>}
              </div>

              {/* Nombre */}
              <div className="ed-campo">
                <label className="ed-label">Nombre Completo *</label>
                <input
                  type="text"
                  name="nombre"
                  value={formEdicion.nombre}
                  onChange={handleEdicionChange}
                  placeholder="Nombre y dos apellidos"
                  className={`ed-input${erroresEdicion.nombre ? " ed-input--error" : ""}`}
                />
                {erroresEdicion.nombre && <span className="ed-campo-error">{erroresEdicion.nombre}</span>}
              </div>

              {/* Correo */}
              <div className="ed-campo">
                <label className="ed-label">Correo Electrónico *</label>
                <input
                  type="email"
                  name="correo_electronico"
                  value={formEdicion.correo_electronico}
                  onChange={handleEdicionChange}
                  placeholder="ejemplo@correo.com"
                  className={`ed-input${erroresEdicion.correo_electronico ? " ed-input--error" : ""}`}
                />
                {erroresEdicion.correo_electronico && <span className="ed-campo-error">{erroresEdicion.correo_electronico}</span>}
              </div>

              {/* Departamento */}
              <div className="ed-campo">
                <label className="ed-label">Departamento *</label>
                <input
                  type="text"
                  name="departamento"
                  value={formEdicion.departamento}
                  onChange={handleEdicionChange}
                  placeholder="Departamento"
                  className={`ed-input${erroresEdicion.departamento ? " ed-input--error" : ""}`}
                />
                {erroresEdicion.departamento && <span className="ed-campo-error">{erroresEdicion.departamento}</span>}
              </div>

              {/* Rol */}
              <div className="ed-campo">
                <label className="ed-label">Rol *</label>
                <select
                  name="idRol"
                  value={formEdicion.idRol}
                  onChange={handleEdicionChange}
                  className={`ed-input ed-select${erroresEdicion.idRol ? " ed-input--error" : ""}`}
                >
                  <option value="">Seleccionar rol</option>
                  {roles.map((r) => (
                    <option key={r.idRol} value={r.idRol}>{r.nombre}</option>
                  ))}
                </select>
                {erroresEdicion.idRol && <span className="ed-campo-error">{erroresEdicion.idRol}</span>}
              </div>

              <div className="ed-footer">
                <button type="button" className="ed-btn-cancel" onClick={cerrarModalEdicion}>Cancelar</button>
                <button type="submit" className="ed-btn-submit">Guardar Cambios</button>
              </div>
            </form>
          </div>
        </div>
      )}

      <ResultDialog dialog={dialog} onClose={closeDialog} />
      <ConfirmDialog confirm={confirmDialog} onClose={closeConfirm} />

      <Toast toast={toast} onClose={hideToast} />
    </div>
  );
};

export default GestionUsuarios;