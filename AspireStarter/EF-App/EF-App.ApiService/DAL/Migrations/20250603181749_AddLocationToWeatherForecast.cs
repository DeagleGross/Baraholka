using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EF_App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToWeatherForecast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "WeatherForecasts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "WeatherForecasts");
        }
    }
}
