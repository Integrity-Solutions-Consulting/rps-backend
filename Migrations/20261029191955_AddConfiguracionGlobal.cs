using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracion_global",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    razon_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ruc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    dia_pago_fin_de_mes = table.Column<int>(type: "integer", nullable: false),
                    dia_pago_dos_pagos1 = table.Column<int>(type: "integer", nullable: false),
                    dia_pago_dos_pagos2 = table.Column<int>(type: "integer", nullable: false),
                    tope_maximo_anticipo = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracion_global", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "rps",
                table: "configuracion_global",
                columns: new[] { "id", "actualizado_en", "dia_pago_dos_pagos1", "dia_pago_dos_pagos2", "dia_pago_fin_de_mes", "moneda", "razon_social", "ruc", "tope_maximo_anticipo" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15, 30, 30, "USD", "", "", 1000000.00m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracion_global",
                schema: "rps");
        }
    }
}
