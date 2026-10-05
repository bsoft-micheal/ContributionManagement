using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamContributionManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationMenusTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "user_mfa_devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "user_mfa_devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_mfa_devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "user_mfa_devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "user_mfa_devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "user_mfa_devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "role_rights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "role_rights",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "role_rights",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "role_rights",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "role_rights",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "role_rights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "members",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "member_type",
                table: "members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Office");

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "members",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "event_types",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "event_types",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "event_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "event_types",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "event_types",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "event_participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "event_participants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "event_participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "event_participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "event_participants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "event_participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "device_login_history",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "device_login_history",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "device_login_history",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "device_login_history",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "device_login_history",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "device_details",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "device_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "device_details",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "device_details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "contributions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "contributions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "contributions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "contributions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "contributions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    expense_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    submitted_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expenses", x => x.expense_id);
                });

            migrationBuilder.CreateTable(
                name: "gallery_photos",
                columns: table => new
                {
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    taken_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gallery_photos", x => x.photo_id);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    txn_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    member_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    payment_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    utr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    verified_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    verified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    screenshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_transactions", x => x.transaction_id);
                });

            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    member_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    member_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    related_event = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ticket_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_to = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ref_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    utr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    attachment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_tickets", x => x.ticket_id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    setting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    setting_value = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.setting_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_expense_date_status",
                table: "expenses",
                columns: new[] { "expense_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_gallery_photos_event_name_category",
                table: "gallery_photos",
                columns: new[] { "event_name", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_payment_date_status",
                table: "payment_transactions",
                columns: new[] { "payment_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_txn_number",
                table: "payment_transactions",
                column: "txn_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_status_priority",
                table: "support_tickets",
                columns: new[] { "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_ticket_no",
                table: "support_tickets",
                column: "ticket_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_setting_key",
                table: "system_settings",
                column: "setting_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "gallery_photos");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "support_tickets");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "user_mfa_devices");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "user_mfa_devices");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_mfa_devices");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "user_mfa_devices");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "user_mfa_devices");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "user_mfa_devices");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "role_rights");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "role_rights");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "role_rights");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "role_rights");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "role_rights");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "role_rights");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "members");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "members");

            migrationBuilder.DropColumn(
                name: "member_type",
                table: "members");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "members");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "members");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "event_types");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "event_types");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "event_types");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "event_types");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "event_types");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "event_participants");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "event_participants");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "event_participants");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "event_participants");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "event_participants");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "event_participants");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "device_login_history");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "device_login_history");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "device_login_history");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "device_login_history");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "device_login_history");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "device_details");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "device_details");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "device_details");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "device_details");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "contributions");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "contributions");
        }
    }
}
