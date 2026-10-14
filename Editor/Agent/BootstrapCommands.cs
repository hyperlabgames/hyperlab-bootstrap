using System;
using Unity.Pipeline.Commands;

namespace Hyperlab.Bootstrap
{
    public static class BootstrapCommands
    {
        [CliCommand("bootstrap_status", "Returns whether the registry token, the Hyperlab scoped registry and the Hyperlab Setup dependency are present.",
            MainThreadRequired = false, Tags = new[] { "hyperlab/bootstrap" })]
        public static object Status()
        {
            var s = BootstrapService.Status(BootstrapPaths.Default());
            return new { token_present = s.TokenPresent, manifest_has_registry = s.ManifestHasRegistry, manifest_has_setup = s.ManifestHasSetup, bootstrap_from_git = s.BootstrapFromGit, complete = s.Complete };
        }

        [CliCommand("bootstrap_login", "Stores a registry token read from an environment variable in ~/.upmconfig.toml after checking it against the registry, and completes the manifest. People log in with Hyperlab > Login (GitHub). Mutates: requires confirm=true; dry_run=true only reports the status.",
            MainThreadRequired = false, Tags = new[] { "hyperlab/bootstrap" })]
        public static object Login(
            [CliArg("token_env", "Name of the environment variable that holds the registry token", Required = true)] string tokenEnv,
            [CliArg("dry_run", "Report the current status; nothing is written")] bool dryRun = false,
            [CliArg("confirm", "Check the token and write the files")] bool confirm = false)
        {
            var paths = BootstrapPaths.Default();
            if (dryRun || !confirm) return new { state = "dry_run", status = Status() };
            var token = Environment.GetEnvironmentVariable(tokenEnv);
            if (string.IsNullOrEmpty(token)) return new { state = "error", message = $"Environment variable {tokenEnv} is empty or not set." };
            var r = BootstrapService.ApplyTokenAsync(new HttpClientAdapter(), paths, token).GetAwaiter().GetResult();
            return new { state = r.Status.ToString(), message = r.Message };
        }
    }
}
