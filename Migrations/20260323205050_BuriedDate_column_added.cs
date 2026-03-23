using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlashMemo.Migrations
{
    /// <inheritdoc />
    public partial class BuriedDate_column_added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BuriedDate",
                table: "Cards",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuriedDate",
                table: "Cards");
        }
    }
}
