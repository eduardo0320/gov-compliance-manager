/**
 * excelExport.js
 * Utilidad compartida para exportar datos del Gantt a Excel usando ExcelJS.
 * Reemplaza la dependencia de `xlsx` (vulnerable) con `exceljs` (mantenida y segura).
 */

/**
 * Exporta los datos del Gantt a un archivo .xlsx con estilos de color por estado.
 *
 * @param {object} params
 * @param {string[]}       params.headers        - Encabezados de columna
 * @param {{ row: string[], estado: string }[]} params.rows - Filas de datos con su estado
 * @param {number}         params.semanaActualIdx - Índice de la columna de semana actual (0-based, desde col 5)
 * @param {string}         params.sheetName       - Nombre de la hoja (ej: "Gantt" o "Mi Gantt")
 * @param {string}         params.fileName        - Nombre del archivo sin extensión
 */
export async function exportarExcelGantt({ headers, rows, semanaActualIdx, sheetName, fileName }) {
  // ExcelJS se importa dinámicamente para no bloquear el bundle inicial
  const ExcelJS = (await import('exceljs')).default;

  const wb = new ExcelJS.Workbook();
  wb.creator = 'Sistema MICITT';
  wb.created = new Date();

  const ws = wb.addWorksheet(sheetName);

  // ── Colores por estado ──────────────────────────────────────────
  const ESTADO_FILL = {
    Pendiente: { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFF9E5' } },  // amarillo claro
    Completada: { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFE6F9ED' } }, // verde claro
    Vencida:   { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFDE6E6' } },  // rojo claro
  };
  const SEMANA_ACTUAL_FILL = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFE6F0FA' } }; // azul claro
  const HEADER_FILL       = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF2C3E50' } }; // gris oscuro

  // ── Fila de encabezados ─────────────────────────────────────────
  const headerRow = ws.addRow(headers);
  headerRow.eachCell((cell) => {
    cell.font  = { bold: true, color: { argb: 'FFFFFFFF' }, size: 11 };
    cell.fill  = HEADER_FILL;
    cell.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
    cell.border = {
      bottom: { style: 'thin', color: { argb: 'FF95A5A6' } },
    };
  });
  headerRow.height = 32;

  // ── Filas de datos ──────────────────────────────────────────────
  rows.forEach(({ row, estado }) => {
    const dataRow = ws.addRow(row);
    dataRow.height = 22;

    dataRow.eachCell({ includeEmpty: true }, (cell, colNumber) => {
      const colIdx = colNumber - 1; // 0-based

      // Columna Estado (índice 2, col 3)
      if (colIdx === 2 && ESTADO_FILL[estado]) {
        cell.fill = ESTADO_FILL[estado];
      }

      // Columna de semana actual (empieza en col 6, índice 5)
      if (semanaActualIdx !== -1 && colIdx === 5 + semanaActualIdx && cell.value) {
        cell.fill = SEMANA_ACTUAL_FILL;
        cell.font = { bold: true };
      }

      cell.alignment = { vertical: 'middle', wrapText: false };
    });
  });

  // ── Anchos de columna automáticos ──────────────────────────────
  ws.columns.forEach((col, i) => {
    if (i === 0) { col.width = 22; return; } // Dominio
    if (i === 1) { col.width = 40; return; } // Actividad (la más larga)
    if (i === 2) { col.width = 12; return; } // Estado
    if (i === 3) { col.width = 10; return; } // % Avance
    if (i === 4) { col.width = 16; return; } // Fecha Compromiso
    col.width = 14;                           // Semanas
  });

  // ── Congelar fila de encabezados ───────────────────────────────
  ws.views = [{ state: 'frozen', ySplit: 1 }];

  // ── Generar y descargar el archivo ────────────────────────────
  const buffer = await wb.xlsx.writeBuffer();
  const blob   = new Blob([buffer], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  const url  = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href     = url;
  link.download = `${fileName}.xlsx`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
