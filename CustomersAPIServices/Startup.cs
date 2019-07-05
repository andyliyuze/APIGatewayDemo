namespace CustomerAPIServices
{
    using Consul;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;

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
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifeTime)
        {

            Action<ConsulClientConfiguration> ConsulConfig = (config) =>
            {
                config.Address = new Uri("http://localhost:8500"); //服务注册的地址，集群中任意一个地址
                config.Datacenter = "dc1";
            };
            using (var consulClient = new ConsulClient(ConsulConfig))
            {
                AgentServiceRegistration asr = new AgentServiceRegistration
                {
                    Address = "localhost",
                    Port = Convert.ToInt32("9001"),
                    ID = "1",
                    Name = "customerService",
                    Check = new AgentServiceCheck
                    {
                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                        HTTP = $"http://localhost:9001/api/Customers",//健康检查访问的地址
                        Interval = TimeSpan.FromSeconds(10),   //健康检查的间隔时间
                        Timeout = TimeSpan.FromSeconds(5),     //多久代表超时
                    },
                };
                consulClient.Agent.ServiceRegister(asr).Wait();
            }
            //注销Consul 
            appLifeTime.ApplicationStopped.Register(() =>
            {
                using (var consulClient = new ConsulClient(ConsulConfig))
                {
                    consulClient.Agent.ServiceDeregister("1").Wait();  //从consul集群中移除服务
                }
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
