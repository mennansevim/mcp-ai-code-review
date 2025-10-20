# Claude MCP Code Review Starter

Minimal, pipeline-friendly AI code review stage:

- Runs on **GitHub Actions**
- Tiny **C# stdio server** calls **Claude**
- Returns **strict JSON findings** and posts **PR review**

## Quick start

1) Add secret: `ANTHROPIC_API_KEY`
2) Commit & open a Pull Request

## Local build

```bash
dotnet build ./src/ReviewSchemas
dotnet build ./src/ReviewMcpServer
dotnet build ./src/ReviewClient
```

## Generic usage in other CI

```bash
git diff BASE...HEAD --unified=3 > patch.diff
./src/ReviewMcpServer/bin/Release/net8.0/ReviewMcpServer &
./src/ReviewClient/bin/Release/net8.0/ReviewClient --patch-file patch.diff --owner <owner> --repo <repo> --pr <pr>
```

## Security notes
- Pin versions, keep secrets minimal.
- Review server code before usage.
