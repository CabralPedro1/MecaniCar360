using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AgregarInvitacionCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvitacionesCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonaId = table.Column<int>(type: "int", nullable: false),
                    EmailDestino = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaConsumida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaInvalidacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmitidaPorUsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitacionesCliente", x => x.Id);
                    table.CheckConstraint("CK_InvitacionesCliente_Vigencia", "[FechaExpiracion] = DATEADD(hour, 48, [FechaCreacion])");
                    table.ForeignKey(
                        name: "FK_InvitacionesCliente_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvitacionesCliente_Usuarios_EmitidaPorUsuarioId",
                        column: x => x.EmitidaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionesCliente_EmitidaPorUsuarioId",
                table: "InvitacionesCliente",
                column: "EmitidaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionesCliente_PersonaId_FechaConsumida_FechaInvalidacion",
                table: "InvitacionesCliente",
                columns: new[] { "PersonaId", "FechaConsumida", "FechaInvalidacion" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionesCliente_TokenHash",
                table: "InvitacionesCliente",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitacionesCliente");
        }
    }
}
