using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class ProtegerIntegridadCalificacionTrabajo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Calificaciones] WITH (UPDLOCK, HOLDLOCK)
                           WHERE [Puntuacion] < 1 OR [Puntuacion] > 5)
                    THROW 51000, 'Existen calificaciones fuera de rango 1..5. Revise los datos antes de migrar.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Calificaciones_Personas_ClienteId",
                table: "Calificaciones");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Calificaciones_Puntuacion",
                table: "Calificaciones",
                sql: "[Puntuacion] >= 1 AND [Puntuacion] <= 5");

            migrationBuilder.AddForeignKey(
                name: "FK_Calificaciones_Personas_ClienteId",
                table: "Calificaciones",
                column: "ClienteId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calificaciones_Personas_ClienteId",
                table: "Calificaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Calificaciones_Puntuacion",
                table: "Calificaciones");

            migrationBuilder.AddForeignKey(
                name: "FK_Calificaciones_Personas_ClienteId",
                table: "Calificaciones",
                column: "ClienteId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
