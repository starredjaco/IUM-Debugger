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


        public static bool memcmp(byte[] dumpbytes, byte[] matchstrbytes)
        {
            for (int i = 0; i < matchstrbytes.Length; i++)
            {
                if (dumpbytes[i] != matchstrbytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        static Tuple<IntPtr, int, IntPtr, byte[]> GetIumInvokeSecureServiceReturn(IntPtr GetIumInvokeSecureServiceBase)
        {
            string matchstr = "SkpsIsProcessDebuggingEnabled";

            byte[] matchstrbytes = Encoding.ASCII.GetBytes(matchstr);


            IntPtr GetIumInvokeSecureServiceBasePtr = new IntPtr((long)GetIumInvokeSecureServiceBase + (long)securekernelbase);

            int readlen = 0x8000;
            int readlenall = readlen;
            byte jmmpbyte = 0xeb;
            byte[] dumpbytes = new byte[readlen];
            Marshal.Copy(GetIumInvokeSecureServiceBasePtr, dumpbytes, 0, readlen);
            // Utils.HexDump(dumpbytes.ToList());
            Instruction insn = null;
            Disassembler disasm = new Disassembler(dumpbytes, ArchitectureMode.x86_64);
            bool firstmatch = false;
            bool nextmatch = false;
            ulong rdxsave = 0;
            IntPtr patchoffset = IntPtr.Zero;
            int patchlen = 0;
            while (true)
            {
                insn = disasm.NextInstruction();
                if (insn != null && !insn.Error && insn.Offset < (ulong)readlenall - 1)
                {
                    string opstr = insn.ToString();

                    if (opstr.Contains("lea rdx"))
                    {
                        firstmatch = true;


                        rdxsave = ((ulong)GetIumInvokeSecureServiceBase + insn.Offset + (ulong)insn.Length + (ulong)insn.Operands.Skip(1).FirstOrDefault().Value) & 0xffffffff;


                    }
                    else if (!opstr.Contains("lea rcx"))
                    {
                        firstmatch = false;
                    }

                    if (opstr.Contains("lea rcx"))
                    {
                        if (firstmatch)
                        {
                            nextmatch = true;
                        }
                        else
                        {
                            firstmatch = false;
                        }
                    }


                    if (nextmatch)
                    {
                        if (insn.Operands.Any() && (insn.Operands.FirstOrDefault().Opcode == ud_operand_code.OP_J))
                        {


                            IntPtr dumpbytesPtr = new IntPtr((long)rdxsave + (long)securekernelbase);
                            readlen = matchstrbytes.Length;
                            byte[] dumpbytesmatch = new byte[readlen];
                            Marshal.Copy(dumpbytesPtr, dumpbytesmatch, 0, readlen);

                            if (memcmp(dumpbytesmatch, matchstrbytes))
                            {



                                while (true)
                                {
                                    insn = disasm.NextInstruction();
                                    if (insn != null && !insn.Error && insn.Offset < (ulong)readlenall - 1)
                                    {
                                        opstr = insn.ToString();
                                        if (insn.Operands.Any() &&
                                            (insn.Operands.FirstOrDefault().Opcode == ud_operand_code.OP_J))
                                        {
                                            if (opstr.Contains("jmp"))
                                            {

                                                ulong func2cuurent = (ulong)insn.Operands.FirstOrDefault().Value;
                                                int fetchoffset =
                                                    (int)((insn.Offset + (ulong)insn.Length + (ulong)func2cuurent) &
                                                          0xffffffff);
                                                if (fetchoffset > 0 && fetchoffset < readlenall
                                                   )
                                                {
                                                    byte[] dumpbyteschk = new byte[]
                                                    {
                                                    0x22, 0, 0, 0xc0
                                                    };

                                                    bool errocdefid = false;
                                                    for (int i = 0; i < 0x20; i++)
                                                    {
                                                        if (dumpbyteschk.SequenceEqual(dumpbytes.Skip(fetchoffset + i)
                                                                .Take(4)))
                                                        {
                                                            errocdefid = true;

                                                        }

                                                        if (errocdefid && dumpbytes[fetchoffset + i] == jmmpbyte)
                                                        {
                                                            int startasm = fetchoffset;
                                                            int endtasm = i + 2;

                                                            dumpbytesmatch =
                                                                dumpbytes.Skip(startasm).Take(endtasm).ToArray();

                                                            patchoffset = new IntPtr((long)GetIumInvokeSecureServiceBase + (long)startasm);
                                                            patchlen = endtasm;

                                                            break;
                                                        }
                                                    }

                                                    if (!errocdefid | patchlen > 0)
                                                    {
                                                        break;
                                                    }

                                                }

                                            }

                                            if (patchlen > 0)
                                            {
                                                break;
                                            }
                                        }
                                        else if (opstr.Contains("call"))
                                        {
                                            break;

                                        }

                                    }
                                }


                            }
                        }
                        else
                        {
                            if (!opstr.Contains("lea rcx"))
                            {
                                firstmatch = false;
                                nextmatch = false;
                            }
                        }
                    }


                    if (opstr.Contains("ret"))
                    {
                        firstmatch = false;
                        nextmatch = false;

                        ulong skfun = ((ulong)GetIumInvokeSecureServiceBase + insn.Offset);
                        ulong skfunpage = ((ulong)GetIumInvokeSecureServiceBase + insn.Offset) & 0xfffffffffffff000;
                        ulong diffstart = skfun - skfunpage;
                        ulong diffskip = insn.Offset - diffstart;
                        return new Tuple<IntPtr, int, IntPtr, byte[]>(patchoffset, patchlen, new IntPtr((long)skfun), dumpbytes.Take((int)insn.Offset).Skip((int)diffskip).ToArray());
                    }
                }
            }

            return new Tuple<IntPtr, int, IntPtr, byte[]>(patchoffset, patchlen, IntPtr.Zero, new byte[] { });
        }

        static IntPtr GetIumInvokeSecureService(IntPtr GetSkCallNormalModeBase)
        {
            IntPtr GetSkCallNormalModeBasePtr = new IntPtr((long)GetSkCallNormalModeBase + (long)securekernelbase);
            int readlen = 0x800;
            byte clibyte = 0xfa;
            byte[] dumpbytes = new byte[readlen];
            Marshal.Copy(GetSkCallNormalModeBasePtr, dumpbytes, 0, readlen);
            // Utils.HexDump(dumpbytes.ToList());
            Disassembler disasm = new Disassembler(dumpbytes, ArchitectureMode.x86_64);
            ulong skfunsave = 0;
            Instruction insn = null;

            while (true)
            {
                insn = disasm.NextInstruction();
                if (insn != null && !insn.Error && insn.Offset < (ulong)readlen - 1)
                {
                    string opstr = insn.ToString();

                    if (insn.Operands.Any() && (insn.Operands.FirstOrDefault().Opcode == ud_operand_code.OP_J))
                    {
                        ulong func2cuurent = (ulong)insn.Operands.FirstOrDefault().Value;
                        ulong skfun = ((ulong)GetSkCallNormalModeBase + insn.Offset + (ulong)insn.Length + (ulong)func2cuurent) & 0xffffffff;
                        skfunsave = skfun;
                        insn = disasm.NextInstruction();
                        opstr = insn.ToString();

                        if (opstr.Contains("test"))
                        {

                            bool firstmatch = false;

                            while (true)
                            {
                                insn = disasm.NextInstruction();
                                if (insn != null && !insn.Error && insn.Offset < (ulong)readlen - 1)
                                {
                                    if (insn.Operands.Any() &&
                                        (insn.Operands.FirstOrDefault().Opcode == ud_operand_code.OP_J))
                                    {
                                        func2cuurent = (ulong)insn.Operands.FirstOrDefault().Value;
                                        int fetchoffset =
                                            (int)((insn.Offset + (ulong)insn.Length + (ulong)func2cuurent) &
                                                  0xffffffff);
                                        if (fetchoffset > 0 && fetchoffset < readlen && dumpbytes[fetchoffset] == clibyte)
                                        {

                                            if (firstmatch)
                                            {



                                                return new IntPtr((long)skfunsave);
                                            }
                                            else
                                            {
                                                firstmatch = true;
                                            }



                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                }
                            }
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
        static IntPtr GetSkCallNormalMode(IntPtr SkAllocateNormalModePoolPtr)
        {
            IntPtr SkAllocateNormalModePoolBase = new IntPtr((long)SkAllocateNormalModePoolPtr - (long)securekernelbase);
            int readlen = 0x100;

            byte[] dumpbytes = new byte[readlen];
            Marshal.Copy(SkAllocateNormalModePoolPtr, dumpbytes, 0, readlen);
            // Utils.HexDump(dumpbytes.ToList());
            Disassembler disasm = new Disassembler(dumpbytes, ArchitectureMode.x86_64);

            Instruction insn = null;
            int idx = 0;
            while (true)
            {
                insn = disasm.NextInstruction();
                if (insn != null && !insn.Error && insn.Offset < (ulong)readlen - 1)
                {
                    string opstr = insn.ToString();

                    if (opstr.Contains("call"))
                    {

                        idx++;
                        ulong func2cuurent = (ulong)insn.Operands.FirstOrDefault().Value;
                        ulong skfun = ((ulong)SkAllocateNormalModePoolBase + insn.Offset + (ulong)insn.Length + (ulong)func2cuurent) & 0xffffffff;

                        if (idx == 2)
                        {
                            return new IntPtr((long)skfun);
                        }

                    }


                }
                else
                {
                    break;
                }
            }

            return IntPtr.Zero;
            ;
        }

        private static IntPtr securekernelbase;

        static void Main(string[] args)
        {
            Ui.Init();
            Ui.Banner("IUM Debugger", "SecureKernel debug-mode patch");

            try
            {
                string exedir = AppContext.BaseDirectory;
                string securekernelpath = Path.Combine(exedir, "securekernel.exe");

                Ui.Section("Stage 1 · Static analysis of securekernel.exe");

                if (!File.Exists(securekernelpath))
                {
                    Ui.Err("securekernel.exe not found next to the executable.");
                    Ui.Kv("looked in", exedir);
                    return;
                }

                securekernelbase = LoadLibraryEx(securekernelpath, IntPtr.Zero, LoadLibraryFlags.DONT_RESOLVE_DLL_REFERENCES);
                if (securekernelbase == IntPtr.Zero)
                {
                    Ui.Err("LoadLibraryEx failed (error " + Marshal.GetLastWin32Error() + ").");
                    return;
                }
                Ui.Ok("securekernel.exe loaded");
                Ui.KvHex("module base", securekernelbase);

                IntPtr SkAllocateNormalModePoolPtr = GetProcAddress(securekernelbase, "SkAllocateNormalModePool");
                if (SkAllocateNormalModePoolPtr == IntPtr.Zero)
                {
                    Ui.Err("Export SkAllocateNormalModePool not found.");
                    return;
                }
                Ui.KvHex("SkAllocateNormalModePool", SkAllocateNormalModePoolPtr);

                IntPtr GetSkCallNormalModeBase = GetSkCallNormalMode(SkAllocateNormalModePoolPtr);
                if (GetSkCallNormalModeBase == IntPtr.Zero)
                {
                    Ui.Err("Could not resolve SkCallNormalMode.");
                    return;
                }
                Ui.Ok("SkCallNormalMode resolved");
                Ui.KvHex("RVA", GetSkCallNormalModeBase);

                IntPtr GetIumInvokeSecureServiceBase = GetIumInvokeSecureService(GetSkCallNormalModeBase);
                if (GetIumInvokeSecureServiceBase == IntPtr.Zero)
                {
                    Ui.Err("Could not resolve IumInvokeSecureService.");
                    return;
                }
                Ui.Ok("IumInvokeSecureService resolved");
                Ui.KvHex("RVA", GetIumInvokeSecureServiceBase);

                Tuple<IntPtr, int, IntPtr, byte[]> ptach = GetIumInvokeSecureServiceReturn(GetIumInvokeSecureServiceBase);
                IntPtr patchoffset = ptach.Item1;
                int patchlen = ptach.Item2;
                IntPtr retoffset = ptach.Item3;
                byte[] checkpag = ptach.Item4;

                Ui.Ok("ret instruction & patch site located");
                Ui.KvHex("ret address (RVA)", retoffset);
                Ui.KvHex("patch site (RVA)", patchoffset);
                Ui.KvHex("patch length", patchlen);
                Ui.KvHex("page-prefix bytes captured", checkpag.Length);
                Ui.Info("first 16 bytes of capture pattern:");
                Ui.HexDump(checkpag.Take(16).ToArray());

                SecurekernelCtx ctx = new SecurekernelCtx(patchoffset, patchlen, retoffset, checkpag);
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
        public IntPtr patchoffset;
        public int patchlen;
        public IntPtr retoffset;

        public byte[] checkpage;

        public SecurekernelCtx(IntPtr patchoffset, int patchlen, IntPtr retoffset, byte[] checkpage)
        {
            this.patchoffset = patchoffset;
            this.patchlen = patchlen;
            this.retoffset = retoffset;
            this.checkpage = checkpage;
        }
    }
}
