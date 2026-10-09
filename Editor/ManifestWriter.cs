using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hyperlab.Bootstrap
{
    public readonly struct ManifestResult
    {
        public readonly string Json;
        public readonly bool Changed;
        public ManifestResult(string json, bool changed) { Json = json; Changed = changed; }
    }

    /// <summary>`Packages/manifest.json` farkı: scoped registry + tek paket bağımlılığı ekler; hiçbir şeyi silmez ya da ezmez.</summary>
    public static class ManifestWriter
    {
        public static ManifestResult Ensure(string manifestJson, string registryName, string registryUrl,
            string[] scopes, string package, string version)
        {
            JObject root;
            try { root = JObject.Parse(manifestJson); }
            catch (JsonException e) { throw new FormatException("manifest.json is not valid JSON: " + e.Message); }

            var changed = false;

            if (!(root["dependencies"] is JObject deps)) { deps = new JObject(); root["dependencies"] = deps; changed = true; }
            if (deps[package] == null) { deps[package] = version; changed = true; }

            if (!(root["scopedRegistries"] is JArray regs)) { regs = new JArray(); root["scopedRegistries"] = regs; changed = true; }
            var reg = regs.OfType<JObject>().FirstOrDefault(r => UpmConfigWriter.SameUrl((string)r["url"], registryUrl));
            if (reg == null)
            {
                regs.Add(new JObject { ["name"] = registryName, ["url"] = registryUrl, ["scopes"] = new JArray(scopes) });
                changed = true;
            }
            else
            {
                if (!(reg["scopes"] is JArray have)) { have = new JArray(); reg["scopes"] = have; changed = true; }
                foreach (var s in scopes)
                    if (!have.Any(x => (string)x == s)) { have.Add(s); changed = true; }
            }

            return changed ? new ManifestResult(root.ToString(Formatting.Indented), true) : new ManifestResult(manifestJson, false);
        }

        /// <summary>
        /// Git URL'den kurulu paketi registry sürümüne geçirir. Registry'nin scope'u aynı paketi sunduğunda Package Manager
        /// registry sürümünü (aynı sürüm olsa da) "Update" diye gösterir. `file:`, sürüm ya da eksik bağımlılığa dokunulmaz.
        /// </summary>
        public static ManifestResult UseRegistryFor(string manifestJson, string package, string version)
        {
            JObject root;
            try { root = JObject.Parse(manifestJson); }
            catch (JsonException e) { throw new FormatException("manifest.json is not valid JSON: " + e.Message); }

            if (!(root["dependencies"] is JObject deps) || !IsGitSource((string)deps[package]))
                return new ManifestResult(manifestJson, false);
            deps[package] = version;
            return new ManifestResult(root.ToString(Formatting.Indented), true);
        }

        /// <summary>UPM'nin Git bağımlılığı biçimleri: `git+…`, `git@…`, `.git` ile biten (ya da `.git#ref`) adres.</summary>
        public static bool IsGitSource(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.StartsWith("git+", StringComparison.Ordinal) || value.StartsWith("git@", StringComparison.Ordinal)) return true;
            var hash = value.IndexOf('#');
            var url = hash < 0 ? value : value.Substring(0, hash);
            return url.Contains("://") && url.EndsWith(".git", StringComparison.OrdinalIgnoreCase);
        }
    }
}
