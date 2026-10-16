using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace Hyperlab.Bootstrap
{
    /// <summary>
    /// verdaccio-github-oauth-ui CLI akışı: tarayıcı `…/-/oauth/authorize/cli` açar, GitHub sonrası sunucu
    /// `http://localhost:8239/?status=…&amp;token=…` adresine yönlendirir. Token yalnız bellekte ve yazılan dosyada durur.
    /// </summary>
    public static class GitHubLogin
    {
        /// <summary>Eklentide sabit; değiştirilemez.</summary>
        public const int CallbackPort = 8239;
        /// <summary>Sunucu mesajı saldırgan denetiminde olabilir; pencerede bu kadarı gösterilir.</summary>
        public const int MaxReasonLength = 120;
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

        public static string AuthorizeUrl(string registryUrl) => registryUrl.TrimEnd('/') + "/-/oauth/authorize/cli";

        // shortcut: OAuth `state` yok. verdaccio-github-oauth-ui CliFlow geri çağrıyı yalnız status/token/message ile
        // yönlendirir, istemci state'ini taşımaz (kaynak doğrulandı); eklenti state desteklerse üret ve ParseCallback'te karşılaştır.
        /// <summary>Yönlendirme sorgusu; <c>status</c> yoksa null (favicon gibi ilgisiz istek: beklemeye devam).</summary>
        public static LoginResult? ParseCallback(NameValueCollection query)
        {
            var status = query?["status"];
            if (string.IsNullOrEmpty(status)) return null;
            if (status != "success")
            {
                var reason = query["message"];
                if (reason != null && reason.Length > MaxReasonLength) reason = reason.Substring(0, MaxReasonLength) + "…";
                return new LoginResult { Status = LoginStatus.Rejected, Message = "GitHub login failed: " + (string.IsNullOrEmpty(reason) ? "unknown error" : reason) };
            }
            var raw = query["token"];
            // Sunucu token'ı iki kez kodlar (encodeURIComponent + querystring); sorgu bir kez çözülmüş gelir.
            var token = string.IsNullOrEmpty(raw) ? null : Uri.UnescapeDataString(raw);
            return string.IsNullOrEmpty(token)
                ? new LoginResult { Status = LoginStatus.Rejected, Message = "The registry did not return a token." }
                : new LoginResult { Status = LoginStatus.Ok, Token = token };
        }

        /// <summary>
        /// Tarayıcıda GitHub girişini başlatır, yönlendirmeyi bekler, token'ı <see cref="BootstrapService.ApplyTokenAsync"/>'e verir.
        /// <paramref name="openUrl"/> çağıranın iş parçacığında, dinleyici açıldıktan hemen sonra çağrılır.
        /// </summary>
        public static async Task<LoginResult> LoginAsync(IHttp http, BootstrapPaths p, Action<string> openUrl, CancellationToken ct,
            int port = CallbackPort, TimeSpan? timeout = null)
        {
            var url = AuthorizeUrl(BootstrapDefaults.RegistryUrl);
            var got = await LoopbackReceiver.WaitAsync(port, () => openUrl(url), timeout ?? DefaultTimeout, ct).ConfigureAwait(false);
            if (got.Status != LoginStatus.Ok) return got;
            return await BootstrapService.ApplyTokenAsync(http, p, got.Token).ConfigureAwait(false);
        }
    }
}
