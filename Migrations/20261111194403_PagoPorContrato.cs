using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class PagoPorContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_periodo",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_honorario_contrato_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_condicion_economica_contrato_id",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.DropIndex(
                name: "ix_condicion_economica_grupo_id",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.DropColumn(
                name: "grupo_id",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.AlterColumn<Guid>(
                name: "proyecto_id",
                schema: "rps",
                table: "honorario",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "proyecto_id",
                schema: "rps",
                table: "condicion_economica",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

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

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_por_contrato",
                schema: "rps",
                table: "condicion_economica",
                columns: new[] { "contrato_id", "mes_inicio_vigencia" },
                unique: true,
                filter: "proyecto_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_contrato_id_periodo",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_honorario_por_contrato",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_condicion_economica_por_contrato",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.AlterColumn<Guid>(
                name: "proyecto_id",
                schema: "rps",
                table: "honorario",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "proyecto_id",
                schema: "rps",
                table: "condicion_economica",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "grupo_id",
                schema: "rps",
                table: "condicion_economica",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_periodo",
                schema: "rps",
                table: "honorario",
                columns: new[] { "colaborador_id", "proyecto_id", "periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_honorario_contrato_id",
                schema: "rps",
                table: "honorario",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_contrato_id",
                schema: "rps",
                table: "condicion_economica",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_grupo_id",
                schema: "rps",
                table: "condicion_economica",
                column: "grupo_id");
        }
    }
}
