using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVersionadoPresupuestoYEvidencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiagnosticoHistoriales_Diagnosticos_DiagnosticoId",
                table: "DiagnosticoHistoriales");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidencias_OrdenesTrabajo_OrdenTrabajoId",
                table: "Evidencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidencias_Usuarios_SubidaPorUsuarioId",
                table: "Evidencias");

            migrationBuilder.AddColumn<int>(
                name: "RegistradoPorUsuarioId",
                table: "DiagnosticoHistoriales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoRegistro",
                table: "DiagnosticoHistoriales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DiagnosticoHistorialEvidencias",
                columns: table => new
                {
                    DiagnosticoHistorialId = table.Column<int>(type: "int", nullable: false),
                    EvidenciaTrabajoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticoHistorialEvidencias", x => new { x.DiagnosticoHistorialId, x.EvidenciaTrabajoId });
                    table.ForeignKey(
                        name: "FK_DiagnosticoHistorialEvidencias_DiagnosticoHistoriales_DiagnosticoHistorialId",
                        column: x => x.DiagnosticoHistorialId,
                        principalTable: "DiagnosticoHistoriales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiagnosticoHistorialEvidencias_Evidencias_EvidenciaTrabajoId",
                        column: x => x.EvidenciaTrabajoId,
                        principalTable: "Evidencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresupuestoVersiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PresupuestoId = table.Column<int>(type: "int", nullable: false),
                    NumeroVersion = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnviadaPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaDecision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecididaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresupuestoVersiones", x => x.Id);
                    table.CheckConstraint("CK_PresupuestoVersiones_NumeroVersion", "[NumeroVersion] > 0");
                    table.CheckConstraint("CK_PresupuestoVersiones_Total", "[Total] >= 0");
                    table.ForeignKey(
                        name: "FK_PresupuestoVersiones_Presupuestos_PresupuestoId",
                        column: x => x.PresupuestoId,
                        principalTable: "Presupuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresupuestoVersiones_Usuarios_DecididaPorUsuarioId",
                        column: x => x.DecididaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresupuestoVersiones_Usuarios_EnviadaPorUsuarioId",
                        column: x => x.EnviadaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresupuestoVersionEvidencias",
                columns: table => new
                {
                    PresupuestoVersionId = table.Column<int>(type: "int", nullable: false),
                    EvidenciaTrabajoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresupuestoVersionEvidencias", x => new { x.PresupuestoVersionId, x.EvidenciaTrabajoId });
                    table.ForeignKey(
                        name: "FK_PresupuestoVersionEvidencias_Evidencias_EvidenciaTrabajoId",
                        column: x => x.EvidenciaTrabajoId,
                        principalTable: "Evidencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresupuestoVersionEvidencias_PresupuestoVersiones_PresupuestoVersionId",
                        column: x => x.PresupuestoVersionId,
                        principalTable: "PresupuestoVersiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresupuestoVersionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PresupuestoVersionId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RepuestoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresupuestoVersionItems", x => x.Id);
                    table.CheckConstraint("CK_PresupuestoVersionItems_Cantidad", "[Cantidad] > 0");
                    table.CheckConstraint("CK_PresupuestoVersionItems_PrecioUnitario", "[PrecioUnitario] >= 0");
                    table.ForeignKey(
                        name: "FK_PresupuestoVersionItems_PresupuestoVersiones_PresupuestoVersionId",
                        column: x => x.PresupuestoVersionId,
                        principalTable: "PresupuestoVersiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresupuestoVersionItems_Repuestos_RepuestoId",
                        column: x => x.RepuestoId,
                        principalTable: "Repuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticoHistoriales_RegistradoPorUsuarioId",
                table: "DiagnosticoHistoriales",
                column: "RegistradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticoHistorialEvidencias_EvidenciaTrabajoId",
                table: "DiagnosticoHistorialEvidencias",
                column: "EvidenciaTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoVersiones_DecididaPorUsuarioId",
                table: "PresupuestoVersiones",
                column: "DecididaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoVersiones_EnviadaPorUsuarioId",
                table: "PresupuestoVersiones",
                column: "EnviadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoVersiones_PresupuestoId_NumeroVersion",
                table: "PresupuestoVersiones",
                columns: new[] { "PresupuestoId", "NumeroVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoVersionEvidencias_EvidenciaTrabajoId",
                table: "PresupuestoVersionEvidencias",
                column: "EvidenciaTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoVersionItems_PresupuestoVersionId",
                table: "PresupuestoVersionItems",
                column: "PresupuestoVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoVersionItems_RepuestoId",
                table: "PresupuestoVersionItems",
                column: "RepuestoId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiagnosticoHistoriales_Diagnosticos_DiagnosticoId",
                table: "DiagnosticoHistoriales",
                column: "DiagnosticoId",
                principalTable: "Diagnosticos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiagnosticoHistoriales_Usuarios_RegistradoPorUsuarioId",
                table: "DiagnosticoHistoriales",
                column: "RegistradoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evidencias_OrdenesTrabajo_OrdenTrabajoId",
                table: "Evidencias",
                column: "OrdenTrabajoId",
                principalTable: "OrdenesTrabajo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evidencias_Usuarios_SubidaPorUsuarioId",
                table: "Evidencias",
                column: "SubidaPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiagnosticoHistoriales_Diagnosticos_DiagnosticoId",
                table: "DiagnosticoHistoriales");

            migrationBuilder.DropForeignKey(
                name: "FK_DiagnosticoHistoriales_Usuarios_RegistradoPorUsuarioId",
                table: "DiagnosticoHistoriales");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidencias_OrdenesTrabajo_OrdenTrabajoId",
                table: "Evidencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidencias_Usuarios_SubidaPorUsuarioId",
                table: "Evidencias");

            migrationBuilder.DropTable(
                name: "DiagnosticoHistorialEvidencias");

            migrationBuilder.DropTable(
                name: "PresupuestoVersionEvidencias");

            migrationBuilder.DropTable(
                name: "PresupuestoVersionItems");

            migrationBuilder.DropTable(
                name: "PresupuestoVersiones");

            migrationBuilder.DropIndex(
                name: "IX_DiagnosticoHistoriales_RegistradoPorUsuarioId",
                table: "DiagnosticoHistoriales");

            migrationBuilder.DropColumn(
                name: "RegistradoPorUsuarioId",
                table: "DiagnosticoHistoriales");

            migrationBuilder.DropColumn(
                name: "TipoRegistro",
                table: "DiagnosticoHistoriales");

            migrationBuilder.AddForeignKey(
                name: "FK_DiagnosticoHistoriales_Diagnosticos_DiagnosticoId",
                table: "DiagnosticoHistoriales",
                column: "DiagnosticoId",
                principalTable: "Diagnosticos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Evidencias_OrdenesTrabajo_OrdenTrabajoId",
                table: "Evidencias",
                column: "OrdenTrabajoId",
                principalTable: "OrdenesTrabajo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Evidencias_Usuarios_SubidaPorUsuarioId",
                table: "Evidencias",
                column: "SubidaPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
