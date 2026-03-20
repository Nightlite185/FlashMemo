using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlashMemo.Migrations
{
    /// <inheritdoc />
    public partial class Filters_added_in_LastSessionData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastFilters_Created",
                table: "LastSessionData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFilters_DeckIds",
                table: "LastSessionData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFilters_Due",
                table: "LastSessionData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastFilters_IncludeChildrenDecks",
                table: "LastSessionData",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LastFilters_Interval",
                table: "LastSessionData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastFilters_IsBuried",
                table: "LastSessionData",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastFilters_IsDue",
                table: "LastSessionData",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastFilters_IsSuspended",
                table: "LastSessionData",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFilters_LastModified",
                table: "LastSessionData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFilters_LastReviewed",
                table: "LastSessionData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastFilters_OverdueByDays",
                table: "LastSessionData",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFilters_States",
                table: "LastSessionData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFilters_TagIds",
                table: "LastSessionData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastFilters_UserId",
                table: "LastSessionData",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastFilters_Created",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_DeckIds",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_Due",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_IncludeChildrenDecks",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_Interval",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_IsBuried",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_IsDue",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_IsSuspended",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_LastModified",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_LastReviewed",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_OverdueByDays",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_States",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_TagIds",
                table: "LastSessionData");

            migrationBuilder.DropColumn(
                name: "LastFilters_UserId",
                table: "LastSessionData");
        }
    }
}
