using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwoGather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandModelColorToItemOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "ItemOptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "ItemOptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "ItemOptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "ItemOptions");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "ItemOptions");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "ItemOptions");
        }
    }
}
