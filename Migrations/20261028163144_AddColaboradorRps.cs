using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddColaboradorRps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rps");

            migrationBuilder.CreateTable(
                name: "colaborador_rps",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tmr_colaborador_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    clasificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    estado_configuracion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colaborador_rps", x => x.id);
                    table.CheckConstraint("ck_colaborador_rps_clasificacion", "clasificacion IN ('Profesional', 'Junior')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_colaborador_rps_tmr_colaborador_id",
                schema: "rps",
                table: "colaborador_rps",
                column: "tmr_colaborador_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "colaborador_rps",
                schema: "rps");
        }
    }
}
