using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlosterIktato.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDataxoFieldsToDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataxoCompletedAt",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataxoStatus",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataxoSubmittedAt",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataxoTransactionId",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataxoCompletedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DataxoStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DataxoSubmittedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DataxoTransactionId",
                table: "Documents");
        }
    }
}
