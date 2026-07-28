using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementCallback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationEvents_Operations_OperationId",
                table: "OperationEvents");

            migrationBuilder.RenameColumn(
                name: "Error",
                table: "PaymentAttempts",
                newName: "FailureMessage");

            migrationBuilder.RenameColumn(
                name: "OperationId",
                table: "Operations",
                newName: "Id");

            migrationBuilder.AddColumn<int>(
                name: "FailureReason",
                table: "PaymentAttempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAt",
                table: "Operations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "Operations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "Operations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "Operations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Operations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Operations_Id",
                table: "Operations",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Operations_Id",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Operations");

            migrationBuilder.RenameColumn(
                name: "FailureMessage",
                table: "PaymentAttempts",
                newName: "Error");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Operations",
                newName: "OperationId");

            migrationBuilder.AddForeignKey(
                name: "FK_OperationEvents_Operations_OperationId",
                table: "OperationEvents",
                column: "OperationId",
                principalTable: "Operations",
                principalColumn: "OperationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
