using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFrecuenciaYCuentaBancaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "colaborador_frecuencia",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frecuencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vigente_desde_mes = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colaborador_frecuencia", x => x.id);
                    table.CheckConstraint("ck_colaborador_frecuencia_frecuencia", "frecuencia IN ('DosPagos', 'FinDeMes')");
                    table.CheckConstraint("ck_colaborador_frecuencia_primer_dia_mes", "EXTRACT(DAY FROM vigente_desde_mes) = 1");
                    table.ForeignKey(
                        name: "fk_colaborador_frecuencia_colaboradores_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuenta_bancaria",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    banco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tipo_cuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    numero_cuenta_cifrado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cuenta_bancaria", x => x.id);
                    table.ForeignKey(
                        name: "fk_cuenta_bancaria_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_colaborador_frecuencia_colaborador_id_vigente_desde_mes",
                schema: "rps",
                table: "colaborador_frecuencia",
                columns: new[] { "colaborador_id", "vigente_desde_mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cuenta_bancaria_colaborador_id",
                schema: "rps",
                table: "cuenta_bancaria",
                column: "colaborador_id",
                unique: true,
                filter: "activa = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "colaborador_frecuencia",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "cuenta_bancaria",
                schema: "rps");
        }
    }
}
