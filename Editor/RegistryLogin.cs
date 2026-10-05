using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hyperlab.Bootstrap
{
    public enum LoginStatus { Ok, WrongCredentials, Unreachable, Rejected }

    public struct LoginResult
    {
        public LoginStatus Status;
        public string Token;
        public string Message;
    }

    /// <summary>`npm login` ile aynı çağrı: PUT /-/user/org.couchdb.user:&lt;ad&gt; (Basic). Parola yalnız bellekte ve istekte durur.</summary>
    public static class RegistryLogin
    {
        public const string ProbePackage = "com.hyperlab.core";

        public static async Task<LoginResult> LoginAsync(IHttp http, string registryUrl, string user, string password)
        {
            var url = registryUrl.TrimEnd('/') + "/-/user/org.couchdb.user:" + Uri.EscapeDataString(user);
            var body = JsonConvert.SerializeObject(new { name = user, password });
            var res = await http.SendAsync("PUT", url, body, user, password, null);
            switch (res.Status)
            {
                case 0:
                    return new LoginResult { Status = LoginStatus.Unreachable, Message = "Cannot reach the Hyperlab registry." };
                case 401: case 403: case 409:
                    return new LoginResult { Status = LoginStatus.WrongCredentials, Message = "Wrong user name or password." };
                case 200: case 201:
                    string token = null;
                    try { token = (string)JObject.Parse(res.Body ?? "{}")["token"]; } catch (JsonException) { }
                    return string.IsNullOrEmpty(token)
                        ? new LoginResult { Status = LoginStatus.Rejected, Message = "The registry did not return a token." }
                        : new LoginResult { Status = LoginStatus.Ok, Token = token, Message = "Logged in." };
                default:
                    return new LoginResult { Status = LoginStatus.Rejected, Message = $"The registry answered with status {res.Status}." };
            }
        }

        public static async Task<bool> TokenWorksAsync(IHttp http, string registryUrl, string token)
            => (await ProbeTokenAsync(http, registryUrl, token)) == 200;

        /// <summary>HTTP durum kodu; 0 = sunucuya ulaşılamadı.</summary>
        public static async Task<int> ProbeTokenAsync(IHttp http, string registryUrl, string token)
        {
            var res = await http.SendAsync("GET", registryUrl.TrimEnd('/') + "/" + ProbePackage, null, null, null, token);
            return res.Status;
        }
    }
}
