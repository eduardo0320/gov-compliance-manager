using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorAuthClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @exists_twofactor_col := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'usuarios'
                      AND COLUMN_NAME = 'TwoFactorEnabled'
                );

                SET @sql_add_twofactor_col := IF(
                    @exists_twofactor_col = 0,
                    'ALTER TABLE `usuarios` ADD COLUMN `TwoFactorEnabled` tinyint(1) NOT NULL DEFAULT FALSE',
                    'SELECT 1'
                );

                PREPARE stmt_add_twofactor_col FROM @sql_add_twofactor_col;
                EXECUTE stmt_add_twofactor_col;
                DEALLOCATE PREPARE stmt_add_twofactor_col;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `TwoFactorCodes` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `UsuarioId` int NOT NULL,
                    `CodigoHash` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                    `ExpiraEn` datetime(6) NOT NULL,
                    `Usado` tinyint(1) NOT NULL DEFAULT FALSE,
                    `CreadoEn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT `PK_TwoFactorCodes` PRIMARY KEY (`Id`),
                    KEY `IX_TwoFactorCodes_UsuarioId` (`UsuarioId`),
                    CONSTRAINT `FK_TwoFactorCodes_usuarios_UsuarioId`
                        FOREIGN KEY (`UsuarioId`) REFERENCES `usuarios` (`Id_Usuario`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS `TwoFactorCodes`;");

            migrationBuilder.Sql(@"
                SET @exists_twofactor_col := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'usuarios'
                      AND COLUMN_NAME = 'TwoFactorEnabled'
                );

                SET @sql_drop_twofactor_col := IF(
                    @exists_twofactor_col = 1,
                    'ALTER TABLE `usuarios` DROP COLUMN `TwoFactorEnabled`',
                    'SELECT 1'
                );

                PREPARE stmt_drop_twofactor_col FROM @sql_drop_twofactor_col;
                EXECUTE stmt_drop_twofactor_col;
                DEALLOCATE PREPARE stmt_drop_twofactor_col;");
        }
    }
}
