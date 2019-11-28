﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Framework;
using Telegram.Bot.Framework.Abstractions;
using WinTenBot.Extensions;
using WinTenBot.Handlers;
using WinTenBot.Handlers.Commands;
using WinTenBot.Interfaces;
using WinTenBot.Options;
using WinTenBot.Services;

namespace WinTenBot
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<WinTenBot>()
                .Configure<BotOptions<WinTenBot>>(Configuration.GetSection("EchoBot"))
                .Configure<CustomBotOptions<WinTenBot>>(Configuration.GetSection("EchoBot"))
                .AddScoped<TextEchoer>()
                .AddScoped<WebhookLogger>()
                .AddScoped<StickerHandler>()
                .AddScoped<WeatherReporter>()
                .AddScoped<ExceptionHandler>()
                .AddScoped<UpdateMembersList>()
                .AddScoped<CallbackQueryHandler>()
                .AddScoped<IWeatherService, WeatherService>();

            services.AddScoped<NewChatMembersCommand>();

            services.AddScoped<PingCommand>()
                .AddScoped<StartCommand>()
                .AddScoped<IdCommand>()
                .AddScoped<InfoCommand>()
                .AddScoped<TagsCommand>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //            if (env.IsDevelopment())
            //            {
            app.UseDeveloperExceptionPage();

            // get bot updates from Telegram via long-polling approach during development
            // this will disable Telegram webhooks
            app.UseTelegramBotLongPolling<WinTenBot>(ConfigureBot(), startAfter: TimeSpan.FromSeconds(1));
            //            }
            //            else
            //            {
            //                use Telegram bot webhook middleware in higher environments
            //                app.UseTelegramBotWebhook<WinTenBot>(ConfigureBot());
            //                and make sure webhook is enabled
            //                app.EnsureWebhookSet<WinTenBot>();
            //            }

            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        private IBotBuilder ConfigureBot()
        {
            return new BotBuilder()
                .Use<ExceptionHandler>()

                // .Use<CustomUpdateLogger>()
                .UseWhen<WebhookLogger>(When.Webhook)

                //.UseWhen<UpdateMembersList>(When.MembersChanged)

                .UseWhen<NewChatMembersCommand>(When.NewChatMembers)

                //.UseWhen(When.MembersChanged, memberChanged => memberChanged
                //    .UseWhen(When.MembersChanged, cmdBranch => cmdBranch
                //        .Use<NewChatMembersCommand>()
                //        )
                //    )

                .UseWhen(When.NewMessage, msgBranch => msgBranch
                    .UseWhen(When.NewTextMessage, txtBranch => txtBranch
                        .Use<TextEchoer>()
                        .UseWhen(When.NewCommand, cmdBranch => cmdBranch
                            .UseCommand<PingCommand>("ping")
                            .UseCommand<StartCommand>("start")
                            .UseCommand<TagsCommand>("tags")
                            .UseCommand<IdCommand>("id")
                            .UseCommand<InfoCommand>("info")
                        )
                    //.Use<NLP>()
                    )
                    .UseWhen<StickerHandler>(When.StickerMessage)
                    .UseWhen<WeatherReporter>(When.LocationMessage)
                )

                .UseWhen<CallbackQueryHandler>(When.CallbackQuery)

                 //.Use<UnhandledUpdateReporter>()
                 ;
        }
    }
}