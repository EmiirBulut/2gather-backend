using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwoGather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalDecisionToItemOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "ItemOptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FinalizedBy",
                table: "ItemOptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinal",
                table: "ItemOptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "ItemOptions");

            migrationBuilder.DropColumn(
                name: "FinalizedBy",
                table: "ItemOptions");

            migrationBuilder.DropColumn(
                name: "IsFinal",
                table: "ItemOptions");
        }
    }
}
