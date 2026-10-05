using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Hyperlab.Bootstrap
{
    [InitializeOnLoad]
    static class AutoOpen
    {
        static AutoOpen()
        {
            EditorApplication.update += Tick;
            EditorApplication.delayCall += OpenOnce;
        }

        static void Tick()
        {
            if (!BootstrapService.ResolveRequested) return;
            BootstrapService.ResolveRequested = false;
            Client.Resolve();
        }

        /// <summary>
        /// Proje başına bir kez: giriş eksikse pencere kendiliğinden açılır. Token varsa pencere açılmaz, manifest eksiği
        /// parolasız tamamlanır. Batchmode'da (CI, headless agent editörleri) hiçbir şey yapılmaz.
        /// </summary>
        static async void OpenOnce()
        {
            if (Application.isBatchMode) return;
            var marker = Path.Combine("Library", "HyperlabBootstrap", "opened");
            try
            {
                if (File.Exists(marker)) return;
                var paths = BootstrapPaths.Default();
                var status = BootstrapService.Status(paths);
                if (!status.Complete)
                {
                    string reason = null;
                    if (status.TokenPresent)
                    {
                        var r = await BootstrapService.CompleteManifestAsync(new HttpClientAdapter(), paths);
                        if (r.Status != LoginStatus.Ok) reason = r.Message;
                    }
                    if (!status.TokenPresent || reason != null) LoginWindow.Open(reason);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(marker));
                File.WriteAllText(marker, "1");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Hyperlab Bootstrap: could not check the login state (" + e.GetType().Name + "). Open Hyperlab > Login manually.");
            }
        }
    }
}
