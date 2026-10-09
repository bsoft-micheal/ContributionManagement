using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamContributionManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSplitPaymentGroupingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "parent_txn_number",
                table: "payment_transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "transaction_group_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_transaction_group_id",
                table: "payment_transactions",
                column: "transaction_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_transaction_group_id",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "transaction_group_id",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "parent_txn_number",
                table: "payment_transactions");
        }
    }
}
