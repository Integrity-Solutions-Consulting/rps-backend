using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AdendasProgramadasYCorregibles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "adenda_id",
                schema: "rps",
                table: "condicion_economica",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "proyectos_anteriores",
                schema: "rps",
                table: "adenda",
                type: "uuid[]",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "proyectos_aplicados",
                schema: "rps",
                table: "adenda",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "proyectos_nuevos",
                schema: "rps",
                table: "adenda",
                type: "uuid[]",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_adenda_id",
                schema: "rps",
                table: "condicion_economica",
                column: "adenda_id");

            migrationBuilder.AddForeignKey(
                name: "fk_condicion_economica_adenda_adenda_id",
                schema: "rps",
                table: "condicion_economica",
                column: "adenda_id",
                principalSchema: "rps",
                principalTable: "adenda",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_condicion_economica_adenda_adenda_id",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.DropIndex(
                name: "ix_condicion_economica_adenda_id",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.DropColumn(
                name: "adenda_id",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.DropColumn(
                name: "proyectos_anteriores",
                schema: "rps",
                table: "adenda");

            migrationBuilder.DropColumn(
                name: "proyectos_aplicados",
                schema: "rps",
                table: "adenda");

            migrationBuilder.DropColumn(
                name: "proyectos_nuevos",
                schema: "rps",
                table: "adenda");
        }
    }
}
