using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smigafestival.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheControllerAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestUserPost",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PostShowDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestUserPost", x => x.PostId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestUserPost_PostShowDate",
                table: "GuestUserPost",
                column: "PostShowDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestUserPost");
        }
    }
}
