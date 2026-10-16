namespace VehicleTelemetryServer.Models
{
    public class PossibleFailureData
    {
        public int Id { get; set; }
        public string VehicleName { get; set; }
        public string BrakeName { get; set; }
        public double VelocityAtFailure { get; set; }
        public double TemperatureAtFailure { get; set; }
        public int BrakingLoopCounterAtFailure { get; set; }
        public string FailureType { get; set; }
        public string Timestamp { get; set; }
    }
}
