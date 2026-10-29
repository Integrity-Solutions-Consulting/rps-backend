using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contrato",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_servicio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tarifa = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contrato", x => x.id);
                    table.CheckConstraint("ck_contrato_fecha_fin", "fecha_fin IS NULL OR fecha_fin >= fecha_inicio");
                    table.ForeignKey(
                        name: "fk_contrato_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contrato_colaborador_id",
                schema: "rps",
                table: "contrato",
                column: "colaborador_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contrato",
                schema: "rps");
        }
    }
}
