using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCambioTmr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cambio_tmr",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proyecto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    valor_anterior = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    valor_nuevo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    periodo_desde = table.Column<DateOnly>(type: "date", nullable: true),
                    detectado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revisado = table.Column<bool>(type: "boolean", nullable: false),
                    revisado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revisado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cambio_tmr", x => x.id);
                    table.ForeignKey(
                        name: "fk_cambio_tmr_colaboradores_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cambio_tmr_proyectos_proyecto_id",
                        column: x => x.proyecto_id,
                        principalSchema: "rps",
                        principalTable: "proyecto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cambio_tmr_colaborador_id",
                schema: "rps",
                table: "cambio_tmr",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_cambio_tmr_proyecto_id",
                schema: "rps",
                table: "cambio_tmr",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_cambio_tmr_revisado",
                schema: "rps",
                table: "cambio_tmr",
                column: "revisado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cambio_tmr",
                schema: "rps");
        }
    }
}
