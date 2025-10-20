using System.CommandLine;
using System.Text.Json;
using Octokit;
using ReviewSchemas;

var patchFileOpt = new Option<string>("--patch-file", description: "Unified diff file path") { IsRequired = true };
var ownerOpt     = new Option<string>("--owner", description: "GitHub owner") { IsRequired = true };
var repoOpt      = new Option<string>("--repo", description: "GitHub repo") { IsRequired = true };
var prOpt        = new Option<int>("--pr", description: "Pull request number") { IsRequired = true };

var root = new RootCommand("Invoke MCP review and post to PR");
root.AddOption(patchFileOpt);
root.AddOption(ownerOpt);
root.AddOption(repoOpt);
root.AddOption(prOpt);

root.SetHandler(async (patchPath, owner, repo, prNumber) =>
{
    var patch = await File.ReadAllTextAsync(patchPath);

    var req = System.Text.Json.JsonSerializer.Serialize(new { method = "review_diff", @params = new { patch } });
    var si = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "./src/ReviewMcpServer/bin/Release/net8.0/ReviewMcpServer",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    using var call = System.Diagnostics.Process.Start(si)!;
    await call.StandardInput.WriteLineAsync(req);
    call.StandardInput.Close();
    var json = await call.StandardOutput.ReadToEndAsync();

    var review = System.Text.Json.JsonSerializer.Deserialize<ReviewResponse>(json) ?? new ReviewResponse("(empty)", new());

    var gh = new GitHubClient(new ProductHeaderValue("ai-review"))
    {
        Credentials = new Credentials(Environment.GetEnvironmentVariable("GITHUB_TOKEN"))
    };

    var body = BuildMarkdown(review);
    await gh.PullRequest.Review.Create(owner, repo, prNumber, new PullRequestReviewCreate
    {
        Body = body,
        Event = PullRequestReviewEvent.Comment
    });

}, patchFileOpt, ownerOpt, repoOpt, prOpt);

return await root.InvokeAsync(args);

static string BuildMarkdown(ReviewResponse r)
{
    var md = $"### 🤖 AI Code Review Summary\n\n{r.Summary}\n\n";
    if (r.Findings.Count == 0) return md + "No issues found.";

    md += "\n| Severity | File:Line | Title | Suggestion |\n|---|---|---|---|\n";
    foreach (var f in r.Findings)
    {
        md += $"| {f.Severity} | `{f.File}:{f.Line}` | {Escape(f.Title)} | {Escape(f.SuggestedFix)} |\n";
    }
    md += "\n> Detailed explanations are in the tool output.\n";
    return md;

    static string Escape(string s) => s.Replace("|", "\\|").Replace("\n", " ");
}
