using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class QuitarExcelDePago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exportacion_item",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "exportacion",
                schema: "rps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exportacion",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contenido = table.Column<byte[]>(type: "bytea", nullable: false),
                    corte = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    generado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    generado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_archivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exportacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exportacion_item",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exportacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    liquidacion_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exportacion_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_exportacion_item_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exportacion_item_exportacion_exportacion_id",
                        column: x => x.exportacion_id,
                        principalSchema: "rps",
                        principalTable: "exportacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exportacion_item_liquidaciones_liquidacion_id",
                        column: x => x.liquidacion_id,
                        principalSchema: "rps",
                        principalTable: "liquidacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_periodo_corte_version",
                schema: "rps",
                table: "exportacion",
                columns: new[] { "periodo", "corte", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_item_colaborador_id",
                schema: "rps",
                table: "exportacion_item",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_item_exportacion_id_colaborador_id",
                schema: "rps",
                table: "exportacion_item",
                columns: new[] { "exportacion_id", "colaborador_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exportacion_item_liquidacion_id",
                schema: "rps",
                table: "exportacion_item",
                column: "liquidacion_id");
        }
    }
}
