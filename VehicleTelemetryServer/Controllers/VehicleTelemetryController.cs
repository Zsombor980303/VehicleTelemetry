using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleTelemetryServer.Models; //include the database, so we can reach it
using Mscc.GenerativeAI;

namespace VehicleTelemetryServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VehicleTelemetryController : ControllerBase
    {

        //HARWARE SOCKET / POINTER DEFINITION
        //This is an empty socket on the wall of our controller class.
        //It does not hold any data or memory yet; it just tells the compiler:
        //"An active database driver engine (AppDbContext) will be plugged into this slot later."
        //It is 'readonly' to ensure that once the connection is established, it cannot be swapped during runtime.
        private readonly AppDbContext _context;

        //RAM dictionary for previous loops
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TelemetryData> _lastKnownStates = new();

        //THE CONSTRUCTOR (PLUGGING IN THE LIVE NETWORK CABLE)
        //This function must match our exact class name. 
        //When a Python request hits the server, the .NET framework automatically boots up the SQL engine,
        //establishes the connection based on appsettings.json, and hands it to this constructor in the 'context' parameter.
        public VehicleTelemetryController(AppDbContext context)
        {
            // We take the live connection cable ('context') handed to us by .NET,
            // and we plug it into our predefined empty socket ('_context').
            // From this point onward, our '_context' slot is alive and ready to send data to the hard drive.
            _context = context;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                status = "ONLINE",
                message = "A kamion telemetria webszerver éles és várja a Python POST kéréseit."
            });
        }

        //This indicates we will receive a posted data flow
        [HttpPost]
        public async Task<IActionResult> ReceiveTelemetry([FromBody] TelemetryData data)
        {
            var brake_pedal_temp_threshold = 60.0;
            Console.WriteLine($"[BEJÖVŐ] Vehicle: {data.Name} | Velocity: {data.velocity} km/h | Brake: {data.brake_pedal_temperature}°C | Cycle: {data.braking_cycle} (Braking Loop: {data.braking_loop_counter} / Cooldown Loop: {data.no_braking_loop_counter}) | Timestamp: {data.timestamp}");

            var ObjRawTelemetryData = new RawTelemetryData
            {
                Name = data.Name,
                Brake = data.Brake,
                velocity = data.velocity,
                brake_pedal_position = data.brake_pedal_position,
                brake_pedal_temperature = data.brake_pedal_temperature,
                timestamp = data.timestamp,
                braking_cycle = data.braking_cycle,
                braking_loop_counter = data.braking_loop_counter,
                no_braking_loop_counter = data.no_braking_loop_counter
            };

            await _context.RawTelemetry.AddAsync(ObjRawTelemetryData);

            if (data.brake_pedal_temperature > brake_pedal_temp_threshold )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[CRITICAL ANOMALY] {data.Name} BRAKE IS HOT: {data.brake_pedal_temperature}°C!");
                Console.ResetColor();

                var ObjPossibleFailureData = new PossibleFailureData
                {
                    VehicleName = data.Name,
                    BrakeName = data.Brake,
                    VelocityAtFailure = data.velocity,
                    TemperatureAtFailure = data.brake_pedal_temperature,
                    BrakingLoopCounterAtFailure = data.braking_loop_counter,
                    FailureType = "BRAKE_FADING_OVERHEAT",
                    Timestamp = data.timestamp
                };

                await _context.PossibleFailures.AddAsync(ObjPossibleFailureData);
            }

            if (_lastKnownStates.TryGetValue(data.Name, out var previousState))
            {
                int pedalGrowth = data.brake_pedal_position - previousState.brake_pedal_position;
                double velocityDrop = previousState.velocity - data.velocity;

                if (pedalGrowth >= 5 && velocityDrop < 0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[LOGIC ANOMALY] {data.Name}: Brake pedal position increased (+{pedalGrowth}%), but no increased deceleration!");
                    Console.ResetColor();

                    var sensorFailureRecord = new PossibleFailureData
                    {
                        VehicleName = data.Name,
                        BrakeName = data.Brake,
                        VelocityAtFailure = data.velocity,
                        TemperatureAtFailure = data.brake_pedal_temperature,
                        BrakingLoopCounterAtFailure = data.braking_loop_counter,
                        FailureType = "SENSOR_OR_ABS_FAILURE",
                        Timestamp = data.timestamp
                    };

                    await _context.PossibleFailures.AddAsync(sensorFailureRecord);
                }
            }

            //At the end of the loop (regardless of conditions): Save the current state into the RAM dictionary,
            // so that in the next iteration, this will serve as the previous reference point for THIS specific vehicle!
            _lastKnownStates[data.Name] = data;

            await _context.SaveChangesAsync();

            //Give a feedback that data package arrived successfully
            return Ok();
        }

        [HttpGet("raw")]
        public async Task<IActionResult> GetRawData()
        {
            var rawData = await _context.RawTelemetry.ToListAsync();
            return Ok(rawData);
        }

        [HttpGet("failures")]
        public async Task<IActionResult> GetFailures()
        {
            var failures = await _context.PossibleFailures.ToListAsync();
            var stats = await _context.PossibleFailures
            .GroupBy(f => new { f.VehicleName, f.FailureType })
            .Select(g => new { Vehicle = g.Key.VehicleName, Failure = g.Key.FailureType, Count = g.Count() }).ToListAsync();
            return Ok(new
            {
                Summary = stats,
                FullLog = failures
            });
        }

        // AI Analysis part
        [HttpGet("AI_Analysis")]
        public async Task<IActionResult> GetAI_Analysis()
        {
            HttpContext.Response.Headers.Append("Connection", "keep-alive");
            var stats = await _context.PossibleFailures
            .GroupBy(f => new { f.VehicleName, f.BrakeName, f.FailureType }) //We need here the brake type as well to provide every neccessary details to AI
            .Select(g => new { Vehicle = g.Key.VehicleName, Brake = g.Key.BrakeName, Failure = g.Key.FailureType, Count = g.Count() }).ToListAsync();

            //Reading brakes.json file with all the brake types
            var brakesJson = await System.IO.File.ReadAllTextAsync("brakes.json");
            //setting up AI
            var apiKey = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["GeminiApiKey"];
            var googleAI = new GoogleAI(apiKey);
            var model = googleAI.GenerativeModel("gemini-3.8-flash");
            try
            {
                //Creating a json text from the sql query to send it to AI
                string statsJson = System.Text.Json.JsonSerializer.Serialize(stats);
                string prompt = $"Autóipari megbízhatósági mérnök vagy. Itt van a flottahibák statisztikája: {statsJson}. Itt van a fékbetétek piaci árlistája: {brakesJson}. Elemezd ki röviden, magyar nyelven, hogy látható-e összefüggés a hiba gyakorisága és a fékbetét ára között!";
                Console.WriteLine("[DEBUG] Most küldöm a promptot a Gemininek...");
                var response = await model.GenerateContent(prompt);
                Console.WriteLine("[DEBUG] Sikerült! A válasz megérkezett a Google-től.");
                Console.WriteLine(response.Text);
                return Ok(new { Analysis = response.Text });
            }
            catch (Exception ex) 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[AI ERROR] {ex.Message}");
                Console.ResetColor();
                return Ok(new { Analysis = "Az AI szerverek jelenleg túlterheltek, kérjük próbálja meg később! (AI servers are dealing with too many tasks currently, try it later)" }); 
            }

        }

    }
}
