using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleTelemetryServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PossibleFailures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrakeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VelocityAtFailure = table.Column<double>(type: "float", nullable: false),
                    TemperatureAtFailure = table.Column<double>(type: "float", nullable: false),
                    BrakingLoopCounterAtFailure = table.Column<int>(type: "int", nullable: false),
                    FailureType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PossibleFailures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawTelemetry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Brake = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    velocity = table.Column<double>(type: "float", nullable: false),
                    brake_pedal_position = table.Column<int>(type: "int", nullable: false),
                    brake_pedal_temperature = table.Column<double>(type: "float", nullable: false),
                    timestamp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    braking_cycle = table.Column<bool>(type: "bit", nullable: false),
                    braking_loop_counter = table.Column<int>(type: "int", nullable: false),
                    no_braking_loop_counter = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawTelemetry", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PossibleFailures");

            migrationBuilder.DropTable(
                name: "RawTelemetry");
        }
    }
}
