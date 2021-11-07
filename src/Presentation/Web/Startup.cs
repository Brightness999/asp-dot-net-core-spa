﻿using System.Collections.Generic;
using AspNetCoreSpa.Application;
using AspNetCoreSpa.Application.Abstractions;
using AspNetCoreSpa.Common;
using AspNetCoreSpa.Infrastructure;
using AspNetCoreSpa.Infrastructure.Localization;
using AspNetCoreSpa.Infrastructure.Persistence;
using AspNetCoreSpa.Web.Services;
using AspNetCoreSpa.Web.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace AspNetCoreSpa.Web
{
    public class Startup
    {
        // Order to run
        //1) Constructor
        //2) Configure services
        //3) Configure
        private IWebHostEnvironment HostingEnvironment { get; }
        public static IConfiguration Configuration { get; set; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            HostingEnvironment = env;
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IApplicationService, ApplicationService>();

            services.AddApplication()
                .AddInfrastructure(Configuration, HostingEnvironment)
                .AddHealthChecks()
                .AddDbContextCheck<LocalizationDbContext>()
                .AddDbContextCheck<ApplicationDbContext>();

            services.AddPersistence(Configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(options =>
                            {
                                // base-address of your identity server
                                options.Authority = Configuration["Auth:Authority"];
                                // name of the API resource
                                options.TokenValidationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidAudiences = Configuration.GetSection("Auth:Audiences").Get<List<string>>(),
                                };
                            });

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist/aspnetcorespa";
            });

        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseInfrastructure(HostingEnvironment);

            if (!HostingEnvironment.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseSwaggerUi3(settings =>
            {
                settings.Path = "/api";
                settings.DocumentPath = "/api/specification.json";
            });

            app.UseRouting();
            app.UseCors(Constants.DefaultCorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                                   name: "default",
                                   pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapRazorPages();

                endpoints.MapHub<Chat>("/chathub");
                endpoints.MapHub<ShapeHub>("/shapeHub");
            });

            app.UseSpa(spa =>
                      {
                          spa.Options.SourcePath = "ClientApp";

                          if (HostingEnvironment.IsDevelopment())
                          {
                              //spa.UseAngularCliServer(npmScript: "start");
                              //   OR
                              spa.UseProxyToSpaDevelopmentServer("http://localhost:4200");
                          }
                      });
        }
    }
}
