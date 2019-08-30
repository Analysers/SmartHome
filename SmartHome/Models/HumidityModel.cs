using System;
using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models
{
    public class Humidity
    {
        [Key] public int HumidityId { get; set; }

        [Required] public float HumidityPercent { get; set; }

        [Required] public DateTime Time { get; set; } = DateTime.UtcNow;
    }
}