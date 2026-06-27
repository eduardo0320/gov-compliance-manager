using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGestionDocumental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.CreateTable(
                name: "documentos",
                columns: table => new
                {
                    id_Documento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoDocumento = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActividadId = table.Column<int>(type: "int", nullable: false),
                    VersionActualId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Borrador")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaAlerta = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Categoria = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confidencialidad = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Interna")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RowVersion = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    EliminadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos", x => x.id_Documento);
                    table.ForeignKey(
                        name: "FK_documentos_Actividad_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividad",
                        principalColumn: "id_Actividad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documentos_usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documentos_usuarios_EliminadoPorId",
                        column: x => x.EliminadoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documentos_usuarios_ModificadoPorId",
                        column: x => x.ModificadoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "metadatos_documento",
                columns: table => new
                {
                    id_MetadatoDocumento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DocumentoId = table.Column<int>(type: "int", nullable: false),
                    Clave = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Valor = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metadatos_documento", x => x.id_MetadatoDocumento);
                    table.ForeignKey(
                        name: "FK_metadatos_documento_documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "documentos",
                        principalColumn: "id_Documento",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_metadatos_documento_usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "relaciones_documento",
                columns: table => new
                {
                    id_RelacionDocumento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DocumentoOrigenId = table.Column<int>(type: "int", nullable: false),
                    DocumentoDestinoId = table.Column<int>(type: "int", nullable: false),
                    TipoRelacion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Orden = table.Column<int>(type: "int", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relaciones_documento", x => x.id_RelacionDocumento);
                    table.ForeignKey(
                        name: "FK_relaciones_documento_documentos_DocumentoDestinoId",
                        column: x => x.DocumentoDestinoId,
                        principalTable: "documentos",
                        principalColumn: "id_Documento",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_relaciones_documento_documentos_DocumentoOrigenId",
                        column: x => x.DocumentoOrigenId,
                        principalTable: "documentos",
                        principalColumn: "id_Documento",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_relaciones_documento_usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "versiones_documento",
                columns: table => new
                {
                    id_VersionDocumento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DocumentoId = table.Column<int>(type: "int", nullable: false),
                    NumeroVersion = table.Column<int>(type: "int", nullable: false),
                    VersionTexto = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoAlmacenamiento = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RutaArchivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    URL = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreArchivoOriginal = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: true),
                    MimeType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChecksumSHA256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comentario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubidoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_versiones_documento", x => x.id_VersionDocumento);
                    table.ForeignKey(
                        name: "FK_versiones_documento_documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "documentos",
                        principalColumn: "id_Documento",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_versiones_documento_usuarios_SubidoPorId",
                        column: x => x.SubidoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IDX_Documentos_Actividad_Estado",
                table: "documentos",
                columns: new[] { "ActividadId", "Estado", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "IDX_Estado",
                table: "documentos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IDX_FechaVencimiento",
                table: "documentos",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_CreadoPorId",
                table: "documentos",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_EliminadoPorId",
                table: "documentos",
                column: "EliminadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_ModificadoPorId",
                table: "documentos",
                column: "ModificadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_VersionActualId",
                table: "documentos",
                column: "VersionActualId");

            migrationBuilder.CreateIndex(
                name: "IX_metadatos_documento_CreadoPorId",
                table: "metadatos_documento",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "UQ_Documento_Clave",
                table: "metadatos_documento",
                columns: new[] { "DocumentoId", "Clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_relaciones_documento_CreadoPorId",
                table: "relaciones_documento",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_relaciones_documento_DocumentoDestinoId",
                table: "relaciones_documento",
                column: "DocumentoDestinoId");

            migrationBuilder.CreateIndex(
                name: "UQ_Relacion",
                table: "relaciones_documento",
                columns: new[] { "DocumentoOrigenId", "DocumentoDestinoId", "TipoRelacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_ChecksumSHA256",
                table: "versiones_documento",
                column: "ChecksumSHA256");

            migrationBuilder.CreateIndex(
                name: "IX_versiones_documento_SubidoPorId",
                table: "versiones_documento",
                column: "SubidoPorId");

            migrationBuilder.CreateIndex(
                name: "UQ_Documento_NumeroVersion",
                table: "versiones_documento",
                columns: new[] { "DocumentoId", "NumeroVersion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_documentos_versiones_documento_VersionActualId",
                table: "documentos",
                column: "VersionActualId",
                principalTable: "versiones_documento",
                principalColumn: "id_VersionDocumento",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documentos_versiones_documento_VersionActualId",
                table: "documentos");

            migrationBuilder.DropTable(
                name: "metadatos_documento");

            migrationBuilder.DropTable(
                name: "relaciones_documento");

            migrationBuilder.DropTable(
                name: "versiones_documento");

            migrationBuilder.DropTable(
                name: "documentos");

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "Id_Usuario", "DebeRestablecerContrasena", "cedula", "contrasena", "correo_electronico", "departamento", "estado", "fechaBloqueado", "fechaCreacion", "fechaUltimaModificacion", "idRol", "intentosLoginFallidos", "nombre", "ultimoAcceso" },
                values: new object[] { 1, false, "admin", "admin123", "admin@micitt.go.cr", "TI", true, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0, "Administrador", null });
        }
    }
}
