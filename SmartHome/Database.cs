using Microsoft.EntityFrameworkCore;
using SmartHome.Models;

namespace SmartHome
{
    public class Database : DbContext
    {
        public Database(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Humidity> Humidity { get; set; }
        public DbSet<Temperature> Temperature { get; set; }
    }
}