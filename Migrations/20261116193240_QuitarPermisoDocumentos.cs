using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class QuitarPermisoDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El módulo Documentos no existe por ahora: se quita de los permisos de los roles.
            migrationBuilder.Sql("UPDATE rps.rol SET permisos = array_remove(permisos, 'documentos');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE rps.rol SET permisos = array_append(permisos, 'documentos')
WHERE nombre IN ('Administrador', 'Administrador Contable', 'Asistente Contable') AND NOT ('documentos' = ANY(permisos));");
        }
    }
}
