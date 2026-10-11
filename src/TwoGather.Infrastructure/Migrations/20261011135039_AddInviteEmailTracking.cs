using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwoGather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteEmailTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "ListInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailStatus",
                table: "ListInvites",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSentAt",
                table: "ListInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "ListInvites",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SendCount",
                table: "ListInvites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ListInvites_ProviderMessageId",
                table: "ListInvites",
                column: "ProviderMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListInvites_ProviderMessageId",
                table: "ListInvites");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "ListInvites");

            migrationBuilder.DropColumn(
                name: "EmailStatus",
                table: "ListInvites");

            migrationBuilder.DropColumn(
                name: "LastSentAt",
                table: "ListInvites");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "ListInvites");

            migrationBuilder.DropColumn(
                name: "SendCount",
                table: "ListInvites");
        }
    }
}
