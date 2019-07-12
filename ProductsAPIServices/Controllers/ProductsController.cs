using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Mvc;

namespace ProductsAPIServices.Controllers
{
    [Route("api/[controller]")]
    public class ProductsController : Controller
    {

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Surface Book 2", "Mac Book Pro" };
        }


        [HttpGet]
        [Route("getFromCustomer")]
        public string GetFromCustomer()
        {
            Action<ConsulClientConfiguration> ConsulConfig = (config) =>
            {
                config.Address = new Uri("http://localhost:8500"); //服务注册的地址，集群中任意一个地址
                config.Datacenter = "dc1";
            };
            using (var consulClient = new ConsulClient(ConsulConfig))
            {
                var services = consulClient.Catalog.Service("customerService").Result.Response.FirstOrDefault();

                var url = $"http://{services.ServiceAddress}:{services.ServicePort}/api/customers";

                using (var httpclient = new HttpClient())
                {
                    var str = httpclient.GetStringAsync(url).Result;
                    return $"GetFromCustomer : {str}";
                }
            }
        }
    }
}
