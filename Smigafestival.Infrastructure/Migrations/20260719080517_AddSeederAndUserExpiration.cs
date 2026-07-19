using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smigafestival.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeederAndUserExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PlanEndDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanID",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanStartDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isPlanExpire",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlan",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    PlanAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlanDuration = table.Column<int>(type: "int", nullable: false),
                    PlanCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlan", x => x.PlanId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlan");

            migrationBuilder.DropColumn(
                name: "PlanEndDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PlanID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PlanStartDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "isPlanExpire",
                table: "Users");
        }
    }
}
