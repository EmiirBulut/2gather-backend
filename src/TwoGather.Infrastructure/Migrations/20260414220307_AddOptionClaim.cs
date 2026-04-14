using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwoGather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OptionClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Percentage = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionClaims", x => x.Id);
                    table.CheckConstraint("CK_OptionClaim_Percentage", "\"Percentage\" = 25 OR \"Percentage\" = 50 OR \"Percentage\" = 75 OR \"Percentage\" = 100");
                    table.ForeignKey(
                        name: "FK_OptionClaims_ItemOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "ItemOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OptionClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OptionClaims_OptionId_UserId",
                table: "OptionClaims",
                columns: new[] { "OptionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_OptionClaims_UserId",
                table: "OptionClaims",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OptionClaims");
        }
    }
}
