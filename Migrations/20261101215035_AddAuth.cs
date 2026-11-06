using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rol",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "token_blacklist",
                schema: "rps",
                columns: table => new
                {
                    jti = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_token_blacklist", x => x.jti);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    hash_password = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bloqueado_hasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    debe_cambiar_password = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sesion",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    ultima_actividad = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sesion", x => x.id);
                    table.ForeignKey(
                        name: "fk_sesion_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "rps",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario_rol",
                schema: "rps",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_rol", x => new { x.usuario_id, x.rol_id });
                    table.ForeignKey(
                        name: "fk_usuario_rol_rol_rol_id",
                        column: x => x.rol_id,
                        principalSchema: "rps",
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuario_rol_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "rps",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    familia_token = table.Column<Guid>(type: "uuid", nullable: false),
                    usado = table.Column<bool>(type: "boolean", nullable: false),
                    revocado = table.Column<bool>(type: "boolean", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_sesiones_sesion_id",
                        column: x => x.sesion_id,
                        principalSchema: "rps",
                        principalTable: "sesion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refresh_token_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "rps",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_familia_token",
                schema: "rps",
                table: "refresh_token",
                column: "familia_token");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_sesion_id",
                schema: "rps",
                table: "refresh_token",
                column: "sesion_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_token_hash",
                schema: "rps",
                table: "refresh_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_usuario_id",
                schema: "rps",
                table: "refresh_token",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_rol_nombre",
                schema: "rps",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sesion_usuario_id",
                schema: "rps",
                table: "sesion",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_email",
                schema: "rps",
                table: "usuario",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_rol_rol_id",
                schema: "rps",
                table: "usuario_rol",
                column: "rol_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_token",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "token_blacklist",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "usuario_rol",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "sesion",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "rol",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "usuario",
                schema: "rps");
        }
    }
}
