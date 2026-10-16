namespace VehicleTelemetryServer.Models
{
    public class RawTelemetryData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Brake { get; set; }
        public double velocity { get; set; }
        public int brake_pedal_position { get; set; }
        public double brake_pedal_temperature { get; set; }
        public string timestamp { get; set; }
        public bool braking_cycle { get; set; }
        public int braking_loop_counter { get; set; }
        public int no_braking_loop_counter { get; set; }
    }
}
