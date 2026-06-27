import { useEffect, useMemo, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import jsPDF from "jspdf";
import autoTable from "jspdf-autotable";
import { getActividadesEnRevision, getReporteSeguimientoPendientes } from "./api";
import "../styles/MisActividades.css";
import { ActividadIconBox } from "../components/ui/TreeIcons";

const normalizarEstado = (estado) =>
  String(estado || "").trim().toLowerCase().replace(/_/g, " ");

const formatFecha = (fecha) => {
  if (!fecha) return "-";
  return new Date(fecha).toLocaleDateString("es-ES", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
};

const formatFechaHora = (fecha) => {
  if (!fecha) return "-";
  return new Date(fecha).toLocaleString("es-ES", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const normalizarActividad = (a) => ({
  idActividad: a?.id ?? a?.idActividad ?? a?.IdActividad ?? null,
  nombre: a?.nombre ?? a?.Nombre ?? "",
  estadoImplementacion:
    a?.estadoImplementacion ?? a?.estado_implementacion ?? a?.EstadoImplementacion ?? "",
  porcentajeAvance:
    Number(a?.porcentajeAvance ?? a?.porcentaje_avance ?? a?.PorcentajeAvance ?? 0) || 0,
  fechaCompromiso: a?.fechaCompromiso ?? a?.fecha_compromiso ?? a?.FechaCompromiso ?? null,
  subdominioId:
    a?.subdominioId ?? a?.SubdominioId ?? a?.subdominio?.id ?? a?.Subdominio?.Id ?? null,
  subdominioNombre:
    a?.subdominioNombre ??
    a?.SubdominioNombre ??
    a?.subdominio?.practicas_gobierno ??
    a?.subdominio?.practicasGobierno ??
    "Sin subdominio",
  responsableNombre:
    a?.funcionariosResponsablesNombre ??
    a?.funcionarios_responsables_nombre ??
    a?.ResponsableNombre ??
    "Sin responsable",
});

const normalizarTareaPendiente = (t) => ({
  tipoAccionPendiente: Array.isArray(t?.tipoAccionPendiente)
    ? t.tipoAccionPendiente.filter(Boolean)
    : typeof t?.tipoAccionPendiente === "string" && t.tipoAccionPendiente.trim().length > 0
      ? [t.tipoAccionPendiente.trim()]
      : [],
  actividad: t?.actividad ?? "Sin actividad",
  procesoRelacionado: t?.procesoRelacionado ?? "Sin proceso",
  fechaVencidaOProxima: t?.fechaVencidaOProxima ?? "Sin fecha compromiso",
  fechaCompromiso: t?.fechaCompromiso ?? null,
});

const normalizarUsuarioReporte = (u) => ({
  usuario: {
    id: u?.usuario?.id ?? 0,
    nombreCompleto: u?.usuario?.nombreCompleto ?? "Sin responsable",
    cedula: u?.usuario?.cedula ?? "",
    correoElectronico: u?.usuario?.correoElectronico ?? "",
  },
  totalTareasPendientes: Number(u?.totalTareasPendientes ?? 0) || 0,
  tareas: (Array.isArray(u?.tareas) ? u.tareas : []).map(normalizarTareaPendiente),
});

const normalizarReporte = (data) => ({
  generadoEn: data?.generadoEn ?? null,
  totalUsuarios: Number(data?.totalUsuarios ?? 0) || 0,
  totalTareasPendientes: Number(data?.totalTareasPendientes ?? 0) || 0,
  usuarios: (Array.isArray(data?.usuarios) ? data.usuarios : []).map(normalizarUsuarioReporte),
});

const cargarLogoInstitucional = () =>
  new Promise((resolve) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = () => resolve(null);
    img.src = "/images/MuniLogo_principal.png";
  });

function BadgeEstado({ estado }) {
  const map = {
    "En Revisión": "badge-estado--pendiente",
    "En Revision": "badge-estado--pendiente",
    "En_Revision": "badge-estado--pendiente",
  };
  const clase = map[estado] ?? "badge-estado--default";
  return <span className={`badge-estado ${clase}`}>{estado || "-"}</span>;
}

function BarraAvance({ porcentaje }) {
  const pct = Math.min(100, Math.max(0, Number(porcentaje) || 0));
  return (
    <div className="barra-avance-wrap">
      <div className="barra-avance-track">
        <div
          className="barra-avance-fill"
          style={{ width: `${pct}%`, background: "#f59e0b" }}
        />
      </div>
      <span className="barra-avance-pct">{pct}%</span>
    </div>
  );
}

export default function ActividadesEnRevision() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actividades, setActividades] = useState([]);
  const [errorReporte, setErrorReporte] = useState("");
  const [loadingReporte, setLoadingReporte] = useState(false);
  const [reporteGenerado, setReporteGenerado] = useState(false);
  const [reporte, setReporte] = useState({
    generadoEn: null,
    totalUsuarios: 0,
    totalTareasPendientes: 0,
    usuarios: [],
  });
  const [filtrosReporte, setFiltrosReporte] = useState({
    incluirPorcentajeSinActualizar: true,
    incluirEstadoIncompleto: true,
    incluirFechaControlSinAsignar: true,
  });



  useEffect(() => {
    let mounted = true;

    (async () => {
      try {
        setLoading(true);
        setError("");
        const dataActividades = await getActividadesEnRevision();
        if (!mounted) return;

        const listaActividades = (Array.isArray(dataActividades) ? dataActividades : [])
          .map(normalizarActividad)
          .filter((a) => {
            const estado = normalizarEstado(a.estadoImplementacion);
            return estado === "en revisión" || estado === "en revision";
          });

        setActividades(listaActividades);
      } catch (e) {
        setError(e.message || "Error cargando la revisión de actividades");
      } finally {
        if (mounted) setLoading(false);
      }
    })();

    return () => {
      mounted = false;
    };
  }, []);

  const totalActividades = actividades.length;
  const totalTareasReporte = reporte.totalTareasPendientes;

  const actividadesOrdenadas = useMemo(() => {
    return [...actividades].sort((a, b) => {
      const af = a.fechaCompromiso ? new Date(a.fechaCompromiso).getTime() : Number.MAX_SAFE_INTEGER;
      const bf = b.fechaCompromiso ? new Date(b.fechaCompromiso).getTime() : Number.MAX_SAFE_INTEGER;
      if (af !== bf) return af - bf;
      return (a.nombre || "").localeCompare(b.nombre || "", "es", { sensitivity: "base" });
    });
  }, [actividades]);

  const descripcionFiltros = useMemo(() => {
    const etiquetas = [];
    if (filtrosReporte.incluirPorcentajeSinActualizar) {
      etiquetas.push("Porcentaje de avance sin actualizar");
    }
    if (filtrosReporte.incluirEstadoIncompleto) {
      etiquetas.push("Estado de implementacion incompleta");
    }
    if (filtrosReporte.incluirFechaControlSinAsignar) {
      etiquetas.push("Fecha de control sin asignar");
    }
    return etiquetas.length > 0 ? etiquetas.join(" | ") : "Sin filtros";
  }, [filtrosReporte]);

  const resumenUsuariosOrdenado = useMemo(() => {
    return [...reporte.usuarios].sort((a, b) => {
      if (b.totalTareasPendientes !== a.totalTareasPendientes) {
        return b.totalTareasPendientes - a.totalTareasPendientes;
      }
      return (a.usuario.nombreCompleto || "").localeCompare(b.usuario.nombreCompleto || "", "es", {
        sensitivity: "base",
      });
    });
  }, [reporte.usuarios]);

  const filasDetalleReporte = useMemo(() => {
    return reporte.usuarios.flatMap((u) =>
      (u.tareas || []).map((t, idx) => ({
        usuarioId: u.usuario.id,
        usuarioNombre: u.usuario.nombreCompleto,
        usuarioCedula: u.usuario.cedula,
        usuarioCorreo: u.usuario.correoElectronico,
        totalUsuario: u.totalTareasPendientes,
        tipoAccionPendiente: (t.tipoAccionPendiente || []).join(" | "),
        actividad: t.actividad,
        procesoRelacionado: t.procesoRelacionado,
        fechaVencidaOProxima: t.fechaVencidaOProxima,
        ordenLocal: idx,
      })),
    );
  }, [reporte.usuarios]);

  const cambiarFiltroReporte = (clave) => {
    setFiltrosReporte((prev) => ({ ...prev, [clave]: !prev[clave] }));
  };



  const generarReporte = async () => {
    setErrorReporte("");

    const hayFiltrosSeleccionados = Object.values(filtrosReporte).some(Boolean);
    if (!hayFiltrosSeleccionados) {
      setErrorReporte("Selecciona al menos un tipo de tarea para generar el reporte.");
      return;
    }

    try {
      setLoadingReporte(true);
      const data = await getReporteSeguimientoPendientes(filtrosReporte);
      setReporte(normalizarReporte(data));
      setReporteGenerado(true);
    } catch (e) {
      setErrorReporte(e.message || "Error generando el reporte de seguimiento");
    } finally {
      setLoadingReporte(false);
    }
  };

  const exportarReporteXlsx = async () => {
    if (!filasDetalleReporte.length) {
      setErrorReporte("Genera el reporte antes de exportar.");
      return;
    }

    const ExcelJS = (await import("exceljs")).default;
    const wb = new ExcelJS.Workbook();
    wb.creator = "Sistema MICITT";
    wb.created = new Date();
    const ws = wb.addWorksheet("Seguimiento");

    const HEADER_FILL = { type: "pattern", pattern: "solid", fgColor: { argb: "FF1E40AF" } };
    const HEADER_FONT = { bold: true, color: { argb: "FFFFFFFF" }, size: 11 };

    const fechaGeneracion = formatFechaHora(reporte.generadoEn || new Date());

    // ── Metadatos ──
    const tituloRow = ws.addRow(["Reporte de seguimiento de tareas pendientes"]);
    tituloRow.getCell(1).font = { bold: true, size: 13 };
    ws.addRow(["Fecha de generación", fechaGeneracion]);
    ws.addRow(["Filtros aplicados", descripcionFiltros]);
    ws.addRow([]);

    // ── Encabezados ──
    const headRow = ws.addRow([
      "Nombre completo",
      "Cédula",
      "Correo electrónico",
      "Total pendientes por usuario",
      "Tipo de acción pendiente",
      "Actividad",
      "Proceso relacionado",
      "Fecha vencida o próxima",
    ]);
    headRow.eachCell((cell) => {
      cell.font = HEADER_FONT;
      cell.fill = HEADER_FILL;
      cell.alignment = { vertical: "middle", horizontal: "center", wrapText: true };
    });
    headRow.height = 32;

    // ── Filas de datos ──
    filasDetalleReporte.forEach((f) => {
      const row = ws.addRow([
        f.usuarioNombre || "-",
        f.usuarioCedula || "-",
        f.usuarioCorreo || "-",
        f.totalUsuario,
        f.tipoAccionPendiente || "-",
        f.actividad || "-",
        f.procesoRelacionado || "-",
        f.fechaVencidaOProxima || "-",
      ]);
      row.height = 22;
      row.eachCell((cell) => {
        cell.alignment = { vertical: "middle", wrapText: false };
      });
    });

    // ── Anchos de columna ──
    ws.columns = [
      { width: 30 },
      { width: 18 },
      { width: 34 },
      { width: 14 },
      { width: 38 },
      { width: 34 },
      { width: 36 },
      { width: 26 },
    ];

    ws.views = [{ state: "frozen", ySplit: 5 }];

    // ── Descargar ──
    const buffer = await wb.xlsx.writeBuffer();
    const blob = new Blob([buffer], {
      type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `reporte-seguimiento-${new Date().toISOString().slice(0, 10)}.xlsx`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const exportarReportePdf = async () => {
    if (!filasDetalleReporte.length) {
      setErrorReporte("Genera el reporte antes de exportar.");
      return;
    }

    const doc = new jsPDF("landscape");
    const fechaGeneracion = formatFechaHora(reporte.generadoEn || new Date());
    const logo = await cargarLogoInstitucional();

    if (logo) {
      doc.addImage(logo, "PNG", 14, 8, 34, 16);
    }

    doc.setFontSize(14);
    doc.text("Reporte de seguimiento de tareas pendientes", 52, 14);
    doc.setFontSize(10);
    doc.text(`Fecha de generacion: ${fechaGeneracion}`, 52, 20);
    doc.text(`Filtros aplicados: ${descripcionFiltros}`, 14, 29);

    const headers = [
      "Nombre completo",
      "Cedula",
      "Correo electronico",
      "Total",
      "Tipo de accion pendiente",
      "Actividad",
      "Proceso relacionado",
      "Fecha vencida o proxima",
    ];

    const body = filasDetalleReporte.map((f) => [
      f.usuarioNombre || "-",
      f.usuarioCedula || "-",
      f.usuarioCorreo || "-",
      String(f.totalUsuario),
      f.tipoAccionPendiente || "-",
      f.actividad || "-",
      f.procesoRelacionado || "-",
      f.fechaVencidaOProxima || "-",
    ]);

    autoTable(doc, {
      startY: 34,
      margin: { left: 10, right: 10, bottom: 12 },
      head: [headers],
      body,
      styles: { fontSize: 7, cellPadding: 2, valign: "middle" },
      headStyles: {
        fillColor: [30, 64, 175],
        textColor: [255, 255, 255],
        fontStyle: "bold",
      },
      didDrawPage: (data) => {
        const currentPage = doc.internal.getCurrentPageInfo().pageNumber;
        const pageCount = doc.internal.getNumberOfPages();
        doc.setFontSize(9);
        doc.text(
          `Pagina ${currentPage} de ${pageCount}`,
          data.settings.margin.left,
          doc.internal.pageSize.height - 6,
        );
      },
    });

    const fechaArchivo = new Date().toISOString().slice(0, 10);
    doc.save(`reporte-seguimiento-${fechaArchivo}.pdf`);
  };

  return (
    <div className="mis-actividades-container">
      <nav className="act-breadcrumb" style={{ marginBottom: '0.75rem' }}>
        <Link to="/">Inicio</Link>
        <span className="sep">›</span>
        <span className="current">Revisión de actividades</span>
      </nav>
      <div className="mis-actividades-header">
        <h2>
          <ActividadIconBox size={32} />
          Revisión de actividades
        </h2>
        <p>
          Actividades que requieren aprobación y generación manual del reporte de tareas
          pendientes por usuario.
        </p>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      {loading ? (
        <div className="actividades-loading">
          <i className="fas fa-spinner fa-spin"></i>
          Cargando revisión de actividades...
        </div>
      ) : (
        <>
          {/* ── Tarjetas resumen ── */}
          <div className="mis-actividades-resumen">
            <div className="resumen-card resumen-card--pendiente">
              <i className="fas fa-user-shield resumen-icon"></i>
              <div className="resumen-numero">{totalActividades}</div>
              <div className="resumen-label">Requieren aprobación</div>
            </div>
          </div>

          {/* ── Tabla de actividades que requieren aprobación ── */}
          <div className="actividades-seccion">
            <div className="actividades-seccion-header actividades-seccion-header--pendiente">
              <i className="fas fa-clipboard-check"></i>
              Actividades que requieren aprobación
              {totalActividades > 0 && (
                <span className="seccion-badge seccion-badge--pendiente">{totalActividades}</span>
              )}
            </div>

            {totalActividades === 0 ? (
              <div className="actividades-empty">
                <i className="fas fa-check-circle"></i>
                No hay actividades pendientes de aceptación.
              </div>
            ) : (
              <div className="actividades-tabla-scroll">
                <table className="actividades-tabla">
                  <thead>
                    <tr>
                      <th>Actividad</th>
                      <th>Subdominio</th>
                      <th>Responsable</th>
                      <th>Estado</th>
                      <th>Avance</th>
                      <th>Fecha compromiso</th>
                      <th>Acción</th>
                    </tr>
                  </thead>
                  <tbody>
                    {actividadesOrdenadas.map((a) => {
                      const puedeAbrir = a.subdominioId != null && a.idActividad != null;
                      return (
                        <tr key={`${a.subdominioId ?? "na"}-${a.idActividad ?? a.nombre}`}>
                          <td>
                            <div className="nombre-actividad">{a.nombre || "-"}</div>
                          </td>
                          <td className="col-subdominio">{a.subdominioNombre || "-"}</td>
                          <td>{a.responsableNombre || "-"}</td>
                          <td>
                            <BadgeEstado estado={a.estadoImplementacion} />
                          </td>
                          <td className="col-avance">
                            <BarraAvance porcentaje={a.porcentajeAvance} />
                          </td>
                          <td className="col-fecha">{formatFecha(a.fechaCompromiso)}</td>
                          <td>
                            <button
                              className="btn-accion-ver"
                              disabled={!puedeAbrir}
                              onClick={() =>
                                navigate(
                                  `/subdominios/${a.subdominioId}/actividades/${a.idActividad}/editar`,
                                )
                              }
                            >
                              <i className="fas fa-check me-1"></i>Revisar
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
        </>
      )}
    </div>
  );
}