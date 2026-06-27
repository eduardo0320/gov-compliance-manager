import React, { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import jsPDF from "jspdf";
import autoTable from "jspdf-autotable";
import { buscarDocumentos, getDominios, getProcesos } from "../services";
import { getReporteSeguimientoPendientes } from "./api";

// Componente select con búsqueda incorporada
function SelectBuscable({ id, value, onChange, opciones, placeholder }) {
  const [busqueda, setBusqueda] = React.useState("");
  const [abierto, setAbierto] = React.useState(false);
  const ref = React.useRef(null);

  const seleccionado = opciones.find((p) => p.codigo === value);
  const filtradas = opciones.filter(
    (p) =>
      !busqueda ||
      p.codigo.toLowerCase().includes(busqueda.toLowerCase()) ||
      p.nombre.toLowerCase().includes(busqueda.toLowerCase())
  );

  React.useEffect(() => {
    const handleClick = (e) => {
      if (ref.current && !ref.current.contains(e.target)) {
        setAbierto(false);
        setBusqueda("");
      }
    };
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  const seleccionar = (codigo) => {
    onChange(codigo);
    setAbierto(false);
    setBusqueda("");
  };

  return (
    <div ref={ref} style={{ position: "relative" }}>
      <div
        id={id}
        onClick={() => setAbierto((a) => !a)}
        style={{
          height: 42,
          border: "1px solid #d1d5db",
          borderRadius: 8,
          padding: "0 36px 0 12px",
          fontSize: 14,
          color: seleccionado ? "#111827" : "#9ca3af",
          background: "#fff",
          cursor: "pointer",
          display: "flex",
          alignItems: "center",
          userSelect: "none",
          position: "relative",
        }}
      >
        {seleccionado ? `${seleccionado.codigo} — ${seleccionado.nombre}` : placeholder}
        <span style={{ position: "absolute", right: 10, color: "#6b7280", fontSize: 12 }}>▾</span>
      </div>

      {abierto && (
        <div style={{
          position: "absolute",
          top: "calc(100% + 4px)",
          left: 0,
          right: 0,
          background: "#fff",
          border: "1px solid #d1d5db",
          borderRadius: 8,
          boxShadow: "0 4px 16px rgba(0,0,0,0.1)",
          zIndex: 100,
          overflow: "hidden",
        }}>
          <div style={{ padding: "8px 8px 4px" }}>
            <input
              autoFocus
              type="text"
              value={busqueda}
              onChange={(e) => setBusqueda(e.target.value)}
              placeholder="Buscá por código o nombre..."
              style={{
                width: "100%",
                border: "1px solid #d1d5db",
                borderRadius: 6,
                padding: "7px 10px",
                fontSize: 13,
                outline: "none",
                boxSizing: "border-box",
              }}
            />
          </div>
          <ul style={{ maxHeight: 220, overflowY: "auto", margin: 0, padding: "4px 0", listStyle: "none" }}>
            <li
              onClick={() => seleccionar("")}
              style={{
                padding: "8px 14px",
                fontSize: 14,
                cursor: "pointer",
                color: "#6b7280",
                background: value === "" ? "#f0fdf4" : "transparent",
              }}
              onMouseEnter={(e) => e.currentTarget.style.background = "#f9fafb"}
              onMouseLeave={(e) => e.currentTarget.style.background = value === "" ? "#f0fdf4" : "transparent"}
            >
              {placeholder}
            </li>
            {filtradas.map((p) => (
              <li
                key={p.id}
                onClick={() => seleccionar(p.codigo)}
                style={{
                  padding: "8px 14px",
                  fontSize: 14,
                  cursor: "pointer",
                  background: value === p.codigo ? "#f0fdf4" : "transparent",
                  color: "#111827",
                }}
                onMouseEnter={(e) => e.currentTarget.style.background = "#f9fafb"}
                onMouseLeave={(e) => e.currentTarget.style.background = value === p.codigo ? "#f0fdf4" : "transparent"}
              >
                <span style={{ fontWeight: 600, marginRight: 6 }}>{p.codigo}</span>
                <span style={{ color: "#6b7280" }}>{p.nombre}</span>
              </li>
            ))}
            {filtradas.length === 0 && (
              <li style={{ padding: "10px 14px", color: "#9ca3af", fontSize: 13 }}>
                Sin resultados
              </li>
            )}
          </ul>
        </div>
      )}
    </div>
  );
}

const Reportes = ({ rolUsuario }) => {
  if (rolUsuario !== "ADMIN" && rolUsuario !== "SUPERADMIN") {
    return (
      <div className="reports-denied">
        <div className="reports-denied-icon">
          <i className="fas fa-lock"></i>
        </div>
        <h2>Acceso Denegado</h2>
        <p>Solo los administradores pueden acceder al módulo de reportes.</p>
      </div>
    );
  }

  const [fechaDesde, setFechaDesde] = useState("");
  const [fechaHasta, setFechaHasta] = useState("");
  const [codigoProceso, setCodigoProceso] = useState("");
  const [resultados, setResultados] = useState([]);
  const [buscando, setBuscando] = useState(false);
  const [error, setError] = useState("");

  const [dominios, setDominios] = useState([]);
  const [dominioId, setDominioId] = useState("");
  const [procesosDisponibles, setProcesosDisponibles] = useState([]);
  const [resultadosProcesos, setResultadosProcesos] = useState([]);
  const [buscandoProcesos, setBuscandoProcesos] = useState(false);
  const [errorProcesos, setErrorProcesos] = useState("");
  const [reporteSeguimiento, setReporteSeguimiento] = useState({
    generadoEn: null,
    totalUsuarios: 0,
    totalTareasPendientes: 0,
    usuarios: [],
  });
  const [filtrosSeguimiento, setFiltrosSeguimiento] = useState({
    incluirPorcentajeSinActualizar: true,
    incluirEstadoIncompleto: true,
    incluirFechaControlSinAsignar: true,
  });
  const [buscandoSeguimiento, setBuscandoSeguimiento] = useState(false);
  const [errorSeguimiento, setErrorSeguimiento] = useState("");
  const [reporteSeguimientoGenerado, setReporteSeguimientoGenerado] = useState(false);
  const [selectedTab, setSelectedTab] = useState("documentos");

  useEffect(() => {
    const cargarDatos = async () => {
      try {
        const data = await getDominios();
        const dominiosNormalizados = (Array.isArray(data) ? data : []).map((dominio) => ({
          id: dominio.id ?? dominio.idDominio ?? dominio.id_Dominio ?? dominio.Id ?? dominio.IdDominio,
          nombre: dominio.nombre ?? dominio.Nombre ?? "Sin dominio",
        }));
        setDominios(dominiosNormalizados);
      } catch {
        // No bloqueamos el módulo de reportes si falla la carga de dominios.
      }
      try {
        const procs = await getProcesos();
        const normalizados = (Array.isArray(procs) ? procs : []).map((p) => ({
          id: p.idProceso ?? p.id,
          codigo: p.codigo ?? "",
          nombre: p.nombre ?? "",
        }));
        setProcesosDisponibles(normalizados);
      } catch {
        // No bloqueamos el módulo de reportes si falla la carga de procesos.
      }
    };
    cargarDatos();
  }, []);

  const formatearFecha = (valor) => {
    if (!valor) return "Sin fecha";
    const fecha = new Date(valor);
    if (Number.isNaN(fecha.getTime())) return "Sin fecha";
    return fecha.toLocaleDateString("es-CR", { day: "2-digit", month: "2-digit", year: "numeric" });
  };

  const calcularDiasVencido = (fecha) => {
    if (!fecha) return "—";
    const hoy = new Date();
    const vencimiento = new Date(fecha);
    return Math.max(0, Math.floor((hoy - vencimiento) / (1000 * 60 * 60 * 24)));
  };

  const formatearPorcentaje = (valor) => {
    const numero = Number(valor ?? 0);
    if (Number.isNaN(numero)) return "0%";
    return `${numero.toFixed(2).replace(/\.00$/, "")} %`.replace(/\s+%$/, "%");
  };

  const normalizarTareaPendiente = (t) => ({
    tipoAccionPendiente: Array.isArray(t?.tipoAccionPendiente)
      ? t.tipoAccionPendiente.filter(Boolean)
      : typeof t?.tipoAccionPendiente === "string" && t?.tipoAccionPendiente.trim().length > 0
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

  const normalizarReporteSeguimiento = (data) => ({
    generadoEn: data?.generadoEn ?? null,
    totalUsuarios: Number(data?.totalUsuarios ?? 0) || 0,
    totalTareasPendientes: Number(data?.totalTareasPendientes ?? 0) || 0,
    usuarios: (Array.isArray(data?.usuarios) ? data.usuarios : []).map(normalizarUsuarioReporte),
  });

  const descripcionFiltrosSeguimiento = () => {
    const etiquetas = [];
    if (filtrosSeguimiento.incluirPorcentajeSinActualizar) etiquetas.push("Porcentaje de avance sin actualizar");
    if (filtrosSeguimiento.incluirEstadoIncompleto) etiquetas.push("Estado de implementacion incompleta");
    if (filtrosSeguimiento.incluirFechaControlSinAsignar) etiquetas.push("Fecha de control sin asignar");
    return etiquetas.length > 0 ? etiquetas.join(" | ") : "Sin filtros";
  };

  const resumenSeguimientoOrdenado = [...reporteSeguimiento.usuarios].sort((a, b) => {
    if (b.totalTareasPendientes !== a.totalTareasPendientes) {
      return b.totalTareasPendientes - a.totalTareasPendientes;
    }
    return (a.usuario.nombreCompleto || "").localeCompare(b.usuario.nombreCompleto || "", "es", {
      sensitivity: "base",
    });
  });

  const filasSeguimiento = reporteSeguimiento.usuarios.flatMap((u) =>
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

  const handleGenerarReporte = async (event) => {
    event.preventDefault();
    setError("");
    setBuscando(true);
    setResultados([]);

    try {
      const filtros = {
        soloVencidos: true,
        vencimientoDesde: fechaDesde || undefined,
        vencimientoHasta: fechaHasta || undefined,
        codigoProceso: codigoProceso.trim() || undefined,
        limite: 1000,
      };
      const data = await buscarDocumentos(filtros);
      setResultados(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err?.message || "No se pudo generar el reporte.");
    } finally {
      setBuscando(false);
    }
  };

  const handleLimpiar = () => {
    setFechaDesde("");
    setFechaHasta("");
    setCodigoProceso("");
    setResultados([]);
    setError("");
  };

  const handleGenerarReporteProcesos = async (event) => {
    event.preventDefault();
    setErrorProcesos("");
    setBuscandoProcesos(true);
    setResultadosProcesos([]);

    try {
      const data = await getProcesos(dominioId || undefined);
      setResultadosProcesos(Array.isArray(data) ? data : []);
    } catch (err) {
      setErrorProcesos(err?.message || "No se pudo generar el reporte de procesos.");
    } finally {
      setBuscandoProcesos(false);
    }
  };

  const cambiarFiltroSeguimiento = (clave) => {
    setFiltrosSeguimiento((prev) => ({ ...prev, [clave]: !prev[clave] }));
  };

  const generarReporteSeguimiento = async () => {
    setErrorSeguimiento("");

    const hayFiltrosSeleccionados = Object.values(filtrosSeguimiento).some(Boolean);
    if (!hayFiltrosSeleccionados) {
      setErrorSeguimiento("Selecciona al menos un tipo de tarea para generar el reporte.");
      return;
    }

    try {
      setBuscandoSeguimiento(true);
      const data = await getReporteSeguimientoPendientes(filtrosSeguimiento);
      setReporteSeguimiento(normalizarReporteSeguimiento(data));
      setReporteSeguimientoGenerado(true);
    } catch (err) {
      setErrorSeguimiento(err?.message || "No se pudo generar el reporte de seguimiento.");
    } finally {
      setBuscandoSeguimiento(false);
    }
  };

  const exportarSeguimientoXLSX = async () => {
    if (!filasSeguimiento.length) {
      setErrorSeguimiento("Genera el reporte antes de exportar.");
      return;
    }

    const ExcelJS = (await import("exceljs")).default;
    const wb = new ExcelJS.Workbook();
    wb.creator = "Sistema MICITT";
    wb.created = new Date();
    const ws = wb.addWorksheet("Seguimiento");

    const HEADER_FILL = { type: "pattern", pattern: "solid", fgColor: { argb: "FF1E40AF" } };
    const HEADER_FONT = { bold: true, color: { argb: "FFFFFFFF" }, size: 11 };

    ws.addRow(["Reporte de seguimiento de tareas pendientes"]);
    ws.addRow(["Fecha de generación", new Date().toLocaleString("es-CR")]);
    ws.addRow(["Filtros aplicados", descripcionFiltrosSeguimiento()]);
    ws.addRow([]);

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

    filasSeguimiento.forEach((f) => {
      ws.addRow([
        f.usuarioNombre || "-",
        f.usuarioCedula || "-",
        f.usuarioCorreo || "-",
        f.totalUsuario,
        f.tipoAccionPendiente || "-",
        f.actividad || "-",
        f.procesoRelacionado || "-",
        f.fechaVencidaOProxima || "-",
      ]);
    });

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

  const exportarSeguimientoPDF = async () => {
    if (!filasSeguimiento.length) {
      setErrorSeguimiento("Genera el reporte antes de exportar.");
      return;
    }

    const doc = new jsPDF("landscape");
    const logoImg = new Image();
    logoImg.crossOrigin = "anonymous";
    logoImg.onload = () => {
      doc.addImage(logoImg, "PNG", 16, 12, 52, 22);
      doc.setFontSize(14);
      doc.text("Reporte de seguimiento de tareas pendientes", 80, 24);
      doc.setFontSize(10);
      doc.text(`Fecha de generación: ${new Date().toLocaleDateString("es-CR")}`, 80, 40);
      doc.text(`Filtros aplicados: ${descripcionFiltrosSeguimiento()}`, 16, 50);

      autoTable(doc, {
        startY: 58,
        margin: { left: 16, right: 16, top: 10, bottom: 20 },
        head: [[
          "Nombre completo",
          "Cédula",
          "Correo electrónico",
          "Total",
          "Tipo de acción pendiente",
          "Actividad",
          "Proceso relacionado",
          "Fecha vencida o próxima",
        ]],
        body: filasSeguimiento.map((f) => [
          f.usuarioNombre || "-",
          f.usuarioCedula || "-",
          f.usuarioCorreo || "-",
          String(f.totalUsuario),
          f.tipoAccionPendiente || "-",
          f.actividad || "-",
          f.procesoRelacionado || "-",
          f.fechaVencidaOProxima || "-",
        ]),
        styles: { fontSize: 7, cellPadding: 2, valign: "middle" },
        headStyles: {
          fillColor: [37, 99, 235],
          textColor: [255, 255, 255],
          fontStyle: "bold",
        },
        didDrawPage: (data) => {
          const currentPage = doc.internal.getCurrentPageInfo().pageNumber;
          const pageCount = doc.internal.getNumberOfPages();
          doc.setFontSize(9);
          doc.text(`Página ${currentPage} de ${pageCount}`, data.settings.margin.left, doc.internal.pageSize.height - 6);
        },
      });

      doc.save(`reporte-seguimiento-${new Date().toISOString().slice(0, 10)}.pdf`);
    };
    logoImg.src = "/images/MuniLogo_principal.png";
  };

  const handleLimpiarProcesos = () => {
    setDominioId("");
    setResultadosProcesos([]);
    setErrorProcesos("");
  };

  const exportarXLSX = async () => {
    const ExcelJS = (await import("exceljs")).default;
    const workbook = new ExcelJS.Workbook();
    workbook.creator = "Sistema MICITT";
    workbook.created = new Date();

    const worksheet = workbook.addWorksheet("Documentos vencidos");
    worksheet.addRow(["Documento", "Código de Proceso", "Actividad", "Responsable", "Fecha de Vencimiento", "Días vencido", "Estado"]);

    resultados.forEach((doc) => {
      worksheet.addRow([
        doc.nombre,
        doc.procesoCodigo || "—",
        doc.actividadNombre || "—",
        doc.responsableNombre || "—",
        doc.fechaVencimiento ? new Date(doc.fechaVencimiento).toLocaleDateString("es-CR") : "—",
        calcularDiasVencido(doc.fechaVencimiento),
        doc.estado || "—",
      ]);
    });

    worksheet.columns.forEach((column) => {
      column.width = 24;
    });

    worksheet.getRow(1).font = { bold: true };

    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer], {
      type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `documentos-vencidos-${new Date().toISOString().slice(0, 10)}.xlsx`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const exportarPDF = () => {
    const doc = new jsPDF({ orientation: "landscape", unit: "pt", format: "a4" });
    const logoImg = new Image();
    logoImg.crossOrigin = "anonymous";
    logoImg.onload = () => {
      doc.addImage(logoImg, "PNG", 16, 12, 52, 22);
      doc.setFontSize(14);
      doc.text("Reporte de documentos vencidos", 80, 24);
      doc.setFontSize(10);
      doc.text(`Fecha de generación: ${new Date().toLocaleDateString("es-CR")}`, 80, 40);

      autoTable(doc, {
        startY: 52,
        margin: { left: 16, right: 16, top: 10, bottom: 20 },
        head: [[
          "Documento",
          "Código de Proceso",
          "Actividad",
          "Responsable",
          "Vencimiento",
          "Días vencido",
          "Estado",
        ]],
        body: resultados.map((docItem) => [
          docItem.nombre,
          docItem.procesoCodigo || "—",
          docItem.actividadNombre || "—",
          docItem.responsableNombre || "—",
          docItem.fechaVencimiento ? new Date(docItem.fechaVencimiento).toLocaleDateString("es-CR") : "—",
          calcularDiasVencido(docItem.fechaVencimiento),
          docItem.estado || "—",
        ]),
        styles: { fontSize: 8, cellPadding: 3, halign: "center", valign: "middle" },
        headStyles: { fillColor: [37, 99, 235], textColor: [255, 255, 255], fontStyle: "bold" },
        columnStyles: {
          0: { cellWidth: 120, halign: "left" },
          1: { cellWidth: 70 },
          2: { cellWidth: 100, halign: "left" },
          3: { cellWidth: 100, halign: "left" },
          4: { cellWidth: 70 },
          5: { cellWidth: 60 },
          6: { cellWidth: 70 },
        },
        theme: "grid",
        didDrawPage: () => {
          const pageCount = doc.internal.getNumberOfPages();
          doc.setFontSize(10);
          doc.text(`Página ${doc.internal.getCurrentPageInfo().pageNumber} de ${pageCount}`, doc.internal.pageSize.width - 70, doc.internal.pageSize.height - 10);
        },
      });
      doc.save(`documentos-vencidos-${new Date().toISOString().slice(0, 10)}.pdf`);
    };
    logoImg.src = "/images/MuniLogo_principal.png";
  };

  const exportarProcesosXLSX = async () => {
    const ExcelJS = (await import("exceljs")).default;
    const workbook = new ExcelJS.Workbook();
    workbook.creator = "Sistema MICITT";
    workbook.created = new Date();

    const logoResponse = await fetch("/images/MuniLogo_principal.png");
    const logoBuffer = logoResponse.ok ? await logoResponse.arrayBuffer() : null;
    const worksheet = workbook.addWorksheet("Implementación de procesos");

    if (logoBuffer) {
      const logoId = workbook.addImage({ buffer: logoBuffer, extension: "png" });
      worksheet.addImage(logoId, { tl: { col: 0.1, row: 0.1 }, ext: { width: 120, height: 40 } });
    }

    worksheet.mergeCells("A1:E1");
    worksheet.getCell("A1").value = "Reporte de implementación de procesos";
    worksheet.getCell("A1").font = { bold: true, size: 14 };

    worksheet.mergeCells("A2:E2");
    worksheet.getCell("A2").value = `Fecha de generación: ${new Date().toLocaleDateString("es-CR")}`;
    worksheet.getCell("A2").font = { size: 11 };

    worksheet.addRow([]);
    worksheet.addRow(["Código de proceso", "Nombre del proceso", "Implementable", "% de avance", "Fecha de conclusión estimada"]);
    worksheet.getRow(4).font = { bold: true };

    resultadosProcesos.forEach((proceso) => {
      worksheet.addRow([
        proceso.codigo || "—",
        proceso.nombre || "—",
        proceso.estadoImplementacion || "No definido",
        formatearPorcentaje(proceso.porcentajeAvance),
        proceso.fechaConclusionImplementacion ? formatearFecha(proceso.fechaConclusionImplementacion) : "No definido",
      ]);
    });

    worksheet.columns = [
      { width: 24 },
      { width: 40 },
      { width: 20 },
      { width: 16 },
      { width: 26 },
    ];

    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer], {
      type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `implementacion-procesos-${new Date().toISOString().slice(0, 10)}.xlsx`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const exportarProcesosPDF = () => {
    const doc = new jsPDF({ orientation: "landscape", unit: "pt", format: "a4" });
    const logoImg = new Image();
    logoImg.crossOrigin = "anonymous";
    logoImg.onload = () => {
      doc.addImage(logoImg, "PNG", 16, 12, 52, 22);
      doc.setFontSize(14);
      doc.text("Reporte de implementación de procesos", 80, 24);
      doc.setFontSize(10);
      doc.text(`Fecha de generación: ${new Date().toLocaleDateString("es-CR")}`, 80, 40);

      autoTable(doc, {
        startY: 52,
        margin: { left: 16, right: 16, top: 10, bottom: 20 },
        head: [[
          "Código de proceso",
          "Nombre del proceso",
          "Implementable",
          "% de avance",
          "Fecha de conclusión estimada",
        ]],
        body: resultadosProcesos.map((proceso) => [
          proceso.codigo || "—",
          proceso.nombre || "—",
          proceso.estadoImplementacion || "No definido",
          formatearPorcentaje(proceso.porcentajeAvance),
          proceso.fechaConclusionImplementacion ? formatearFecha(proceso.fechaConclusionImplementacion) : "No definido",
        ]),
        styles: { fontSize: 8, cellPadding: 3, halign: "center", valign: "middle" },
        headStyles: { fillColor: [37, 99, 235], textColor: [255, 255, 255], fontStyle: "bold" },
        columnStyles: {
          0: { cellWidth: 100 },
          1: { cellWidth: 180, halign: "left" },
          2: { cellWidth: 80 },
          3: { cellWidth: 70 },
          4: { cellWidth: 120 },
        },
        theme: "grid",
        didDrawPage: () => {
          const pageCount = doc.internal.getNumberOfPages();
          doc.setFontSize(10);
          doc.text(`Página ${doc.internal.getCurrentPageInfo().pageNumber} de ${pageCount}`, doc.internal.pageSize.width - 70, doc.internal.pageSize.height - 10);
        },
      });
      doc.save(`implementacion-procesos-${new Date().toISOString().slice(0, 10)}.pdf`);
    };
    logoImg.src = "/images/MuniLogo_principal.png";
  };

  return (
    <div className="reports-page">
      <div className="reports-header">
        <div className="reports-breadcrumb">
          <span><Link to="/">Inicio</Link></span>
          <span className="sep">/</span>
          <span className="cur">Reportes</span>
        </div>
        <h1>Módulo de Reportes</h1>
        <p>Generá manualmente reportes de documentos vencidos, implementación de procesos y seguimiento de actividades. Descargalos en PDF o Excel.</p>
      </div>

      <div className="reports-tabs">
        <button
          type="button"
          className={`reports-tab-btn ${selectedTab === "documentos" ? "active" : ""}`}
          onClick={() => setSelectedTab("documentos")}
        >
          Documentos vencidos
        </button>
        <button
          type="button"
          className={`reports-tab-btn ${selectedTab === "procesos" ? "active" : ""}`}
          onClick={() => setSelectedTab("procesos")}
        >
          Implementación de procesos
        </button>
        <button
          type="button"
          className={`reports-tab-btn ${selectedTab === "seguimiento" ? "active" : ""}`}
          onClick={() => setSelectedTab("seguimiento")}
        >
          Seguimiento de actividades
        </button>
      </div>

      {selectedTab === "documentos" && (
        <>
          <div className="reports-card">
        <div className="reports-card-header">
          <div>
            <div className="reports-card-title">Filtro de documentos vencidos</div>
            <div className="reports-card-subtitle">Filtrá por fechas y código de proceso para generar el reporte.</div>
          </div>
          <div className="reports-card-icon">
            <i className="fas fa-file-invoice"></i>
          </div>
        </div>

        <form className="reports-form" onSubmit={handleGenerarReporte}>
          <div className="form-row">
            <label htmlFor="codigoProceso">Proceso</label>
            <SelectBuscable
              id="codigoProceso"
              value={codigoProceso}
              onChange={setCodigoProceso}
              opciones={procesosDisponibles}
              placeholder="Todos los procesos"
            />
          </div>
          <div className="form-row">
            <label htmlFor="fechaDesde">Vencimiento desde</label>
            <input
              id="fechaDesde"
              type="date"
              value={fechaDesde}
              onChange={(e) => setFechaDesde(e.target.value)}
            />
          </div>
          <div className="form-row">
            <label htmlFor="fechaHasta">Vencimiento hasta</label>
            <input
              id="fechaHasta"
              type="date"
              value={fechaHasta}
              onChange={(e) => setFechaHasta(e.target.value)}
            />
          </div>

          <div className="form-actions">
            <button type="button" className="btn-secondary" onClick={handleLimpiar} disabled={buscando}>
              Limpiar
            </button>
            <button type="submit" className="btn-primary" disabled={buscando}>
              {buscando ? (
                <><i className="fas fa-spinner fa-spin"></i> Generando...</>
              ) : (
                <>Generar reporte</>
              )}
            </button>
          </div>
        </form>
      </div>
    </>
  )}

      {selectedTab === "procesos" && (
        <>
          <div className="reports-card">
            <div className="reports-card-header">
              <div>
                <div className="reports-card-title">Filtro de implementación de procesos</div>
                <div className="reports-card-subtitle">Filtrá por dominio para generar el reporte de implementación de procesos.</div>
              </div>
              <div className="reports-card-icon">
                <i className="fas fa-cogs"></i>
              </div>
            </div>

            <form className="reports-form" onSubmit={handleGenerarReporteProcesos}>
          <div className="form-row">
            <label htmlFor="dominioId">Dominio</label>
            <select
              id="dominioId"
              value={dominioId}
              onChange={(e) => setDominioId(e.target.value)}
            >
              <option value="">Todos los dominios</option>
              {dominios.map((dominio) => (
                <option key={dominio.id} value={dominio.id}>
                  {dominio.nombre || "Sin dominio"}
                </option>
              ))}
            </select>
          </div>

          <div className="form-actions">
            <button type="button" className="btn-secondary" onClick={handleLimpiarProcesos} disabled={buscandoProcesos}>
              Limpiar
            </button>
            <button type="submit" className="btn-primary" disabled={buscandoProcesos}>
              {buscandoProcesos ? (
                <><i className="fas fa-spinner fa-spin"></i> Generando...</>
              ) : (
                <>Generar reporte</>
              )}
            </button>
          </div>
        </form>
      </div>
    </>
  )}

      {selectedTab === "procesos" && errorProcesos && (
        <div className="reports-result-card" style={{ borderColor: "#f8d7da", background: "#fff1f2" }}>
          <div className="reports-result-note" style={{ color: "#842029", borderColor: "#f5c2c7", background: "#f8d7da" }}>
            <i className="fas fa-exclamation-triangle"></i>
            {errorProcesos}
          </div>
        </div>
      )}

      {selectedTab === "procesos" && resultadosProcesos.length > 0 && (
        <div className="reports-result-card">
          <div className="reports-result-header">
            <h2>Implementación de procesos</h2>
            <span>{resultadosProcesos.length} registro(s)</span>
          </div>

          <div className="reports-result-grid">
            <div>
              <span>Dominio</span>
              <strong>{dominioId ? (dominios.find((dom) => String(dom.id) === dominioId)?.nombre || "No definido") : "Todos los dominios"}</strong>
            </div>
            <div>
              <span>Fecha de generación</span>
              <strong>{new Date().toLocaleDateString("es-CR")}</strong>
            </div>
          </div>

          <div className="reports-result-note">
            <i className="fas fa-info-circle"></i>
            Este reporte se generó con los filtros seleccionados. Podés exportarlo en PDF o Excel.
          </div>

          <div className="reports-export-row">
            <button className="btn-success" onClick={exportarProcesosXLSX}>
              <i className="fas fa-file-excel"></i> Exportar XLSX
            </button>
            <button className="btn-danger" onClick={exportarProcesosPDF}>
              <i className="fas fa-file-pdf"></i> Exportar PDF
            </button>
          </div>

          <div className="doc-table-container reports-table-container">
            <table className="logs-table doc-table-min">
              <thead>
                <tr>
                  <th>Código de proceso</th>
                  <th>Nombre del proceso</th>
                  <th>Implementable</th>
                  <th>% de avance</th>
                  <th>Fecha de conclusión estimada</th>
                </tr>
              </thead>
              <tbody>
                {resultadosProcesos.map((proceso) => (
                  <tr key={proceso.idProceso || proceso.codigo}>
                    <td>{proceso.codigo || "—"}</td>
                    <td>{proceso.nombre || "—"}</td>
                    <td>{proceso.estadoImplementacion || "No definido"}</td>
                    <td>{formatearPorcentaje(proceso.porcentajeAvance)}</td>
                    <td>{proceso.fechaConclusionImplementacion ? formatearFecha(proceso.fechaConclusionImplementacion) : "No definido"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {selectedTab === "procesos" && resultadosProcesos.length === 0 && !buscandoProcesos && !errorProcesos && (
        <div className="reports-result-card">
          <div className="reports-result-note">
            <i className="fas fa-info-circle"></i>
            Generá el reporte para ver los procesos con su información de implementación.
          </div>
        </div>
      )}

      {selectedTab === "seguimiento" && (
        <>
          <div className="reports-card">
            <div className="reports-card-header">
              <div>
                <div className="reports-card-title">Seguimiento de actividades</div>
                <div className="reports-card-subtitle">Generá un reporte de tareas pendientes por usuario con el mismo estilo del módulo de reportes.</div>
              </div>
              <div className="reports-card-icon">
                <i className="fas fa-tasks"></i>
              </div>
            </div>

            <div className="reports-form" style={{ display: "grid", gap: 16 }}>
              <div className="reports-result-note" style={{ margin: 0 }}>
                <i className="fas fa-filter"></i>
                Seleccioná los tipos de tarea que querés incluir.
              </div>

              <div className="reporte-filtros-grid">
                <label className="reporte-filtro-item">
                  <input
                    type="checkbox"
                    checked={filtrosSeguimiento.incluirPorcentajeSinActualizar}
                    onChange={() => cambiarFiltroSeguimiento("incluirPorcentajeSinActualizar")}
                  />
                  Porcentaje de avance sin actualizar
                </label>
                <label className="reporte-filtro-item">
                  <input
                    type="checkbox"
                    checked={filtrosSeguimiento.incluirEstadoIncompleto}
                    onChange={() => cambiarFiltroSeguimiento("incluirEstadoIncompleto")}
                  />
                  Estado de implementación incompleta
                </label>
                <label className="reporte-filtro-item">
                  <input
                    type="checkbox"
                    checked={filtrosSeguimiento.incluirFechaControlSinAsignar}
                    onChange={() => cambiarFiltroSeguimiento("incluirFechaControlSinAsignar")}
                  />
                  Fecha de control sin asignar
                </label>
              </div>

              <div className="reporte-acciones-manuales">
                <button type="button" className="btn-primary" onClick={generarReporteSeguimiento} disabled={buscandoSeguimiento}>
                  {buscandoSeguimiento ? <><i className="fas fa-spinner fa-spin"></i> Generando...</> : <>Generar reporte</>}
                </button>
                <button type="button" className="btn-danger" onClick={exportarSeguimientoPDF} disabled={!filasSeguimiento.length || buscandoSeguimiento}>
                  <i className="fas fa-file-pdf"></i> Exportar PDF
                </button>
                <button type="button" className="btn-success" onClick={exportarSeguimientoXLSX} disabled={!filasSeguimiento.length || buscandoSeguimiento}>
                  <i className="fas fa-file-excel"></i> Exportar XLSX
                </button>
              </div>
            </div>
          </div>

          {errorSeguimiento && (
            <div className="reports-result-card" style={{ borderColor: "#f8d7da", background: "#fff1f2" }}>
              <div className="reports-result-note" style={{ color: "#842029", borderColor: "#f5c2c7", background: "#f8d7da" }}>
                <i className="fas fa-exclamation-triangle"></i>
                {errorSeguimiento}
              </div>
            </div>
          )}

          {buscandoSeguimiento ? (
            <div className="reports-result-card">
              <div className="reports-result-note">
                <i className="fas fa-spinner fa-spin"></i>
                Generando reporte de seguimiento...
              </div>
            </div>
          ) : !reporteSeguimientoGenerado ? (
            <div className="reports-result-card">
              <div className="reports-result-note">
                <i className="fas fa-info-circle"></i>
                Generá el reporte para ver el resumen por usuario y el detalle de tareas.
              </div>
            </div>
          ) : filasSeguimiento.length === 0 ? (
            <div className="reports-result-card">
              <div className="reports-result-note">
                <i className="fas fa-check-circle"></i>
                No se encontraron tareas pendientes para los filtros seleccionados.
              </div>
            </div>
          ) : (
            <div className="reports-result-card">
              <div className="reports-result-header">
                <h2>Reporte de seguimiento</h2>
                <span>{filasSeguimiento.length} registro(s)</span>
              </div>

              <div className="reports-result-grid">
                <div>
                  <span>Fecha de generación</span>
                  <strong>{new Date(reporteSeguimiento.generadoEn || new Date()).toLocaleDateString("es-CR")}</strong>
                </div>
                <div>
                  <span>Total usuarios</span>
                  <strong>{reporteSeguimiento.totalUsuarios}</strong>
                </div>
                <div>
                  <span>Total tareas pendientes</span>
                  <strong>{reporteSeguimiento.totalTareasPendientes}</strong>
                </div>
              </div>

              <div className="reports-result-note">
                <i className="fas fa-info-circle"></i>
                Este reporte se generó con los filtros seleccionados. Podés exportarlo en PDF o Excel.
              </div>

              <div className="reporte-resumen-usuarios" style={{ marginTop: 16 }}>
                {resumenSeguimientoOrdenado.map((usuario) => (
                  <div className="reporte-resumen-usuario-card" key={`seg-${usuario.usuario.id}-${usuario.usuario.cedula}`}>
                    <div className="reporte-resumen-usuario-nombre">{usuario.usuario.nombreCompleto || "Sin responsable"}</div>
                    <div className="reporte-resumen-usuario-meta">{usuario.usuario.cedula || "Sin cédula"}</div>
                    <div className="reporte-resumen-usuario-total">{usuario.totalTareasPendientes} pendientes</div>
                  </div>
                ))}
              </div>

              <div className="doc-table-container reports-table-container">
                <table className="logs-table doc-table-min">
                  <thead>
                    <tr>
                      <th>Nombre completo</th>
                      <th>Cédula</th>
                      <th>Correo electrónico</th>
                      <th>Total pendientes</th>
                      <th>Tipo de acción pendiente</th>
                      <th>Actividad</th>
                      <th>Proceso relacionado</th>
                      <th>Fecha vencida o próxima</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filasSeguimiento.map((fila, index) => (
                      <tr key={`${fila.usuarioId}-${fila.ordenLocal}-${index}`}>
                        <td>{fila.usuarioNombre || "-"}</td>
                        <td>{fila.usuarioCedula || "-"}</td>
                        <td>{fila.usuarioCorreo || "-"}</td>
                        <td>{fila.totalUsuario}</td>
                        <td>{fila.tipoAccionPendiente || "-"}</td>
                        <td>{fila.actividad || "-"}</td>
                        <td>{fila.procesoRelacionado || "-"}</td>
                        <td>{fila.fechaVencidaOProxima || "-"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}

      {error && (
        <div className="reports-result-card" style={{ borderColor: "#f8d7da", background: "#fff1f2" }}>
          <div className="reports-result-note" style={{ color: "#842029", borderColor: "#f5c2c7", background: "#f8d7da" }}>
            <i className="fas fa-exclamation-triangle"></i>
            {error}
          </div>
        </div>
      )}

      {selectedTab === "documentos" && resultados.length > 0 && (
        <div className="reports-result-card">
          <div className="reports-result-header">
            <h2>Documentos vencidos</h2>
            <span>{resultados.length} registro(s)</span>
          </div>

          <div className="reports-result-grid">
            <div>
              <span>Vencimiento desde</span>
              <strong>{fechaDesde || "No definido"}</strong>
            </div>
            <div>
              <span>Vencimiento hasta</span>
              <strong>{fechaHasta || "No definido"}</strong>
            </div>
            <div>
              <span>Código de proceso</span>
              <strong>{codigoProceso || "No definido"}</strong>
            </div>
          </div>

          <div className="reports-result-note">
            <i className="fas fa-info-circle"></i>
            Este reporte se generó con los filtros seleccionados. Podés exportarlo en PDF o Excel.
          </div>

          <div className="reports-export-row">
            <button className="btn-success" onClick={exportarXLSX}>
              <i className="fas fa-file-excel"></i> Exportar XLSX
            </button>
            <button className="btn-danger" onClick={exportarPDF}>
              <i className="fas fa-file-pdf"></i> Exportar PDF
            </button>
          </div>

          <div className="doc-table-container reports-table-container">
            <table className="logs-table doc-table-min">
              <thead>
                <tr>
                  <th>Documento</th>
                  <th>Código proceso</th>
                  <th>Actividad</th>
                  <th>Responsable</th>
                  <th>Vencimiento</th>
                  <th>Días vencido</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                {resultados.map((doc) => (
                  <tr key={doc.id}>
                    <td>{doc.nombre}</td>
                    <td>{doc.procesoCodigo || "—"}</td>
                    <td>{doc.actividadNombre || "—"}</td>
                    <td>{doc.responsableNombre || "—"}</td>
                    <td>{formatearFecha(doc.fechaVencimiento)}</td>
                    <td>{calcularDiasVencido(doc.fechaVencimiento)}</td>
                    <td>{doc.estado?.replace("_", " ") || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {selectedTab === "documentos" && resultados.length === 0 && !buscando && !error && (
        <div className="reports-result-card">
          <div className="reports-result-note">
            <i className="fas fa-info-circle"></i>
            Generá el reporte para ver los documentos vencidos con los filtros aplicados.
          </div>
        </div>
      )}
    </div>
  );
};

export default Reportes;