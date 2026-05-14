using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDisasm.Udis86;
using Hvlibdotnet;
using System.IO;
using System.Reflection;

namespace IUMDebugger
{

    [Flags]
    public enum LoadLibraryFlags : uint
    {
        DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
        LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
        LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
        LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
        LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
        LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
    }

    public class Utils
    {
        public static string HexDump(List<byte> bytes, int bytesPerLine = 16) => HexDump(bytes.ToArray(), bytesPerLine);

        public static string HexDump(byte[] bytes, int bytesPerLine = 16)
        {
            Ui.HexDump(bytes, 0, bytesPerLine);
            return string.Empty;
        }
    }

    public static class Ui
    {
        private static readonly object _lock = new object();
        public static bool UseColor { get; private set; } = true;

        public static void Init()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            UseColor = !Console.IsOutputRedirected;
        }

        private static int SafeWidth()
        {
            try { return Console.WindowWidth; } catch { return 80; }
        }

        public static void Banner(string title, string subtitle)
        {
            const int width = 54;
            string Pad(string s) => " " + s + new string(' ', Math.Max(0, width - 2 - s.Length)) + " ";
            WithColor(ConsoleColor.Cyan, () =>
            {
                Console.WriteLine();
                Console.WriteLine("  ╭" + new string('─', width) + "╮");
                Console.WriteLine("  │" + Pad(title) + "│");
                Console.WriteLine("  │" + Pad(subtitle) + "│");
                Console.WriteLine("  ╰" + new string('─', width) + "╯");
            });
        }

        public static void Section(string title)
        {
            Console.WriteLine();
            WithColor(ConsoleColor.Cyan, () =>
            {
                string head = "── " + title + " ";
                Console.WriteLine(head + new string('─', Math.Max(2, 60 - head.Length)));
            });
        }

        public static void Info(string msg) => Tagged("[*]", ConsoleColor.Cyan, msg);
        public static void Ok(string msg) => Tagged("[+]", ConsoleColor.Green, msg);
        public static void Warn(string msg) => Tagged("[!]", ConsoleColor.Yellow, msg);
        public static void Err(string msg) => Tagged("[x]", ConsoleColor.Red, msg);
        public static void Step(string msg) => Tagged("[>]", ConsoleColor.Magenta, msg);

        public static void Kv(string key, string value)
        {
            lock (_lock)
            {
                Console.Write("      ");
                WithColor(ConsoleColor.DarkGray, () => Console.Write(key.PadRight(30)));
                Console.Write(" ");
                WithColor(ConsoleColor.White, () => Console.WriteLine(value));
            }
        }

        public static void KvHex(string key, ulong value) => Kv(key, "0x" + value.ToString("x"));
        public static void KvHex(string key, long value) => KvHex(key, (ulong)value);
        public static void KvHex(string key, IntPtr value) => KvHex(key, (ulong)(long)value);
        public static void KvHex(string key, UIntPtr value) => KvHex(key, value.ToUInt64());
        public static void KvHex(string key, int value) => KvHex(key, (ulong)(uint)value);

        public static void Result(string label, string value)
        {
            const int width = 62;
            WithColor(ConsoleColor.Green, () =>
            {
                Console.WriteLine();
                Console.WriteLine("  ╭" + new string('─', width) + "╮");
                string l1 = "  " + label;
                Console.WriteLine("  │" + l1.PadRight(width) + "│");
                Console.WriteLine("  │" + new string(' ', width) + "│");
                string l2 = "    " + value;
                Console.WriteLine("  │" + l2.PadRight(width) + "│");
                Console.WriteLine("  ╰" + new string('─', width) + "╯");
                Console.WriteLine();
            });
        }

