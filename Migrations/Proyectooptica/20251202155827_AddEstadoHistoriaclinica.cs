using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica1.Migrations.Proyectooptica
{
    /// <inheritdoc />
    public partial class AddEstadoHistoriaclinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "historiaclinica",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "historiaclinica");
        }
    }
}
