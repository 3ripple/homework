using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShort.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "links",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_links_IdempotencyKey",
                table: "links",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_links_IdempotencyKey",
                table: "links");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "links");
        }
    }
}
