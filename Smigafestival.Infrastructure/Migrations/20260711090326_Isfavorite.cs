using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smigafestival.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Isfavorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Posts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubscribedUserId",
                table: "Posts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SubscribedUserId",
                table: "Posts",
                column: "SubscribedUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_SubscribedUserId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "SubscribedUserId",
                table: "Posts");
        }
    }
}
