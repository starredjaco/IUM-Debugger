# The IUM Debugger

I created this tool to be able to debug processes running in the Isolated User Mode of VTL 1.

## Instructions

1. Download the latest version of LiveCloudKd (Included in the repository).
2. Install WinDbg, then copy all files from LiveCloudKd to `C:\Program Files (x86)\Windows Kits\10\Debuggers\x64`
3. Install [the VC runtime library x64 version](https://aka.ms/vs/17/release/vc_redist.x64.exe)
4. Register the ExdiHvSrv.dll library from LiveCloudKd `regsvr32 "C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\ExdiHvSrv.dll"`
5. Execute `Set-VMProcessor -VMName YourVMName -ExposeVirtualizationExtensions $true`
6. Copy the file `C:\Windows\System32\securekernel.exe` from the virtual machine to: `C:\Program Files (x86)\Windows Kits\10\Debuggers\x64` and the same directory as the IUM-Debugger executable.
7. After the virtual machine has started, launch the IUM-Debugger. If the base address of securekernel.exe is displayed, the process completed successfully.

![Debugging the `LsaIso.exe` process](images/windbg-debug-success-text.png)