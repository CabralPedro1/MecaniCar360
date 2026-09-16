using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AgregarBajaLogicaProveedorRepuesto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProveedorRepuestos_RepuestoId",
                table: "ProveedorRepuestos");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "ProveedorRepuestos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaBaja",
                table: "ProveedorRepuestos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorRepuestos_RepuestoId",
                table: "ProveedorRepuestos",
                column: "RepuestoId",
                unique: true,
                filter: "[Principal] = 1 AND [Activo] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [ProveedorRepuestos] WITH (UPDLOCK, HOLDLOCK)
                           WHERE [Activo] = 0 OR [FechaBaja] IS NOT NULL)
                    THROW 51000, 'No se puede revertir la baja logica: existen relaciones desvinculadas o fechas de baja.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ProveedorRepuestos_RepuestoId",
                table: "ProveedorRepuestos");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "ProveedorRepuestos");

            migrationBuilder.DropColumn(
                name: "FechaBaja",
                table: "ProveedorRepuestos");

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorRepuestos_RepuestoId",
                table: "ProveedorRepuestos",
                column: "RepuestoId",
                unique: true,
                filter: "[Principal] = 1");
        }
    }
}
