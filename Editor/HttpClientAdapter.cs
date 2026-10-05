using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Hyperlab.Bootstrap
{
    public sealed class HttpClientAdapter : IHttp
    {
        static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        public async Task<HttpResult> SendAsync(string method, string url, string jsonBody, string basicUser, string basicPassword, string bearer)
        {
            try
            {
                using var req = new HttpRequestMessage(new HttpMethod(method), url);
                if (jsonBody != null) req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                if (basicUser != null)
                    req.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuth.Header(basicUser, basicPassword));
                else if (bearer != null)
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                using var res = await Client.SendAsync(req);
                return new HttpResult { Status = (int)res.StatusCode, Body = await res.Content.ReadAsStringAsync() };
            }
            catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException || e is OperationCanceledException)
            {
                return new HttpResult { Status = 0 };
            }
        }
    }
}
