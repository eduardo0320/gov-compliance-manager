using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(NormasDb))]
    [Migration("20260415190000_FixPostTwoFactorMigrations")]
    public partial class FixPostTwoFactorMigrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure Notificaciones.Tipo has no default to match the current model snapshot.
            migrationBuilder.Sql(@"
                SET @exists_notificaciones_tipo := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Notificaciones'
                      AND COLUMN_NAME = 'Tipo'
                );

                SET @sql_drop_tipo_default := IF(
                    @exists_notificaciones_tipo = 1,
                    'ALTER TABLE `Notificaciones` ALTER COLUMN `Tipo` DROP DEFAULT',
                    'SELECT 1'
                );

                PREPARE stmt_drop_tipo_default FROM @sql_drop_tipo_default;
                EXECUTE stmt_drop_tipo_default;
                DEALLOCATE PREPARE stmt_drop_tipo_default;");

            // Ensure historial table exists for HistorialVersionActividad model.
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `HistorialVersionesActividades` (
                    `id_HistorialActividad` int NOT NULL AUTO_INCREMENT,
                    `ActividadId` int NOT NULL,
                    `Version` int NOT NULL,
                    `FechaRegistro` datetime(6) NOT NULL,
                    `UsuarioModificacionId` int NULL,
                    `Nombre` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                    `Implementable` varchar(12) CHARACTER SET utf8mb4 NOT NULL,
                    `FechaCompromiso` datetime(6) NULL,
                    `EstadoImplementacion` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
                    `PorcentajeAvance` decimal(5,2) NOT NULL,
                    `FuncionariosResponsablesId` int NOT NULL,
                    `FechaControl` datetime(6) NULL,
                    `Documentos` longtext CHARACTER SET utf8mb4 NULL,
                    `DocumentosAnteriores` longtext CHARACTER SET utf8mb4 NULL,
                    `Observaciones` longtext CHARACTER SET utf8mb4 NULL,
                    `DescripcionCambios` longtext CHARACTER SET utf8mb4 NULL,
                    CONSTRAINT `PK_HistorialVersionesActividades` PRIMARY KEY (`id_HistorialActividad`),
                    CONSTRAINT `FK_HistorialVersionesActividades_Actividad_ActividadId`
                        FOREIGN KEY (`ActividadId`) REFERENCES `Actividad` (`id_Actividad`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;");

            // Ensure required indexes exist.
            migrationBuilder.Sql(@"
                SET @idx_historial_actividad_version_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'HistorialVersionesActividades'
                      AND INDEX_NAME = 'IX_HistorialVersionesActividades_ActividadId_Version'
                );

                SET @sql_create_idx_historial_actividad_version := IF(
                    @idx_historial_actividad_version_exists = 0,
                    'CREATE UNIQUE INDEX `IX_HistorialVersionesActividades_ActividadId_Version` ON `HistorialVersionesActividades` (`ActividadId`, `Version`)',
                    'SELECT 1'
                );

                PREPARE stmt_create_idx_historial_actividad_version FROM @sql_create_idx_historial_actividad_version;
                EXECUTE stmt_create_idx_historial_actividad_version;
                DEALLOCATE PREPARE stmt_create_idx_historial_actividad_version;

                SET @idx_historial_fecha_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'HistorialVersionesActividades'
                      AND INDEX_NAME = 'IX_HistorialVersionesActividades_FechaRegistro'
                );

                SET @sql_create_idx_historial_fecha := IF(
                    @idx_historial_fecha_exists = 0,
                    'CREATE INDEX `IX_HistorialVersionesActividades_FechaRegistro` ON `HistorialVersionesActividades` (`FechaRegistro`)',
                    'SELECT 1'
                );

                PREPARE stmt_create_idx_historial_fecha FROM @sql_create_idx_historial_fecha;
                EXECUTE stmt_create_idx_historial_fecha;
                DEALLOCATE PREPARE stmt_create_idx_historial_fecha;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS `HistorialVersionesActividades`;");

            migrationBuilder.Sql(@"
                SET @exists_notificaciones_tipo := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Notificaciones'
                      AND COLUMN_NAME = 'Tipo'
                );

                SET @sql_restore_tipo_default := IF(
                    @exists_notificaciones_tipo = 1,
                    'ALTER TABLE `Notificaciones` ALTER COLUMN `Tipo` SET DEFAULT ''info''',
                    'SELECT 1'
                );

                PREPARE stmt_restore_tipo_default FROM @sql_restore_tipo_default;
                EXECUTE stmt_restore_tipo_default;
                DEALLOCATE PREPARE stmt_restore_tipo_default;");
        }
    }
}