using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAutoriaRevisionYAnticipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "revisado_por_usuario_id",
                schema: "rps",
                table: "liquidacion",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "creado_por",
                schema: "rps",
                table: "anticipo_adicional",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "revisado_por_usuario_id",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.DropColumn(
                name: "creado_por",
                schema: "rps",
                table: "anticipo_adicional");
        }
    }
}
