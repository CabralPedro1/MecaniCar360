using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIdempotenciaMovimientosStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperacionesStockIdempotentes",
                columns: table => new
                {
                    Clave = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TipoOperacion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HuellaSolicitud = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    MovimientoStockId = table.Column<int>(type: "int", nullable: false),
                    MensajeResultado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaConfirmacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacionesStockIdempotentes", x => x.Clave);
                    table.CheckConstraint("CK_OperacionStock_Clave", "[Clave] <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_OperacionStock_Tipo", "[TipoOperacion] IN ('Ingreso','Salida','Ajuste')");
                    table.ForeignKey(
                        name: "FK_OperacionesStockIdempotentes_MovimientosStock_MovimientoStockId",
                        column: x => x.MovimientoStockId,
                        principalTable: "MovimientosStock",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperacionesStockIdempotentes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesStockIdempotentes_MovimientoStockId",
                table: "OperacionesStockIdempotentes",
                column: "MovimientoStockId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesStockIdempotentes_UsuarioId",
                table: "OperacionesStockIdempotentes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperacionesStockIdempotentes");
        }
    }
}
