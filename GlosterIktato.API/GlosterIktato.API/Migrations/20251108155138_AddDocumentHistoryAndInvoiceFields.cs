using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlosterIktato.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentHistoryAndInvoiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_AssignedToId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_CreatedById",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CompanyId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CreatedById",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "RegistrationNumber",
                table: "Documents",
                newName: "StoragePath");

            migrationBuilder.RenameColumn(
                name: "AssignedToId",
                table: "Documents",
                newName: "ModifiedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_AssignedToId",
                table: "Documents",
                newName: "IX_Documents_ModifiedByUserId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Documents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ArchiveNumber",
                table: "Documents",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                table: "Documents",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssueDate",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadline",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PerformanceDate",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentHistories_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ArchiveNumber",
                table: "Documents",
                column: "ArchiveNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AssignedToUserId",
                table: "Documents",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CompanyId_DocumentTypeId_CreatedAt",
                table: "Documents",
                columns: new[] { "CompanyId", "DocumentTypeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedAt",
                table: "Documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedByUserId",
                table: "Documents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status",
                table: "Documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHistories_DocumentId_CreatedAt",
                table: "DocumentHistories",
                columns: new[] { "DocumentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHistories_UserId",
                table: "DocumentHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_AssignedToUserId",
                table: "Documents",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_CreatedByUserId",
                table: "Documents",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_ModifiedByUserId",
                table: "Documents",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_AssignedToUserId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_CreatedByUserId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_ModifiedByUserId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentHistories");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ArchiveNumber",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_AssignedToUserId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CompanyId_DocumentTypeId_CreatedAt",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CreatedAt",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CreatedByUserId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Status",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ArchiveNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IssueDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PaymentDeadline",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PerformanceDate",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "StoragePath",
                table: "Documents",
                newName: "RegistrationNumber");

            migrationBuilder.RenameColumn(
                name: "ModifiedByUserId",
                table: "Documents",
                newName: "AssignedToId");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_ModifiedByUserId",
                table: "Documents",
                newName: "IX_Documents_AssignedToId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CompanyId",
                table: "Documents",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedById",
                table: "Documents",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_AssignedToId",
                table: "Documents",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_CreatedById",
                table: "Documents",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
