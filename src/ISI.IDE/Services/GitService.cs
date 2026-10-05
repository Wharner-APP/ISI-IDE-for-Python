namespace ISI.IDE.Services;

/// <summary>Thin wrapper around the git command line (v0.1: status, commit, pull, push, log).</summary>
public static class GitService
{
    public static Task<ProcessResult> RunAsync(string repoDir, params string[] args) =>
        ProcessUtil.RunAsync("git", args, repoDir);

    /// <summary>Current branch name, or empty when the folder is not a git repository / git is missing.</summary>
    public static async Task<string> GetBranchAsync(string dir)
    {
        var r = await RunAsync(dir, "rev-parse", "--abbrev-ref", "HEAD");
        return r.ExitCode == 0 ? r.StdOut.Trim() : "";
    }
}
