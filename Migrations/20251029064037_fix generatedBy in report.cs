using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistic_Shipment_tracker.Migrations
{
    /// <inheritdoc />
    public partial class fixgeneratedByinreport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_GeneraetdBy",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_GeneraetdBy",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "GeneraetdBy",
                table: "Reports");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GeneratedBy",
                table: "Reports",
                column: "GeneratedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_GeneratedBy",
                table: "Reports",
                column: "GeneratedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_GeneratedBy",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_GeneratedBy",
                table: "Reports");

            migrationBuilder.AddColumn<Guid>(
                name: "GeneraetdBy",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GeneraetdBy",
                table: "Reports",
                column: "GeneraetdBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_GeneraetdBy",
                table: "Reports",
                column: "GeneraetdBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
