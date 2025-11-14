using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABC_Retailers_Part3.Migrations
{
    /// <inheritdoc />
    public partial class fixhasdatastaticvalue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 14, 12, 12, 10, 59, DateTimeKind.Utc).AddTicks(5604));
        }
    }
}
