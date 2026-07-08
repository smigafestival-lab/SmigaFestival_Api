using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smigafestival.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostBackground : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSpecial",
                table: "Categories");

            migrationBuilder.CreateTable(
                name: "BackgroundPost",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PostUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundPost", x => x.PostId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundPost");

            migrationBuilder.AddColumn<bool>(
                name: "IsSpecial",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
