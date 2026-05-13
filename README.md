# The IUM Debugger

The IUM Debugger is a small Windows utility, run on the Hyper-V host, that persuades a guest's Secure Kernel to permit debugging of its own trustlets.

## Background

Virtualization-Based Security splits Windows across two **Virtual Trust Levels** on the same hypervisor:

- **VTL 0** — the NT kernel and every normal user-mode process.
- **VTL 1** — the **Secure Kernel** (`securekernel.exe`) and the **Isolated User Mode** trustlets (`LsaIso.exe`, `vmsp.exe`, the biometric trustlet, etc.). VTL 0 cannot read or write VTL 1 memory, even with kernel privilege.

The Secure Kernel decides, per process, whether debugging is allowed. On a default consumer system it always says no, so every attempt to attach a debugger to a trustlet is rejected.

The check is enforced from code inside `securekernel.exe`. The file on disk is signed — patching it there fails signature verification at boot and the guest bluescreens. The only viable target is the live Secure Kernel image as it sits in the running guest's physical memory.

## How it works

A Hyper-V parent partition can read and write the physical memory of any guest it hosts, including pages assigned to VTL 1. LiveCloudKd ships these primitives behind a signed driver (`hvmm.sys`).

The IUM Debugger runs on the host and uses `hvmm.sys` to:

1. Locate the Secure Kernel code that enforces the debug check inside the running guest.
2. Overwrite that check in the guest's live physical memory so it always allows debugging.

The on-disk binary is never modified and the patch disappears on reboot. After it has run, a WinDbg launched **inside the guest** attaches to trustlets like `LsaIso.exe` as a regular user-mode debugger.

## Installation

1. Use the LiveCloudKd build bundled in this repository.
2. Install WinDbg, then copy every file from the LiveCloudKd folder into `C:\Program Files (x86)\Windows Kits\10\Debuggers\x64`.
3. Install [the x64 VC++ runtime](https://aka.ms/vs/17/release/vc_redist.x64.exe).
4. Register the LiveCloudKd EXDI server: `regsvr32 "C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\ExdiHvSrv.dll"`.
5. On the host, expose virtualisation extensions to the guest: `Set-VMProcessor -VMName <name> -ExposeVirtualizationExtensions $true`.
6. From inside the guest, copy `C:\Windows\System32\securekernel.exe` to **both** `C:\Program Files (x86)\Windows Kits\10\Debuggers\x64` and the directory holding the `IUM-Debugger` executable. The on-disk copy is read for static analysis only; the live guest's binary is never modified.
7. Boot the guest, then run `IUM-Debugger` on the host. Once it reports success, a WinDbg launched **inside the guest** can attach to any trustlet.

![Debugging the `LsaIso.exe` process](images/windbg-debug-success-text.png)

## Acknowledgements

- [LiveCloudKd](https://github.com/comaeio/LiveCloudKd) — parent-partition memory access via `hvmm.sys`.
- [SharpDisasm](https://github.com/spazzarama/SharpDisasm) — x86_64 disassembly used internally.
