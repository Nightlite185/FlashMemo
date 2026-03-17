using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlashMemo.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTagNamePerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_UserId",
                table: "Tags");

            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE __TagDups AS
                SELECT
                    Id AS DupId,
                    MIN(Id) OVER (PARTITION BY UserId, lower(Name)) AS KeepId
                FROM Tags;

                DELETE FROM __TagDups
                WHERE DupId = KeepId;

                INSERT OR IGNORE INTO CardEntityTag (CardsId, TagsId)
                SELECT ct.CardsId, d.KeepId
                FROM CardEntityTag AS ct
                JOIN __TagDups AS d ON d.DupId = ct.TagsId;

                DELETE FROM CardEntityTag
                WHERE TagsId IN (SELECT DupId FROM __TagDups);

                DELETE FROM Tags
                WHERE Id IN (SELECT DupId FROM __TagDups);

                DROP TABLE __TagDups;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tags",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId_Name",
                table: "Tags",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_UserId_Name",
                table: "Tags");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tags",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId",
                table: "Tags",
                column: "UserId");
        }
    }
}
