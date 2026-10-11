using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretSanta.Api.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Exchanges",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DrawnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Exchanges", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Participants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExchangeId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                InviteToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                RecipientId = table.Column<Guid>(type: "uuid", nullable: true),
                FirstViewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Participants", x => x.Id);
                table.ForeignKey(
                    name: "FK_Participants_Exchanges_ExchangeId",
                    column: x => x.ExchangeId,
                    principalTable: "Exchanges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Participants_Participants_RecipientId",
                    column: x => x.RecipientId,
                    principalTable: "Participants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Participants_ExchangeId",
            table: "Participants",
            column: "ExchangeId");

        migrationBuilder.CreateIndex(
            name: "IX_Participants_InviteToken",
            table: "Participants",
            column: "InviteToken",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Participants_RecipientId",
            table: "Participants",
            column: "RecipientId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Participants");

        migrationBuilder.DropTable(
            name: "Exchanges");
    }
}
