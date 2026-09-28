using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class AgregaAutenticacionHibrida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "IdentificadorExterno",
                table: "Usuarios",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<int>(
                name: "ProveedorAutenticacion",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Personas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Personas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Personas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Personas",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "Apellido",
                table: "Personas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ProveedorAutenticacion_IdentificadorExterno",
                table: "Usuarios",
                columns: new[] { "ProveedorAutenticacion", "IdentificadorExterno" },
                unique: true,
                filter: "[IdentificadorExterno] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_ProveedorAutenticacion",
                table: "Usuarios",
                sql: "([ProveedorAutenticacion] = 0 AND [PasswordHash] IS NOT NULL AND LEN(LTRIM(RTRIM([PasswordHash]))) > 0 AND [IdentificadorExterno] IS NULL) OR ([ProveedorAutenticacion] = 1 AND [PasswordHash] IS NULL AND [IdentificadorExterno] IS NOT NULL AND LEN(LTRIM(RTRIM([IdentificadorExterno]))) > 0 AND [PrimerLogin] = 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mantener los datos estables hasta terminar la transacción de la migración.
            // Credenciales = 0; cualquier otro proveedor no existe en el esquema anterior.
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM [Usuarios] WITH (TABLOCKX, HOLDLOCK)
    WHERE [ProveedorAutenticacion] <> 0
       OR [IdentificadorExterno] IS NOT NULL
       OR [PasswordHash] IS NULL
       OR LEN(LTRIM(RTRIM([PasswordHash]))) = 0
)
BEGIN
    THROW 51000, 'No se puede revertir AgregaAutenticacionHibrida: existen usuarios incompatibles con el esquema de credenciales anterior.', 1;
END;

IF EXISTS (
    SELECT 1 FROM [Personas] WITH (TABLOCKX, HOLDLOCK)
    WHERE [Nombre] IS NULL OR [Apellido] IS NULL OR [Dni] IS NULL
       OR [Telefono] IS NULL OR [Email] IS NULL
)
BEGIN
    THROW 51001, 'No se puede revertir AgregaAutenticacionHibrida: existen personas con datos NULL requeridos por el esquema anterior.', 1;
END;");
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ProveedorAutenticacion_IdentificadorExterno",
                table: "Usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_ProveedorAutenticacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IdentificadorExterno",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ProveedorAutenticacion",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Personas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Personas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Personas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Personas",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Apellido",
                table: "Personas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
