using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using rps_backend.Persistence;

#nullable disable

namespace rps_backend.Migrations
{
    // Solo datos (no cambia tablas, por eso no lleva .Designer.cs ni toca el snapshot): agrega el
    // permiso nuevo "configuracion" a los roles que ya existen y lo tenían de hecho.
    // El Asistente Contable y los roles creados a mano quedan sin él; se activa en Configuración > Roles.
    [DbContext(typeof(RpsDbContext))]
    [Migration("20261120180000_PermisoConfiguracion")]
    public partial class PermisoConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE rps.rol SET permisos = array_append(permisos, 'configuracion')
WHERE nombre IN ('Administrador', 'Administrador Contable') AND NOT ('configuracion' = ANY(permisos));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE rps.rol SET permisos = array_remove(permisos, 'configuracion');");
        }
    }
}
