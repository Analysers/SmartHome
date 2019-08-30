using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Models;

namespace SmartHome.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemperatureController : ControllerBase
    {
        private readonly Database _context;

        public TemperatureController(Database context)
        {
            _context = context;
        }

        // GET: api/Temperature
        [HttpGet]
        public async Task<ActionResult<Temperature>> GetTemperature()
        {
            return await _context.Temperature.LastAsync();
        }

        // GET: api/Temperature/5
        [HttpGet("{unixEpoch}")]
        public async Task<ActionResult<Temperature>> GetTemperature(int unixEpoch)
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixEpoch).UtcDateTime;
            var temperature = await _context.Temperature.OrderBy(t => Math.Abs((t.Time - dateTime).TotalSeconds))
                .FirstAsync();

            if (temperature == null) return NotFound();

            return temperature;
        }
    }
}