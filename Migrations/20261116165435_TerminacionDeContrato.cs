using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class TerminacionDeContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "fecha_terminacion",
                schema: "rps",
                table: "contrato",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_terminacion",
                schema: "rps",
                table: "contrato",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_terminacion",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "motivo_terminacion",
                schema: "rps",
                table: "contrato");
        }
    }
}
