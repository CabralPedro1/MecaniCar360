using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSecurityStampUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Usuarios",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            // NEWID se evalua por fila para los usuarios existentes.
            migrationBuilder.Sql(
                "UPDATE [Usuarios] SET [SecurityStamp] = REPLACE(CONVERT(varchar(36), NEWID()), '-', '') WHERE [SecurityStamp] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "SecurityStamp",
                table: "Usuarios",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Usuarios");
        }
    }
}
