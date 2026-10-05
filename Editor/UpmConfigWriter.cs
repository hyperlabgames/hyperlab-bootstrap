using System;
using System.Collections.Generic;

namespace Hyperlab.Bootstrap
{
    /// <summary>`~/.upmconfig.toml` metni üzerinde yalnız `[npmAuth."URL"]` bölümünü ekler/günceller; başka satırlara dokunmaz.</summary>
    public static class UpmConfigWriter
    {
        public static string SetToken(string toml, string registryUrl, string token)
        {
            if (string.IsNullOrEmpty(token) || token.IndexOfAny(new[] { ' ', '"', '\n', '\r', '\t', '\\' }) >= 0)
                throw new ArgumentException("Token is empty or contains forbidden characters.", nameof(token));

            toml ??= "";
            var nl = toml.Contains("\r\n") ? "\r\n" : "\n";
            var lines = new List<string>(toml.Replace("\r\n", "\n").Split('\n'));
            if (lines.Count > 0 && lines[lines.Count - 1].Length == 0) lines.RemoveAt(lines.Count - 1);

            var header = lines.FindIndex(l => IsHeaderFor(l, registryUrl));
            if (header < 0)
            {
                if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length > 0) lines.Add("");
                lines.Add($"[npmAuth.\"{registryUrl}\"]");
                lines.Add($"token = \"{token}\"");
                lines.Add("alwaysAuth = true");
            }
            else
            {
                var end = header + 1;
                while (end < lines.Count && !lines[end].TrimStart().StartsWith("[", StringComparison.Ordinal)) end++;
                var kept = new List<string>();
                for (var i = header + 1; i < end; i++)
                {
                    var key = lines[i].TrimStart();
                    if (key.StartsWith("token", StringComparison.Ordinal) || key.StartsWith("alwaysAuth", StringComparison.Ordinal)) continue;
                    kept.Add(lines[i]);
                }
                lines.RemoveRange(header + 1, end - header - 1);
                var insert = new List<string> { $"token = \"{token}\"", "alwaysAuth = true" };
                insert.AddRange(kept);
                lines.InsertRange(header + 1, insert);
                lines[header] = $"[npmAuth.\"{registryUrl}\"]";
            }
            return string.Join(nl, lines) + nl;
        }

        public static string TokenFor(string toml, string registryUrl)
        {
            if (string.IsNullOrEmpty(toml)) return null;
            var inSection = false;
            foreach (var raw in toml.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("[", StringComparison.Ordinal)) { inSection = IsHeaderFor(line, registryUrl); continue; }
                if (!inSection || !line.StartsWith("token", StringComparison.Ordinal)) continue;
                var eq = line.IndexOf('=');
                if (eq < 0) continue;
                var value = line.Substring(eq + 1).Trim().Trim('"');
                return value.Length == 0 ? null : value;
            }
            return null;
        }

        static bool IsHeaderFor(string line, string registryUrl)
        {
            line = line.Trim();
            if (!line.StartsWith("[npmAuth.", StringComparison.Ordinal)) return false;
            var s = line.IndexOf('"');
            var e = line.LastIndexOf('"');
            return s >= 0 && e > s && SameUrl(line.Substring(s + 1, e - s - 1), registryUrl);
        }

        public static bool SameUrl(string a, string b) =>
            string.Equals((a ?? "").TrimEnd('/'), (b ?? "").TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }
}
