using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps_backend.Migrations
{
    /// <inheritdoc />
    public partial class RealinearConDocumentoRequisitos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anticipo",
                schema: "rps");

            migrationBuilder.DropCheckConstraint(
                name: "ck_liquidacion_estado",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.DropIndex(
                name: "ix_honorario_colaborador_id_contrato_id_periodo_numero_pago",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropCheckConstraint(
                name: "ck_honorario_estado",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropCheckConstraint(
                name: "ck_honorario_numero_pago",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_contrato_colaborador_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropCheckConstraint(
                name: "ck_contrato_fecha_fin",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "actualizado_en",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.DropColumn(
                name: "estado",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.DropColumn(
                name: "actualizado_en",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "descuento",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "estado",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "tarifa",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "tipo_servicio",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "tope_maximo_anticipo",
                schema: "rps",
                table: "configuracion_global");

            migrationBuilder.RenameColumn(
                name: "pagada_en",
                schema: "rps",
                table: "liquidacion",
                newName: "revisado_en");

            migrationBuilder.RenameColumn(
                name: "monto_total",
                schema: "rps",
                table: "liquidacion",
                newName: "neto_consolidado");

            migrationBuilder.RenameColumn(
                name: "numero_pago",
                schema: "rps",
                table: "honorario",
                newName: "dias_reconocidos");

            migrationBuilder.RenameColumn(
                name: "monto",
                schema: "rps",
                table: "honorario",
                newName: "total_retenciones");

            migrationBuilder.AddColumn<bool>(
                name: "revisado",
                schema: "rps",
                table: "liquidacion",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "condicion_economica_id",
                schema: "rps",
                table: "honorario",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "dias_reales",
                schema: "rps",
                table: "honorario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "iva",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "modalidad",
                schema: "rps",
                table: "honorario",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "neto",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_iva",
                schema: "rps",
                table: "honorario",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_retencion_fuente",
                schema: "rps",
                table: "honorario",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_retencion_iva",
                schema: "rps",
                table: "honorario",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "proporcional_acordado",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "proyecto_id",
                schema: "rps",
                table: "honorario",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "retencion_fuente",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "retencion_iva",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_documento",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "fecha_fin",
                schema: "rps",
                table: "contrato",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observaciones",
                schema: "rps",
                table: "contrato",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "proyecto_id",
                schema: "rps",
                table: "contrato",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "referencia",
                schema: "rps",
                table: "contrato",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_iva",
                schema: "rps",
                table: "configuracion_global",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_maximo_anticipo",
                schema: "rps",
                table: "configuracion_global",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_retencion_fuente",
                schema: "rps",
                table: "configuracion_global",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_retencion_iva_junior",
                schema: "rps",
                table: "configuracion_global",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_retencion_iva_profesional",
                schema: "rps",
                table: "configuracion_global",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "adenda",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nueva_fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_registro = table.Column<DateOnly>(type: "date", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_adenda", x => x.id);
                    table.ForeignKey(
                        name: "fk_adenda_contratos_contrato_id",
                        column: x => x.contrato_id,
                        principalSchema: "rps",
                        principalTable: "contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "anticipo_adicional",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anticipo_adicional", x => x.id);
                    table.ForeignKey(
                        name: "fk_anticipo_adicional_colaboradores_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "corte",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    liquidacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valor_base = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    recuperacion_adicional = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    valor_preparado = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corte", x => x.id);
                    table.ForeignKey(
                        name: "fk_corte_liquidaciones_liquidacion_id",
                        column: x => x.liquidacion_id,
                        principalSchema: "rps",
                        principalTable: "liquidacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proyecto",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cliente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proyecto", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cuota_recuperacion",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    anticipo_adicional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    corte = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    importe = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cuota_recuperacion", x => x.id);
                    table.CheckConstraint("ck_cuota_recuperacion_importe", "importe > 0");
                    table.ForeignKey(
                        name: "fk_cuota_recuperacion_anticipo_adicional_anticipo_adicional_id",
                        column: x => x.anticipo_adicional_id,
                        principalSchema: "rps",
                        principalTable: "anticipo_adicional",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asignacion",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proyecto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_salida = table.Column<DateOnly>(type: "date", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asignacion", x => x.id);
                    table.CheckConstraint("ck_asignacion_fecha_salida", "fecha_salida IS NULL OR fecha_salida >= fecha_inicio");
                    table.ForeignKey(
                        name: "fk_asignacion_colaboradores_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_asignacion_proyectos_proyecto_id",
                        column: x => x.proyecto_id,
                        principalSchema: "rps",
                        principalTable: "proyecto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "condicion_economica",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proyecto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    importe_mensual = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    modalidad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    mes_inicio_vigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_condicion_economica", x => x.id);
                    table.ForeignKey(
                        name: "fk_condicion_economica_colaborador_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_condicion_economica_contratos_contrato_id",
                        column: x => x.contrato_id,
                        principalSchema: "rps",
                        principalTable: "contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_condicion_economica_proyectos_proyecto_id",
                        column: x => x.proyecto_id,
                        principalSchema: "rps",
                        principalTable: "proyecto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "rps",
                table: "configuracion_global",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "porcentaje_iva", "porcentaje_maximo_anticipo", "porcentaje_retencion_fuente", "porcentaje_retencion_iva_junior", "porcentaje_retencion_iva_profesional" },
                values: new object[] { 15m, 100m, 10m, 70m, 100m });

            migrationBuilder.CreateIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_periodo",
                schema: "rps",
                table: "honorario",
                columns: new[] { "colaborador_id", "proyecto_id", "periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_honorario_condicion_economica_id",
                schema: "rps",
                table: "honorario",
                column: "condicion_economica_id");

            migrationBuilder.CreateIndex(
                name: "ix_honorario_proyecto_id",
                schema: "rps",
                table: "honorario",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_contrato_colaborador_id_proyecto_id",
                schema: "rps",
                table: "contrato",
                columns: new[] { "colaborador_id", "proyecto_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contrato_proyecto_id",
                schema: "rps",
                table: "contrato",
                column: "proyecto_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_contrato_fecha_fin",
                schema: "rps",
                table: "contrato",
                sql: "fecha_fin >= fecha_inicio");

            migrationBuilder.CreateIndex(
                name: "ix_adenda_contrato_id",
                schema: "rps",
                table: "adenda",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "ix_anticipo_adicional_colaborador_id",
                schema: "rps",
                table: "anticipo_adicional",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_asignacion_colaborador_id_proyecto_id",
                schema: "rps",
                table: "asignacion",
                columns: new[] { "colaborador_id", "proyecto_id" });

            migrationBuilder.CreateIndex(
                name: "ix_asignacion_proyecto_id",
                schema: "rps",
                table: "asignacion",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_colaborador_id_proyecto_id_mes_inicio_v",
                schema: "rps",
                table: "condicion_economica",
                columns: new[] { "colaborador_id", "proyecto_id", "mes_inicio_vigencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_contrato_id",
                schema: "rps",
                table: "condicion_economica",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "ix_condicion_economica_proyecto_id",
                schema: "rps",
                table: "condicion_economica",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_corte_liquidacion_id_tipo",
                schema: "rps",
                table: "corte",
                columns: new[] { "liquidacion_id", "tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cuota_recuperacion_anticipo_adicional_id_periodo_corte",
                schema: "rps",
                table: "cuota_recuperacion",
                columns: new[] { "anticipo_adicional_id", "periodo", "corte" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_contrato_proyectos_proyecto_id",
                schema: "rps",
                table: "contrato",
                column: "proyecto_id",
                principalSchema: "rps",
                principalTable: "proyecto",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_honorario_condicion_economica_condicion_economica_id",
                schema: "rps",
                table: "honorario",
                column: "condicion_economica_id",
                principalSchema: "rps",
                principalTable: "condicion_economica",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_honorario_proyectos_proyecto_id",
                schema: "rps",
                table: "honorario",
                column: "proyecto_id",
                principalSchema: "rps",
                principalTable: "proyecto",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contrato_proyectos_proyecto_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropForeignKey(
                name: "fk_honorario_condicion_economica_condicion_economica_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropForeignKey(
                name: "fk_honorario_proyectos_proyecto_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropTable(
                name: "adenda",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "asignacion",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "condicion_economica",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "corte",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "cuota_recuperacion",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "proyecto",
                schema: "rps");

            migrationBuilder.DropTable(
                name: "anticipo_adicional",
                schema: "rps");

            migrationBuilder.DropIndex(
                name: "ix_honorario_colaborador_id_proyecto_id_periodo",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_honorario_condicion_economica_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_honorario_proyecto_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropIndex(
                name: "ix_contrato_colaborador_id_proyecto_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropIndex(
                name: "ix_contrato_proyecto_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropCheckConstraint(
                name: "ck_contrato_fecha_fin",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "revisado",
                schema: "rps",
                table: "liquidacion");

            migrationBuilder.DropColumn(
                name: "condicion_economica_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "dias_reales",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "iva",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "modalidad",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "neto",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "porcentaje_iva",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "porcentaje_retencion_fuente",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "porcentaje_retencion_iva",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "proporcional_acordado",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "proyecto_id",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "retencion_fuente",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "retencion_iva",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "subtotal",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "total_documento",
                schema: "rps",
                table: "honorario");

            migrationBuilder.DropColumn(
                name: "observaciones",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "proyecto_id",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "referencia",
                schema: "rps",
                table: "contrato");

            migrationBuilder.DropColumn(
                name: "porcentaje_iva",
                schema: "rps",
                table: "configuracion_global");

            migrationBuilder.DropColumn(
                name: "porcentaje_maximo_anticipo",
                schema: "rps",
                table: "configuracion_global");

            migrationBuilder.DropColumn(
                name: "porcentaje_retencion_fuente",
                schema: "rps",
                table: "configuracion_global");

            migrationBuilder.DropColumn(
                name: "porcentaje_retencion_iva_junior",
                schema: "rps",
                table: "configuracion_global");

            migrationBuilder.DropColumn(
                name: "porcentaje_retencion_iva_profesional",
                schema: "rps",
                table: "configuracion_global");

            migrationBuilder.RenameColumn(
                name: "revisado_en",
                schema: "rps",
                table: "liquidacion",
                newName: "pagada_en");

            migrationBuilder.RenameColumn(
                name: "neto_consolidado",
                schema: "rps",
                table: "liquidacion",
                newName: "monto_total");

            migrationBuilder.RenameColumn(
                name: "total_retenciones",
                schema: "rps",
                table: "honorario",
                newName: "monto");

            migrationBuilder.RenameColumn(
                name: "dias_reconocidos",
                schema: "rps",
                table: "honorario",
                newName: "numero_pago");

            migrationBuilder.AddColumn<DateTime>(
                name: "actualizado_en",
                schema: "rps",
                table: "liquidacion",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "estado",
                schema: "rps",
                table: "liquidacion",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Generada");

            migrationBuilder.AddColumn<DateTime>(
                name: "actualizado_en",
                schema: "rps",
                table: "honorario",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "descuento",
                schema: "rps",
                table: "honorario",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "estado",
                schema: "rps",
                table: "honorario",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pendiente");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "fecha_fin",
                schema: "rps",
                table: "contrato",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<decimal>(
                name: "tarifa",
                schema: "rps",
                table: "contrato",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "tipo_servicio",
                schema: "rps",
                table: "contrato",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "tope_maximo_anticipo",
                schema: "rps",
                table: "configuracion_global",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "anticipo",
                schema: "rps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    fecha_solicitud = table.Column<DateOnly>(type: "date", nullable: false),
                    honorario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anticipo", x => x.id);
                    table.CheckConstraint("ck_anticipo_estado", "estado IN ('Pendiente', 'Aprobado', 'Descontado', 'Anulado')");
                    table.ForeignKey(
                        name: "fk_anticipo_colaboradores_rps_colaborador_id",
                        column: x => x.colaborador_id,
                        principalSchema: "rps",
                        principalTable: "colaborador_rps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_anticipo_honorarios_honorario_id",
                        column: x => x.honorario_id,
                        principalSchema: "rps",
                        principalTable: "honorario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "rps",
                table: "configuracion_global",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "tope_maximo_anticipo",
                value: 1000000.00m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_liquidacion_estado",
                schema: "rps",
                table: "liquidacion",
                sql: "estado IN ('Generada', 'Pagada')");

            migrationBuilder.CreateIndex(
                name: "ix_honorario_colaborador_id_contrato_id_periodo_numero_pago",
                schema: "rps",
                table: "honorario",
                columns: new[] { "colaborador_id", "contrato_id", "periodo", "numero_pago" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_honorario_estado",
                schema: "rps",
                table: "honorario",
                sql: "estado IN ('Pendiente', 'Aprobado', 'Pagado', 'Anulado')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_honorario_numero_pago",
                schema: "rps",
                table: "honorario",
                sql: "numero_pago IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "ix_contrato_colaborador_id",
                schema: "rps",
                table: "contrato",
                column: "colaborador_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_contrato_fecha_fin",
                schema: "rps",
                table: "contrato",
                sql: "fecha_fin IS NULL OR fecha_fin >= fecha_inicio");

            migrationBuilder.CreateIndex(
                name: "ix_anticipo_colaborador_id",
                schema: "rps",
                table: "anticipo",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_anticipo_honorario_id",
                schema: "rps",
                table: "anticipo",
                column: "honorario_id");
        }
    }
}
