using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamContributionManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMfaDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "two_factor_otp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_otp_expiry",
                table: "users");

            migrationBuilder.CreateTable(
                name: "user_mfa_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    secret_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_added = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_mfa_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_mfa_devices_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_devices_user_id",
                table: "user_mfa_devices",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_mfa_devices");

            migrationBuilder.AddColumn<string>(
                name: "two_factor_otp",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "two_factor_otp_expiry",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
