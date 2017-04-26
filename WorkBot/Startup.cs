using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

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
            services.AddSingleton(_ => Configuration);

#if DEBUG
            // Authentication for Microsoft Bot Framework.
            services.AddSingleton<MicrosoftAppCredentials>(_ => new MicrosoftAppCredentials(
                Configuration.GetSection("BotConfiguration"),
                _.GetService<ILoggerFactory>().CreateLogger<MicrosoftAppCredentials>()));            
#else
            services.AddSingleton<MicrosoftAppCredentials>(_ => new MicrosoftAppCredentials(
                    Environment.GetEnvironmentVariable("CLIENT_ID"),
                    Environment.GetEnvironmentVariable("CLIENT_SECRET"),
                    _.GetService<ILoggerFactory>().CreateLogger<MicrosoftAppCredentials>()));
#endif                    

            services.AddMvc(options =>
            {
                options.Filters.Add(new TrustServiceUrlAttribute());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, MicrosoftAppCredentials credentials)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseStaticFiles();

            app.UseBotAuthentication(credentials.MicrosoftAppId, credentials.MicrosoftAppPassword);

            app.UseMvc();
        }
    }
}
