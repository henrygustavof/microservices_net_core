using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoesOnContainers.Services.CartApi.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception type for app exceptions
    /// </summary>
    public class CartDomainException : Exception
    {
        public CartDomainException()
        { }

        public CartDomainException(string message)
            : base(message)
        { }

        public CartDomainException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
