using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlashMemo.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LastSessionData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    LastLoadedUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    LastUsedDeckId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastSessionData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeckOptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Scheduling_GoodMultiplier = table.Column<float>(type: "REAL", nullable: false),
                    Scheduling_EasyMultiplier = table.Column<float>(type: "REAL", nullable: false),
                    Scheduling_HardMultiplier = table.Column<float>(type: "REAL", nullable: false),
                    Scheduling_LearningStages = table.Column<string>(type: "TEXT", nullable: false),
                    Scheduling_GraduateDayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Scheduling_EasyOnNewDayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Scheduling_GoodOnNewStage = table.Column<int>(type: "INTEGER", nullable: false),
                    Scheduling_AgainOnReviewStage = table.Column<int>(type: "INTEGER", nullable: false),
                    Scheduling_HardOnNewStage = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyLimits_DailyReviewsLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyLimits_DailyLessonsLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    Sorting_LessonsOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Sorting_ReviewsOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Sorting_ReviewsSortDir = table.Column<int>(type: "INTEGER", nullable: false),
                    Sorting_LessonsSortDir = table.Column<int>(type: "INTEGER", nullable: false),
                    Sorting_CardStateOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeckOptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IntColor = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Decks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    OptionsId = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentDeckId = table.Column<long>(type: "INTEGER", nullable: true),
                    IsTemporary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decks_DeckOptions_OptionsId",
                        column: x => x.OptionsId,
                        principalTable: "DeckOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Decks_Decks_ParentDeckId",
                        column: x => x.ParentDeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Decks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    FrontContent = table.Column<string>(type: "TEXT", nullable: false),
                    BackContent = table.Column<string>(type: "TEXT", nullable: true),
                    DeckId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsBuried = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSuspended = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Due = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastReviewed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Interval = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    LearningStage = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cards_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardEntityTag",
                columns: table => new
                {
                    CardsId = table.Column<long>(type: "INTEGER", nullable: false),
                    TagsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardEntityTag", x => new { x.CardsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_CardEntityTag_Cards_CardsId",
                        column: x => x.CardsId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardEntityTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    CardId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    Answer = table.Column<int>(type: "INTEGER", nullable: true),
                    AnswerTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    NewCardState = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardLogs_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardEntityTag_TagsId",
                table: "CardEntityTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_CardLogs_CardId",
                table: "CardLogs",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardLogs_UserId",
                table: "CardLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_DeckId",
                table: "Cards",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_DeckOptions_UserId",
                table: "DeckOptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_OptionsId",
                table: "Decks",
                column: "OptionsId");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_ParentDeckId",
                table: "Decks",
                column: "ParentDeckId");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_UserId",
                table: "Decks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId",
                table: "Tags",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardEntityTag");

            migrationBuilder.DropTable(
                name: "CardLogs");

            migrationBuilder.DropTable(
                name: "LastSessionData");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "Decks");

            migrationBuilder.DropTable(
                name: "DeckOptions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
