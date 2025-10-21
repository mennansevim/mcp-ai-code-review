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
    try
    {
        if (!File.Exists(patchPath))
        {
            Console.WriteLine($"Error: Patch file not found at {patchPath}");
            Environment.Exit(1);
        }

        var patch = await File.ReadAllTextAsync(patchPath);
        
        if (string.IsNullOrWhiteSpace(patch))
        {
            Console.WriteLine("Warning: Patch file is empty. No changes to review.");
            Environment.Exit(0);
        }
        
        // Check patch size limit
        var maxBytes = int.TryParse(Environment.GetEnvironmentVariable("REVIEW_MAX_PATCH_BYTES"), out var limit) 
            ? limit 
            : 350000;
        
        var patchBytes = System.Text.Encoding.UTF8.GetByteCount(patch);
        if (patchBytes > maxBytes)
        {
            Console.WriteLine($"Warning: Patch size ({patchBytes} bytes) exceeds limit ({maxBytes} bytes). Truncating...");
            // Truncate patch to fit within limit
            var truncated = patch.Substring(0, Math.Min(patch.Length, maxBytes / 4)); // Conservative estimate
            patch = truncated + "\n\n... [Patch truncated due to size limit]";
        }

        var req = System.Text.Json.JsonSerializer.Serialize(new { method = "review_diff", @params = new { patch } });
        
        // Server path'i environment variable'dan veya default path'den al
        var serverPath = Environment.GetEnvironmentVariable("REVIEW_SERVER_PATH") 
            ?? "./src/ReviewMcpServer/bin/Release/net8.0/ReviewMcpServer";
        
        if (!File.Exists(serverPath))
        {
            Console.WriteLine($"Error: Server not found at {serverPath}");
            Environment.Exit(1);
        }

        Console.WriteLine($"Starting review server from: {serverPath}");
        
        var si = new System.Diagnostics.ProcessStartInfo
        {
            FileName = serverPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var call = System.Diagnostics.Process.Start(si)!;
        await call.StandardInput.WriteLineAsync(req);
        call.StandardInput.Close();
        
        var json = await call.StandardOutput.ReadToEndAsync();
        var stderr = await call.StandardError.ReadToEndAsync();
        
        if (!string.IsNullOrEmpty(stderr))
        {
            Console.WriteLine($"Server stderr: {stderr}");
        }
        
        if (string.IsNullOrWhiteSpace(json))
        {
            Console.WriteLine("Error: No response from review server");
            Environment.Exit(1);
        }

        Console.WriteLine($"Received review response: {json.Substring(0, Math.Min(100, json.Length))}...");

        var review = System.Text.Json.JsonSerializer.Deserialize<ReviewResponse>(json) 
            ?? new ReviewResponse("Server returned invalid response", new());

        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrEmpty(githubToken))
        {
            Console.WriteLine("Error: GITHUB_TOKEN environment variable not set");
            Environment.Exit(1);
        }

        var gh = new GitHubClient(new ProductHeaderValue("ai-review"))
        {
            Credentials = new Credentials(githubToken)
        };

        var body = BuildMarkdown(review);
        Console.WriteLine($"Posting review to PR #{prNumber} on {owner}/{repo}");
        
        await gh.PullRequest.Review.Create(owner, repo, prNumber, new PullRequestReviewCreate
        {
            Body = body,
            Event = PullRequestReviewEvent.Comment
        });

        Console.WriteLine("Review posted successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        Environment.Exit(1);
    }
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
