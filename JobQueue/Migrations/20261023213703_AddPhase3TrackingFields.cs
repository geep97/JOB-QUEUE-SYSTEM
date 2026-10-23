using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobQueue.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase3TrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessAfter",
                table: "Jobs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Jobs",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ProcessAfter",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Jobs");
        }
    }
}
