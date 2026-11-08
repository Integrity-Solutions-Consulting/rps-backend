using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class EliminarDocumentosMensuales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documento_mensual",
                schema: "rps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documento_mensual",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    marcado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documento_mensual", x => x.id);
                    table.ForeignKey(
                        name: "fk_documento_mensual_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documento_mensual_colaborador_id_periodo",
                schema: "rps",
                table: "documento_mensual",
                columns: new[] { "colaborador_id", "periodo" },
                unique: true);
        }
    }
}
