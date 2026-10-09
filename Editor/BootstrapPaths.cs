using System;
using System.IO;

namespace Hyperlab.Bootstrap
{
    public static class BootstrapDefaults
    {
        public const string RegistryUrl = "https://upm.hyperlab.games";
        public const string RegistryName = "Hyperlab";
        public static readonly string[] Scopes = { "com.hyperlab", "com.cysharp" };
        public const string SetupPackage = "com.hyperlab.setup";
        public const string SetupVersion = "0.3.0";
        public const string BootstrapPackage = "com.hyperlab.bootstrap";
        /// <summary>Git URL'den kurulan bootstrap'ın login sonrası geçirildiği registry sürümü; `package.json` ile eşit (test denetler).</summary>
        public const string BootstrapVersion = "0.1.4";
    }

    public sealed class BootstrapPaths
    {
        public string UpmConfig;
        public string Manifest;

        public static BootstrapPaths Default() => new BootstrapPaths
        {
            UpmConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".upmconfig.toml"),
            // Editör proje kökünde çalışır; agent komutu ana iş parçacığı dışında koştuğundan Application.dataPath kullanılmaz.
            Manifest = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json"),
        };
    }
}
