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
    public class HumidityController : ControllerBase
    {
        private readonly Database _context;

        public HumidityController(Database context)
        {
            _context = context;
        }

        // GET: api/Humidity
        [HttpGet]
        public async Task<ActionResult<Humidity>> GetHumidity()
        {
            return await _context.Humidity.LastAsync();
        }

        // GET: api/Humidity/5
        [HttpGet("{unixEpoch}")]
        public async Task<ActionResult<Humidity>> GetHumidity(int unixEpoch)
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixEpoch).UtcDateTime;
            var humidity = await _context.Humidity.OrderBy(t => Math.Abs((t.Time - dateTime).TotalSeconds))
                .FirstAsync();

            if (humidity == null) return NotFound();

            return humidity;
        }
    }
}