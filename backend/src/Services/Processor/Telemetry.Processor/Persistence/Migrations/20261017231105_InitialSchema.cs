using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Telemetry.Processor.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meter_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reading_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meter_batches", x => x.id);
                    table.CheckConstraint("ck_meter_batches_reading_count_positive", "reading_count > 0");
                });

            migrationBuilder.CreateTable(
                name: "meters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    location = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "readings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    meter_id = table.Column<int>(type: "integer", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    energy = table.Column<double>(type: "double precision", nullable: true),
                    co2 = table.Column<int>(type: "integer", nullable: true),
                    pm25 = table.Column<int>(type: "integer", nullable: true),
                    humidity = table.Column<int>(type: "integer", nullable: true),
                    motion_detected = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_readings", x => x.id);
                    table.CheckConstraint("ck_readings_air_quality_non_negative", "co2 >= 0 AND pm25 >= 0");
                    table.CheckConstraint("ck_readings_energy_non_negative", "energy >= 0");
                    table.CheckConstraint("ck_readings_humidity_percentage", "humidity BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_readings_single_measurement", "num_nonnulls(energy, co2, motion_detected) = 1 AND (co2 IS NULL) = (pm25 IS NULL) AND (co2 IS NULL) = (humidity IS NULL)");
                    table.ForeignKey(
                        name: "fk_readings_meter_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "meter_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_readings_meters_meter_id",
                        column: x => x.meter_id,
                        principalTable: "meters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_meter_batches_fetched_at",
                table: "meter_batches",
                column: "fetched_at");

            migrationBuilder.CreateIndex(
                name: "ix_meters_location_type",
                table: "meters",
                columns: new[] { "location", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_readings_batch_id_meter_id",
                table: "readings",
                columns: new[] { "batch_id", "meter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_readings_meter_id_recorded_at",
                table: "readings",
                columns: new[] { "meter_id", "recorded_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_readings_recorded_at",
                table: "readings",
                column: "recorded_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "readings");

            migrationBuilder.DropTable(
                name: "meter_batches");

            migrationBuilder.DropTable(
                name: "meters");
        }
    }
}
