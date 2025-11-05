# AI Code Review - GitHub Actions

🤖 Otomatik AI destekli kod incelemesi için GitHub Actions workflow'u.

## ⚠️ Önemli Not: MCP vs Script-Based

**Bu proje şu anda basit bir script-based implementasyondur, gerçek bir MCP (Model Context Protocol) implementasyonu DEĞİLDİR.**

### Mevcut Yapı:
- ✅ GitHub Actions ile otomatik tetikleme
- ✅ Pull Request'lere AI review yorumu
- ✅ Groq (ÜCRETSIZ), OpenAI veya Anthropic desteği
- ❌ Gerçek MCP protokolü yok
- ❌ Claude Desktop entegrasyonu yok

**Gerçek MCP implementasyonu için:** `MCP_IMPLEMENTATION_PLAN.md` dosyasına bakın.
---

## 🚀 Özellikler

- 🆓 **ÜCRETSIZ** - Groq API kullanarak tamamen ücretsiz
- ⚡ **Hızlı** - ~30 saniye içinde review
- 🔒 **Güvenlik Odaklı** - SQL Injection, XSS, RCE gibi açıkları yakalar
- 📊 **Detaylı Raporlama** - Severity bazlı kategorize edilmiş bulgular
- 🎯 **Otomatik** - Her PR'da otomatik çalışır

---

## 📋 Desteklenen AI Providerlar

| Provider | Maliyet | Model | Hız |
|----------|---------|-------|-----|
| **Groq** (Önerilen) | 🆓 ÜCRETSIZ | llama-3.3-70b | ⚡ Çok Hızlı |
| OpenAI | 💰 Ücretli | gpt-4o-mini | 🚀 Hızlı |
| Anthropic | 💰 Ücretli | claude-3.5-sonnet | 🐢 Orta |

---

## 🛠️ Kurulum

### 1️⃣ Groq API Key Alın (Önerilen - ÜCRETSIZ)

1. https://console.groq.com adresine gidin
2. Ücretsiz hesap oluşturun
3. **API Keys** → **Create API Key**
4. Key'i kopyalayın

### 2️⃣ GitHub Secret Ekleyin

Repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

- **Name:** `GROQ_API_KEY`
- **Secret:** Kopyaladığınız API key
- **Add secret**

### 3️⃣ Workflow Dosyasını Ekleyin

`.github/workflows/ai-code-review.yml`:

```yaml
name: AI Code Review

on:
  pull_request:
    types: [opened, synchronize, reopened]

permissions:
  contents: read
  pull-requests: write

jobs:
  ai-review:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore & build tools
        run: |
          dotnet restore ./src/ReviewSchemas/ReviewSchemas.csproj
          dotnet restore ./src/ReviewMcpServer/ReviewMcpServer.csproj
          dotnet restore ./src/ReviewClient/ReviewClient.csproj
          dotnet build -c Release ./src/ReviewSchemas/ReviewSchemas.csproj
          dotnet build -c Release ./src/ReviewMcpServer/ReviewMcpServer.csproj
          dotnet build -c Release ./src/ReviewClient/ReviewClient.csproj

      - name: Run AI review
        env:
          AI_PROVIDER: "groq"
          GROQ_API_KEY: ${{ secrets.GROQ_API_KEY }}
          GROQ_MODEL: "llama-3.3-70b-versatile"
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          REVIEW_MAX_PATCH_BYTES: 350000
          REVIEW_INCLUDE_PATTERNS: "**/*.cs,**/*.ts,**/*.js,**/*.py,**/*.java,**/*.go,**/*.rb,**/*.rs,**/*.cpp,**/*.h"
        run: |
          bash scripts/run-review.sh
```

---

## 🎯 Kullanım

1. **PR Oluştur** - Yeni bir Pull Request açın
2. **Bekle** - ~30 saniye
3. **Review Görüntüle** - PR'da AI review yorumunu görün

### Örnek AI Review:

