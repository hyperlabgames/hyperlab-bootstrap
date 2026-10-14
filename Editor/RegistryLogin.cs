using System;
using System.Threading.Tasks;

namespace Hyperlab.Bootstrap
{
    public enum LoginStatus { Ok, WrongCredentials, Unreachable, Rejected }

    public struct LoginResult
    {
        public LoginStatus Status;
        public string Token;
        public string Message;
    }

    /// <summary>Registry token yoklaması: GET &lt;registry&gt;/com.hyperlab.core (Bearer).</summary>
    public static class RegistryLogin
    {
        public const string ProbePackage = "com.hyperlab.core";

        public static async Task<bool> TokenWorksAsync(IHttp http, string registryUrl, string token)
            => (await ProbeTokenAsync(http, registryUrl, token)) == 200;

        /// <summary>HTTP durum kodu; 0 = sunucuya ulaşılamadı.</summary>
        public static async Task<int> ProbeTokenAsync(IHttp http, string registryUrl, string token)
        {
            var res = await http.SendAsync("GET", registryUrl.TrimEnd('/') + "/" + ProbePackage, null, token);
            return res.Status;
        }
    }
}
