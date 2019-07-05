namespace APIGateway
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;
    using Ocelot.Provider.Consul;
    using Ocelot.Provider.Polly;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
            builder.SetBasePath(env.ContentRootPath)
                   .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables();


            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //Action<ConfigurationBuilderCachePart> settings = (x) =>
            //{
            //    x.WithMicrosoftLogging(log =>
            //    {
            //        log.AddConsole(LogLevel.Debug);

            //    }).WithDictionaryHandle();
            //};
            var conig = Configuration;
            services.AddOcelot(conig).AddConsul();
        }

        public async void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            await app.UseOcelot();
        }
    }
}
