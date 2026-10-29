using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddHonorario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "honorario",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    numero_pago = table.Column<int>(type: "integer", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_honorario", x => x.id);
                    table.CheckConstraint("ck_honorario_estado", "estado IN ('Pendiente', 'Aprobado', 'Pagado', 'Anulado')");
                    table.CheckConstraint("ck_honorario_numero_pago", "numero_pago IN (1, 2)");
                    table.ForeignKey(
                        name: "fk_honorario_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_honorario_contrato_contrato_id",
                        column: x => x.contrato_id,
                        principalSchema: "rps",
                        principalTable: "contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_honorario_colaborador_id_contrato_id_periodo_numero_pago",
                schema: "rps",
                table: "honorario",
                columns: new[] { "colaborador_id", "contrato_id", "periodo", "numero_pago" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_honorario_contrato_id",
                schema: "rps",
                table: "honorario",
                column: "contrato_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "honorario",
                schema: "rps");
        }
    }
}
