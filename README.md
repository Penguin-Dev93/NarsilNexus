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

NarsilNexus is planned as a C#/.NET 8 WPF application for repeatable network testing. The goal is to give admins a clean workspace for selecting a saved target or typing an ad hoc domain, IP, hostname, or URL, then running structured diagnostics and producing ticket-ready reports.

## Planned Capabilities

- Saved targets for domains, IPs, and URLs.
- Configuration import/export so teams can share known targets and defaults.
- Native Test-NetConnection-style diagnostics.
- Ping, DNS lookup with custom resolver support, TCP port checks, HTTP/S checks, traceroute, RDAP lookup, and path MTU testing.
- Bundled iperf3 client for basic TCP throughput testing.
- Internal speed-test result reader from configured HTTP/S JSON endpoints.
- Local JSON history.
- Ticket-ready PDF reports.

## Status

This repository is in early implementation. The initial .NET 8 WPF solution scaffold is in place, including core models, app data path handling, bundled-tool packaging structure, and a starter diagnostic workspace UI.

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

## License

NarsilNexus is licensed under the [MIT License](LICENSE).
