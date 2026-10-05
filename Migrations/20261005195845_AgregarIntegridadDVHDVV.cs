using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIntegridadDVHDVV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Usuarios",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Roles",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Repuestos",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Personas",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "PersonaRoles",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Pagos",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "MovimientosStockLote",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "MovimientosStock",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "LotesRepuesto",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Facturas",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "FacturaItems",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DVH",
                table: "Auditorias",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DigitosVerificadoresVerticales",
                columns: table => new
                {
                    NombreEntidad = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VersionAlgoritmo = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    CantidadRegistros = table.Column<long>(type: "bigint", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitosVerificadoresVerticales", x => x.NombreEntidad);
                    table.CheckConstraint("CK_DVV_Cantidad", "[CantidadRegistros] >= 0");
                    table.CheckConstraint("CK_DVV_Valor", "DATALENGTH([Valor]) = 64 AND [Valor] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
                    table.CheckConstraint("CK_DVV_Version", "[VersionAlgoritmo] > 0");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_DVH",
                table: "Usuarios",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_DVH",
                table: "Roles",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Repuestos_DVH",
                table: "Repuestos",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Personas_DVH",
                table: "Personas",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PersonaRoles_DVH",
                table: "PersonaRoles",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pagos_DVH",
                table: "Pagos",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MovimientosStockLote_DVH",
                table: "MovimientosStockLote",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MovimientosStock_DVH",
                table: "MovimientosStock",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LotesRepuesto_DVH",
                table: "LotesRepuesto",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Facturas_DVH",
                table: "Facturas",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FacturaItems_DVH",
                table: "FacturaItems",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Auditorias_DVH",
                table: "Auditorias",
                sql: "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigitosVerificadoresVerticales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_DVH",
                table: "Usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_DVH",
                table: "Roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Repuestos_DVH",
                table: "Repuestos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Personas_DVH",
                table: "Personas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PersonaRoles_DVH",
                table: "PersonaRoles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pagos_DVH",
                table: "Pagos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MovimientosStockLote_DVH",
                table: "MovimientosStockLote");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MovimientosStock_DVH",
                table: "MovimientosStock");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LotesRepuesto_DVH",
                table: "LotesRepuesto");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Facturas_DVH",
                table: "Facturas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FacturaItems_DVH",
                table: "FacturaItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Auditorias_DVH",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Repuestos");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "PersonaRoles");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "MovimientosStockLote");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "LotesRepuesto");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "FacturaItems");

            migrationBuilder.DropColumn(
                name: "DVH",
                table: "Auditorias");
        }
    }
}
