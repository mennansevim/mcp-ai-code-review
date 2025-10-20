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
        var oneShot = Environment.GetEnvironmentVariable("REVIEW_SERVER_ONE_SHOT") == "1";
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            try
            {
                var req = JsonSerializer.Deserialize<Request>(line, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req?.Method == "review_diff")
                {
                    var result = await ReviewAsync(req.Params!.Patch);
                    var json = JsonSerializer.Serialize(result);
                    await writer.WriteLineAsync(json);
                    if (oneShot)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                var error = new ReviewResponse(
                    Summary: $"Review failed: {ex.Message}",
                    Findings: new()
                );
                await writer.WriteLineAsync(JsonSerializer.Serialize(error));
                if (oneShot)
                {
                    break;
                }
            }
        }
    }

    private async Task<ReviewResponse> ReviewAsync(string patch)
    {
        var prompt = PromptLibrary.BuildForPatch(patch);
        var text = await Claude.CallAsync(prompt);
        
        // Claude bazen JSON'u markdown code block içinde dönebilir, temizle
        text = text.Trim();
        if (text.StartsWith("```json"))
        {
            text = text[7..];
            if (text.EndsWith("```"))
                text = text[..^3];
            text = text.Trim();
        }
        else if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline > 0)
                text = text[(firstNewline + 1)..];
            if (text.EndsWith("```"))
                text = text[..^3];
            text = text.Trim();
        }
        
        return JsonSerializer.Deserialize<ReviewResponse>(text) ?? new ReviewResponse(
            Summary: "Model returned no valid JSON. Check logs.",
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
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        var payload = new
        {
            model = "claude-3-5-sonnet-20241022",
            max_tokens = 4096,
            temperature = 0,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[] { new { type = "text", text = prompt } }
                }
            }
        };

        var res = await http.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        );
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Anthropic API error ({(int)res.StatusCode}): {err}");
        }

        using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
        return content ?? "{\"summary\":\"No content\",\"findings\":[]}";
    }
}
