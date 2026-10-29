using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamContributionManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveTablesToCmsSchema : Migration
    {
        private static readonly string[] CmsTables = new[]
        {
            "birthday_reminder_logs",
            "budget_calculation_history",
            "budget_calculations",
            "contributions",
            "device_details",
            "device_login_history",
            "event_participants",
            "event_sponsor_histories",
            "event_sponsors",
            "event_type_payment_settings",
            "event_types",
            "events",
            "exit_clearance_histories",
            "exit_clearances",
            "expense_sequences",
            "expenses",
            "gallery_photos",
            "payment_modes",
            "payment_transactions",
            "priorities",
            "statuses",
            "support_tickets",
            "system_settings",
            "ticket_resolution_notes",
            "ticket_types",
            "user_mfa_devices",
            "work_types"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "cms");

            foreach (var table in CmsTables)
            {
                migrationBuilder.RenameTable(
                    name: table,
                    schema: "public",
                    newName: table,
                    newSchema: "cms");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in CmsTables)
            {
                migrationBuilder.RenameTable(
                    name: table,
                    schema: "cms",
                    newName: table,
                    newSchema: "public");
            }
        }
    }
}
