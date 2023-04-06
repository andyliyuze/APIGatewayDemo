using Consul;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace CustomersServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRateLimiter(_ => _
            .AddFixedWindowLimiter(policyName: "fixed", options =>
            {
                options.PermitLimit = 2;
                options.Window = TimeSpan.FromSeconds(60);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 1;
            }));
            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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
                    var serverUrls = app.Services.GetService<IServer>()?.Features?.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
                    var httpPort = serverUrls?.Split(':').Last().Replace("/", "");
                    var httpAddress = serverUrls?.Split(':')[1].Replace("/", "");
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
            
            IHostApplicationLifetime appLifeTime = app.Services.GetService<IHostApplicationLifetime>();
            //注销Consul 
            appLifeTime.ApplicationStopped.Register(() =>
            {
                using var consulClient = new ConsulClient(ConsulConfig);
                consulClient.Agent.ServiceDeregister(serviceID).Wait();  //从consul集群中移除服务
            });

            if (builder.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }            

            app.UseRouting();
            app.UseRateLimiter();
            app.UseAuthorization();

            app.MapDefaultControllerRoute().RequireRateLimiting("fixed");
            
            app.Run();
        }
    }
}