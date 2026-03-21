using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlashMemo.Migrations
{
    /// <inheritdoc />
    public partial class UserId_added_on_CardEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Cards");
        }
    }
}
