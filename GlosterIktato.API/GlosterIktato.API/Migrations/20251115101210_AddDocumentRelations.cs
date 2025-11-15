using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlosterIktato.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    RelatedDocumentId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentRelations_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentRelations_Documents_RelatedDocumentId",
                        column: x => x.RelatedDocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentRelations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRelations_CreatedByUserId",
                table: "DocumentRelations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRelations_DocumentId",
                table: "DocumentRelations",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRelations_DocumentId_RelatedDocumentId",
                table: "DocumentRelations",
                columns: new[] { "DocumentId", "RelatedDocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRelations_RelatedDocumentId",
                table: "DocumentRelations",
                column: "RelatedDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentRelations");
        }
    }
}
