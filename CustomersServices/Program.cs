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
                config.Address = new Uri("http://localhost:8500"); //����ע��ĵ�ַ����Ⱥ������һ����ַ
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
                            HTTP = $"{serverUrls}/api/Customers",//���������ʵĵ�ַ
                            Interval = TimeSpan.FromSeconds(10),   //�������ļ��ʱ��
                            Timeout = TimeSpan.FromSeconds(5),     //��ô���ʱ
                        },
                    };
                    consulClient.Agent.ServiceRegister(asr).Wait();
                }
            });
            
            IHostApplicationLifetime appLifeTime = app.Services.GetService<IHostApplicationLifetime>();
            //ע��Consul 
            appLifeTime.ApplicationStopped.Register(() =>
            {
                using var consulClient = new ConsulClient(ConsulConfig);
                consulClient.Agent.ServiceDeregister(serviceID).Wait();  //��consul��Ⱥ���Ƴ�����
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