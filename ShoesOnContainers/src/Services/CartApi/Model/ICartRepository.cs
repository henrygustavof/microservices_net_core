using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoesOnContainers.Services.CartApi.Model
{
    public interface ICartRepository
    {
        Task<Cart> GetCartAsync(string cartId);
         IEnumerable<string>  GetUsers();
        Task<Cart> UpdateCartAsync(Cart basket);
        Task<bool> DeleteCartAsync(string id);
    }
}
