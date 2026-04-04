using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlashMemo.Migrations
{
    /// <inheritdoc />
    public partial class HeatmapVisAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Options_ShowHeatmap",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Options_ShowHeatmap",
                table: "Users");
        }
    }
}
