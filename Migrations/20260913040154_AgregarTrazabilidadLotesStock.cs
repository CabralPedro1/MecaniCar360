using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTrazabilidadLotesStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No reconstruir procedencia ni aplicar FIFO retroactivo. Validar antes del DDL.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Repuestos] WITH (TABLOCKX, HOLDLOCK) WHERE [StockActual] <> 0)
                    THROW 51000, 'Existen saldos de repuestos sin conciliacion por lotes. No se crearan lotes ficticios.', 1;
                IF EXISTS (SELECT 1 FROM [MovimientosStock] WITH (TABLOCKX, HOLDLOCK))
                    THROW 51000, 'Existen movimientos historicos. Conciliar su trazabilidad antes de habilitar lotes.', 1;
                IF EXISTS (SELECT 1 FROM [Repuestos] WHERE [StockMinimo] < 0)
                    THROW 51000, 'Existen valores negativos de stock minimo. Corregirlos antes de migrar.', 1;
                IF EXISTS (
                    SELECT [RepuestoId] FROM [ProveedorRepuestos] WITH (TABLOCKX, HOLDLOCK)
                    WHERE [Principal] = 1 GROUP BY [RepuestoId] HAVING COUNT_BIG(*) > 1)
                    THROW 51000, 'Existen varios proveedores principales para un repuesto. Resolverlos antes de migrar.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Usuarios_RealizadoPorUsuarioId",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_ProveedorRepuestos_RepuestoId",
                table: "ProveedorRepuestos");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ProveedorRepuestos_Id_RepuestoId",
                table: "ProveedorRepuestos",
                columns: new[] { "Id", "RepuestoId" });

            migrationBuilder.CreateTable(
                name: "LotesRepuesto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoLote = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RepuestoId = table.Column<int>(type: "int", nullable: false),
                    ProveedorRepuestoId = table.Column<int>(type: "int", nullable: false),
                    CantidadIngresada = table.Column<int>(type: "int", nullable: false),
                    CantidadDisponible = table.Column<int>(type: "int", nullable: false),
                    PrecioCompra = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesRepuesto", x => x.Id);
                    table.CheckConstraint("CK_LotesRepuesto_CantidadDisponible", "[CantidadDisponible] >= 0 AND [CantidadDisponible] <= [CantidadIngresada]");
                    table.CheckConstraint("CK_LotesRepuesto_CantidadIngresada", "[CantidadIngresada] > 0");
                    table.CheckConstraint("CK_LotesRepuesto_CodigoLote", "LEN(LTRIM(RTRIM([CodigoLote]))) > 0");
                    table.CheckConstraint("CK_LotesRepuesto_PrecioCompra", "[PrecioCompra] > 0");
                    table.ForeignKey(
                        name: "FK_LotesRepuesto_ProveedorRepuestos_ProveedorRepuestoId_RepuestoId",
                        columns: x => new { x.ProveedorRepuestoId, x.RepuestoId },
                        principalTable: "ProveedorRepuestos",
                        principalColumns: new[] { "Id", "RepuestoId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotesRepuesto_Repuestos_RepuestoId",
                        column: x => x.RepuestoId,
                        principalTable: "Repuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStockLote",
                columns: table => new
                {
                    MovimientoStockId = table.Column<int>(type: "int", nullable: false),
                    LoteRepuestoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStockLote", x => new { x.MovimientoStockId, x.LoteRepuestoId });
                    table.CheckConstraint("CK_MovimientosStockLote_Cantidad", "[Cantidad] > 0");
                    table.ForeignKey(
                        name: "FK_MovimientosStockLote_LotesRepuesto_LoteRepuestoId",
                        column: x => x.LoteRepuestoId,
                        principalTable: "LotesRepuesto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStockLote_MovimientosStock_MovimientoStockId",
                        column: x => x.MovimientoStockId,
                        principalTable: "MovimientosStock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Repuestos_StockActual",
                table: "Repuestos",
                sql: "[StockActual] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Repuestos_StockMinimo",
                table: "Repuestos",
                sql: "[StockMinimo] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorRepuestos_RepuestoId",
                table: "ProveedorRepuestos",
                column: "RepuestoId",
                unique: true,
                filter: "[Principal] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MovimientosStock_Cantidad",
                table: "MovimientosStock",
                sql: "[Cantidad] <> 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MovimientosStock_TipoDatos",
                table: "MovimientosStock",
                sql: "([Tipo] = 0 AND [Cantidad] > 0 AND [ProveedorRepuestoId] IS NOT NULL) OR ([Tipo] = 1 AND [Cantidad] < 0 AND [OrdenTrabajoId] IS NOT NULL) OR ([Tipo] = 2 AND [Cantidad] <> 0)");

            migrationBuilder.CreateIndex(
                name: "IX_LotesRepuesto_CodigoLote",
                table: "LotesRepuesto",
                column: "CodigoLote",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotesRepuesto_ProveedorRepuestoId_RepuestoId",
                table: "LotesRepuesto",
                columns: new[] { "ProveedorRepuestoId", "RepuestoId" });

            migrationBuilder.CreateIndex(
                name: "IX_LotesRepuesto_RepuestoId_FechaIngreso_Id",
                table: "LotesRepuesto",
                columns: new[] { "RepuestoId", "FechaIngreso", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStockLote_LoteRepuestoId",
                table: "MovimientosStockLote",
                column: "LoteRepuestoId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Usuarios_RealizadoPorUsuarioId",
                table: "MovimientosStock",
                column: "RealizadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [LotesRepuesto] WITH (TABLOCKX, HOLDLOCK))
                    THROW 51000, 'Existen lotes con historia, incluso agotados. No se permite eliminar su trazabilidad.', 1;
                IF EXISTS (SELECT 1 FROM [MovimientosStockLote] WITH (TABLOCKX, HOLDLOCK))
                    THROW 51000, 'Existen desgloses de movimientos por lote. No se permite destruirlos.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Usuarios_RealizadoPorUsuarioId",
                table: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "MovimientosStockLote");

            migrationBuilder.DropTable(
                name: "LotesRepuesto");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Repuestos_StockActual",
                table: "Repuestos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Repuestos_StockMinimo",
                table: "Repuestos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ProveedorRepuestos_Id_RepuestoId",
                table: "ProveedorRepuestos");

            migrationBuilder.DropIndex(
                name: "IX_ProveedorRepuestos_RepuestoId",
                table: "ProveedorRepuestos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MovimientosStock_Cantidad",
                table: "MovimientosStock");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MovimientosStock_TipoDatos",
                table: "MovimientosStock");

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorRepuestos_RepuestoId",
                table: "ProveedorRepuestos",
                column: "RepuestoId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Usuarios_RealizadoPorUsuarioId",
                table: "MovimientosStock",
                column: "RealizadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
