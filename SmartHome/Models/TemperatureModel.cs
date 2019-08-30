using System;
using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models
{
    public class Temperature
    {
        [Key] public int TemperatureId { get; set; }

        [Required] public float TemperatureCelsius { get; set; }

        [Required] public DateTime Time { get; set; } = DateTime.UtcNow;
    }
}