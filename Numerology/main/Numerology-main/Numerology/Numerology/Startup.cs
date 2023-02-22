﻿using Core.Configs;
using Core.Security;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Newtonsoft.Json;

namespace Numerology
{
    // TODO: Add autoupdated -
    // https://github.com/ElectronNET/electron.net-api-demos/blob/master/ElectronNET-API-Demos/electron.manifest.json
    // https://github.com/ElectronNET/electron.net-api-demos/blob/master/ElectronNET-API-Demos/Controllers/UpdateController.cs
    // https://github.com/zaherg/electron-auto-update-example/blob/master/electron-builder.json
    // https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token
    public class Startup
    {
        private readonly AppConfiguration _appConfiguration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _appConfiguration = EnsureAppConfig();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddElectron();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });
            services.AddCors();
            services.AddControllersWithViews();

            services.AddSingleton<AppConfiguration>(_appConfiguration);

            /*services.AddDbContext<AppDBContext>((provider, dbContextBuilder) =>
            {
                var context = provider.GetRequiredService<IHttpContextAccessor>();
                var dbEnv = context.HttpContext?.Request.Headers.FirstOrDefault(x => x.Key.ToLower() == "dbenv");

                if (dbEnv != null)
                {
                    var env = dbEnv.Value.Value.ToString();
                    var connection = _appConfiguration.Connections.FirstOrDefault(x => x.Environment == env);

                    if (connection != null)
                    {
                        dbContextBuilder.UseNpgsql(connection.DBConnectionString);
                    }
                    else
                        throw new Exception($"Could not find connection with name: {env}");
                }
                else
                    throw new Exception($"Could not find dbenv header in request");
            });*/

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "clientapp/build";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            if (!env.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseRouting();
            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "clientapp";
                var reactEnv = HybridSupport.IsElectronActive ? "win" : "web";
                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: reactEnv);
                }
            });

            if (HybridSupport.IsElectronActive)
            {
                CreateElectronWindow();
            }
        }

        private AppConfiguration EnsureAppConfig()
        {
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Numerology");
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            //C:\ProgramData\Numerology
            var filePath = Path.Combine(basePath, "appConfig");

            if (File.Exists(filePath))
            {
                var guard = new ConfigurationGuard();
                var fileBytes = File.ReadAllBytes(filePath);
                var fileString = guard.Decrypt(fileBytes);

                // !TODO: Not sure how it will behave. It may create config each time app is run
                if (!fileString.Contains("Username"))
                {
                    return CreateConfig(filePath);
                }
                else
                {
                    return JsonConvert.DeserializeObject<AppConfiguration>(fileString);
                }
            }
            else
            {
                return CreateConfig(filePath);
            }
        }

        private static AppConfiguration CreateConfig(string filePath)
        {
            var appConfig = new AppConfiguration();
            var appConfigString = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
            var guard = new ConfigurationGuard();
            var appConfigBytes = guard.Encrypt(appConfigString);
            File.WriteAllBytes(filePath, appConfigBytes);

            return appConfig;
        }

        private async void CreateElectronWindow()
        {
            //https://www.electronjs.org/docs/latest/tutorial/windows-taskbar

            var browserWindow = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {
                Show = false,
                WebPreferences = new WebPreferences { NodeIntegration = true, ContextIsolation = true, EnableRemoteModule = true, DevTools = true },
                Frame = false,
                Resizable = true,
                TitleBarStyle = TitleBarStyle.hidden,
                AutoHideMenuBar = true,
            });
            browserWindow.SetMenuBarVisibility(false);
            browserWindow.OnReadyToShow += () => browserWindow.Show();
            //https://github.com/ElectronNET/electron.net-api-demos/blob/master/ElectronNET-API-Demos/Views/Ipc/Index.cshtml

            Electron.Notification.Show(new ElectronNET.API.Entities.NotificationOptions("Numerology start", "Current Version: 1.0.0"));
        }
    }
}
