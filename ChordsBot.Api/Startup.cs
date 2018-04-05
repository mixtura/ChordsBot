using System;
using System.Collections.Generic;
using System.Linq;
using ChordsBot.Api.Implementation;
using ChordsBot.Api.Interfaces;
using ChordsBot.Implementation;
using ChordsBot.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Telegram.Bot;

namespace ChordsBot.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public string Token => Configuration.GetValue<string>("botToken");

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();

            var mvcBuilder = services.AddMvcCore();

            mvcBuilder.AddApiExplorer()
                .AddAuthorization()
                .AddFormatterMappings(x =>
                {
                    x.SetMediaTypeMappingForFormat("json", 
                        MediaTypeHeaderValue.Parse("application/json"));
                })
                .AddDataAnnotations()
                .AddJsonFormatters(x => { x.NullValueHandling = NullValueHandling.Ignore; });

            services.AddScheme<TelegramTokenOptions, TelegramTokenHandler>(
                "TelegramAuthScheme",
                options => { options.Token = Token; }
            );
            
            // app services
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(Token));
            services.AddSingleton<IBotUpdateProcessor, BotUpdateProcessor>();
            services.AddSingleton<IChordsService, ChordsService>();
            services.AddSingleton<IWebPageLoader, DefaultWebPageLoader>();
            services.AddSingleton<IChordsGrabber, EChordsGrabber>();
            services.AddSingleton<IChordsGrabber, MyChordsGrabber>();
            services.AddSingleton<IChordsFormatter, ChordsFormatter>();

            services.AddSingleton<IReadOnlyCollection<IChordsGrabber>>(
                x => x.GetServices<IChordsGrabber>().ToList());

            services.AddSingleton<Func<IChordsService>>(x =>
                () => new ChordsServiceCacheProxy(x.GetService<IChordsService>()));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
