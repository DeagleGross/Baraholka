using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EF_App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddGetHotForecastsProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION get_hot_forecasts(min_temp integer)
                RETURNS TABLE(id integer, date date, temperaturec integer, summary text, location text)
                AS $$
                BEGIN
                    RETURN QUERY
                    SELECT id, date, temperaturec, summary, location
                    FROM ""WeatherForecasts""
                    WHERE temperaturec > min_temp;
                END;
                $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
