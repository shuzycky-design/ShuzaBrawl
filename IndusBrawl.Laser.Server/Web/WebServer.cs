using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using IndusBrawl.Laser.Server.Database;

namespace IndusBrawl.Laser.Server.Web
{
    public class WebServer
    {
        private IHost _host;
        private int _port = IndusBrawl.Laser.Server.Settings.Configuration.Instance.WebPort > 0 ? IndusBrawl.Laser.Server.Settings.Configuration.Instance.WebPort : 8085;

        public void Start()
{
    try
    {
        Console.WriteLine($"[WEB] Попытка запуска на порту {_port}...");

        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // ИСПРАВЛЕНО: используем 0.0.0.0 вместо * и localhost
                webBuilder.UseUrls($"http://0.0.0.0:{_port}");
                webBuilder.ConfigureServices(services =>
                {
                    services.AddCors(options =>
                    {
                        options.AddPolicy("AllowAll",
                            builder =>
                            {
                                builder.AllowAnyOrigin()
                                       .AllowAnyMethod()
                                       .AllowAnyHeader();
                            });
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseCors("AllowAll");

                    var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                    if (Directory.Exists(wwwrootPath))
                    {
                        app.UseStaticFiles(new StaticFileOptions
                        {
                            FileProvider = new PhysicalFileProvider(wwwrootPath),
                            RequestPath = ""
                        });
                        Console.WriteLine($"[WEB] Статика из: {wwwrootPath}");
                    }

                    app.Map("/api", apiApp =>
                    {
                        apiApp.Run(async context =>
                        {
                            var path = context.Request.Path.Value?.ToLower() ?? "";
                            
                            if (context.Request.Method == "OPTIONS")
                            {
                                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                                context.Response.StatusCode = 200;
                                return;
                            }

                            string json = "[]";
                            if (path.Contains("/ranked/solo"))
                                json = GetRankingJson("solo");
                            else if (path.Contains("/ranked/team"))
                                json = GetRankingJson("team");
                            else if (path.Contains("/global"))
                                json = GetRankingJson("global");
                            else
                            {
                                context.Response.StatusCode = 404;
                                await context.Response.WriteAsync("{\"error\":\"Not found\"}");
                                return;
                            }

                            context.Response.ContentType = "application/json; charset=utf-8";
                            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                            await context.Response.WriteAsync(json);
                        });
                    });

                    app.Run(async context =>
                    {
                        var filePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "rankings.html");
                        if (File.Exists(filePath))
                        {
                            context.Response.ContentType = "text/html; charset=utf-8";
                            await context.Response.SendFileAsync(filePath);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("<h1>404 - rankings.html not found</h1>");
                        }
                    });
                });
            })
            .Build();

        _host.Start();
        Console.WriteLine($"[WEB] ✅ Kestrel УСПЕШНО ЗАПУЩЕН НА ПОРТУ {_port}!");
        Console.WriteLine($"[WEB] Страница: http://{IndusBrawl.Laser.Server.Settings.Configuration.Instance.UdpHost}:{_port}/");
        Console.WriteLine($"[WEB] API: http://{IndusBrawl.Laser.Server.Settings.Configuration.Instance.UdpHost}:{_port}/api/ranked/solo");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WEB] ❌ Ошибка: {ex.Message}");
        Console.WriteLine($"[WEB] Детали: {ex.StackTrace}");
    }
}

        public void Stop()
        {
            _host?.StopAsync().Wait();
            Console.WriteLine("[WEB] Сервер остановлен");
        }

        private string GetRankingJson(string type)
        {
            try
            {
                object topList = null;
                if (type == "solo")
                {
                    var accounts = Accounts.GetSoloRankingList();
                    topList = accounts.Select(a => new
                    {
                        id = a.AccountId,
                        name = a.Avatar?.Name ?? "Безымянный",
                        rank = a.Home?.RankedSoloRank ?? 1,
                        progress = a.Home?.RankedSoloProgress ?? 0,
                        trophies = a.Avatar?.Trophies ?? 0
                    }).Take(100).ToList();
                }
                else if (type == "team")
                {
                    var accounts = Accounts.GetTrioRankingList();
                    topList = accounts.Select(a => new
                    {
                        id = a.AccountId,
                        name = a.Avatar?.Name ?? "Безымянный",
                        rank = a.Home?.RankedTrioRank ?? 1,
                        progress = a.Home?.RankedTrioProgress ?? 0,
                        trophies = a.Avatar?.Trophies ?? 0
                    }).Take(100).ToList();
                }
                else if (type == "global")
                {
                    var accounts = Accounts.GetRankingList();
                    topList = accounts.Select(a => new
                    {
                        id = a.AccountId,
                        name = a.Avatar?.Name ?? "Безымянный",
                        trophies = a.Avatar?.Trophies ?? 0
                    }).Take(100).ToList();
                }
                return JsonConvert.SerializeObject(topList, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEB] Ошибка формирования JSON: {ex.Message}");
                return "[]";
            }
        }
    }
}