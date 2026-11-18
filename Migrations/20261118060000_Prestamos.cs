using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class Prestamos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cuotas de préstamos descontadas en cada corte (0 en las liquidaciones que ya existen).
            migrationBuilder.AddColumn<decimal>(
                name: "descuento_prestamos",
                schema: "rps",
                table: "corte",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            // Sin columna "xmin" a propósito: es una columna de sistema que PostgreSQL ya tiene en
            // cada tabla (ver AgregarAuditoriaIdempotenciaConcurrencia).
            migrationBuilder.CreateTable(
                name: "prestamo",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prestamo", x => x.id);
                    table.CheckConstraint("ck_prestamo_monto", "monto > 0");
                    table.ForeignKey(
                        name: "fk_prestamo_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuota_prestamo",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    prestamo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    corte = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    importe = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cuota_prestamo", x => x.id);
                    table.CheckConstraint("ck_cuota_prestamo_importe", "importe > 0");
                    table.ForeignKey(
                        name: "fk_cuota_prestamo_prestamo_prestamo_id",
                        column: x => x.prestamo_id,
                        principalSchema: "rps",
                        principalTable: "prestamo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_prestamo_colaborador_id",
                schema: "rps",
                table: "prestamo",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuota_prestamo_prestamo_id_periodo_corte",
                schema: "rps",
                table: "cuota_prestamo",
                columns: new[] { "prestamo_id", "periodo", "corte" },
                unique: true);

            // Módulo nuevo en Roles: los roles de administración lo tienen desde el inicio.
            migrationBuilder.Sql(@"
UPDATE rps.rol SET permisos = array_append(permisos, 'prestamos')
WHERE nombre IN ('Administrador', 'Administrador Contable') AND NOT ('prestamos' = ANY(permisos));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE rps.rol SET permisos = array_remove(permisos, 'prestamos');");

            migrationBuilder.DropTable(
                name: "cuota_prestamo",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "prestamo",
                schema: "rps");

            migrationBuilder.DropColumn(
                name: "descuento_prestamos",
                schema: "rps",
                table: "corte");
        }
    }
}
