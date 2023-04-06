using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Values;
using System.Collections.Concurrent;

namespace GatewayServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container. 
            builder.Services.AddMvc();

            builder.Services.AddAuthentication("Bearer").AddIdentityServerAuthentication("Bearer", delegate (IdentityServerAuthenticationOptions options)
            {
                options.Authority = builder.Configuration["IdentityServer"];
                options.RequireHttpsMetadata = false;
                options.ApiName = "cameraGatewayProxy";
                options.ApiSecret = "secret";
                options.SupportedTokens = SupportedTokens.Jwt;
             
                ConcurrentDictionary<string, string> dictionary = new ConcurrentDictionary<string, string>();
                options.JwtBearerEvents = new JwtBearerEvents
                {
                 
                };
            });

            var conbuilder = new ConfigurationBuilder();
            conbuilder.SetBasePath(builder.Environment.ContentRootPath)
                 .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
                  // .AddJsonFile("configuration.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables();  

             
            builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile("configuration.json", optional: false, reloadOnChange: true);
        
            builder.Services.AddOcelot(conbuilder.Build()).AddConsul();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            //app.UseAuthorization();

            //app.MapRazorPages();

            app.UseOcelot().Wait();
            app.Run();
        }
    }
}