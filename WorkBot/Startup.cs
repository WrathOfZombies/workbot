using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using WorkBot.Core;
using WorkBot.Core.Models;

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
            // Add the support for IOptions dependency injection.
            // This will allow access to any configured Configuration 
            // via IOptions<>
            services.AddOptions();

            // Expose the default Configuration Root
            services.AddSingleton<IConfigurationRoot>(config => Configuration);
            services.AddSingleton<Conversation>();

#if DEBUG
            // If in DEBUG mode, then get the configuration from the "BotConfiguration" node in appsettings.json.
            // refer to readme.md for more details on appsettings.json
            services.Configure<BotConfiguration>(Configuration.GetSection("BotConfiguration"));
#else
            // If in PROD mode, then get the configuration from Environment variables.
            services.Configure<BotConfiguration>(config =>
            {
                config.AppId = Environment.GetEnvironmentVariable("BOT_APP_ID");
                config.AppSecret = Environment.GetEnvironmentVariable("BOT_APP_SECRET");
                config.Handle = Environment.GetEnvironmentVariable("BOT_HANDLE");
                config.Name = Environment.GetEnvironmentVariable("BOT_NAME");
            });
#endif

            // Register a singleton for MicrosoftAppCredentials for the bot. This is required by
            // the .NET core port of the Bot Framework.
            services.AddSingleton<MicrosoftAppCredentials>(_ =>
            {
                var config = _.GetService<IOptions<BotConfiguration>>().Value;
                return new MicrosoftAppCredentials(
                    config.AppId,
                    config.AppSecret,
                    _.GetService<ILoggerFactory>().CreateLogger<MicrosoftAppCredentials>());
            });            

            // Register a singleton for the BotActivityHandler, remember the order here matters,
            // as BotActivityHandler depends on MicrosoftAppCredentials and BotConfiguration
            services.AddSingleton<BotActivityHandler>();

            services.AddMvc(options =>
            {
                // Add support for the Bot role in Authorize attribute
                options.Filters.Add(new TrustServiceUrlAttribute());
            });
        }

        //This method is invoked when ASPNETCORE_ENVIRONMENT is 'Development' or is not defined
        //The allowed values are Development,Staging and Production
        public void ConfigureDevelopment(IApplicationBuilder app, ILoggerFactory loggerFactory, BotActivityHandler activityHandler)
        {
            loggerFactory.AddConsole(minLevel: LogLevel.Information);

            // Display custom error page in production when error occurs
            // During development use the ErrorPage middleware to display error information in the browser
            app.UseDeveloperExceptionPage();

            this.Configure(app, loggerFactory, activityHandler);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, BotActivityHandler activityHandler)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseStaticFiles();
            
            // Register the Bot's credentails so that it can communicate to and from channels.
            app.UseBotAuthentication(activityHandler.config.AppId, activityHandler.config.AppSecret);
            app.UseMvc();
        }
    }
}
