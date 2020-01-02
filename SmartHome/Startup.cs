using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartHome.Services;

namespace SmartHome
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<Database>(options => options.UseSqlite("Data Source=data.db"));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            var dbOptionsBuilder = new DbContextOptionsBuilder<Database>();
            dbOptionsBuilder.UseSqlite("Data Source=data.db");

            services.AddSingleton<ITempSensor>(provider => new TempSensor(provider.GetService<IConfiguration>(),
                provider.GetService<ILogger<TempSensor>>(), dbOptionsBuilder.Options));

            if (Configuration["Telegram:Enabled"].ToLower() == "true")
            {
                services.AddSingleton<ITelegramBot>(provider => new TelegramBot(provider.GetService<IConfiguration>(),
                    provider.GetService<ILogger<TelegramBot>>(), dbOptionsBuilder.Options));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseMvc();

            app.ApplicationServices.GetService<ITempSensor>().StartService();

            if (Configuration["Telegram:Enabled"].ToLower() == "true")
            {
                app.ApplicationServices.GetService<ITelegramBot>().StartService();
            }
        }
    }
}