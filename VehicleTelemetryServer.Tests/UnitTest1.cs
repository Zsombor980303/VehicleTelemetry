using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleTelemetryServer.Controllers;
using VehicleTelemetryServer.Models;
using Xunit;

namespace VehicleTelemetryServer.Tests
{
    public class TelemetryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly VehicleTelemetryController _controller;

        public TelemetryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _controller = new VehicleTelemetryController(_context);
        }

        [Fact]
        public async Task ReceiveTelemetry_ShouldTriggerSensorFailure_WhenBrakePedalIncreasesButVelocityDoesNotDrop()
        {
            var step1 = new TelemetryData { Name = "TRUCK_TEST_01", Brake = "Standard", velocity = 80.0, brake_pedal_position = 0, brake_pedal_temperature = 100.0, timestamp = DateTime.UtcNow.AddSeconds(-1).ToString("o") };
            var step2 = new TelemetryData { Name = "TRUCK_TEST_01", Brake = "Standard", velocity = 79.95, brake_pedal_position = 10, brake_pedal_temperature = 100.0, timestamp = DateTime.UtcNow.ToString("o") };

            await _controller.ReceiveTelemetry(step1);
            var result = await _controller.ReceiveTelemetry(step2);

            var savedFailure = await _context.PossibleFailures.FirstOrDefaultAsync(f => f.FailureType == "SENSOR_OR_ABS_FAILURE");
            Assert.NotNull(savedFailure);
        }

        [Fact]
        public async Task ReceiveTelemetry_ShouldTriggerBrakeOverheat_WhenTemperatureIsExtremelyHigh()
        {
            var payload = new TelemetryData { Name = "TRUCK_TEST_02", Brake = "HeavyDuty", velocity = 50.0, brake_pedal_position = 20, brake_pedal_temperature = 600.0, timestamp = DateTime.UtcNow.ToString("o") };

            var result = await _controller.ReceiveTelemetry(payload);

            var savedFailure = await _context.PossibleFailures.FirstOrDefaultAsync(f => f.VehicleName == "TRUCK_TEST_02");
            Assert.NotNull(savedFailure);
            Assert.Equal("BRAKE_FADING_OVERHEAT", savedFailure.FailureType);
        }

        [Fact]
        public async Task ReceiveTelemetry_ShouldNotTriggerAnyFailures_WhenPhysicsAreNormalAndBrakesAreCool()
        {
            var ThresholdCopy = 60;

            // NEGATIVE TEST (HAPPY PATH): Perfectly smooth driving with zero changes
            var step1 = new TelemetryData { Name = "TRUCK_OK", Brake = "Standard", velocity = 80.0, brake_pedal_position = 0, brake_pedal_temperature = ThresholdCopy, timestamp = DateTime.UtcNow.AddSeconds(-1).ToString("o") };
            var step2 = new TelemetryData { Name = "TRUCK_OK", Brake = "Standard", velocity = 80.0, brake_pedal_position = 0, brake_pedal_temperature = ThresholdCopy, timestamp = DateTime.UtcNow.ToString("o") };

            // ACT: Process both completely static packets
            await _controller.ReceiveTelemetry(step1);
            var result = await _controller.ReceiveTelemetry(step2);

            // ASSERT: Explicitly verify that the database table is completely EMPTY
            var savedFailures = await _context.PossibleFailures.ToListAsync();
            Assert.Empty(savedFailures);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}