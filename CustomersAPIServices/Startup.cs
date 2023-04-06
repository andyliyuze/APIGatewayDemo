namespace CustomerAPIServices
{
    using Consul;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifeTime, IWebHostEnvironment env)
        {
            Action<ConsulClientConfiguration> ConsulConfig = (config) =>
            {
                config.Address = new Uri("http://localhost:8500"); //服务注册的地址，集群中任意一个地址
                config.Datacenter = "dc1";
            };
            var serviceID = Guid.NewGuid().ToString("N");
            Task.Run(() =>
            {
                Task.Delay(1000).Wait();

                using (var consulClient = new ConsulClient(ConsulConfig))
                {
                    var serverUrls = app.ApplicationServices.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.FirstOrDefault();
                    var httpPort = serverUrls?.Split(':').Last();
                    var httpAddress = serverUrls?.Split(':')[1].Replace("//","");
                    AgentServiceRegistration asr = new AgentServiceRegistration
                    {
                        Address = httpAddress,
                        Port = Convert.ToInt32(httpPort),
                        ID = serviceID,
                        Name = "customerService",
                        Check = new AgentServiceCheck
                        {
                            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                            HTTP = $"{serverUrls}/api/Customers",//健康检查访问的地址
                            Interval = TimeSpan.FromSeconds(10),   //健康检查的间隔时间
                            Timeout = TimeSpan.FromSeconds(5),     //多久代表超时
                        },
                    };
                    consulClient.Agent.ServiceRegister(asr).Wait();
                }
            });
            //注销Consul 
            appLifeTime.ApplicationStopped.Register(() =>
            {
                using (var consulClient = new ConsulClient(ConsulConfig))
                {
                    consulClient.Agent.ServiceDeregister(serviceID).Wait();  //从consul集群中移除服务
                }
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
