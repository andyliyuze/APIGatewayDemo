namespace CustomersServices.Controllers
{
    
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;

    [Route("api/[controller]")]
    [EnableRateLimiting("fixed")]
    public class CustomersController : Controller
    { 
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Catcher Wong", "James Li", "James Li" };
        }

        [HttpGet("{id}")]
        public string Get(int id)
        {
            return $"Catcher Wong - {id}";
        }            
    }
}
