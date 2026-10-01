using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIntegrityAndQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Email",
                table: "Doctors",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctors_Email",
                table: "Doctors");
        }
    }
}
