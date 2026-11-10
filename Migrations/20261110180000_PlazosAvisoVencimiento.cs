using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <summary>
    /// RN 13: plazos de aviso de vencimiento de contratos configurables (antes fijos en 30, 15 y 7 días).
    /// Las filas existentes quedan con 30, 15 y 7 por el valor por defecto de cada columna.
    /// </summary>
    public partial class PlazosAvisoVencimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dias_aviso_vencimiento1",
                schema: "rps",
                table: "configuracion_global",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "dias_aviso_vencimiento2",
                schema: "rps",
                table: "configuracion_global",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<int>(
                name: "dias_aviso_vencimiento3",
                schema: "rps",
                table: "configuracion_global",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.UpdateData(
                schema: "rps",
                table: "configuracion_global",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "dias_aviso_vencimiento1", "dias_aviso_vencimiento2", "dias_aviso_vencimiento3" },
                values: new object[] { 30, 15, 7 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dias_aviso_vencimiento1",
                schema: "rps",
                table: "configuracion_global");

            migrationBuilder.DropColumn(
                name: "dias_aviso_vencimiento2",
                schema: "rps",
                table: "configuracion_global");

            migrationBuilder.DropColumn(
                name: "dias_aviso_vencimiento3",
                schema: "rps",
                table: "configuracion_global");
        }
    }
}
