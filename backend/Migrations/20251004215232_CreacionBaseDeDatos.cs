using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class CreacionBaseDeDatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ejecutar el archivo de esquema (creación de tablas)
            var schemaPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "01_Schema.sql");
            var schemaScript = File.ReadAllText(schemaPath);
            migrationBuilder.Sql(schemaScript);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar todas las tablas en orden inverso (por las foreign keys)
            migrationBuilder.Sql("DROP TABLE IF EXISTS `actividad`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `auditoria`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `subdominio`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `proceso`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `usuarios`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `dominio`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `rol`;");
        }
    }
}
