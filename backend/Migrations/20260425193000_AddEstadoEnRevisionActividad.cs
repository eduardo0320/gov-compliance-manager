using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(NormasDb))]
    [Migration("20260425193000_AddEstadoEnRevisionActividad")]
    public partial class AddEstadoEnRevisionActividad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @actividad_table := (
                    SELECT TABLE_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND LOWER(TABLE_NAME) = 'actividad'
                      AND LOWER(COLUMN_NAME) = 'estadoimplementacion'
                    LIMIT 1
                );

                SET @estado_is_enum := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = @actividad_table
                      AND LOWER(COLUMN_NAME) = 'estadoimplementacion'
                      AND DATA_TYPE = 'enum'
                );

                SET @has_en_revision := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = @actividad_table
                      AND LOWER(COLUMN_NAME) = 'estadoimplementacion'
                      AND (LOCATE('En Revisión', COLUMN_TYPE) > 0 OR LOCATE('En Revision', COLUMN_TYPE) > 0)
                );

                SET @sql_alter_estado := IF(
                    @actividad_table IS NULL OR @estado_is_enum = 0 OR @has_en_revision > 0,
                    'SELECT 1',
                    CONCAT(
                        'ALTER TABLE `', @actividad_table, '` ',
                        'MODIFY COLUMN `EstadoImplementacion` ',
                        'enum(''Pendiente'',''En Progreso'',''En Revisión'',''Implementado'') ',
                        'NOT NULL DEFAULT ''Pendiente''' 
                    )
                );

                PREPARE stmt_alter_estado FROM @sql_alter_estado;
                EXECUTE stmt_alter_estado;
                DEALLOCATE PREPARE stmt_alter_estado;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @actividad_table := (
                    SELECT TABLE_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND LOWER(TABLE_NAME) = 'actividad'
                      AND LOWER(COLUMN_NAME) = 'estadoimplementacion'
                    LIMIT 1
                );

                SET @estado_is_enum := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = @actividad_table
                      AND LOWER(COLUMN_NAME) = 'estadoimplementacion'
                      AND DATA_TYPE = 'enum'
                );

                SET @has_en_revision := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = @actividad_table
                      AND LOWER(COLUMN_NAME) = 'estadoimplementacion'
                      AND (LOCATE('En Revisión', COLUMN_TYPE) > 0 OR LOCATE('En Revision', COLUMN_TYPE) > 0)
                );

                SET @sql_replace_values := IF(
                    @actividad_table IS NULL OR @estado_is_enum = 0,
                    'SELECT 1',
                    CONCAT(
                        'UPDATE `', @actividad_table, '` ',
                        'SET `EstadoImplementacion` = ''En Progreso'' ',
                        'WHERE `EstadoImplementacion` IN (''En Revisión'',''En Revision'')'
                    )
                );

                PREPARE stmt_replace_values FROM @sql_replace_values;
                EXECUTE stmt_replace_values;
                DEALLOCATE PREPARE stmt_replace_values;

                SET @sql_revert_estado := IF(
                    @actividad_table IS NULL OR @estado_is_enum = 0 OR @has_en_revision = 0,
                    'SELECT 1',
                    CONCAT(
                        'ALTER TABLE `', @actividad_table, '` ',
                        'MODIFY COLUMN `EstadoImplementacion` ',
                        'enum(''Pendiente'',''En Progreso'',''Implementado'') ',
                        'NOT NULL DEFAULT ''Pendiente''' 
                    )
                );

                PREPARE stmt_revert_estado FROM @sql_revert_estado;
                EXECUTE stmt_revert_estado;
                DEALLOCATE PREPARE stmt_revert_estado;
            ");
        }
    }
}
