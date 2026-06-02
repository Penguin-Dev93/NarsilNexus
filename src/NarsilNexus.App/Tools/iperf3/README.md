# Bundled iperf3

NarsilNexus packages iperf3 with the application so users do not need internet access, package servers, or a separate iperf3 installation on target devices.

Before release, place the pinned Windows `iperf3.exe` build in this folder and update:

- `VERSION.txt`
- `LICENSE.txt`
- `THIRD_PARTY_NOTICES.md`

Preferred source policy:

- Build or pin a Windows iperf3 executable from source or a trusted build pipeline.
- Record source repository, version/tag, build method, and checksum.
- Do not download iperf3 at runtime or install time.

