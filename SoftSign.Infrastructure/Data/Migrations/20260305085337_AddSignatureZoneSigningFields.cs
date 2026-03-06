using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftSign.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureZoneSigningFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignatureImage",
                table: "SignatureZones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "SignatureZones",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SignedByUserId",
                table: "SignatureZones",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignatureZones_SignedByUserId",
                table: "SignatureZones",
                column: "SignedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SignatureZones_Users_SignedByUserId",
                table: "SignatureZones",
                column: "SignedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SignatureZones_Users_SignedByUserId",
                table: "SignatureZones");

            migrationBuilder.DropIndex(
                name: "IX_SignatureZones_SignedByUserId",
                table: "SignatureZones");

            migrationBuilder.DropColumn(
                name: "SignatureImage",
                table: "SignatureZones");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "SignatureZones");

            migrationBuilder.DropColumn(
                name: "SignedByUserId",
                table: "SignatureZones");
        }
    }
}
