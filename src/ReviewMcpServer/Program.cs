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
        
        // Clean up markdown code blocks
        text = text.Trim();
        if (text.StartsWith("```json"))
            text = text[7..];
        else if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline > 0)
                text = text[(firstNewline + 1)..];
        }
        if (text.EndsWith("```"))
            text = text[..^3];
        text = text.Trim();
        
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
            
            return JsonSerializer.Deserialize<ReviewResponse>(text, options) ?? new ReviewResponse(
                Summary: "AI returned empty response",
                Findings: new()
            );
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"❌ JSON Parse Error: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse AI response: {ex.Message}", ex);
        }
    }

    private sealed record Request(string Method, Params? Params);
    private sealed record Params(string Patch);
}

static class PromptLibrary
{
    public static string BuildForPatch(string patch) => $$"""
    You are a senior staff engineer performing a strict code review.
    
    CRITICAL: Return ONLY valid JSON. No markdown, no code blocks, just raw JSON.
    
    JSON Schema (use exact field names and types):
    {
      "summary": "string - brief review summary",
      "findings": [
        {
          "file": "string - filename from diff",
          "line": number - integer line number,
          "severity": "Info" | "Low" | "Medium" | "High",
          "title": "string - short issue title",
          "explanation": "string - detailed explanation",
          "suggested_fix": "string - how to fix it"
        }
      ]
    }
    
    EXAMPLE VALID OUTPUT (copy this format exactly):
    {
      "summary": "Found 2 security issues and 1 code smell.",
      "findings": [
        {
          "file": "Program.cs",
          "line": 25,
          "severity": "High",
          "title": "SQL Injection vulnerability",
          "explanation": "String concatenation in SQL query allows injection attacks.",
          "suggested_fix": "Use parameterized queries or ORM."
        },
        {
          "file": "Program.cs",
          "line": 32,
          "severity": "Medium",
          "title": "Magic number",
          "explanation": "Hardcoded value reduces maintainability.",
          "suggested_fix": "Extract to named constant."
        }
      ]
    }
    
    Guidelines:
    - Severity must be EXACTLY: "Info", "Low", "Medium", or "High" (case-sensitive)
    - Focus on: security, correctness, performance, resource leaks, concurrency
    - Use "High" for security issues, data loss, breaking changes
    - Use "Medium" for performance issues, code smells
    - Use "Low" for style issues, minor improvements
    - Use "Info" for suggestions
    - If line cannot be determined, use line=1
    - Return empty findings array if no issues found
    
    Input is a unified git diff between BASE and HEAD:
    ---BEGIN DIFF---
    {{patch}}
    ---END DIFF---
    
    Return ONLY the JSON object, nothing else:
    """;
}

static class AiClient
{
    public static async Task<string> CallAsync(string prompt)
    {
        var provider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "groq";
        
        return provider.ToLower() switch
        {
            "openai" => await CallOpenAiAsync(prompt),
            "anthropic" => await CallAnthropicAsync(prompt),
            "groq" => await CallGroqAsync(prompt),
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
        
        Console.Error.WriteLine($"🤖 Using {model}");

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

    private static async Task<string> CallGroqAsync(string prompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? throw new InvalidOperationException("GROQ_API_KEY not set");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        var model = Environment.GetEnvironmentVariable("GROQ_MODEL") ?? "llama-3.3-70b-versatile";
        
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
        
        Console.Error.WriteLine($"🤖 Using {model}");

        var res = await http.PostAsync(
            "https://api.groq.com/openai/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        );
        
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"Groq API Error: {err}");
            throw new InvalidOperationException($"Groq API error ({(int)res.StatusCode}): {err}");
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
        
        Console.Error.WriteLine($"🤖 Using {model}");

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
