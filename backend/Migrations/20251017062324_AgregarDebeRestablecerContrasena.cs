using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDebeRestablecerContrasena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DebeRestablecerContrasena",
                table: "usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);


            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "Id_Usuario",
                keyValue: 1,
                column: "DebeRestablecerContrasena",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebeRestablecerContrasena",
                table: "usuarios");

        }
    }
}
