using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class CancelacionCondicionYExportacionPorCorte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_condicion_economica_colaborador_id_proyecto_id_mes_inicio_v",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelado_en",
                schema: "rps",
                table: "condicion_economica",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "condicion_economica_movimiento",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condicion_economica_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_condicion_economica_movimiento", x => x.id);
                    table.ForeignKey(
                        name: "fk_condicion_economica_movimiento_condicion_economica_condicio",
                        column: x => x.condicion_economica_id,
                        principalSchema: "rps",
                        principalTable: "condicion_economica",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exportacion_pago",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    corte = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    generada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    nombre_archivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contenido = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exportacion_pago", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exportacion_pago_detalle",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exportacion_pago_id = table.Column<Guid>(type: "uuid", nullable: false),
                    liquidacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tmr_colaborador_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    banco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cuenta_enmascarada = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exportacion_pago_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_exportacion_pago_detalle_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exportacion_pago_detalle_exportacion_pago_exportacion_pago_",
                        column: x => x.exportacion_pago_id,
                        principalSchema: "rps",
                        principalTable: "exportacion_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exportacion_pago_detalle_liquidaciones_liquidacion_id",
                        column: x => x.liquidacion_id,
                        principalSchema: "rps",
                        principalTable: "liquidacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_colaborador_id_proyecto_id_mes_inicio_v",
                schema: "rps",
                table: "condicion_economica",
                columns: new[] { "colaborador_id", "proyecto_id", "mes_inicio_vigencia" },
                unique: true,
                filter: "cancelado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_movimiento_condicion_economica_id",
                schema: "rps",
                table: "condicion_economica_movimiento",
                column: "condicion_economica_id");

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_pago_periodo_corte_version",
                schema: "rps",
                table: "exportacion_pago",
                columns: new[] { "periodo", "corte", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_pago_detalle_colaborador_id",
                schema: "rps",
                table: "exportacion_pago_detalle",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_pago_detalle_exportacion_pago_id",
                schema: "rps",
                table: "exportacion_pago_detalle",
                column: "exportacion_pago_id");

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_pago_detalle_liquidacion_id",
                schema: "rps",
                table: "exportacion_pago_detalle",
                column: "liquidacion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "condicion_economica_movimiento",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "exportacion_pago_detalle",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "exportacion_pago",
                schema: "rps");

            migrationBuilder.DropIndex(
                name: "ix_condicion_economica_colaborador_id_proyecto_id_mes_inicio_v",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.DropColumn(
                name: "cancelado_en",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_colaborador_id_proyecto_id_mes_inicio_v",
                schema: "rps",
                table: "condicion_economica",
                columns: new[] { "colaborador_id", "proyecto_id", "mes_inicio_vigencia" },
                unique: true);
        }
    }
}
