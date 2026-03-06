using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftSign.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentWorkflowStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWorkflowCustomized",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DocumentWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OriginalAssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomAssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalAssignedRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomAssignedRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StepType = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowSteps_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowSteps_Users_CustomAssignedUserId",
                        column: x => x.CustomAssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowSteps_WorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "WorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowSteps_CustomAssignedUserId",
                table: "DocumentWorkflowSteps",
                column: "CustomAssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowSteps_DocumentId",
                table: "DocumentWorkflowSteps",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowSteps_WorkflowStepId",
                table: "DocumentWorkflowSteps",
                column: "WorkflowStepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentWorkflowSteps");

            migrationBuilder.DropColumn(
                name: "IsWorkflowCustomized",
                table: "Documents");
        }
    }
}
