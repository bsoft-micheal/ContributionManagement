using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamContributionManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add2FAFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_two_factor_enabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_two_factor_enabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_otp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_otp_expiry",
                table: "users");
        }
    }
}
