using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class HonorarioGrupal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "grupo_id",
                schema: "rps",
                table: "condicion_economica",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_grupo_id",
                schema: "rps",
                table: "condicion_economica",
                column: "grupo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_condicion_economica_grupo_id",
                schema: "rps",
                table: "condicion_economica");

            migrationBuilder.DropColumn(
                name: "grupo_id",
                schema: "rps",
                table: "condicion_economica");
        }
    }
}
