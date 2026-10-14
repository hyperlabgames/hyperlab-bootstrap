using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hyperlab.Bootstrap
{
    /// <summary>Tarayıcı yönlendirmesini localhost'ta tek seferlik bekler; sonuçtan, iptalden ya da zaman aşımından sonra portu bırakır.</summary>
    public static class LoopbackReceiver
    {
        public const string CancelledMessage = "Login cancelled.";
        public const string TimedOutMessage = "GitHub login timed out; try again.";
        public static string PortInUseMessage(int port) => $"Port {port} is in use; close the other login and retry.";

        /// <summary>
        /// <paramref name="onListening"/> dinleyici açıldıktan sonra, ilk await'ten önce çağrılır: editörde ana iş
        /// parçacığında tarayıcıyı açmak için. Sonraki adımlar iş parçacığı havuzunda sürer (ConfigureAwait(false)).
        /// </summary>
        public static async Task<LoginResult> WaitAsync(int port, Action onListening, TimeSpan timeout, CancellationToken ct)
        {
            var listener = new HttpListener();
            // Eklenti tarayıcıyı http://localhost:port/'a yönlendirir; Mono "localhost"u IPv4 yerine başka
            // adrese bağlayabildiği için 127.0.0.1 de dinlenir: IPv4'te tutulan port çakışması da yakalanır.
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try { listener.Start(); }
            catch (Exception e) when (e is HttpListenerException || e is SocketException)
            {
                listener.Close();
                return Rejected(PortInUseMessage(port));
            }

            using (var stop = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                stop.CancelAfter(timeout);
                // GetContextAsync iptal almaz: dinleyiciyi durdurmak bekleyen çağrıyı istisnayla bitirir.
                var registration = stop.Token.Register(() =>
                {
                    // Close sonrası gelen iptal çağıranın Cancel()'ına istisna sızdırmasın.
                    try { listener.Stop(); }
                    catch (ObjectDisposedException) { }
                });
                using (registration)
                {
                    try
                    {
                        onListening?.Invoke();
                        while (true)
                        {
                            HttpListenerContext ctx;
                            try { ctx = await listener.GetContextAsync().ConfigureAwait(false); }
                            catch (Exception e) when (e is HttpListenerException || e is ObjectDisposedException || e is InvalidOperationException)
                            {
                                return Rejected(ct.IsCancellationRequested ? CancelledMessage : TimedOutMessage);
                            }
                            var result = GitHubLogin.ParseCallback(ctx.Request.QueryString);
                            Respond(ctx, result);
                            if (result != null) return result.Value;
                        }
                    }
                    finally
                    {
                        // Önce kaydı bırak (çalışan geri çağrıyı bekler), sonra kapat.
                        registration.Dispose();
                        listener.Close();
                    }
                }
            }
        }

        static LoginResult Rejected(string message) => new LoginResult { Status = LoginStatus.Rejected, Message = message };

        static void Respond(HttpListenerContext ctx, LoginResult? result)
        {
            try
            {
                if (result == null) { ctx.Response.StatusCode = 404; ctx.Response.Close(); return; }
                var body = result.Value.Status == LoginStatus.Ok
                    ? "<h1>Hyperlab Login</h1><p>Logged in to GitHub. You can return to Unity.</p>"
                    : "<h1>Hyperlab Login</h1><p>Login failed. Return to Unity for details.</p>";
                var bytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body style=\"font-family:sans-serif;text-align:center;margin-top:20%\">" + body + "</body></html>");
                ctx.Response.ContentType = "text/html; charset=utf-8";
                ctx.Response.ContentLength64 = bytes.Length;
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                ctx.Response.Close();
            }
            catch (Exception e) when (e is HttpListenerException || e is ObjectDisposedException || e is System.IO.IOException)
            {
                // Tarayıcı bağlantıyı erken kapattı; giriş sonucu yine geçerli.
            }
        }
    }
}
