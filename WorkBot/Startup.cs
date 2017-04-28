using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using WorkBot.Core;

namespace WorkBot
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddSingleton<IConfigurationRoot>(config => Configuration);

#if DEBUG
            services.Configure<BotConfiguration>(Configuration.GetSection("BotConfiguration"));
#else
            services.Configure<BotConfiguration>(config =>
            {
                config.AppId = Environment.GetEnvironmentVariable("BOT_APP_ID");
                config.AppSecret = Environment.GetEnvironmentVariable("BOT_APP_SECRET");
                config.Handle = Environment.GetEnvironmentVariable("BOT_HANDLE");
                config.Name = Environment.GetEnvironmentVariable("BOT_NAME");
            });
#endif
            services.AddSingleton<BotActivityHandler>();

            services.AddMvc(options =>
            {
                options.Filters.Add(new TrustServiceUrlAttribute());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<BotConfiguration> config)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseStaticFiles();
            app.UseBotAuthentication(config.Value?.AppId, config.Value?.AppSecret);
            app.UseMvc();
        }
    }
}
