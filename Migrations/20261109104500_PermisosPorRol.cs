using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class PermisosPorRol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "permisos",
                schema: "rps",
                table: "rol",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            // Roles ya sembrados: mismos permisos que PermisosModulo.PorDefecto.
            migrationBuilder.Sql(@"
UPDATE rps.rol SET permisos = ARRAY['colaboradores','contratos','anticipos','liquidaciones','documentos','exportaciones']
WHERE nombre IN ('Administrador', 'Administrador Contable');
UPDATE rps.rol SET permisos = ARRAY['colaboradores','liquidaciones','documentos']
WHERE nombre = 'Asistente Contable';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "permisos",
                schema: "rps",
                table: "rol");
        }
    }
}
