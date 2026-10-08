using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class PermitirInvitacionClientePublica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "EmitidaPorUsuarioId",
                table: "InvitacionesCliente",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [InvitacionesCliente] WHERE [EmitidaPorUsuarioId] IS NULL)
                    THROW 51000, 'No se puede revertir: existen solicitudes web sin emisor interno.', 1;
                """);
            migrationBuilder.AlterColumn<int>(
                name: "EmitidaPorUsuarioId",
                table: "InvitacionesCliente",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
