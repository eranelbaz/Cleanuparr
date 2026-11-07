using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleanuparr.Persistence.Migrations.Data
{
    /// <inheritdoc />
    public partial class AddSeedingRuleAbandon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "abandon_after_hours",
                table: "u_torrent_seeding_rules",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_below_percentage",
                table: "u_torrent_seeding_rules",
                type: "REAL",
                nullable: false,
                defaultValue: -1.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_after_hours",
                table: "transmission_seeding_rules",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_below_percentage",
                table: "transmission_seeding_rules",
                type: "REAL",
                nullable: false,
                defaultValue: -1.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_after_hours",
                table: "q_bit_seeding_rules",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_below_percentage",
                table: "q_bit_seeding_rules",
                type: "REAL",
                nullable: false,
                defaultValue: -1.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_after_hours",
                table: "deluge_seeding_rules",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "abandon_below_percentage",
                table: "deluge_seeding_rules",
                type: "REAL",
                nullable: false,
                defaultValue: -1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "abandon_after_hours",
                table: "u_torrent_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_below_percentage",
                table: "u_torrent_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_after_hours",
                table: "transmission_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_below_percentage",
                table: "transmission_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_after_hours",
                table: "q_bit_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_below_percentage",
                table: "q_bit_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_after_hours",
                table: "deluge_seeding_rules");

            migrationBuilder.DropColumn(
                name: "abandon_below_percentage",
                table: "deluge_seeding_rules");
        }
    }
}
