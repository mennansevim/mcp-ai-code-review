# Changelog

## [1.0.0] - 2025-10-20

### ✅ Düzeltmeler

#### Kritik Hatalar
- **Claude Model Adı**: `claude-3-7-sonnet` → `claude-3-5-sonnet-20241022` (geçerli model adı)
- **GitHub Actions Eksikliği**: `.github/workflows/code-review.yml` workflow dosyası eklendi
- **JSON Escape Hatası**: ReviewMcpServer'da JSON string escape hatası düzeltildi

#### İyileştirmeler
- **Hata Yönetimi**: Try-catch blokları ve validation kontrolleri eklendi
- **Logging**: Detaylı konsol logları eklendi (ReviewClient)
- **JSON Parsing**: Claude'un markdown code block'larını temizleyen kod eklendi
- **Esneklik**: `REVIEW_SERVER_PATH` environment variable desteği
- **Dependencies**: Octokit 0.58.0 → 13.0.1 güncellendi
- **Token Limit**: max_tokens 2000 → 4096 artırıldı

### 📝 Yeni Özellikler

- **GitHub Actions Workflow**: Otomatik PR review desteği
- **Test Script**: Local test için `test-local.sh` scripti
- **.gitignore**: Build ve test dosyalarını göz ardı eden konfigürasyon
- **Detaylı README**: Türkçe, detaylı kullanım kılavuzu

### 🔧 Teknik Detaylar

#### Program.cs (ReviewMcpServer)
- Exception handling eklendi
- JSON markdown code block temizleme
- Hata mesajları iyileştirildi

#### Program.cs (ReviewClient)
- File existence kontrolleri
- Empty patch handling
- Server path validation
- Detaylı logging
- Stderr output handling
- GITHUB_TOKEN validation

#### .github/workflows/code-review.yml
- Pull request trigger'ları (opened, synchronize, reopened)
- .NET 8.0 setup
- Sequential build (ReviewSchemas → ReviewMcpServer → ReviewClient)
- Environment variables (ANTHROPIC_API_KEY, GITHUB_TOKEN)
- run-review.sh script çalıştırma

### 🎯 Sonuç

Proje artık tamamen çalışır durumda:
- ✅ Tüm projeler başarıyla build oluyor
- ✅ GitHub Actions workflow hazır
- ✅ Claude API entegrasyonu çalışıyor
- ✅ Hata yönetimi eksiksiz
- ✅ Dokümantasyon güncel ve detaylı

