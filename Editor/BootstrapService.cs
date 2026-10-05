using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Hyperlab.Bootstrap
{
    public struct BootstrapStatus
    {
        public bool TokenPresent;
        public bool ManifestHasRegistry;
        public bool ManifestHasSetup;
        /// <summary>Bootstrap `file:` ile kurulu (yerel geliştirme / Sandbox): manifest'e dokunulmaz.</summary>
        public bool LocalDev;
        public bool NeedsLogin => !TokenPresent;
        /// <summary>Manifest'e yazılacak bir şey kalmadı: setup zaten var ya da yerel geliştirme kurulumu.</summary>
        public bool ManifestSettled => LocalDev || ManifestHasSetup;
        public bool Complete => TokenPresent && (LocalDev || (ManifestHasRegistry && ManifestHasSetup));
    }

    public static class BootstrapService
    {
        /// <summary>Agent komutu başka iş parçacığında koşar; Client.Resolve ana iş parçacığında `AutoOpen` ile çağrılır.</summary>
        public static volatile bool ResolveRequested;

        public static BootstrapStatus Status(BootstrapPaths p)
        {
            var toml = File.Exists(p.UpmConfig) ? File.ReadAllText(p.UpmConfig) : "";
            var hasRegistry = false;
            var hasSetup = false;
            var localDev = false;
            if (File.Exists(p.Manifest))
            {
                try
                {
                    var j = JObject.Parse(File.ReadAllText(p.Manifest));
                    var boot = (string)j["dependencies"]?["com.hyperlab.bootstrap"];
                    localDev = boot != null && boot.StartsWith("file:", StringComparison.Ordinal);
                    hasSetup = j["dependencies"]?[BootstrapDefaults.SetupPackage] != null;
                    if (j["scopedRegistries"] is JArray regs)
                        foreach (var r in regs)
                            if (UpmConfigWriter.SameUrl((string)r["url"], BootstrapDefaults.RegistryUrl)) hasRegistry = true;
                }
                catch (Newtonsoft.Json.JsonException) { }
            }
            return new BootstrapStatus
            {
                TokenPresent = UpmConfigWriter.TokenFor(toml, BootstrapDefaults.RegistryUrl) != null,
                ManifestHasRegistry = hasRegistry,
                ManifestHasSetup = hasSetup,
                LocalDev = localDev,
            };
        }

        public static async Task<LoginResult> RunAsync(IHttp http, BootstrapPaths p, string user, string password)
        {
            var login = await RegistryLogin.LoginAsync(http, BootstrapDefaults.RegistryUrl, user, password);
            if (login.Status != LoginStatus.Ok) return login;

            // Setup zaten kuruluyken login (token yenileme) "being installed" demesin.
            var setupInstalled = Status(p).ManifestHasSetup;
            var manifest = PrepareManifest(p, out var manifestError);
            if (manifestError != null) return manifestError.Value;

            try
            {
                var toml = File.Exists(p.UpmConfig) ? File.ReadAllText(p.UpmConfig) : "";
                WriteAtomic(p.UpmConfig, UpmConfigWriter.SetToken(toml, BootstrapDefaults.RegistryUrl, login.Token), restrict: true);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Token ve parola mesaja girmez.
                return new LoginResult
                {
                    Status = LoginStatus.Rejected,
                    Message = "Could not write ~/.upmconfig.toml: " + p.UpmConfig
                        + ". Check that the file and its folder are writable, then retry; the token is never shown here.",
                };
            }

            if (manifest.Changed)
            {
                try { WriteAtomic(p.Manifest, manifest.Json, restrict: false); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    return new LoginResult { Status = LoginStatus.Rejected, Message = "Token saved; Packages/manifest.json could not be written: " + p.Manifest
                        + ". Use Hyperlab > Login > Complete setup after fixing it." };
                }
            }
            ResolveRequested = true;
            return new LoginResult
            {
                Status = LoginStatus.Ok,
                Message = setupInstalled ? "Logged in. Hyperlab Setup is already installed." : "Logged in. Hyperlab Setup is being installed.",
            };
        }

        /// <summary>
        /// Token zaten varsa parolasız yol: token registry'de çalışıyorsa yalnız manifest eksiği tamamlanır (spec §3 adım 6).
        /// </summary>
        public static async Task<LoginResult> CompleteManifestAsync(IHttp http, BootstrapPaths p)
        {
            // Yerel (`file:`) kurulum ya da setup zaten manifest'te: registry yazılmaz, ağa çıkılmaz, Resolve istenmez.
            if (Status(p).ManifestSettled)
                return new LoginResult { Status = LoginStatus.Ok, Message = "Nothing to complete: Packages/manifest.json already references Hyperlab Setup or the local bootstrap." };
            var toml = File.Exists(p.UpmConfig) ? File.ReadAllText(p.UpmConfig) : "";
            var token = UpmConfigWriter.TokenFor(toml, BootstrapDefaults.RegistryUrl);
            if (token == null)
                return new LoginResult { Status = LoginStatus.Rejected, Message = "No stored registry token; log in first." };
            var probe = await RegistryLogin.ProbeTokenAsync(http, BootstrapDefaults.RegistryUrl, token);
            if (probe == 0)
                return new LoginResult { Status = LoginStatus.Unreachable, Message = "Cannot reach the Hyperlab registry; check the network and retry." };
            if (probe != 200)
                return new LoginResult { Status = LoginStatus.WrongCredentials, Message = "The stored token is not accepted by the registry; log in again." };

            var manifest = PrepareManifest(p, out var manifestError);
            if (manifestError != null) return manifestError.Value;
            if (manifest.Changed)
            {
                try { WriteAtomic(p.Manifest, manifest.Json, restrict: false); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    return new LoginResult { Status = LoginStatus.Rejected, Message = "Packages/manifest.json could not be written: " + p.Manifest };
                }
            }
            ResolveRequested = true;
            return new LoginResult { Status = LoginStatus.Ok, Message = "Hyperlab Setup is being installed." };
        }

        static ManifestResult PrepareManifest(BootstrapPaths p, out LoginResult? error)
        {
            error = null;
            string manifestText;
            try { manifestText = File.ReadAllText(p.Manifest); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                error = new LoginResult { Status = LoginStatus.Rejected, Message = "Packages/manifest.json could not be read." };
                return default;
            }
            try
            {
                return ManifestWriter.Ensure(manifestText, BootstrapDefaults.RegistryName,
                    BootstrapDefaults.RegistryUrl, BootstrapDefaults.Scopes, BootstrapDefaults.SetupPackage, BootstrapDefaults.SetupVersion);
            }
            catch (FormatException)
            {
                error = new LoginResult { Status = LoginStatus.Rejected, Message = "Packages/manifest.json is not valid JSON; fix it and retry." };
                return default;
            }
        }

        static void WriteAtomic(string path, string content, bool restrict)
        {
            var tmp = path + ".hltmp";
            try
            {
                if (restrict)
                {
                    // Önce boş dosya + chmod 600, sonra içerik: token hiç 644 ile görünmez.
                    File.WriteAllText(tmp, "");
                    TryChmod600(tmp);
                }
                File.WriteAllText(tmp, content);
                // Var olan dosya yerine konmadan silinmez: Replace başarısız olursa özgün dosya durur.
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch (Exception) { /* en iyi çaba */ }
            }
        }

        static void TryChmod600(string path)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) return;
            try
            {
                using var proc = Process.Start(new ProcessStartInfo("chmod", "600 \"" + path + "\"") { UseShellExecute = false, CreateNoWindow = true });
                proc?.WaitForExit(2000);
            }
            catch (Exception) { /* izin ayarı en iyi çaba; dosya yine yazılır */ }
        }
    }
}
