using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using rps_backend.Persistence;

#nullable disable

namespace rps_backend.Migrations
{
    // Rol a pagar: factura mensual de cada colaborador (plazo día 7, gracia hasta el 14), días
    // descontados del pago por contrato y observaciones. Una fila por colaborador y mes.
    // Escrita a mano (sin .Designer.cs); el snapshot se actualizó con la misma tabla.
    [DbContext(typeof(RpsDbContext))]
    [Migration("20261123210000_ControlMensual")]
    public partial class AgregarControlMensual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "control_mensual",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    factura_recibida_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    factura_marcada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dias_descontados = table.Column<int>(type: "integer", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    dias_actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dias_actualizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_control_mensual", x => x.id);
                    table.CheckConstraint("ck_control_mensual_dias_descontados", "dias_descontados >= 0 AND dias_descontados <= 31");
                    table.ForeignKey(
                        name: "fk_control_mensual_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_control_mensual_colaborador_id_periodo",
                schema: "rps",
                table: "control_mensual",
                columns: new[] { "colaborador_id", "periodo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "control_mensual",
                schema: "rps");
        }
    }
}
