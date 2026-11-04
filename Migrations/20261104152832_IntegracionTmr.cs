using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class IntegracionTmr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "eliminado_en_origen",
                schema: "rps",
                table: "proyecto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "sincronizado_en",
                schema: "rps",
                table: "proyecto",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tmr_proyecto_id",
                schema: "rps",
                table: "proyecto",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "activo_en_tmr",
                schema: "rps",
                table: "colaborador_rps",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "apellidos",
                schema: "rps",
                table: "colaborador_rps",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correo",
                schema: "rps",
                table: "colaborador_rps",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "eliminado_en_origen",
                schema: "rps",
                table: "colaborador_rps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "identificacion",
                schema: "rps",
                table: "colaborador_rps",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nombres",
                schema: "rps",
                table: "colaborador_rps",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sincronizado_en",
                schema: "rps",
                table: "colaborador_rps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "eliminada_en_origen",
                schema: "rps",
                table: "asignacion",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "sincronizada_en",
                schema: "rps",
                table: "asignacion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_proyecto_tmr_proyecto_id",
                schema: "rps",
                table: "proyecto",
                column: "tmr_proyecto_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_proyecto_tmr_proyecto_id",
                schema: "rps",
                table: "proyecto");

            migrationBuilder.DropColumn(
                name: "eliminado_en_origen",
                schema: "rps",
                table: "proyecto");

            migrationBuilder.DropColumn(
                name: "sincronizado_en",
                schema: "rps",
                table: "proyecto");

            migrationBuilder.DropColumn(
                name: "tmr_proyecto_id",
                schema: "rps",
                table: "proyecto");

            migrationBuilder.DropColumn(
                name: "activo_en_tmr",
                schema: "rps",
                table: "colaborador_rps");

            migrationBuilder.DropColumn(
                name: "apellidos",
                schema: "rps",
                table: "colaborador_rps");

            migrationBuilder.DropColumn(
                name: "correo",
                schema: "rps",
                table: "colaborador_rps");

            migrationBuilder.DropColumn(
                name: "eliminado_en_origen",
                schema: "rps",
                table: "colaborador_rps");

            migrationBuilder.DropColumn(
                name: "identificacion",
                schema: "rps",
                table: "colaborador_rps");

            migrationBuilder.DropColumn(
                name: "nombres",
                schema: "rps",
                table: "colaborador_rps");

            migrationBuilder.DropColumn(
                name: "sincronizado_en",
                schema: "rps",
                table: "colaborador_rps");

            migrationBuilder.DropColumn(
                name: "eliminada_en_origen",
                schema: "rps",
                table: "asignacion");

            migrationBuilder.DropColumn(
                name: "sincronizada_en",
                schema: "rps",
                table: "asignacion");
        }
    }
}
