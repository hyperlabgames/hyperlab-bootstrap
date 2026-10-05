using System.Threading.Tasks;

namespace Hyperlab.Bootstrap
{
    public struct HttpResult
    {
        public int Status;
        public string Body;
    }

    /// <summary>Test için ayrılmış HTTP sınırı; `Status == 0` ağ hatasıdır.</summary>
    public interface IHttp
    {
        Task<HttpResult> SendAsync(string method, string url, string jsonBody, string basicUser, string basicPassword, string bearer);
    }
}
