using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class GarantiaSobreFacturaItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No existe una equivalencia de IDs entre el editable y el snapshot facturado.
            // Bloquear antes de cualquier DDL si aparecen datos que requieren conciliación manual.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [GarantiaItems] WITH (TABLOCKX, HOLDLOCK))
                    THROW 51000, 'Existen GarantiaItems históricos: conciliar su origen antes de migrar. No se convertirán IDs automáticamente.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_GarantiaItems_PresupuestoItems_PresupuestoItemId",
                table: "GarantiaItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Garantias_Usuarios_CreadaPorUsuarioId",
                table: "Garantias");

            migrationBuilder.DropIndex(name: "IX_GarantiaItems_PresupuestoItemId", table: "GarantiaItems");
            migrationBuilder.DropColumn(name: "PresupuestoItemId", table: "GarantiaItems");
            migrationBuilder.AddColumn<int>(name: "FacturaItemId", table: "GarantiaItems", type: "int", nullable: false);
            migrationBuilder.CreateIndex(name: "IX_GarantiaItems_FacturaItemId", table: "GarantiaItems", column: "FacturaItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_GarantiaItems_FacturaItems_FacturaItemId",
                table: "GarantiaItems",
                column: "FacturaItemId",
                principalTable: "FacturaItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Garantias_Usuarios_CreadaPorUsuarioId",
                table: "Garantias",
                column: "CreadaPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [GarantiaItems] WITH (TABLOCKX, HOLDLOCK))
                    THROW 51000, 'Existen GarantiaItems facturados: no se puede reconstruir automáticamente su origen editable.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_GarantiaItems_FacturaItems_FacturaItemId",
                table: "GarantiaItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Garantias_Usuarios_CreadaPorUsuarioId",
                table: "Garantias");

            migrationBuilder.DropIndex(name: "IX_GarantiaItems_FacturaItemId", table: "GarantiaItems");
            migrationBuilder.DropColumn(name: "FacturaItemId", table: "GarantiaItems");
            migrationBuilder.AddColumn<int>(name: "PresupuestoItemId", table: "GarantiaItems", type: "int", nullable: false);
            migrationBuilder.CreateIndex(name: "IX_GarantiaItems_PresupuestoItemId", table: "GarantiaItems", column: "PresupuestoItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_GarantiaItems_PresupuestoItems_PresupuestoItemId",
                table: "GarantiaItems",
                column: "PresupuestoItemId",
                principalTable: "PresupuestoItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Garantias_Usuarios_CreadaPorUsuarioId",
                table: "Garantias",
                column: "CreadaPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
