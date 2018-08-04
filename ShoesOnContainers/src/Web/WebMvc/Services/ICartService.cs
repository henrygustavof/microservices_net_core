using ShoesOnContainers.Web.WebMvc.Models;
using ShoesOnContainers.Web.WebMvc.Models.CartModels;
using ShoesOnContainers.Web.WebMvc.Models.OrderModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoesOnContainers.Web.WebMvc.Services
{
    public interface ICartService
    {
        Task<Cart> GetCart(ApplicationUser user);
        Task AddItemToCart(ApplicationUser user, CartItem product);
        Task<Cart> UpdateCart(Cart Cart);
        Task<Cart> SetQuantities(ApplicationUser user, Dictionary<string, int> quantities);
        Order MapCartToOrder(Cart Cart);
        Task ClearCart(ApplicationUser user);

    }
}
