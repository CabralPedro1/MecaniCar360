using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AutenticacionMultimetodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Usuarios] WHERE LEN(LTRIM(RTRIM([Username]))) = 0
                    OR CHARINDEX('@', [Username]) > 0
                    OR DATALENGTH([Username]) <> DATALENGTH(LTRIM(RTRIM([Username])))
                    OR LEN(LTRIM(RTRIM([EmailLogin]))) = 0
                    OR DATALENGTH([EmailLogin]) <> DATALENGTH(LTRIM(RTRIM([EmailLogin]))))
                    THROW 51010, 'Identificadores de cuenta incompatibles. Revisar antes de migrar; no se renombraron cuentas.', 1;
                IF EXISTS (SELECT [Username] COLLATE Latin1_General_100_CI_AS FROM [Usuarios]
                    GROUP BY [Username] COLLATE Latin1_General_100_CI_AS HAVING COUNT(*) > 1)
                    OR EXISTS (SELECT [EmailLogin] COLLATE Latin1_General_100_CI_AS FROM [Usuarios]
                    GROUP BY [EmailLogin] COLLATE Latin1_General_100_CI_AS HAVING COUNT(*) > 1)
                    THROW 51011, 'Identificadores duplicados bajo la comparacion explicita. Revisar antes de migrar.', 1;
                """);

            migrationBuilder.CreateTable(
                name: "IdentidadesExternas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Proveedor = table.Column<int>(type: "int", nullable: false),
                    IdentificadorExterno = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    FechaVinculacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentidadesExternas", x => x.Id);
                    table.CheckConstraint("CK_IdentidadesExternas_Identificador", "LEN(LTRIM(RTRIM([IdentificadorExterno]))) > 0");
                    table.CheckConstraint("CK_IdentidadesExternas_Proveedor", "[Proveedor] = 1");
                    table.ForeignKey(
                        name: "FK_IdentidadesExternas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Fecha de incorporacion al nuevo registro; no se inventa una fecha historica de OAuth.
            migrationBuilder.Sql("""
                INSERT INTO [IdentidadesExternas] ([UsuarioId], [Proveedor], [IdentificadorExterno], [FechaVinculacion])
                SELECT [Id], 1, [IdentificadorExterno], SYSUTCDATETIME()
                FROM [Usuarios] WHERE [ProveedorAutenticacion] = 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ProveedorAutenticacion_IdentificadorExterno",
                table: "Usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_ProveedorAutenticacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IdentificadorExterno",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ProveedorAutenticacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TokenRecuperacionExpira",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TokenRecuperacionPassword",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Usuarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "EmailLogin",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_CredencialLocal",
                table: "Usuarios",
                sql: "[PasswordHash] IS NULL OR LEN(LTRIM(RTRIM([PasswordHash]))) > 0");

            migrationBuilder.CreateIndex(
                name: "IX_IdentidadesExternas_Proveedor_IdentificadorExterno",
                table: "IdentidadesExternas",
                columns: new[] { "Proveedor", "IdentificadorExterno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentidadesExternas_UsuarioId_Proveedor",
                table: "IdentidadesExternas",
                columns: new[] { "UsuarioId", "Proveedor" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Usuarios] u WHERE
                    (u.[PasswordHash] IS NOT NULL AND EXISTS (SELECT 1 FROM [IdentidadesExternas] i WHERE i.[UsuarioId] = u.[Id]))
                    OR (u.[PasswordHash] IS NULL AND NOT EXISTS (SELECT 1 FROM [IdentidadesExternas] i WHERE i.[UsuarioId] = u.[Id]))
                    OR (u.[PasswordHash] IS NOT NULL AND LEN(LTRIM(RTRIM(u.[PasswordHash]))) = 0)
                    OR (u.[PrimerLogin] = 1 AND EXISTS (SELECT 1 FROM [IdentidadesExternas] i WHERE i.[UsuarioId] = u.[Id])))
                    OR EXISTS (SELECT [UsuarioId] FROM [IdentidadesExternas] GROUP BY [UsuarioId] HAVING COUNT(*) > 1)
                    OR EXISTS (SELECT 1 FROM [IdentidadesExternas] WHERE [Proveedor] <> 1)
                    THROW 51012, 'Rollback incompatible: cuenta multim metodo, sin metodo o identidad no representable. No se modificaron datos.', 1;
                IF EXISTS (SELECT [Username] COLLATE DATABASE_DEFAULT FROM [Usuarios]
                    GROUP BY [Username] COLLATE DATABASE_DEFAULT HAVING COUNT(*) > 1)
                    OR EXISTS (SELECT [EmailLogin] COLLATE DATABASE_DEFAULT FROM [Usuarios]
                    GROUP BY [EmailLogin] COLLATE DATABASE_DEFAULT HAVING COUNT(*) > 1)
                    THROW 51013, 'Rollback incompatible con la collation anterior.', 1;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_CredencialLocal",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Usuarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldCollation: "Latin1_General_100_CI_AS",
                collation: "DATABASE_DEFAULT");

            migrationBuilder.AlterColumn<string>(
                name: "EmailLogin",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_CI_AS",
                collation: "DATABASE_DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "IdentificadorExterno",
                table: "Usuarios",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<int>(
                name: "ProveedorAutenticacion",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenRecuperacionExpira",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenRecuperacionPassword",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE u SET [ProveedorAutenticacion] = i.[Proveedor], [IdentificadorExterno] = i.[IdentificadorExterno]
                FROM [Usuarios] u INNER JOIN [IdentidadesExternas] i ON i.[UsuarioId] = u.[Id];
                """);

            migrationBuilder.DropTable(
                name: "IdentidadesExternas");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ProveedorAutenticacion_IdentificadorExterno",
                table: "Usuarios",
                columns: new[] { "ProveedorAutenticacion", "IdentificadorExterno" },
                unique: true,
                filter: "[IdentificadorExterno] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_ProveedorAutenticacion",
                table: "Usuarios",
                sql: "([ProveedorAutenticacion] = 0 AND [PasswordHash] IS NOT NULL AND LEN(LTRIM(RTRIM([PasswordHash]))) > 0 AND [IdentificadorExterno] IS NULL) OR ([ProveedorAutenticacion] = 1 AND [PasswordHash] IS NULL AND [IdentificadorExterno] IS NOT NULL AND LEN(LTRIM(RTRIM([IdentificadorExterno]))) > 0 AND [PrimerLogin] = 0)");
        }
    }
}