```markdown
### 🤖 AI Code Review Summary

Found 3 critical security issues.

**Issue Summary:**
- 🔴 High: 3
- 🟡 Medium: 2
- 🔵 Low: 1

| Severity | File:Line | Title | Suggestion |
|---|---|---|---|
| 🔴 High | Program.cs:24 | SQL Injection vulnerability | Use parameterized queries |
| 🔴 High | Program.cs:32 | Hardcoded password | Use secure credential storage |
| 🔴 High | Program.cs:81 | Command Injection | Validate and sanitize input |
```

---

## 🔧 Yapılandırma

### AI Provider Değiştirme

**Groq'tan OpenAI'ye:**
```yaml
env:
  AI_PROVIDER: "openai"
  OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
  OPENAI_MODEL: "gpt-4o-mini"
```

**Groq'tan Anthropic'e:**
```yaml
env:
  AI_PROVIDER: "anthropic"
  ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
  ANTHROPIC_MODEL: "claude-3-5-sonnet-20240620"
```

### Review Davranışı

**Request Changes (High severity'de):**
```yaml
env:
  REVIEW_BEHAVIOR: "request_changes"
```

**Pipeline Fail (High severity'de):**
```yaml
env:
  REVIEW_FAIL_ON_HIGH_SEVERITY: "true"
```

---

## 📊 Yakalanan Güvenlik Açıkları

- 🔴 **SQL Injection** - String concatenation in queries
- 🔴 **Command Injection** - Unsanitized shell commands
- 🔴 **Path Traversal** - No input validation on file paths
- 🔴 **XSS** - Unescaped user input
- 🔴 **Hardcoded Credentials** - Passwords/secrets in code
- 🔴 **Insecure Deserialization** - BinaryFormatter usage
- 🔴 **XXE** - XML External Entity attacks
- 🔴 **LDAP Injection** - Unsafe LDAP filters
- 🔴 **SSRF** - Server-Side Request Forgery
- 🟡 **Magic Numbers** - Hardcoded values
- 🔵 **Code Smells** - Best practice violations

---

## 🏗️ Proje Yapısı

```
├── .github/workflows/
│   └── ai-code-review.yml    # GitHub Actions workflow
├── scripts/
│   └── run-review.sh          # Review script
├── src/
│   ├── ReviewSchemas/         # Veri modelleri
│   ├── ReviewMcpServer/       # AI API client (stdio server)
│   └── ReviewClient/          # GitHub PR client
└── README.md
```

### Workflow Akışı:

```
1. PR Açıldı/Güncellendi
   ↓
2. GitHub Actions Tetiklendi
   ↓
3. run-review.sh Çalıştı
   ├─ Git diff aldı (patch.diff)
   ├─ ReviewMcpServer başlattı (stdio)
   ├─ ReviewClient çalıştırdı
   │  ├─ Diff'i okudu
   │  ├─ Server'a gönderdi
   │  ├─ AI API çağrısı (Groq)
   │  └─ Response aldı
   └─ PR'a yorum yazdı
```

---

## 💰 Maliyet Karşılaştırması

| Kullanım | Groq | OpenAI | Anthropic |
|----------|------|--------|-----------|
| 100 review/ay | $0 🆓 | ~$6 | ~$300 |
| 1000 review/ay | $0 🆓 | ~$60 | ~$3000 |

---

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing`)
3. Commit edin (`git commit -m 'feat: Add amazing feature'`)
4. Push edin (`git push origin feature/amazing`)
5. Pull Request açın

---

## 📚 Gelecek Planlar

- [ ] **Gerçek MCP Implementasyonu** - `MCP_IMPLEMENTATION_PLAN.md`
- [ ] Claude Desktop entegrasyonu
- [ ] VSCode extension
- [ ] Daha fazla dil desteği
- [ ] Custom rules/patterns
- [ ] CI/CD metrikler dashboard

---

## 📄 Lisans

MIT License - Detaylar için `LICENSE` dosyasına bakın.

---

## 🙏 Teşekkürler

- **Groq** - Ücretsiz ve hızlı AI inference
- **Anthropic** - MCP protokolü
- **OpenAI** - GPT modelleri
- **GitHub** - Actions platform

---

**Made with ❤️ by mennansevim**
