using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarInconsistenciasCorte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ajuste_por_prorrateo",
                schema: "rps",
                table: "corte",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "recuperacion_no_aplicada",
                schema: "rps",
                table: "corte",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ajuste_por_prorrateo",
                schema: "rps",
                table: "corte");

            migrationBuilder.DropColumn(
                name: "recuperacion_no_aplicada",
                schema: "rps",
                table: "corte");
        }
    }
}