        public static void HexDump(byte[] bytes, ulong baseAddr = 0, int bytesPerLine = 16)
        {
            if (bytes == null || bytes.Length == 0) return;
            lock (_lock)
            {
                for (int i = 0; i < bytes.Length; i += bytesPerLine)
                {
                    int line = Math.Min(bytesPerLine, bytes.Length - i);
                    Console.Write("      ");
                    WithColor(ConsoleColor.DarkGray, () => Console.Write((baseAddr + (ulong)i).ToString("x8")));
                    Console.Write("  ");
                    var hex = new StringBuilder();
                    for (int j = 0; j < bytesPerLine; j++)
                    {
                        if (j == 8) hex.Append(' ');
                        if (j < line) hex.Append(bytes[i + j].ToString("x2") + " ");
                        else hex.Append("   ");
                    }
                    Console.Write(hex.ToString());
                    Console.Write(' ');
                    WithColor(ConsoleColor.DarkGray, () =>
                    {
                        var ascii = new StringBuilder();
                        for (int j = 0; j < line; j++)
                        {
                            byte b = bytes[i + j];
                            ascii.Append(b >= 32 && b < 127 ? (char)b : '·');
                        }
                        Console.WriteLine(ascii.ToString());
                    });
                }
            }
        }

        private static int _spinnerTick;
        private static readonly char[] _spinFrames = { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };

        public static void Progress(string label, ulong pos, ulong scanned)
        {
            lock (_lock)
            {
                char frame = _spinFrames[_spinnerTick++ % _spinFrames.Length];
                string mb = scanned >= (1UL << 30)
                    ? $"{scanned / (double)(1UL << 30):F2} GiB"
                    : $"{scanned / (double)(1UL << 20):F1} MiB";
                string line = $"  {frame} {label}  0x{pos:x}   ({mb} scanned)";
                int pad = Math.Max(0, SafeWidth() - 1 - line.Length);
                WithColor(ConsoleColor.Cyan, () => Console.Write("\r" + line + new string(' ', pad)));
            }
        }

        public static void ProgressDone()
        {
            lock (_lock)
            {
                Console.Write("\r" + new string(' ', Math.Max(0, SafeWidth() - 1)) + "\r");
            }
        }

        private static void Tagged(string tag, ConsoleColor c, string msg)
        {
            lock (_lock)
            {
                Console.Write("  ");
                WithColor(c, () => Console.Write(tag));
                Console.WriteLine(" " + msg);
            }
        }

