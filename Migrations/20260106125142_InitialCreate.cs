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
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    HashedPassword = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedulers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true),
                    GoodMultiplier = table.Column<float>(type: "REAL", nullable: false),
                    EasyMultiplier = table.Column<float>(type: "REAL", nullable: false),
                    HardMultiplier = table.Column<float>(type: "REAL", nullable: false),
                    AgainDayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LearningStages = table.Column<string>(type: "TEXT", nullable: false),
                    AgainStageFallback = table.Column<int>(type: "INTEGER", nullable: false),
                    GoodOnNewStage = table.Column<int>(type: "INTEGER", nullable: false),
                    EasyOnNewDayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HardOnNewStage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedulers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedulers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Decks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true),
                    SchedulerId = table.Column<long>(type: "INTEGER", nullable: true),
                    IsTemporary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decks_Schedulers_SchedulerId",
                        column: x => x.SchedulerId,
                        principalTable: "Schedulers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Decks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NextReview = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastReviewed = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                name: "CardEntityTagEntity",
                columns: table => new
                {
                    CardsId = table.Column<long>(type: "INTEGER", nullable: false),
                    TagsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardEntityTagEntity", x => new { x.CardsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_CardEntityTagEntity_Cards_CardsId",
                        column: x => x.CardsId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardEntityTagEntity_Tags_TagsId",
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
                name: "IX_CardEntityTagEntity_TagsId",
                table: "CardEntityTagEntity",
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
                name: "IX_Decks_SchedulerId",
                table: "Decks",
                column: "SchedulerId");

            migrationBuilder.CreateIndex(
                name: "IX_Decks_UserId",
                table: "Decks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedulers_UserId",
                table: "Schedulers",
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
                name: "CardEntityTagEntity");

            migrationBuilder.DropTable(
                name: "CardLogs");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "Decks");

            migrationBuilder.DropTable(
                name: "Schedulers");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
