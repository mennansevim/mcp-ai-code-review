using System.Text.Json;
using ReviewSchemas;

// Minimal MCP-like stdio server
var server = new ReviewServer();
await server.RunAsync();

sealed class ReviewServer
{
    public async Task RunAsync()
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin);
        using var writer = new StreamWriter(stdout) { AutoFlush = true };

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            var req = JsonSerializer.Deserialize<Request>(line);
            if (req?.Method == "review_diff")
            {
                var result = await ReviewAsync(req.Params!.Patch);
                var json = JsonSerializer.Serialize(result);
                await writer.WriteLineAsync(json);
            }
        }
    }

    private async Task<ReviewResponse> ReviewAsync(string patch)
    {
        var prompt = PromptLibrary.BuildForPatch(patch);
        var text = await Claude.CallAsync(prompt);
        return JsonSerializer.Deserialize<ReviewResponse>(text) ?? new ReviewResponse(
            Summary: "Model returned no JSON. Check logs.",
            Findings: new()
        );
    }

    private sealed record Request(string Method, Params? Params);
    private sealed record Params(string Patch);
}

static class PromptLibrary
{
    public static string BuildForPatch(string patch) => $$"""
    You are a senior staff engineer performing a strict code review.
    Return ONLY JSON matching this C# schema (use keys exactly):
    {
      "summary": string,
      "findings": [
        { "file": string, "line": number, "severity": "Info|Low|Medium|High",
          "title": string, "explanation": string, "suggested_fix": string }
      ]
    }
    Guidelines:
    - Focus on correctness, security, performance, resource leaks, concurrency, API breaking, style-invariants.
    - If line cannot be determined, set line=1 and explain.
    - Keep suggestions minimal and actionable.

    Input is a unified git diff between BASE and HEAD:
    ---BEGIN DIFF---
    {{patch}}
    ---END DIFF---
    """;
}

static class Claude
{
    public static async Task<string> CallAsync(string prompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY not set");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var payload = new
        {
            model = "claude-3-7-sonnet",
            max_tokens = 2000,
            temperature = 0,
            messages = new object[] { new { role = "user", content = prompt } }
        };

        var res = await http.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        );
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
        return content ?? "{"summary":"No content","findings":[]}";
    }
}