        public static void WithColor(ConsoleColor c, Action a)
        {
            if (!UseColor) { a(); return; }
            var prev = Console.ForegroundColor;
            try { Console.ForegroundColor = c; a(); }
            finally { Console.ForegroundColor = prev; }
        }
    }


    class Program
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);


        [DllImport("kernel32.dll", SetLastError = true)]
        private extern static IntPtr GetProcAddress(IntPtr hModule, string lpProcName);


        private static IEnumerable<Instruction> WalkInstructions(Disassembler disasm, int limit)
        {
            while (true)
            {
                var insn = disasm.NextInstruction();
                if (insn == null || insn.Error || insn.Offset >= (ulong)(limit - 1))
                    yield break;
                yield return insn;
            }
        }

        private static bool IsJump(Instruction insn) =>
            insn.Operands.Any() && insn.Operands[0].Opcode == ud_operand_code.OP_J;

        private static ulong ComputeLeaTarget(Instruction insn, IntPtr funcRva) =>
            ((ulong)funcRva + insn.Offset + (ulong)insn.Length +
             (ulong)insn.Operands.Skip(1).FirstOrDefault().Value) & 0xffffffff;

        private static int JumpTargetOffset(Instruction insn) =>
            (int)((insn.Offset + (ulong)insn.Length + (ulong)insn.Operands[0].Value) & 0xffffffff);

        private static ulong JumpTargetRva(Instruction insn, IntPtr funcRva) =>
            ((ulong)funcRva + insn.Offset + (ulong)insn.Length + (ulong)insn.Operands[0].Value) & 0xffffffff;

        private static bool NextJumpsAllTargetByte(
            IEnumerator<Instruction> walker, byte[] dumpBytes, int totalLength,
            byte expectedByte, int requiredCount)
        {
            int matches = 0;
            while (walker.MoveNext())
            {
                var insn = walker.Current;
                if (!IsJump(insn)) continue;

                int target = JumpTargetOffset(insn);
                if (target <= 0 || target >= totalLength || dumpBytes[target] != expectedByte)
                    return false;

                if (++matches >= requiredCount) return true;
            }
            return false;
        }

        private static bool RdxPointsToSymbol(ulong rdxValue, byte[] expected)
        {
            var ptr = new IntPtr((long)rdxValue + (long)_secureKernelBase);
            var buffer = new byte[expected.Length];
            Marshal.Copy(ptr, buffer, 0, expected.Length);
            for (int i = 0; i < expected.Length; i++)
                if (buffer[i] != expected[i]) return false;
            return true;
        }

        private static bool TryLocatePatchSite(
            Disassembler disasm, byte[] dumpBytes, int totalLength,
            IntPtr funcRva, out IntPtr patchOffset, out int patchLength)
        {
            const byte JmpShortOpcode = 0xeb;
            patchOffset = IntPtr.Zero;
            patchLength = 0;

            foreach (var insn in WalkInstructions(disasm, totalLength))
            {
                string opString = insn.ToString();

                if (!IsJump(insn))
                {
                    if (opString.Contains("call")) return false;
                    continue;
                }
                if (!opString.Contains("jmp")) continue;

                int fetchOffset = JumpTargetOffset(insn);
                if (fetchOffset <= 0 || fetchOffset >= totalLength) continue;

                bool errorCodeFound = false;
                for (int i = 0; i < 0x20; i++)
                {
                    int p = fetchOffset + i;
                    if (p >= totalLength) break;

                    if (!errorCodeFound && p + 3 < totalLength &&
                        dumpBytes[p]     == 0x22 && dumpBytes[p + 1] == 0x00 &&
                        dumpBytes[p + 2] == 0x00 && dumpBytes[p + 3] == 0xc0)
                    {
                        errorCodeFound = true;
                    }

                    if (errorCodeFound && dumpBytes[p] == JmpShortOpcode)
                    {
                        patchOffset = new IntPtr((long)funcRva + fetchOffset);
                        patchLength = i + 2;
                        return true;
                    }
                }

                if (!errorCodeFound) return false;
            }
            return false;
        }

        static Tuple<IntPtr, int, IntPtr, byte[]> GetIumInvokeSecureServiceReturn(IntPtr iumInvokeSecureServiceRva)
        {
            const string TargetSymbol = "SkpsIsProcessDebuggingEnabled";
            const int ReadLength = 0x8000;

            byte[] targetSymbolBytes = Encoding.ASCII.GetBytes(TargetSymbol);
            IntPtr iumInvokeSecureServicePtr = new IntPtr((long)iumInvokeSecureServiceRva + (long)_secureKernelBase);

            byte[] dumpBytes = new byte[ReadLength];
            Marshal.Copy(iumInvokeSecureServicePtr, dumpBytes, 0, ReadLength);

            var disasm = new Disassembler(dumpBytes, ArchitectureMode.x86_64);

            bool sawLeaRdx = false, sawLeaRcx = false;
            ulong rdxValue = 0;
            IntPtr patchOffset = IntPtr.Zero;
            int patchLength = 0;

            foreach (var insn in WalkInstructions(disasm, ReadLength))
            {
                string opString = insn.ToString();

                if (opString.Contains("ret"))
                {
                    ulong retAddress = (ulong)iumInvokeSecureServiceRva + insn.Offset;
                    int pageStart = (int)insn.Offset - (int)(retAddress & 0xfff);
                    return new Tuple<IntPtr, int, IntPtr, byte[]>(
                        patchOffset, patchLength,
                        new IntPtr((long)retAddress),
                        dumpBytes[pageStart..(int)insn.Offset]);
                }

                if (opString.Contains("lea rdx"))
                {
                    rdxValue = ComputeLeaTarget(insn, iumInvokeSecureServiceRva);
                    sawLeaRdx = true;
                    sawLeaRcx = false;
                    continue;
                }

                if (opString.Contains("lea rcx"))
                {
                    sawLeaRcx = sawLeaRdx;
                    continue;
                }

                if (sawLeaRcx && IsJump(insn) &&
                    RdxPointsToSymbol(rdxValue, targetSymbolBytes) &&
                    TryLocatePatchSite(disasm, dumpBytes, ReadLength,
                        iumInvokeSecureServiceRva, out var foundOffset, out var foundLength))
                {
                    patchOffset = foundOffset;
                    patchLength = foundLength;
                }

                sawLeaRdx = false;
                sawLeaRcx = false;
            }

            throw new InvalidOperationException(
                $"IumInvokeSecureService 'ret' not found within first 0x{ReadLength:X} bytes.");
        }

        // SkCallNormalMode contains:
        //     call IumInvokeSecureService     ; the function we want to resolve
        //     test ..., ...                   ; immediately followed by a test
        //     ...
        //     j* X                            ; two J*'s whose targets each begin
        //     j* Y                            ; with 0xFA (cli)
        // Match that shape and return the call target as IumInvokeSecureService.
        static IntPtr GetIumInvokeSecureService(IntPtr skCallNormalModeRva)
        {
            const int ReadLength = 0x800;
            const byte CliOpcode = 0xfa;
            const int RequiredCliJumps = 2;

            IntPtr skCallNormalModePtr = new IntPtr((long)skCallNormalModeRva + (long)_secureKernelBase);
            byte[] dumpBytes = new byte[ReadLength];
            Marshal.Copy(skCallNormalModePtr, dumpBytes, 0, ReadLength);

            var disasm = new Disassembler(dumpBytes, ArchitectureMode.x86_64);
            using var walker = WalkInstructions(disasm, ReadLength).GetEnumerator();

            while (walker.MoveNext())
            {
                var callInsn = walker.Current;
                if (!IsJump(callInsn)) continue;

                ulong candidateRva = JumpTargetRva(callInsn, skCallNormalModeRva);

                if (!walker.MoveNext()) break;
                if (!walker.Current.ToString().StartsWith("test ")) continue;

                if (NextJumpsAllTargetByte(walker, dumpBytes, ReadLength, CliOpcode, RequiredCliJumps))
                    return new IntPtr((long)candidateRva);
            }

            return IntPtr.Zero;
        }
        static IntPtr GetSkCallNormalMode(IntPtr skAllocateNormalModePoolPtr)
        {
            IntPtr skAllocateNormalModePoolBase = new IntPtr((long)skAllocateNormalModePoolPtr - (long)_secureKernelBase);
            int readLength = 0x100;

            byte[] dumpBytes = new byte[readLength];
            Marshal.Copy(skAllocateNormalModePoolPtr, dumpBytes, 0, readLength);
            // Utils.HexDump(dumpBytes.ToList());
            Disassembler disasm = new Disassembler(dumpBytes, ArchitectureMode.x86_64);

            Instruction insn = null;
            int callIndex = 0;
            while (true)
            {
                insn = disasm.NextInstruction();
                if (insn != null && !insn.Error && insn.Offset < (ulong)readLength - 1)
                {
                    string opString = insn.ToString();

                    if (opString.Contains("call"))
                    {

                        callIndex++;
                        ulong jumpRelOffset = (ulong)insn.Operands.FirstOrDefault().Value;
                        ulong funcAddress = ((ulong)skAllocateNormalModePoolBase + insn.Offset + (ulong)insn.Length + jumpRelOffset) & 0xffffffff;

                        if (callIndex == 2)
                        {
                            return new IntPtr((long)funcAddress);
                        }

                    }


                }
                else
                {
                    break;
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr _secureKernelBase;

        static void Main(string[] args)
        {
            Ui.Init();
            Ui.Banner("IUM Debugger", "SecureKernel debug-mode patch");

            try
            {
                string exeDir = AppContext.BaseDirectory;
                string secureKernelPath = Path.Combine(exeDir, "securekernel.exe");

                Ui.Section("Stage 1 · Static analysis of securekernel.exe");

                if (!File.Exists(secureKernelPath))
                {
                    Ui.Err("securekernel.exe not found next to the executable.");
                    Ui.Kv("looked in", exeDir);
                    return;
                }

                _secureKernelBase = LoadLibraryEx(secureKernelPath, IntPtr.Zero, LoadLibraryFlags.DONT_RESOLVE_DLL_REFERENCES);
                if (_secureKernelBase == IntPtr.Zero)
                {
                    Ui.Err("LoadLibraryEx failed (error " + Marshal.GetLastWin32Error() + ").");
                    return;
                }
                Ui.Ok("securekernel.exe loaded");
                Ui.KvHex("module base", _secureKernelBase);

                IntPtr skAllocateNormalModePoolPtr = GetProcAddress(_secureKernelBase, "SkAllocateNormalModePool");
                if (skAllocateNormalModePoolPtr == IntPtr.Zero)
                {
                    Ui.Err("Export SkAllocateNormalModePool not found.");
                    return;
                }
                Ui.KvHex("SkAllocateNormalModePool", skAllocateNormalModePoolPtr);

                IntPtr skCallNormalModeRva = GetSkCallNormalMode(skAllocateNormalModePoolPtr);
                if (skCallNormalModeRva == IntPtr.Zero)
                {
                    Ui.Err("Could not resolve SkCallNormalMode.");
                    return;
                }
                Ui.Ok("SkCallNormalMode resolved");
                Ui.KvHex("RVA", skCallNormalModeRva);

                IntPtr iumInvokeSecureServiceRva = GetIumInvokeSecureService(skCallNormalModeRva);
                if (iumInvokeSecureServiceRva == IntPtr.Zero)
                {
                    Ui.Err("Could not resolve IumInvokeSecureService.");
                    return;
                }
                Ui.Ok("IumInvokeSecureService resolved");
                Ui.KvHex("RVA", iumInvokeSecureServiceRva);

                Tuple<IntPtr, int, IntPtr, byte[]> patchResult = GetIumInvokeSecureServiceReturn(iumInvokeSecureServiceRva);
                IntPtr patchOffset = patchResult.Item1;
                int patchLength = patchResult.Item2;
                IntPtr retOffset = patchResult.Item3;
                byte[] checkPage = patchResult.Item4;

                Ui.Ok("ret instruction & patch site located");
                Ui.KvHex("ret address (RVA)", retOffset);
                Ui.KvHex("patch site (RVA)", patchOffset);
                Ui.KvHex("patch length", patchLength);
                Ui.KvHex("page-prefix bytes captured", checkPage.Length);
                Ui.Info("first 16 bytes of capture pattern:");
                Ui.HexDump(checkPage.Take(16).ToArray());

                SecurekernelCtx ctx = new SecurekernelCtx(patchOffset, patchLength, retOffset, checkPage);
                SecurekernelPatch.Patch(ctx);
            }
            catch (Exception e)
            {
                Ui.Err("Unhandled exception:");
                Ui.WithColor(ConsoleColor.Red, () => Console.WriteLine(e));
                throw;
            }
        }
    }

    public class SecurekernelCtx
    {
        public IntPtr PatchOffset;
        public int PatchLength;
        public IntPtr RetOffset;

        public byte[] CheckPage;

        public SecurekernelCtx(IntPtr patchOffset, int patchLength, IntPtr retOffset, byte[] checkPage)
        {
            this.PatchOffset = patchOffset;
            this.PatchLength = patchLength;
            this.RetOffset = retOffset;
            this.CheckPage = checkPage;
        }
    }
}
