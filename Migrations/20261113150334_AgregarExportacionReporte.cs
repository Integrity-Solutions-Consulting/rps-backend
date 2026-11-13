using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarExportacionReporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exportacion_reporte",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vista = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    formato = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: true),
                    nombre_archivo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    registros = table.Column<int>(type: "integer", nullable: false),
                    total_neto = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    huella_datos = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    generado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exportacion_reporte", x => x.id);
                    table.ForeignKey(
                        name: "fk_exportacion_reporte_usuarios_generado_por_usuario_id",
                        column: x => x.generado_por_usuario_id,
                        principalSchema: "rps",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_reporte_generado_en",
                schema: "rps",
                table: "exportacion_reporte",
                column: "generado_en");

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_reporte_generado_por_usuario_id",
                schema: "rps",
                table: "exportacion_reporte",
                column: "generado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_reporte_periodo_generado_en",
                schema: "rps",
                table: "exportacion_reporte",
                columns: new[] { "periodo", "generado_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exportacion_reporte",
                schema: "rps");
        }
    }
}
