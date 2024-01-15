using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro
{
    public class EntraID
    {
        private readonly string _primaryRefreshTokenCookie;
        private readonly string _authorizationHeader;

        // Create a nonce to use for PRT cookie requests
        static string CreateNonce()
        {
            string nonce = null;
            return nonce;
        }

        // Create a PRT cookie to use for access token requests
        static byte[] CreateSSOCookie(string nonce)
        {
            byte[] ssoCookie = null;
            return ssoCookie;
        }
    }
}
