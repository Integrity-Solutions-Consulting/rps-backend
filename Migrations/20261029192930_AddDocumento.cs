using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documento",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: true),
                    liquidacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre_original = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ruta_archivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tamano_bytes = table.Column<long>(type: "bigint", nullable: false),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Subido"),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documento", x => x.id);
                    table.CheckConstraint("ck_documento_una_referencia", "(colaborador_id IS NOT NULL)::int + (contrato_id IS NOT NULL)::int + (liquidacion_id IS NOT NULL)::int = 1");
                    table.ForeignKey(
                        name: "fk_documento_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documento_contrato_contrato_id",
                        column: x => x.contrato_id,
                        principalSchema: "rps",
                        principalTable: "contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documento_liquidaciones_liquidacion_id",
                        column: x => x.liquidacion_id,
                        principalSchema: "rps",
                        principalTable: "liquidacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documento_colaborador_id",
                schema: "rps",
                table: "documento",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_documento_contrato_id",
                schema: "rps",
                table: "documento",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "ix_documento_liquidacion_id",
                schema: "rps",
                table: "documento",
                column: "liquidacion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documento",
                schema: "rps");
        }
    }
}
