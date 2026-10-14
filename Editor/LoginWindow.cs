using System.Threading;
using UnityEditor;
using UnityEngine.UIElements;

namespace Hyperlab.Bootstrap
{
    public sealed class LoginWindow : EditorWindow
    {
        Label message;
        Button login, cancel, complete;
        CancellationTokenSource running;

        string pending;

        [MenuItem("Hyperlab/Login")]
        static void OpenMenu() => Open();

        public static void Open(string reason = null)
        {
            var w = GetWindow<LoginWindow>("Hyperlab Login");
            w.minSize = new UnityEngine.Vector2(380, 200);
            if (reason == null) return;
            w.pending = reason;
            if (w.message != null) { w.message.text = reason; w.pending = null; }
        }

        void CreateGUI() => Build();

        internal void BuildForTest() => Build();

        void Build()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.paddingTop = root.style.paddingBottom = root.style.paddingLeft = root.style.paddingRight = 12;
            root.Add(new Label("Hyperlab Login") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold, fontSize = 14, marginBottom = 6 } });
            root.Add(new Label("Log in with your GitHub account. Access requires membership in the hyperlabgames studio-developer or studio-cofounder team. The token is stored in ~/.upmconfig.toml.")
                { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });
            login = new Button(OnLogin) { name = "login", text = "Log in with GitHub" };
            cancel = new Button(() => running?.Cancel()) { name = "cancel", text = "Cancel" };
            cancel.style.display = DisplayStyle.None;
            message = new Label { style = { whiteSpace = WhiteSpace.Normal, marginTop = 8 } };
            complete = new Button(OnComplete) { name = "complete", text = "Complete setup (token already stored)" };
            root.Add(login); root.Add(cancel); root.Add(complete); root.Add(message);
            Refresh(true);
            if (pending != null) { message.text = pending; pending = null; }
        }

        // Pencere kapanınca bekleyen giriş iptal olur: port 8239 serbest kalır.
        void OnDisable() => running?.Cancel();

        /// <summary>Düğme görünürlüğünü ayarlar; mesaja yalnız <paramref name="resetMessage"/> ile dokunur.</summary>
        void Refresh(bool resetMessage = false)
        {
            var s = BootstrapService.Status(BootstrapPaths.Default());
            if (resetMessage) message.text = s.Complete ? "Already set up. Open Hyperlab → Setup." : "";
            var waiting = running != null;
            login.style.display = waiting ? DisplayStyle.None : DisplayStyle.Flex;
            cancel.style.display = waiting ? DisplayStyle.Flex : DisplayStyle.None;
            complete.style.display = !waiting && s.TokenPresent && !s.Complete ? DisplayStyle.Flex : DisplayStyle.None;
        }

        async void OnComplete()
        {
            complete.SetEnabled(false);
            message.text = "Completing setup…";
            try
            {
                var r = await BootstrapService.CompleteManifestAsync(new HttpClientAdapter(), BootstrapPaths.Default());
                message.text = r.Message;
            }
            catch (System.Exception e)
            {
                message.text = "Setup failed unexpectedly (" + e.GetType().Name + ").";
            }
            finally
            {
                complete.SetEnabled(true);
                Refresh();
            }
        }

        async void OnLogin()
        {
            running = new CancellationTokenSource();
            Refresh();
            message.text = "Waiting for GitHub in your browser…";
            try
            {
                var r = await GitHubLogin.LoginAsync(new HttpClientAdapter(), BootstrapPaths.Default(), UnityEngine.Application.OpenURL, running.Token);
                if (this == null) return; // pencere beklerken kapandı
                message.text = r.Message;
                if (LoginFollowUp.ShouldClose(r))
                {
                    UnityEngine.Debug.Log("Hyperlab Login: " + r.Message);
                    LoginFollowUp.Request();
                    Close();
                    return;
                }
            }
            catch (System.Exception e)
            {
                // Mesaj yalnız istisna türünü taşır; token asla yazılmaz.
                if (this != null) message.text = "Login failed unexpectedly (" + e.GetType().Name + ").";
            }
            finally
            {
                running?.Dispose();
                running = null;
                if (this != null && message != null) Refresh();
            }
        }
    }
}
