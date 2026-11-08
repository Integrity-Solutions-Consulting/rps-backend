using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class UniqueLiquidacionPorColaboradorYPeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_liquidacion_colaborador_id_periodo",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.CreateIndex(
                name: "ix_liquidacion_colaborador_id_periodo",
                schema: "rps",
                table: "liquidacion",
                columns: new[] { "colaborador_id", "periodo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_liquidacion_colaborador_id_periodo",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.CreateIndex(
                name: "ix_liquidacion_colaborador_id_periodo",
                schema: "rps",
                table: "liquidacion",
                columns: new[] { "colaborador_id", "periodo" });
        }
    }
}
