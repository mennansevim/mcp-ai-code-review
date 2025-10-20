# Claude MCP Code Review Starter

Minimal, pipeline-friendly AI code review stage:

- Runs on **GitHub Actions**
- Tiny **C# stdio server** calls **Claude 3.5 Sonnet**
- Returns **strict JSON findings** and posts **PR review**

## Quick start

1) Repository ayarlarından **Secrets** bölümüne `ANTHROPIC_API_KEY` ekleyin
2) Bir Pull Request açın ve AI otomatik olarak kod incelemesi yapacak

## Gereksinimler

- .NET 8.0 SDK
- Anthropic API anahtarı
- GitHub token (Actions'da otomatik sağlanır)

## Local build

```bash
# Projeleri sırayla build et
dotnet build ./src/ReviewSchemas --configuration Release
dotnet build ./src/ReviewMcpServer --configuration Release
dotnet build ./src/ReviewClient --configuration Release
```

## Local test

```bash
# Test için örnek bir diff oluştur
git diff HEAD~1 HEAD --unified=3 > test.diff

# Server'ı arka planda başlat
export ANTHROPIC_API_KEY="your-key-here"
./src/ReviewMcpServer/bin/Release/net8.0/ReviewMcpServer &
SERVER_PID=$!

# Client'ı çalıştır (local test için GitHub token gerekmez)
export GITHUB_TOKEN="your-github-token"
./src/ReviewClient/bin/Release/net8.0/ReviewClient \
  --patch-file test.diff \
  --owner your-username \
  --repo your-repo \
  --pr 1

# Server'ı durdur
kill $SERVER_PID
```

## GitHub Actions kullanımı

Proje `.github/workflows/code-review.yml` dosyasını içerir. Bu workflow:

1. Her PR açıldığında, güncellendiğinde veya yeniden açıldığında tetiklenir
2. Kodu checkout eder ve .NET 8.0 kurar
3. Projeleri build eder
4. Kod değişikliklerini analiz eder
5. Claude API ile kod incelemesi yapar
6. Sonuçları PR'a yorum olarak ekler

## Diğer CI/CD sistemlerinde kullanım

```bash
# Değişiklikleri diff dosyasına çıkar
git diff BASE...HEAD --unified=3 > patch.diff

# Server'ı başlat
export ANTHROPIC_API_KEY="your-key"
./src/ReviewMcpServer/bin/Release/net8.0/ReviewMcpServer &
SERVER_PID=$!

# Review'ı çalıştır
export GITHUB_TOKEN="your-token"
./src/ReviewClient/bin/Release/net8.0/ReviewClient \
  --patch-file patch.diff \
  --owner <owner> \
  --repo <repo> \
  --pr <pr-number>

# Server'ı durdur
kill $SERVER_PID
```

## Yapılandırma

### Environment Variables

- `ANTHROPIC_API_KEY`: Claude API anahtarı (zorunlu)
- `GITHUB_TOKEN`: GitHub API erişimi için (zorunlu)
- `REVIEW_SERVER_PATH`: Server binary'sinin özel path'i (opsiyonel)

## Özellikler

- ✅ Claude 3.5 Sonnet ile kod analizi
- ✅ Güvenlik, performans, doğruluk kontrolü
- ✅ Otomatik PR yorumu
- ✅ JSON formatında yapılandırılmış çıktı
- ✅ Severity seviyeleri (Info, Low, Medium, High)
- ✅ Önerilen düzeltmeler

## Güvenlik notları

- Dependency versiyonlarını sabitle
- Secret'ları minimal tut
- Server kodunu kullanmadan önce incele
- API anahtarlarını asla commit'leme
