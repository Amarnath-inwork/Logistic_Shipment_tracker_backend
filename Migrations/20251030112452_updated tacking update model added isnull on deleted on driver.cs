using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistic_Shipment_tracker.Migrations
{
    /// <inheritdoc />
    public partial class updatedtackingupdatemodeladdedisnullondeletedondriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackingUpdates_Users_UpdatedBy",
                table: "TrackingUpdates");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackingUpdates_Users_UpdatedBy",
                table: "TrackingUpdates",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackingUpdates_Users_UpdatedBy",
                table: "TrackingUpdates");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackingUpdates_Users_UpdatedBy",
                table: "TrackingUpdates",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
