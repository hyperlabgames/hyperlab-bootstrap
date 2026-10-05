using UnityEditor;
using UnityEngine.UIElements;

namespace Hyperlab.Bootstrap
{
    public sealed class LoginWindow : EditorWindow
    {
        TextField user, password;
        Label message;
        Button login, complete;

        string pending;

        [MenuItem("Hyperlab/Login")]
        static void OpenMenu() => Open();

        public static void Open(string reason = null)
        {
            var w = GetWindow<LoginWindow>("Hyperlab Login");
            w.minSize = new UnityEngine.Vector2(380, 220);
            if (reason == null) return;
            w.pending = reason;
            if (w.message != null) { w.message.text = reason; w.pending = null; }
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop = root.style.paddingBottom = root.style.paddingLeft = root.style.paddingRight = 12;
            root.Add(new Label("Hyperlab Login") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold, fontSize = 14, marginBottom = 6 } });
            root.Add(new Label("Enter the team's Hyperlab registry account. The token is stored in ~/.upmconfig.toml; the password is not saved.")
                { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });
            user = new TextField("User name");
            password = new TextField("Password") { isPasswordField = true };
            login = new Button(OnLogin) { text = "Log in" };
            message = new Label { style = { whiteSpace = WhiteSpace.Normal, marginTop = 8 } };
            complete = new Button(OnComplete) { text = "Complete setup (token already stored)" };
            root.Add(user); root.Add(password); root.Add(login); root.Add(complete); root.Add(message);
            Refresh(true);
            if (pending != null) { message.text = pending; pending = null; }
        }

        /// <summary>Düğme görünürlüğünü ayarlar; mesaja yalnız <paramref name="resetMessage"/> ile dokunur.</summary>
        void Refresh(bool resetMessage = false)
        {
            var s = BootstrapService.Status(BootstrapPaths.Default());
            if (resetMessage) message.text = s.Complete ? "Already set up. Open Hyperlab → Setup." : "";
            complete.style.display = s.TokenPresent && !s.Complete ? DisplayStyle.Flex : DisplayStyle.None;
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
            if (string.IsNullOrWhiteSpace(user.value))
            {
                message.text = "Enter the user name.";
                return;
            }
            var pwd = password.value;
            password.value = "";
            login.SetEnabled(false);
            message.text = "Logging in…";
            try
            {
                var r = await BootstrapService.RunAsync(new HttpClientAdapter(), BootstrapPaths.Default(), user.value.Trim(), pwd);
                message.text = r.Message;
            }
            catch (System.Exception e)
            {
                // Mesaj yalnız istisna türünü taşır; token/parola asla yazılmaz.
                message.text = "Login failed unexpectedly (" + e.GetType().Name + ").";
            }
            finally
            {
                login.SetEnabled(true);
                Refresh();
            }
        }
    }
}
