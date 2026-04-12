using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TwoGather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "IsSystem", "ListId", "Name", "RoomLabel" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), true, null, "Salon", "Salon" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), true, null, "Yatak Odası", "Yatak Odası" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), true, null, "Mutfak", "Mutfak" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), true, null, "Banyo", "Banyo" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), true, null, "Çocuk Odası", "Çocuk Odası" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), true, null, "Genel", "Genel" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"));
        }
    }
}
