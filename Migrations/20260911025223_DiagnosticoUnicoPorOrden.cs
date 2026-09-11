using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class DiagnosticoUnicoPorOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Diagnosticos_OrdenTrabajoId",
                table: "Diagnosticos");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinalizacion",
                table: "Diagnosticos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_OrdenTrabajoId",
                table: "Diagnosticos",
                column: "OrdenTrabajoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Diagnosticos_OrdenTrabajoId",
                table: "Diagnosticos");

            migrationBuilder.DropColumn(
                name: "FechaFinalizacion",
                table: "Diagnosticos");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_OrdenTrabajoId",
                table: "Diagnosticos",
                column: "OrdenTrabajoId");
        }
    }
}
