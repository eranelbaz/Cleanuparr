using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleanuparr.Persistence.Postgres.Migrations.Data
{
    /// <inheritdoc />
    public partial class AddSeedingRuleAbandon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "abandon_after_hours",
                schema: "data",
                table: "u_torrent_seeding_rules",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_below_percentage",
                schema: "data",
                table: "u_torrent_seeding_rules",
                type: "double precision",
                nullable: false,
                defaultValue: -1.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_after_hours",
                schema: "data",
                table: "transmission_seeding_rules",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_below_percentage",
                schema: "data",
                table: "transmission_seeding_rules",
                type: "double precision",
                nullable: false,
                defaultValue: -1.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_after_hours",
                schema: "data",
                table: "q_bit_seeding_rules",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_below_percentage",
                schema: "data",
                table: "q_bit_seeding_rules",
                type: "double precision",
                nullable: false,
                defaultValue: -1.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_after_hours",
                schema: "data",
                table: "deluge_seeding_rules",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_below_percentage",
                schema: "data",
                table: "deluge_seeding_rules",
                type: "double precision",
                nullable: false,
                defaultValue: -1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "abandon_after_hours",
                schema: "data",
                table: "u_torrent_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_below_percentage",
                schema: "data",
                table: "u_torrent_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_after_hours",
                schema: "data",
                table: "transmission_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_below_percentage",
                schema: "data",
                table: "transmission_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_after_hours",
                schema: "data",
                table: "q_bit_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_below_percentage",
                schema: "data",
                table: "q_bit_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_after_hours",
                schema: "data",
                table: "deluge_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_below_percentage",
                schema: "data",
                table: "deluge_seeding_rules");
        }
    }
}
