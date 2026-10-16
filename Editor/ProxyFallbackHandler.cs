using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hyperlab.Bootstrap
{
    // Birlikte değişir: com.hyperlab.setup/Editor/Install/ProxyFallbackHandler.cs ile aynı kod. Setup bootstrap'a, bootstrap Setup'a
    // referans veremez (bootstrap herkese açık yansımaya kopyalanır), bu yüzden kopya.
    /// <summary>Unity editöründeki Mono `HttpClientHandler` bazı makinelerde sistem proxy yolunda her adrese "Connection refused"
    /// ile düşer (terminal `curl` çalışırken). Bağlantı hatasında isteği `UseProxy = false` işleyiciyle bir kez daha dener;
    /// HTTP durum kodları, iptal ve gövdeli istekler yeniden denenmez. İki deneme de düşerse ilk hata atılır.</summary>
    internal sealed class ProxyFallbackHandler : HttpMessageHandler
    {
        readonly HttpMessageInvoker primary;
        readonly HttpMessageInvoker fallback;

        public ProxyFallbackHandler() : this(new HttpClientHandler(), new HttpClientHandler { UseProxy = false }) { }

        /// <summary>Testte sahte işleyiciler verilir.</summary>
        public ProxyFallbackHandler(HttpMessageHandler primary, HttpMessageHandler fallback)
        {
            this.primary = new HttpMessageInvoker(primary);
            this.fallback = new HttpMessageInvoker(fallback);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var retry = request.Content == null ? Clone(request) : null;
            try
            {
                return await primary.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException first) when (retry != null && !ct.IsCancellationRequested)
            {
                try
                {
                    return await fallback.SendAsync(retry, ct).ConfigureAwait(false);
                }
                catch (HttpRequestException)
                {
                    throw first;
                }
            }
        }

        static HttpRequestMessage Clone(HttpRequestMessage request)
        {
            var copy = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
            foreach (var header in request.Headers) copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
            return copy;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                primary.Dispose();
                fallback.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
