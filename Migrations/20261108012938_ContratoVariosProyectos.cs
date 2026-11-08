using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class ContratoVariosProyectos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contrato_proyectos_proyecto_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropIndex(
                name: "ix_contrato_colaborador_id_proyecto_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropIndex(
                name: "ix_contrato_proyecto_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "proyecto_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.CreateTable(
                name: "contrato_proyecto",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proyecto_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contrato_proyecto", x => x.id);
                    table.ForeignKey(
                        name: "fk_contrato_proyecto_contrato_contrato_id",
                        column: x => x.contrato_id,
                        principalSchema: "rps",
                        principalTable: "contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contrato_proyecto_proyectos_proyecto_id",
                        column: x => x.proyecto_id,
                        principalSchema: "rps",
                        principalTable: "proyecto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contrato_colaborador_id",
                schema: "rps",
                table: "contrato",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_contrato_proyecto_contrato_id_proyecto_id",
                schema: "rps",
                table: "contrato_proyecto",
                columns: new[] { "contrato_id", "proyecto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contrato_proyecto_proyecto_id",
                schema: "rps",
                table: "contrato_proyecto",
                column: "proyecto_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contrato_proyecto",
                schema: "rps");

            migrationBuilder.DropIndex(
                name: "ix_contrato_colaborador_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.AddColumn<Guid>(
                name: "proyecto_id",
                schema: "rps",
                table: "contrato",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_contrato_colaborador_id_proyecto_id",
                schema: "rps",
                table: "contrato",
                columns: new[] { "colaborador_id", "proyecto_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contrato_proyecto_id",
                schema: "rps",
                table: "contrato",
                column: "proyecto_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contrato_proyectos_proyecto_id",
                schema: "rps",
                table: "contrato",
                column: "proyecto_id",
                principalSchema: "rps",
                principalTable: "proyecto",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
