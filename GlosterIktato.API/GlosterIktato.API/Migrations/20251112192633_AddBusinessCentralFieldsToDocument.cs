using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlosterIktato.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCentralFieldsToDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BcInvoiceId",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BcPushedAt",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BcStatus",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessUnit",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenter",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Employee",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GptCode",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BcInvoiceId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BcPushedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BcStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BusinessUnit",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CostCenter",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Employee",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "GptCode",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "Documents");
        }
    }
}
