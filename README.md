# NarsilNexus
A Windows desktop network diagnostics toolkit for IT/admin troubleshooting.

[![C#](https://img.shields.io/badge/C%23-.NET_8-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows%20Desktop-0078D4)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![Built with Codex](https://img.shields.io/badge/Built%20with-Codex-111827)](https://openai.com/codex)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/Penguin-Dev93/NarsilNexus?style=social)](https://github.com/Penguin-Dev93/NarsilNexus/stargazers)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20me%20a%20coffee-%E2%98%95-yellow?style=flat&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/penguin.dev93)
[![Follow on X](https://img.shields.io/badge/Follow-%40Penguin__Dev93-1DA1F2?style=flat&logo=twitter&logoColor=white)](https://x.com/Penguin_Dev93)

## Overview

NarsilNexus is a C#/.NET 8 WPF application for repeatable network testing. It gives admins a clean workspace for selecting a saved target or typing an ad hoc domain, IP, hostname, or URL, then running structured diagnostics and producing ticket-ready reports.

## Current Capabilities

- Saved targets for domains, IPs, hostnames, and URLs.
- Configuration import/export so teams can share known targets and defaults.
- Test-NetConnection-style diagnostics.
- Ping, DNS lookup with custom resolver support, TCP port checks, HTTP/S checks, traceroute, RDAP lookup, and path MTU testing.
- Speed tests from configured JSON, HTTP probe, or LibreSpeed backend endpoints.
- Saved speed-test endpoints with duration, ping sample, download, upload, and latency settings.
- Local JSON run history.
- Ticket-ready PDF reports from current results or saved history.
- Modern dark/light WPF interface with workflow tabs.

## Roadmap

- v1.1.0: bundled iperf3 client workflow for basic TCP throughput testing.

## Status

v1.0.0 is the first release build. It is packaged as a portable, self-contained Windows executable.

## Platform

- Windows desktop
- C# / .NET 8
- WPF

## Build

Open `NarsilNexus.sln` in Visual Studio on Windows, or build from the command line:

```powershell
dotnet restore .\NarsilNexus.sln
dotnet build .\NarsilNexus.sln
```

The planned release package is a self-contained Windows build so target devices do not need a preinstalled .NET runtime.

## Release Build

Publish a portable Windows executable:

```powershell
dotnet publish .\src\NarsilNexus.App\NarsilNexus.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

## License

NarsilNexus is licensed under the [MIT License](LICENSE).
