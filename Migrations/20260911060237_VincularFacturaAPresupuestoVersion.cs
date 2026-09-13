using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class VincularFacturaAPresupuestoVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PresupuestoVersionOrigenId",
                table: "Facturas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_PresupuestoVersionOrigenId",
                table: "Facturas",
                column: "PresupuestoVersionOrigenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_PresupuestoVersiones_PresupuestoVersionOrigenId",
                table: "Facturas",
                column: "PresupuestoVersionOrigenId",
                principalTable: "PresupuestoVersiones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_PresupuestoVersiones_PresupuestoVersionOrigenId",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_PresupuestoVersionOrigenId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "PresupuestoVersionOrigenId",
                table: "Facturas");
        }
    }
}
