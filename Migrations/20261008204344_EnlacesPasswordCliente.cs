using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class EnlacesPasswordCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnlacesPasswordCliente",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StampHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmailHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TeniaPassword = table.Column<bool>(type: "bit", nullable: false),
                    Finalidad = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaConsumida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaInvalidacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnlacesPasswordCliente", x => x.UsuarioId);
                    table.CheckConstraint("CK_EnlacesPasswordCliente_Vigencia", "[Finalidad] IN (1,2,3) AND [FechaExpiracion] > [FechaCreacion]");
                    table.ForeignKey(
                        name: "FK_EnlacesPasswordCliente_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnlacesPasswordCliente_TokenHash",
                table: "EnlacesPasswordCliente",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnlacesPasswordCliente");
        }
    }
}
