using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAnticipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "descuento",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "anticipo",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    fecha_solicitud = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    honorario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anticipo", x => x.id);
                    table.CheckConstraint("ck_anticipo_estado", "estado IN ('Pendiente', 'Aprobado', 'Descontado', 'Anulado')");
                    table.ForeignKey(
                        name: "fk_anticipo_colaboradores_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_anticipo_honorarios_honorario_id",
                        column: x => x.honorario_id,
                        principalSchema: "rps",
                        principalTable: "honorario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_anticipo_colaborador_id",
                schema: "rps",
                table: "anticipo",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_anticipo_honorario_id",
                schema: "rps",
                table: "anticipo",
                column: "honorario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anticipo",
                schema: "rps");

            migrationBuilder.DropColumn(
                name: "descuento",
                schema: "rps",
                table: "honorario");
        }
    }
}
