using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class VigenciaPorFecha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_contrato_id_periodo",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_honorario_por_contrato",
                schema: "rps",
                table: "honorario");

            migrationBuilder.RenameColumn(
                name: "mes_inicio_vigencia",
                schema: "rps",
                table: "condicion_economica",
                newName: "fecha_inicio_vigencia");

            migrationBuilder.RenameIndex(
                name: "ix_condicion_economica_colaborador_id_proyecto_id_mes_inicio_v",
                schema: "rps",
                table: "condicion_economica",
                newName: "ix_condicion_economica_colaborador_id_proyecto_id_fecha_inicio");

            migrationBuilder.AddColumn<DateOnly>(
                name: "fecha_vigencia",
                schema: "rps",
                table: "adenda",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Las adendas que ya existían regían desde el día en que se registraron.
            migrationBuilder.Sql("UPDATE rps.adenda SET fecha_vigencia = fecha_registro;");

            migrationBuilder.CreateIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_contrato_id_periodo_co",
                schema: "rps",
                table: "honorario",
                columns: new[] { "colaborador_id", "proyecto_id", "contrato_id", "periodo", "condicion_economica_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_honorario_por_contrato",
                schema: "rps",
                table: "honorario",
                columns: new[] { "contrato_id", "periodo", "condicion_economica_id" },
                unique: true,
                filter: "proyecto_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_contrato_id_periodo_co",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_honorario_por_contrato",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "fecha_vigencia",
                schema: "rps",
                table: "adenda");

            migrationBuilder.RenameColumn(
                name: "fecha_inicio_vigencia",
                schema: "rps",
                table: "condicion_economica",
                newName: "mes_inicio_vigencia");

            migrationBuilder.RenameIndex(
                name: "ix_condicion_economica_colaborador_id_proyecto_id_fecha_inicio",
                schema: "rps",
                table: "condicion_economica",
                newName: "ix_condicion_economica_colaborador_id_proyecto_id_mes_inicio_v");

            migrationBuilder.CreateIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_contrato_id_periodo",
                schema: "rps",
                table: "honorario",
                columns: new[] { "colaborador_id", "proyecto_id", "contrato_id", "periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_honorario_por_contrato",
                schema: "rps",
                table: "honorario",
                columns: new[] { "contrato_id", "periodo" },
                unique: true,
                filter: "proyecto_id IS NULL");
        }
    }
}
