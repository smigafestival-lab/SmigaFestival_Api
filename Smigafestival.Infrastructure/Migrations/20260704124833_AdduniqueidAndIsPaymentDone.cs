using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smigafestival.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdduniqueidAndIsPaymentDone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentDone",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubscribedUserId",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SubscribedUserId",
                table: "Users",
                column: "SubscribedUserId",
                unique: true,
                filter: "[SubscribedUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_SubscribedUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPaymentDone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscribedUserId",
                table: "Users");
        }
    }
}
