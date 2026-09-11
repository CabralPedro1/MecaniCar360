using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class EliminarFechaFinalizacionDiagnostico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaFinalizacion",
                table: "Diagnosticos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinalizacion",
                table: "Diagnosticos",
                type: "datetime2",
                nullable: true);
        }
    }
}
