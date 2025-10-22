# MCP (Model Context Protocol) İmplementasyon Planı

## 🎯 Mevcut Durum vs Hedef MCP

### **Şu Anki Yapı (Script-Based):**
```
GitHub Actions Workflow
  ↓
run-review.sh (bash script)
  ↓
ReviewMcpServer (basit stdio JSON-RPC)
  ├─ Groq/OpenAI API çağrısı
  └─ Cevabı döndür
  ↓
ReviewClient
  └─ PR'a yorum yaz
```

**Sorunlar:**
- ❌ Sadece GitHub Actions'da çalışır
- ❌ MCP spec'e uygun değil
- ❌ Claude Desktop entegrasyonu yok
- ❌ Gerçek MCP özellikleri yok (tools, resources, prompts)

---

## 🚀 Gerçek MCP İmplementasyonu

### **1. MCP Server (Standart)**

```csharp
// MCP Protocol Implementation
public class McpServer
{
    // MCP Required Methods
    public async Task<InitializeResult> Initialize(InitializeRequest request)
    {
        return new InitializeResult
        {
            ProtocolVersion = "2024-11-05",
            ServerInfo = new ServerInfo
            {
                Name = "code-review-server",
                Version = "1.0.0"
            },
            Capabilities = new Capabilities
            {
                Tools = new ToolsCapability(),
                Resources = new ResourcesCapability(),
                Prompts = new PromptsCapability()
            }
        };
    }

    // List Available Tools
    public Task<ListToolsResult> ListTools()
    {
        return Task.FromResult(new ListToolsResult
        {
            Tools = new[]
            {
                new Tool
                {
                    Name = "review_code",
                    Description = "Review code for security issues and best practices",
                    InputSchema = new JsonSchema
                    {
                        Type = "object",
                        Properties = new
                        {
                            code = new { type = "string", description = "Code to review" },
                            language = new { type = "string", description = "Programming language" }
                        },
                        Required = new[] { "code" }
                    }
                },
                new Tool
                {
                    Name = "analyze_security",
                    Description = "Deep security analysis of code",
                    InputSchema = new JsonSchema { /* ... */ }
                },
                new Tool
                {
                    Name = "suggest_fixes",
                    Description = "Suggest code fixes for issues",
                    InputSchema = new JsonSchema { /* ... */ }
                }
            }
        });
    }

    // Execute Tool
    public async Task<CallToolResult> CallTool(CallToolRequest request)
    {
        return request.Params.Name switch
        {
            "review_code" => await ReviewCode(request.Params.Arguments),
            "analyze_security" => await AnalyzeSecurity(request.Params.Arguments),
            "suggest_fixes" => await SuggestFixes(request.Params.Arguments),
            _ => throw new MethodNotFoundException(request.Params.Name)
        };
    }

    // List Resources (Git repos, PRs, etc.)
    public Task<ListResourcesResult> ListResources()
    {
        return Task.FromResult(new ListResourcesResult
        {
            Resources = new[]
            {
                new Resource
                {
                    Uri = "git://mennansevim/mcp-ai-code-review",
                    Name = "Current Repository",
                    MimeType = "application/x-git"
                },
                new Resource
                {
                    Uri = "pr://mennansevim/mcp-ai-code-review/11",
                    Name = "Pull Request #11",
                    MimeType = "application/vnd.github+json"
                }
            }
        });
    }

    // Read Resource
    public async Task<ReadResourceResult> ReadResource(ReadResourceRequest request)
    {
        var uri = request.Params.Uri;
        
        if (uri.StartsWith("git://"))
        {
            // Git repo içeriğini döndür
            var diff = await GetGitDiff();
            return new ReadResourceResult
            {
                Contents = new[]
                {
                    new ResourceContent
                    {
                        Uri = uri,
                        MimeType = "text/x-diff",
                        Text = diff
                    }
                }
            };
        }
        else if (uri.StartsWith("pr://"))
        {
            // PR bilgilerini döndür
            var prData = await GetPullRequest(uri);
            return new ReadResourceResult { /* ... */ };
        }
        
        throw new ResourceNotFoundException(uri);
    }

    // List Prompts
    public Task<ListPromptsResult> ListPrompts()
    {
        return Task.FromResult(new ListPromptsResult
        {
            Prompts = new[]
            {
                new Prompt
                {
                    Name = "security_review",
                    Description = "Comprehensive security code review",
                    Arguments = new[]
                    {
                        new PromptArgument
                        {
                            Name = "severity",
                            Description = "Minimum severity level",
                            Required = false
                        }
                    }
                }
            }
        });
    }

    // Get Prompt
    public Task<GetPromptResult> GetPrompt(GetPromptRequest request)
    {
        if (request.Params.Name == "security_review")
        {
            return Task.FromResult(new GetPromptResult
            {
                Messages = new[]
                {
                    new PromptMessage
                    {
                        Role = "user",
                        Content = new TextContent
                        {
                            Type = "text",
                            Text = @"
You are a senior security engineer reviewing code.
Focus on:
- SQL Injection
- XSS vulnerabilities
- Authentication issues
- Cryptographic weaknesses
- Input validation
                            ".Trim()
                        }
                    }
                }
            });
        }
        
        throw new PromptNotFoundException(request.Params.Name);
    }
}
```

