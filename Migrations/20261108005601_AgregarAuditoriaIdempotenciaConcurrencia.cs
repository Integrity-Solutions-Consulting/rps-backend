using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAuditoriaIdempotenciaConcurrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sin AddColumn para "xmin" a propósito: es una columna de sistema que PostgreSQL ya
            // tiene en toda tabla (no se puede crear ni borrar). EF solo necesita saber mapearla
            // como shadow property para el token de concurrencia — eso ya quedó en el modelo.

            migrationBuilder.CreateTable(
                name: "auditoria",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    entidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidad_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    campo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    valor_anterior = table.Column<string>(type: "text", nullable: true),
                    valor_nuevo = table.Column<string>(type: "text", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotencia",
                schema: "rps",
                columns: table => new
                {
                    clave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cuerpo = table.Column<byte[]>(type: "bytea", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotencia", x => x.clave);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_entidad_entidad_id",
                schema: "rps",
                table: "auditoria",
                columns: new[] { "entidad", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_fecha",
                schema: "rps",
                table: "auditoria",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_idempotencia_creado_en",
                schema: "rps",
                table: "idempotencia",
                column: "creado_en");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "idempotencia",
                schema: "rps");
        }
    }
}
