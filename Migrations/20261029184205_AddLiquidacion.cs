using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLiquidacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "liquidacion_id",
                schema: "rps",
                table: "honorario",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "liquidacion",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    monto_total = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Generada"),
                    pagada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_liquidacion", x => x.id);
                    table.CheckConstraint("ck_liquidacion_estado", "estado IN ('Generada', 'Pagada')");
                    table.ForeignKey(
                        name: "fk_liquidacion_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_honorario_liquidacion_id",
                schema: "rps",
                table: "honorario",
                column: "liquidacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_liquidacion_colaborador_id_periodo",
                schema: "rps",
                table: "liquidacion",
                columns: new[] { "colaborador_id", "periodo" });

            migrationBuilder.AddForeignKey(
                name: "fk_honorario_liquidaciones_liquidacion_id",
                schema: "rps",
                table: "honorario",
                column: "liquidacion_id",
                principalSchema: "rps",
                principalTable: "liquidacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_honorario_liquidaciones_liquidacion_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropTable(
                name: "liquidacion",
                schema: "rps");

            migrationBuilder.DropIndex(
                name: "ix_honorario_liquidacion_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "liquidacion_id",
                schema: "rps",
                table: "honorario");
        }
    }
}