### **2. Claude Desktop Konfigürasyonu**

`~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "code-review": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/mcp-ai-code-review/src/ReviewMcpServer"
      ],
      "env": {
        "GROQ_API_KEY": "your-groq-api-key"
      }
    }
  }
}
```

### **3. Kullanım (Claude Desktop'ta)**

```
You: @code-review review the changes in PR #11

Claude: I'll use the code-review MCP server to analyze PR #11.
        [Calls: review_code tool with PR diff]
        
        Found 10 critical security issues:
        1. 🔴 SQL Injection in GetUserData()...
        2. 🔴 Insecure Deserialization in DeserializeData()...
        ...
```

---

## 🔄 Dönüşüm Adımları

### **Faz 1: MCP SDK Entegrasyonu**
```bash
dotnet add package ModelContextProtocol.SDK
```

### **Faz 2: Server Refactor**
- [ ] MCP protokol implementasyonu
- [ ] Tools tanımla (review_code, analyze_security)
- [ ] Resources tanımla (git://, pr://)
- [ ] Prompts tanımla (security_review)

### **Faz 3: Claude Desktop Entegrasyonu**
- [ ] MCP server'ı Claude Desktop'a ekle
- [ ] Test et
- [ ] Dokümantasyon yaz

### **Faz 4: GitHub Actions Entegrasyonu (Opsiyonel)**
- [ ] MCP client olarak GitHub Action
- [ ] MCP server'a bağlan
- [ ] Tool'ları kullan

---

## 📊 Karşılaştırma

| Özellik | Şu Anki (Script) | Gerçek MCP |
|---------|------------------|------------|
| Protokol | Custom JSON | MCP Standart |
| Entegrasyon | Sadece GitHub Actions | Claude Desktop, IDE'ler |
| Tools | Yok | ✅ Evet |
| Resources | Yok | ✅ Git, PR'lar |
| Prompts | Hardcoded | ✅ Dinamik |
| Yeniden Kullanılabilirlik | Düşük | Yüksek |
| Standart | Yok | MCP Spec |

---

## 🎯 Öneri

**Kısa Vadeli:** Mevcut script-based yaklaşımı kullan (çalışıyor)

**Uzun Vadeli:** Gerçek MCP implementasyonu yap:
1. Daha geniş kullanım (Claude Desktop, Cursor, vs.)
2. Standart protokol
3. Daha fazla özellik (tools, resources, prompts)
4. Daha iyi entegrasyon

---

## 📚 Kaynaklar

- MCP Spec: https://spec.modelcontextprotocol.io/
- MCP TypeScript SDK: https://github.com/modelcontextprotocol/typescript-sdk
- MCP Python SDK: https://github.com/modelcontextprotocol/python-sdk
- MCP .NET SDK: (Topluluk tarafından geliştirilmeli)

---

**Not:** Şu anki implementasyon basit ama etkili. Gerçek MCP için daha fazla geliştirme gerekiyor.

