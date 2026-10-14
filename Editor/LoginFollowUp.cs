using UnityEditor;

namespace Hyperlab.Bootstrap
{
    /// <summary>
    /// Başarılı girişten sonra: pencere kapanır, paketler inip alan yeniden yüklendiğinde Setup sihirbazı açılır. Alan
    /// yeniden yüklenince statikler sıfırlandığı için "Setup'ı aç" isteği <see cref="SessionState"/>'te taşınır.
    /// </summary>
    public static class LoginFollowUp
    {
        const string Key = "Hyperlab.Bootstrap.OpenSetupAfterLogin";
        const string SetupMenu = "Hyperlab/Setup";

        public static bool ShouldClose(LoginResult result) => result.Status == LoginStatus.Ok;

        public static bool ShouldOpenSetup(bool requested, bool menuExists) => requested && menuExists;

        internal static void Request() => SessionState.SetBool(Key, true);

        /// <summary>Setup menüsü henüz yoksa (paket inmedi) istek korunur ve sonraki yeniden yüklemede denenir.</summary>
        internal static void OpenSetupIfRequested()
        {
            var requested = SessionState.GetBool(Key, false);
            if (!requested) return;
            var menuExists = Menu.GetEnabled(SetupMenu);
            if (!ShouldOpenSetup(requested, menuExists)) return;
            SessionState.EraseBool(Key);
            EditorApplication.ExecuteMenuItem(SetupMenu);
        }
    }
}
