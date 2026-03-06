using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftSign.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowCreatedByIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "WorkflowSteps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StepType",
                table: "WorkflowSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Workflows",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Workflows",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "WorkflowSteps");

            migrationBuilder.DropColumn(
                name: "StepType",
                table: "WorkflowSteps");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Workflows");
        }
    }
}
