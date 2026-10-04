using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class RedisenarRecepcionVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No fabricar datos de recepcion para filas anteriores. Fallar antes de cualquier DDL.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [IngresosVehiculo])
                    THROW 51000, 'La migracion requiere IngresosVehiculo vacia: los datos de recepcion obligatorios no pueden inferirse. Preparar los datos de desarrollo antes de aplicar.', 1;
                IF EXISTS (SELECT [VehiculoId] FROM [DominiosVehiculares]
                           WHERE [FechaHasta] IS NULL GROUP BY [VehiculoId] HAVING COUNT(*) > 1)
                    THROW 51001, 'Existen vehiculos con mas de una titularidad vigente. Revisar los datos antes de aplicar.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesTrabajo_Vehiculos_VehiculoId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesTrabajo_VehiculoId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropIndex(
                name: "IX_DominiosVehiculares_VehiculoId",
                table: "DominiosVehiculares");

            migrationBuilder.DropColumn(
                name: "VehiculoId",
                table: "OrdenesTrabajo");

            migrationBuilder.AlterColumn<int>(
                name: "VehiculoId",
                table: "Turnos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Accesorios",
                table: "IngresosVehiculo",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ClienteDniSnapshot",
                table: "IngresosVehiculo",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ClienteNombreSnapshot",
                table: "IngresosVehiculo",
                type: "nvarchar(201)",
                maxLength: 201,
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "DatosVerificadosConCliente",
                table: "IngresosVehiculo",
                type: "bit",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "EstadoExterior",
                table: "IngresosVehiculo",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "Kilometraje",
                table: "IngresosVehiculo",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "NivelCombustible",
                table: "IngresosVehiculo",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesEstadoExterior",
                table: "IngresosVehiculo",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtrosAccesorios",
                table: "IngresosVehiculo",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistradoPorUsuarioId",
                table: "IngresosVehiculo",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "VehiculoDescripcionSnapshot",
                table: "IngresosVehiculo",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "VehiculoId",
                table: "IngresosVehiculo",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "VehiculoPatenteSnapshot",
                table: "IngresosVehiculo",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_IngresosVehiculo_RegistradoPorUsuarioId",
                table: "IngresosVehiculo",
                column: "RegistradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_IngresosVehiculo_VehiculoId",
                table: "IngresosVehiculo",
                column: "VehiculoId",
                unique: true,
                filter: "[FechaEgreso] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngresosVehiculo_Accesorios",
                table: "IngresosVehiculo",
                sql: "[Accesorios] >= 0 AND [Accesorios] <= 63");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngresosVehiculo_Combustible",
                table: "IngresosVehiculo",
                sql: "[NivelCombustible] IN (0, 1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngresosVehiculo_EstadoExterior",
                table: "IngresosVehiculo",
                sql: "([EstadoExterior] = 0 AND [ObservacionesEstadoExterior] IS NULL) OR ([EstadoExterior] = 1 AND [ObservacionesEstadoExterior] IS NOT NULL AND LEN(LTRIM(RTRIM([ObservacionesEstadoExterior]))) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngresosVehiculo_Fechas",
                table: "IngresosVehiculo",
                sql: "[FechaEgreso] IS NULL OR [FechaEgreso] >= [FechaIngreso]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngresosVehiculo_Kilometraje",
                table: "IngresosVehiculo",
                sql: "[Kilometraje] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngresosVehiculo_OtrosAccesorios",
                table: "IngresosVehiculo",
                sql: "(([Accesorios] & 32) = 0 AND [OtrosAccesorios] IS NULL) OR (([Accesorios] & 32) = 32 AND [OtrosAccesorios] IS NOT NULL AND LEN(LTRIM(RTRIM([OtrosAccesorios]))) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngresosVehiculo_Snapshot",
                table: "IngresosVehiculo",
                sql: "LEN(LTRIM(RTRIM([ClienteNombreSnapshot]))) > 0 AND LEN(LTRIM(RTRIM([ClienteDniSnapshot]))) > 0 AND LEN(LTRIM(RTRIM([VehiculoPatenteSnapshot]))) > 0 AND LEN(LTRIM(RTRIM([VehiculoDescripcionSnapshot]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IngresosVehiculo_Verificacion",
                table: "IngresosVehiculo",
                sql: "[DatosVerificadosConCliente] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DominiosVehiculares_VehiculoId",
                table: "DominiosVehiculares",
                column: "VehiculoId",
                unique: true,
                filter: "[FechaHasta] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_IngresosVehiculo_Usuarios_RegistradoPorUsuarioId",
                table: "IngresosVehiculo",
                column: "RegistradoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IngresosVehiculo_Vehiculos_VehiculoId",
                table: "IngresosVehiculo",
                column: "VehiculoId",
                principalTable: "Vehiculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // El esquema anterior no representa reservas sin vehiculo ni la nueva constancia.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [IngresosVehiculo])
                    THROW 51002, 'No se puede revertir mientras existan recepciones: se perderia la constancia registrada.', 1;
                IF EXISTS (SELECT 1 FROM [Turnos] WHERE [VehiculoId] IS NULL)
                    THROW 51003, 'No se puede revertir: existen turnos sin vehiculo previsto.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_IngresosVehiculo_Usuarios_RegistradoPorUsuarioId",
                table: "IngresosVehiculo");

            migrationBuilder.DropForeignKey(
                name: "FK_IngresosVehiculo_Vehiculos_VehiculoId",
                table: "IngresosVehiculo");

            migrationBuilder.DropIndex(
                name: "IX_IngresosVehiculo_RegistradoPorUsuarioId",
                table: "IngresosVehiculo");

            migrationBuilder.DropIndex(
                name: "IX_IngresosVehiculo_VehiculoId",
                table: "IngresosVehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngresosVehiculo_Accesorios",
                table: "IngresosVehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngresosVehiculo_Combustible",
                table: "IngresosVehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngresosVehiculo_EstadoExterior",
                table: "IngresosVehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngresosVehiculo_Fechas",
                table: "IngresosVehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngresosVehiculo_Kilometraje",
                table: "IngresosVehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngresosVehiculo_OtrosAccesorios",
                table: "IngresosVehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngresosVehiculo_Snapshot",
                table: "IngresosVehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IngresosVehiculo_Verificacion",
                table: "IngresosVehiculo");

            migrationBuilder.DropIndex(
                name: "IX_DominiosVehiculares_VehiculoId",
                table: "DominiosVehiculares");

            migrationBuilder.DropColumn(
                name: "Accesorios",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "ClienteDniSnapshot",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "ClienteNombreSnapshot",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "DatosVerificadosConCliente",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "EstadoExterior",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "Kilometraje",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "NivelCombustible",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "ObservacionesEstadoExterior",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "OtrosAccesorios",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "RegistradoPorUsuarioId",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "VehiculoDescripcionSnapshot",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "VehiculoId",
                table: "IngresosVehiculo");

            migrationBuilder.DropColumn(
                name: "VehiculoPatenteSnapshot",
                table: "IngresosVehiculo");

            migrationBuilder.AlterColumn<int>(
                name: "VehiculoId",
                table: "Turnos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehiculoId",
                table: "OrdenesTrabajo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_VehiculoId",
                table: "OrdenesTrabajo",
                column: "VehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_DominiosVehiculares_VehiculoId",
                table: "DominiosVehiculares",
                column: "VehiculoId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesTrabajo_Vehiculos_VehiculoId",
                table: "OrdenesTrabajo",
                column: "VehiculoId",
                principalTable: "Vehiculos",
                principalColumn: "Id");
        }
    }
}
