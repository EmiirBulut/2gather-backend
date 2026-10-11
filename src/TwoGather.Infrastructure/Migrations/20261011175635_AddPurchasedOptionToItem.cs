using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwoGather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasedOptionToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchasedOptionId",
                table: "Items",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchasedOptionId",
                table: "Items");
        }
    }
}
