using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AvisoModificacionLiquidacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "modificada_en",
                schema: "rps",
                table: "liquidacion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_modificacion",
                schema: "rps",
                table: "liquidacion",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "revision_anulada",
                schema: "rps",
                table: "liquidacion",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "modificada_en",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.DropColumn(
                name: "motivo_modificacion",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.DropColumn(
                name: "revision_anulada",
                schema: "rps",
                table: "liquidacion");
        }
    }
}
