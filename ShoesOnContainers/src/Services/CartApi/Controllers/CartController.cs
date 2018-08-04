using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ShoesOnContainers.Services.CartApi.Model;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace ShoesOnContainers.Services.CartApi.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CartController : Controller
    {
        private ICartRepository _repository;
        private ILogger _logger;
        public CartController(ICartRepository repository, ILoggerFactory factory)
        {
            _repository = repository;
            _logger = factory.CreateLogger<CartController>();
        }
        // GET api/values/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Cart), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get(string id)
        {
            var basket = await _repository.GetCartAsync(id);

            return Ok(basket);
        }

        // POST api/values
        [HttpPost]
       
        [ProducesResponseType(typeof(Cart), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Post([FromBody]Cart value)
        {
            var basket = await _repository.UpdateCartAsync(value);

            return Ok(basket);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            _logger.LogInformation("Delete method in Cart controller reached");
            _repository.DeleteCartAsync(id);

        }
    }
}