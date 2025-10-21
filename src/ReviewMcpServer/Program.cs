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
        var text = await AiClient.CallAsync(prompt);
        
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
        { "file": string, "line": number, "severity": "Info" | "Low" | "Medium" | "High",
          "title": string, "explanation": string, "suggested_fix": string }
      ]
    }
    Guidelines:
    - Severity levels (use exact case): "Info", "Low", "Medium", "High"
    - Focus on correctness, security, performance, resource leaks, concurrency, API breaking, style-invariants.
    - Use "High" for security issues, data loss, breaking changes
    - Use "Medium" for performance issues, code smells
    - Use "Low" for style issues, minor improvements
    - Use "Info" for suggestions
    - If line cannot be determined, set line=1 and explain.
    - Keep suggestions minimal and actionable.

    Input is a unified git diff between BASE and HEAD:
    ---BEGIN DIFF---
    {{patch}}
    ---END DIFF---
    """;
}

static class AiClient
{
    public static async Task<string> CallAsync(string prompt)
    {
        var provider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "openai";
        
        return provider.ToLower() switch
        {
            "openai" => await CallOpenAiAsync(prompt),
            "anthropic" => await CallAnthropicAsync(prompt),
            _ => throw new InvalidOperationException($"Unsupported AI provider: {provider}")
        };
    }

    private static async Task<string> CallOpenAiAsync(string prompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY not set");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4-turbo-preview";
        
        var payload = new
        {
            model = model,
            temperature = 0,
            messages = new object[]
            {
                new { role = "system", content = "You are a senior staff engineer performing strict code reviews. Return only valid JSON." },
                new { role = "user", content = prompt }
            }
        };
        
        Console.Error.WriteLine($"Using OpenAI model: {model}, prompt size: {prompt.Length} chars");

        var res = await http.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        );
        
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"OpenAI API Error: {err}");
            throw new InvalidOperationException($"OpenAI API error ({(int)res.StatusCode}): {err}");
        }

        using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? "{\"summary\":\"No content\",\"findings\":[]}";
    }

    private static async Task<string> CallAnthropicAsync(string prompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY not set");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        var model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-3-5-sonnet-20240620";
        
        var payload = new
        {
            model = model,
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
        
        Console.Error.WriteLine($"Using Anthropic model: {model}, prompt size: {prompt.Length} chars");

        var res = await http.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        );
        
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"Anthropic API Error: {err}");
            throw new InvalidOperationException($"Anthropic API error ({(int)res.StatusCode}): {err}");
        }

        using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
        return content ?? "{\"summary\":\"No content\",\"findings\":[]}";
    }
}
