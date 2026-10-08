using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaniCar360.Migrations
{
    /// <inheritdoc />
    public partial class UnicidadDniPersona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nunca reescribir Personas protegidas: abortar antes de cualquier DDL.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT CONVERT(varchar(15), TRY_CONVERT(bigint, REPLACE(REPLACE(REPLACE([Dni], '.', ''), '-', ''), ' ', '')))
                    FROM [Personas] WHERE [Dni] IS NOT NULL
                    GROUP BY CONVERT(varchar(15), TRY_CONVERT(bigint, REPLACE(REPLACE(REPLACE([Dni], '.', ''), '-', ''), ' ', '')))
                    HAVING COUNT(*) > 1
                ) THROW 51012, 'Existen DNI duplicados o equivalentes. Resolver mediante el flujo autorizado antes de migrar.', 1;
                IF EXISTS (SELECT 1 FROM [Personas] WHERE [Dni] IS NOT NULL AND (
                    [Dni] COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%'
                    OR LEN([Dni]) = 0
                    OR TRY_CONVERT(bigint, [Dni]) IS NULL
                    OR DATALENGTH([Dni]) <> DATALENGTH(CONVERT(nvarchar(15), TRY_CONVERT(bigint, [Dni])))
                    OR [Dni] <> CONVERT(nvarchar(15), TRY_CONVERT(bigint, [Dni]))
                )) THROW 51013, 'Existen DNI no canonicos. No se modificaron datos; requiere revision autorizada.', 1;
                """);
            migrationBuilder.CreateIndex(
                name: "IX_Personas_Dni",
                table: "Personas",
                column: "Dni",
                unique: true,
                filter: "[Dni] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Personas_Dni",
                table: "Personas");
        }
    }
}
