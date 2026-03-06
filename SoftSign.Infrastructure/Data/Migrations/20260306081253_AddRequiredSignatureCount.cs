using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftSign.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredSignatureCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequiredSignatureCount",
                table: "WorkflowSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredSignatureCount",
                table: "WorkflowSteps");
        }
    }
}
