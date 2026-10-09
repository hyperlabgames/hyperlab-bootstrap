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

        [CliCommand("bootstrap_login", "Logs in to the Hyperlab registry, stores the token in ~/.upmconfig.toml and completes the manifest. The password is read from an environment variable. Mutates: requires confirm=true; dry_run=true only reports the status.",
            MainThreadRequired = false, Tags = new[] { "hyperlab/bootstrap" })]
        public static object Login(
            [CliArg("user", "Registry user name", Required = true)] string user,
            [CliArg("password_env", "Name of the environment variable that holds the password", Required = true)] string passwordEnv,
            [CliArg("dry_run", "Report the current status; nothing is written")] bool dryRun = false,
            [CliArg("confirm", "Log in and write the files")] bool confirm = false)
        {
            var paths = BootstrapPaths.Default();
            if (dryRun || !confirm) return new { state = "dry_run", status = Status() };
            var password = Environment.GetEnvironmentVariable(passwordEnv);
            if (string.IsNullOrEmpty(password)) return new { state = "error", message = $"Environment variable {passwordEnv} is empty or not set." };
            var r = BootstrapService.RunAsync(new HttpClientAdapter(), paths, user, password).GetAwaiter().GetResult();
            return new { state = r.Status.ToString(), message = r.Message };
        }
    }
}
