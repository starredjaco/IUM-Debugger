using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using IUMDebugger;
using static Hvlibdotnet.Hvlib;

namespace Hvlibdotnet
{
    public enum VTL_LEVEL
    {
        Vtl0 = 0,
        Vtl1 = 1,
        BadVtl = 2
    }

    public enum HV_MAP_GPA_FLAGS
    {
        HV_MAP_GPA_READABLE = 0x00000001,
        HV_MAP_GPA_WRITABLE = 0x00000002,
        HV_MAP_GPA_EXECUTABLE = 0x00000004

    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTDescriptor
    {
        public ushort pad1;
        public ushort pad2;
        public ushort pad3;
        public ushort Limit;
        public IntPtr Base;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct HV_MODIFY_VTL_PROTECTION_MASK
    {
        // Partition ID this request targets.
        UInt64 TargetPartitionId;

        HV_MAP_GPA_FLAGS MapFlags;

        UInt32 InputVtl;

        // Base guest physical page number at which the mapping begins.
        UInt64 TargetGpaBase;

        public HV_MODIFY_VTL_PROTECTION_MASK(ulong targetPartitionId, HV_MAP_GPA_FLAGS mapFlags, uint inputVtl, ulong targetGpaBase)
        {
            TargetPartitionId = targetPartitionId;
            MapFlags = mapFlags;
            InputVtl = inputVtl;
            TargetGpaBase = targetGpaBase;
        }

        public byte[] ToByteArray()
        {
            int rawsize = Marshal.SizeOf(this); // raw byte size of the struct
            byte[] rawdatas = new byte[rawsize]; // managed destination buffer
            IntPtr ptr = Marshal.AllocHGlobal(rawsize); // unmanaged buffer for the marshalled struct
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, rawdatas, 0, rawsize);
            Marshal.FreeHGlobal(ptr);
            return rawdatas;
        }

        public IntPtr ToIntPtr()
        {
            int onepage = 0x1000;
            //int rawsize = Marshal.SizeOf(this);//Get size of struct data
            IntPtr ptr = Marshal.AllocHGlobal(onepage); // page-sized unmanaged buffer for the hypercall input
            Marshal.StructureToPtr(this, ptr, true);
            return ptr;
        }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HV_INPUT_GET_VP_REGISTERS
    {
        UInt64 PartitionId;
        UInt32 VpIndex;
        UInt32 InputVtl;
        HV_REGISTER_NAME Names1;
        HV_REGISTER_NAME Names2;
        HV_REGISTER_NAME Names3;

        public HV_INPUT_GET_VP_REGISTERS(ulong partitionId, UInt32 vpIndex, UInt32 ivtl, HV_REGISTER_NAME names1, HV_REGISTER_NAME names2, HV_REGISTER_NAME names3)
        {
            PartitionId = partitionId;
            VpIndex = vpIndex;
            InputVtl = ivtl;
            Names1 = names1;
            Names2 = names2;
            Names3 = names3;

        }

        public byte[] ToByteArray()
        {
            int rawsize = Marshal.SizeOf(this); // raw byte size of the struct
            byte[] rawdatas = new byte[rawsize]; // managed destination buffer
            IntPtr ptr = Marshal.AllocHGlobal(rawsize); // unmanaged buffer for the marshalled struct
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, rawdatas, 0, rawsize);
            Marshal.FreeHGlobal(ptr);
            return rawdatas;
        }

        public IntPtr ToIntPtr()
        {
            int onepage = 0x1000;
            //int rawsize = Marshal.SizeOf(this);//Get size of struct data
            IntPtr ptr = Marshal.AllocHGlobal(onepage); // page-sized unmanaged buffer for the hypercall input
            Marshal.StructureToPtr(this, ptr, true);
            return ptr;
        }
    }


    public enum HV_REGISTER_NAME
    {
        // Suspend Registers
        HvRegisterExplicitSuspend = 0x00000000,
        HvRegisterInterceptSuspend = 0x00000001,

        // Pending Interruption Register
        HvX64RegisterPendingInterruption = 0x00010002,

        // Guest Crash Registers 
        HvRegisterGuestCrashP0 = 0x00000210,
        HvRegisterGuestCrashP1 = 0x00000211,
        HvRegisterGuestCrashP2 = 0x00000212,
        HvRegisterGuestCrashP3 = 0x00000213,
        HvRegisterGuestCrashP4 = 0x00000214,
        HvRegisterGuestCrashCtl = 0x00000215,

        // Interrupt State Register
        HvX64RegisterInterruptState = 0x00010003,

        // User-Mode Registers
        HvX64RegisterRax = 0x00020000,
        HvX64RegisterRcx = 0x00020001,
        HvX64RegisterRdx = 0x00020002,
        HvX64RegisterRbx = 0x00020003,
        HvX64RegisterRsp = 0x00020004,
        HvX64RegisterRbp = 0x00020005,
        HvX64RegisterRsi = 0x00020006,
        HvX64RegisterRdi = 0x00020007,
        HvX64RegisterR8 = 0x00020008,
        HvX64RegisterR9 = 0x00020009,
        HvX64RegisterR10 = 0x0002000A,
        HvX64RegisterR11 = 0x0002000B,
        HvX64RegisterR12 = 0x0002000C,
        HvX64RegisterR13 = 0x0002000D,
        HvX64RegisterR14 = 0x0002000E,
        HvX64RegisterR15 = 0x0002000F,
        HvX64RegisterRip = 0x00020010,
        HvX64RegisterRflags = 0x00020011,

        // Floating Point and Vector Registers
        HvX64RegisterXmm0 = 0x00030000,
        HvX64RegisterXmm1 = 0x00030001,
        HvX64RegisterXmm2 = 0x00030002,
        HvX64RegisterXmm3 = 0x00030003,
        HvX64RegisterXmm4 = 0x00030004,
        HvX64RegisterXmm5 = 0x00030005,
        HvX64RegisterXmm6 = 0x00030006,
        HvX64RegisterXmm7 = 0x00030007,
        HvX64RegisterXmm8 = 0x00030008,
        HvX64RegisterXmm9 = 0x00030009,
        HvX64RegisterXmm10 = 0x0003000A,
        HvX64RegisterXmm11 = 0x0003000B,
        HvX64RegisterXmm12 = 0x0003000C,
        HvX64RegisterXmm13 = 0x0003000D,
        HvX64RegisterXmm14 = 0x0003000E,
        HvX64RegisterXmm15 = 0x0003000F,
        HvX64RegisterFpMmx0 = 0x00030010,
        HvX64RegisterFpMmx1 = 0x00030011,
        HvX64RegisterFpMmx2 = 0x00030012,
        HvX64RegisterFpMmx3 = 0x00030013,
        HvX64RegisterFpMmx4 = 0x00030014,
        HvX64RegisterFpMmx5 = 0x00030015,
        HvX64RegisterFpMmx6 = 0x00030016,
        HvX64RegisterFpMmx7 = 0x00030017,
        HvX64RegisterFpControlStatus = 0x00030018,
        HvX64RegisterXmmControlStatus = 0x00030019,

        // Control Registers
        HvX64RegisterCr0 = 0x00040000,
        HvX64RegisterCr2 = 0x00040001,
        HvX64RegisterCr3 = 0x00040002,
        HvX64RegisterCr4 = 0x00040003,
        HvX64RegisterCr8 = 0x00040004,

        // Debug Registers
        HvX64RegisterDr0 = 0x00050000,
        HvX64RegisterDr1 = 0x00050001,
        HvX64RegisterDr2 = 0x00050002,
        HvX64RegisterDr3 = 0x00050003,
        HvX64RegisterDr6 = 0x00050004,
        HvX64RegisterDr7 = 0x00050005,

        // Segment Registers
        HvX64RegisterEs = 0x00060000,
        HvX64RegisterCs = 0x00060001,
        HvX64RegisterSs = 0x00060002,
        HvX64RegisterDs = 0x00060003,
        HvX64RegisterFs = 0x00060004,
        HvX64RegisterGs = 0x00060005,
        HvX64RegisterLdtr = 0x00060006,
        HvX64RegisterTr = 0x00060007,

        // Table Registers
        HvX64RegisterIdtr = 0x00070000,
        HvX64RegisterGdtr = 0x00070001,

        // Virtualized MSRs
        HvX64RegisterTsc = 0x00080000,
        HvX64RegisterEfer = 0x00080001,
        HvX64RegisterKernelGsBase = 0x00080002,
        HvX64RegisterApicBase = 0x00080003,
        HvX64RegisterPat = 0x00080004,
        HvX64RegisterSysenterCs = 0x00080005,
        HvX64RegisterSysenterEip = 0x00080006,
        HvX64RegisterSysenterEsp = 0x00080007,
        HvX64RegisterStar = 0x00080008,
        HvX64RegisterLstar = 0x00080009,
        HvX64RegisterCstar = 0x0008000A,
        HvX64RegisterSfmask = 0x0008000B,
        HvX64RegisterInitialApicId = 0x0008000C,

        //
        // Cache control MSRs
        //
        HvX64RegisterMsrMtrrCap = 0x0008000D,
        HvX64RegisterMsrMtrrDefType = 0x0008000E,
        HvX64RegisterMsrMtrrPhysBase0 = 0x00080010,
        HvX64RegisterMsrMtrrPhysBase1 = 0x00080011,
        HvX64RegisterMsrMtrrPhysBase2 = 0x00080012,
        HvX64RegisterMsrMtrrPhysBase3 = 0x00080013,
        HvX64RegisterMsrMtrrPhysBase4 = 0x00080014,
        HvX64RegisterMsrMtrrPhysBase5 = 0x00080015,
        HvX64RegisterMsrMtrrPhysBase6 = 0x00080016,
        HvX64RegisterMsrMtrrPhysBase7 = 0x00080017,
        HvX64RegisterMsrMtrrPhysMask0 = 0x00080040,
        HvX64RegisterMsrMtrrPhysMask1 = 0x00080041,
        HvX64RegisterMsrMtrrPhysMask2 = 0x00080042,
        HvX64RegisterMsrMtrrPhysMask3 = 0x00080043,
        HvX64RegisterMsrMtrrPhysMask4 = 0x00080044,
        HvX64RegisterMsrMtrrPhysMask5 = 0x00080045,
        HvX64RegisterMsrMtrrPhysMask6 = 0x00080046,
        HvX64RegisterMsrMtrrPhysMask7 = 0x00080047,
        HvX64RegisterMsrMtrrFix64k00000 = 0x00080070,
        HvX64RegisterMsrMtrrFix16k80000 = 0x00080071,
        HvX64RegisterMsrMtrrFix16kA0000 = 0x00080072,
        HvX64RegisterMsrMtrrFix4kC0000 = 0x00080073,
        HvX64RegisterMsrMtrrFix4kC8000 = 0x00080074,
        HvX64RegisterMsrMtrrFix4kD0000 = 0x00080075,
        HvX64RegisterMsrMtrrFix4kD8000 = 0x00080076,
        HvX64RegisterMsrMtrrFix4kE0000 = 0x00080077,
        HvX64RegisterMsrMtrrFix4kE8000 = 0x00080078,
        HvX64RegisterMsrMtrrFix4kF0000 = 0x00080079,
        HvX64RegisterMsrMtrrFix4kF8000 = 0x0008007A,

        // Hypervisor-defined MSRs (Misc)
        HvX64RegisterHypervisorPresent = 0x00090000,
        HvX64RegisterHypercall = 0x00090001,
        HvX64RegisterGuestOsId = 0x00090002,
        HvX64RegisterVpIndex = 0x00090003,
        HvX64RegisterVpRuntime = 0x00090004,
        HvRegisterCpuManagementVersion = 0x00090007,

        // Virtual APIC MSRs
        HvX64RegisterEoi = 0x00090010,
        HvX64RegisterIcr = 0x00090011,
        HvX64RegisterTpr = 0x00090012,
        HvRegisterVpAssistPage = 0x00090013,
        HvRegisterReferenceTsc = 0x00090017,

        // Performance statistics MSRs
        HvRegisterStatsPartitionRetail = 0x00090020,
        HvRegisterStatsPartitionInternal = 0x00090021,
        HvRegisterStatsVpRetail = 0x00090022,
        HvRegisterStatsVpInternal = 0x00090023,
        // Partition Timer Assist Registers

        HvX64RegisterEmulatedTimerPeriod = 0x00090030,
        HvX64RegisterEmulatedTimerControl = 0x00090031,
        HvX64RegisterPmTimerAssist = 0x00090032,

        // Hypervisor-defined MSRs (Synic)
        HvX64RegisterSint0 = 0x000A0000,
        HvX64RegisterSint1 = 0x000A0001,
        HvX64RegisterSint2 = 0x000A0002,
        HvX64RegisterSint3 = 0x000A0003,
        HvX64RegisterSint4 = 0x000A0004,
        HvX64RegisterSint5 = 0x000A0005,
        HvX64RegisterSint6 = 0x000A0006,
        HvX64RegisterSint7 = 0x000A0007,
        HvX64RegisterSint8 = 0x000A0008,
        HvX64RegisterSint9 = 0x000A0009,
        HvX64RegisterSint10 = 0x000A000A,
        HvX64RegisterSint11 = 0x000A000B,
        HvX64RegisterSint12 = 0x000A000C,
        HvX64RegisterSint13 = 0x000A000D,
        HvX64RegisterSint14 = 0x000A000E,
        HvX64RegisterSint15 = 0x000A000F,
        HvX64RegisterSynicBase = 0x000A0010,
        HvX64RegisterSversion = 0x000A0011,
        HvX64RegisterSifp = 0x000A0012,
        HvX64RegisterSipp = 0x000A0013,
        HvX64RegisterEom = 0x000A0014,
        HvRegisterSirbp = 0x000A0015,

        // Hypervisor-defined MSRs (Synthetic Timers)
        HvX64RegisterStimer0Config = 0x000B0000,
        HvX64RegisterStimer0Count = 0x000B0001,
        HvX64RegisterStimer1Config = 0x000B0002,
        HvX64RegisterStimer1Count = 0x000B0003,
        HvX64RegisterStimer2Config = 0x000B0004,
        HvX64RegisterStimer2Count = 0x000B0005,
        HvX64RegisterStimer3Config = 0x000B0006,
        HvX64RegisterStimer3Count = 0x000B0007,

        // XSAVE/XRSTOR register names. 

        // XSAVE AVX extended state registers.
        HvX64RegisterYmm0Low = 0x000C0000,
        HvX64RegisterYmm1Low = 0x000C0001,
        HvX64RegisterYmm2Low = 0x000C0002,
        HvX64RegisterYmm3Low = 0x000C0003,
        HvX64RegisterYmm4Low = 0x000C0004,
        HvX64RegisterYmm5Low = 0x000C0005,
        HvX64RegisterYmm6Low = 0x000C0006,
        HvX64RegisterYmm7Low = 0x000C0007,
        HvX64RegisterYmm8Low = 0x000C0008,
        HvX64RegisterYmm9Low = 0x000C0009,
        HvX64RegisterYmm10Low = 0x000C000A,
        HvX64RegisterYmm11Low = 0x000C000B,
        HvX64RegisterYmm12Low = 0x000C000C,
        HvX64RegisterYmm13Low = 0x000C000D,
        HvX64RegisterYmm14Low = 0x000C000E,
        HvX64RegisterYmm15Low = 0x000C000F,
        HvX64RegisterYmm0High = 0x000C0010,
        HvX64RegisterYmm1High = 0x000C0011,
        HvX64RegisterYmm2High = 0x000C0012,
        HvX64RegisterYmm3High = 0x000C0013,
        HvX64RegisterYmm4High = 0x000C0014,
        HvX64RegisterYmm5High = 0x000C0015,
        HvX64RegisterYmm6High = 0x000C0016,
        HvX64RegisterYmm7High = 0x000C0017,
        HvX64RegisterYmm8High = 0x000C0018,
        HvX64RegisterYmm9High = 0x000C0019,
        HvX64RegisterYmm10High = 0x000C001A,
        HvX64RegisterYmm11High = 0x000C001B,
        HvX64RegisterYmm12High = 0x000C001C,
        HvX64RegisterYmm13High = 0x000C001D,
        HvX64RegisterYmm14High = 0x000C001E,
        HvX64RegisterYmm15High = 0x000C001F,

        // Other MSRs
        HvX64RegisterMsrIa32MiscEnable = 0x000800A0,
        HvX64RegisterIa32FeatureControl = 0x000800A1,

        //
        // Synthetic VSM registers
        //

        HvRegisterVsmVpVtlControl = 0x000D0000,
        HvRegisterVsmCodePageOffsets = 0x000D0002,
        HvRegisterVsmVpStatus = 0x000D0003,
        HvRegisterVsmPartitionStatus = 0x000D0004,
        HvRegisterVsmVina = 0x000D0005,
        HvRegisterVsmCapabilities = 0x000D0006,
        HvRegisterVsmPartitionConfig = 0x000D0007,
        HvRegisterVsmVpSecureConfigVtl0 = 0x000D0010,
        HvRegisterVsmVpSecureConfigVtl1 = 0x000D0011,
        HvRegisterVsmVpSecureConfigVtl2 = 0x000D0012,
        HvRegisterVsmVpSecureConfigVtl3 = 0x000D0013,
        HvRegisterVsmVpSecureConfigVtl4 = 0x000D0014,
        HvRegisterVsmVpSecureConfigVtl5 = 0x000D0015,
        HvRegisterVsmVpSecureConfigVtl6 = 0x000D0016,
        HvRegisterVsmVpSecureConfigVtl7 = 0x000D0017,
        HvRegisterVsmVpSecureConfigVtl8 = 0x000D0018,
        HvRegisterVsmVpSecureConfigVtl9 = 0x000D0019,
        HvRegisterVsmVpSecureConfigVtl10 = 0x000D001A,
        HvRegisterVsmVpSecureConfigVtl11 = 0x000D001B,
        HvRegisterVsmVpSecureConfigVtl12 = 0x000D001C,
        HvRegisterVsmVpSecureConfigVtl13 = 0x000D001D,
        HvRegisterVsmVpSecureConfigVtl14 = 0x000D001E,

        // Mask Registers
        HvX64RegisterCrInterceptControl = 0x000E0000,
        HvX64RegisterCrInterceptCr0Mask = 0x000E0001,
        HvX64RegisterCrInterceptCr4Mask = 0x000E0002,
        HvX64RegisterCrInterceptIa32MiscEnableMask = 0x000E0003,

    }

    public class Utils
    {


        public static string HexDump(List<byte> bytes, int bytesPerLine = 16)
        {
            return HexDump(bytes.ToArray(), bytesPerLine);

        }

        public static string HexDump(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";
            int bytesLength = bytes.Length;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                8 // 8 hex digits for the address
                + 3; // 3-space gutter

            int firstCharColumn = firstHexColumn
                                  + bytesPerLine * 3 // 2 hex digits + 1 space per byte
                                  + (bytesPerLine - 1) / 8 // extra space between each 8-byte group
                                  + 2; // 2-space gutter before the ASCII column

            int lineLength = firstCharColumn
                             + bytesPerLine // ASCII column: one char per byte
                             + Environment.NewLine.Length; // CR + LF (typically 2 bytes on Windows)

            char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine)
                .ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            System.Text.StringBuilder result = new System.Text.StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }

                    hexColumn += 3;
                    charColumn++;
                }

                result.Append(line);
                result.Append(Environment.NewLine);
            }

            Console.WriteLine(result);
            return result.ToString();
        }
    }

    public class VmListBox
    {
        public UInt64 VmHandle { get; set; }
        public string VMName { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class KD_HYPERCALL
    {
        public UInt64 HypercallInputs;
        public IntPtr InputBuffer;
        public UInt64 InputBufferLen;
        public IntPtr OuputBuffer;
        public UInt64 OuputBufferLen;
        public UInt64 HypercallResult;

        public KD_HYPERCALL()
        {
        }

        public byte[] ToByteArray()
        {
            int rawsize = Marshal.SizeOf(this); // raw byte size of the struct
            byte[] rawdatas = new byte[rawsize]; // managed destination buffer
            IntPtr ptr = Marshal.AllocHGlobal(rawsize); // unmanaged buffer for the marshalled struct
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, rawdatas, 0, rawsize);
            Marshal.FreeHGlobal(ptr);
            return rawdatas;
        }

        public IntPtr ToIntPtr()
        {
            int rawsize = Marshal.SizeOf(this); // raw byte size of the struct
            IntPtr ptr = Marshal.AllocHGlobal(rawsize); // unmanaged buffer to hand to DeviceIoControl
            Marshal.StructureToPtr(this, ptr, true);
            return ptr;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct _SYSTEM_HANDLE_INFORMATION_EX
    {
        private static int TypeSize = Marshal.SizeOf(typeof(_SYSTEM_HANDLE_INFORMATION_EX));
        public IntPtr NumberOfHandles;
        public IntPtr Reserved;


        public uint GetNumberOfHandles()
        {
            return (uint)NumberOfHandles.ToInt64();
        }
        public static SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX HandleAt(IntPtr handleInfoPtr, ulong index)
        {
            IntPtr thisPtr = new IntPtr(handleInfoPtr.ToInt64());
            thisPtr = new IntPtr(thisPtr.ToInt64() + TypeSize + Marshal.SizeOf(typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX)) * (int)index);

            return (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX)Marshal.PtrToStructure(thisPtr, typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

        }
    }


    //https://docs.microsoft.com/en-us/windows/win32/api/ntdef/ns-ntdef-_unicode_string
    //https://www.pinvoke.net/default.aspx/Structures/UNICODE_STRING.html
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct UNICODE_STRING
    {
        public readonly ushort Length;
        public readonly ushort MaximumLength;
        [MarshalAs(UnmanagedType.LPWStr)]
        public readonly string Buffer;

        public UNICODE_STRING(string s)
        {
            Length = (ushort)(s.Length * 2);
            MaximumLength = (ushort)(Length + 2);
            Buffer = s;
        }

        public override string ToString()
        {
            return Buffer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_NAME_INFORMATION
    {
        public UNICODE_STRING Name;
    }

    //https://docs.microsoft.com/en-za/windows-hardware/drivers/ddi/ntifs/ns-ntifs-__public_object_type_information
    //http://www.jasinskionline.com/technicalwiki/OBJECT_TYPE_INFORMATION-WinApi-Struct.ashx
    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_TYPE_INFORMATION
    {
        public UNICODE_STRING TypeName;
        public int ObjectCount;
        public int HandleCount;
        int Reserved1;
        int Reserved2;
        int Reserved3;
        int Reserved4;
        public int PeakObjectCount;
        public int PeakHandleCount;
        int Reserved5;
        int Reserved6;
        int Reserved7;
        int Reserved8;
        public int InvalidAttributes;
        public GENERIC_MAPPING GenericMapping;
        public int ValidAccess;
        byte Unknown;
        public byte MaintainHandleDatabase;
        public int PoolType;
        public int PagedPoolUsage;
        public int NonPagedPoolUsage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GENERIC_MAPPING
    {
        internal uint GenericRead;
        internal uint GenericWrite;
        internal uint GenericExecute;
        internal uint GenericAll;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
    { // entry returned by NtQuerySystemInformation(SystemExtendedHandleInformation = 0x40)
        public IntPtr ObjectPointer;
        public IntPtr ProcessID;
        public IntPtr HandleValue;
        public uint GrantedAccess;
        public ushort CreatorBackTrackIndex;
        public ushort ObjectType;
        public uint HandleAttributes;
        public uint Reserved;
    }

    //https://www.pinvoke.net/default.aspx/Enums.OBJECT_INFORMATION_CLASS
    public enum OBJECT_INFORMATION_CLASS
    {
        ObjectBasicInformation = 0,
        ObjectNameInformation = 1,
        ObjectTypeInformation = 2,
        ObjectAllTypesInformation = 3,
        ObjectHandleInformation = 4
    }
    [StructLayout(LayoutKind.Sequential)]
    public class SECURITY_ATTRIBUTES
    {
        /// <summary>
        /// The size, in bytes, of this structure.
        /// This value is set by the constructor to the size of the <see cref="SECURITY_ATTRIBUTES"/> structure.
        /// </summary>
        public int nLength;


        /// <summary>
        /// A pointer to a <see cref="SECURITY_DESCRIPTOR"/> structure that controls access to the object. If the value of this member is NULL, the object is assigned the default security descriptor associated with the access token of the calling process. This is not the same as granting access to everyone by assigning a NULL discretionary access control list (DACL). By default, the default DACL in the access token of a process allows access only to the user represented by the access token.
        /// For information about creating a security descriptor, see Creating a Security Descriptor.
        /// </summary>
        public IntPtr lpSecurityDescriptor;


        /// <summary>
        /// A Boolean value that specifies whether the returned handle is inherited when a new process is created. If this member is TRUE, the new process inherits the handle.
        /// </summary>
        public int bInheritHandle;


        /// <summary>
        /// Gets a value indicating whether the returned handle is inherited when a new process is created. If this member is TRUE, the new process inherits the handle.
        /// </summary>
        public bool InheritHandle => this.bInheritHandle != 0;


        /// <summary>
        /// Initializes a new instance of the <see cref="SECURITY_ATTRIBUTES"/> struct.
        /// </summary>
        /// <returns>A new instance of <see cref="SECURITY_ATTRIBUTES"/>.</returns>
        public static SECURITY_ATTRIBUTES Create()
        {
            return new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)),
            };
        }
    }

    public class SecurekernelPatch
    {
        public const string NTDLL = "ntdll.dll";

        public const string KERNEL32 = "kernel32.dll";


        [DllImport("ntdll")]
        public static extern uint NtQuerySystemInformation(
            [In] uint SystemInformationClass,
            [In] IntPtr SystemInformation,
            [In] uint SystemInformationLength,
            [Out] out uint ReturnLength);


        [DllImport("ntdll.dll")]
        protected static extern uint NtQueryObject(
            [In] IntPtr Handle,
            [In] OBJECT_INFORMATION_CLASS ObjectInformationClass,
            [In] IntPtr ObjectInformation,
            [In] uint ObjectInformationLength,
            [Out] out uint ReturnLength
        );

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern SafeFileHandle CreateFile(String lpFileName,
            uint dwDesiredAccess, System.IO.FileShare dwShareMode,
            SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode,
            IntPtr InBuffer, int nInBufferSize,
            IntPtr OutBuffer, int nOutBufferSize,
            out int pBytesReturned, IntPtr lpOverlapped);

        public static readonly uint SystemExtendedHandleInformation = 0x40;
        public static readonly uint STATUS_INFO_LENGTH_MISMATCH = 0xc0000004;
        public static readonly uint STATUS_SUCCESS = 0;

        public static UInt64 Hypercall(ushort code, bool fast, bool Nested, ushort CountOfElements, ushort RepCount, ushort RepStartIndex)
        {

            UInt64 HypercallCode = code;
            if (fast)
            {
                HypercallCode |= ((UInt64)1 << 16);
            }

            if (Nested)
            {
                HypercallCode |= ((UInt64)1 << 31);
            }
            HypercallCode |= ((UInt64)CountOfElements << 32);
            return HypercallCode;
        }

        public static void handleDump()
        {



            List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> result = new List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            uint handleInfoSize = 1024 * 1024;
            IntPtr handleInfoPtr = Marshal.AllocHGlobal((int)handleInfoSize);
            uint returnSize = 0;
            uint status = 0;
            while ((status = NtQuerySystemInformation(SecurekernelPatch.SystemExtendedHandleInformation, handleInfoPtr, handleInfoSize, out returnSize)) ==
                   SecurekernelPatch.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(handleInfoPtr);
                handleInfoPtr = Marshal.AllocHGlobal(new IntPtr(handleInfoSize *= 2));
            }
            if (status != SecurekernelPatch.STATUS_SUCCESS)
            {
                //Console.WriteLine("NtQuerySystemInformation failed. ErrCode: " + Marshal.GetLastWin32Error());
                goto ret;
            }
            _SYSTEM_HANDLE_INFORMATION_EX handleInfo = (_SYSTEM_HANDLE_INFORMATION_EX)Marshal.PtrToStructure(handleInfoPtr, typeof(_SYSTEM_HANDLE_INFORMATION_EX));

            uint NumberOfHandles = handleInfo.GetNumberOfHandles();
            for (uint i = 0; i < NumberOfHandles; i++)
            {
                SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handleEntry = _SYSTEM_HANDLE_INFORMATION_EX.HandleAt(handleInfoPtr, i);
                result.Add(handleEntry);
            }
        ret:
            Marshal.FreeHGlobal(handleInfoPtr);
            Console.WriteLine(result.Count);
            foreach (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handleTableEntryInfoEx in result)
            {
                IntPtr handleDuplicate = handleTableEntryInfoEx.HandleValue;
                uint length;
                NtQueryObject(handleDuplicate, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, IntPtr.Zero, 0, out length);
                if (length == 0)
                {
                    continue;
                }
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    ptr = Marshal.AllocHGlobal((int)length);
                    if (NtQueryObject(handleDuplicate, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, ptr, length,
                            out length) != SecurekernelPatch.STATUS_SUCCESS)

                    {
                        Marshal.FreeHGlobal(ptr);
                        continue;
                    }

                    OBJECT_TYPE_INFORMATION typeInfo = Marshal.PtrToStructure<OBJECT_TYPE_INFORMATION>(ptr);
                    string TypeString = typeInfo.TypeName.ToString();

                    if (TypeString == "File")
                    {
                        if (ptr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                        ptr = IntPtr.Zero;
                        length = 0;
                        NtQueryObject(handleDuplicate, OBJECT_INFORMATION_CLASS.ObjectNameInformation, IntPtr.Zero,
                            0, out length);
                        if (length == 0)
                        {
                            if (ptr != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(ptr);
                            }
                            continue;
                        }

                        try
                        {
                            ptr = Marshal.AllocHGlobal((int)length);
                            if (NtQueryObject(handleDuplicate, OBJECT_INFORMATION_CLASS.ObjectNameInformation, ptr,
                                    length,
                                    out length) != SecurekernelPatch.STATUS_SUCCESS)

                            {
                                if (ptr != IntPtr.Zero)
                                {
                                    Marshal.FreeHGlobal(ptr);
                                }
                                continue;
                            }

                            OBJECT_NAME_INFORMATION nameInfo = Marshal.PtrToStructure<OBJECT_NAME_INFORMATION>(ptr);
                            string TypeName = nameInfo.Name.ToString();

                            Console.WriteLine(TypeString);
                            Console.WriteLine(TypeName);
                            Console.WriteLine(handleDuplicate.ToString("x"));
                        }
                        finally
                        {
                            if (ptr != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(ptr);
                            }

                            ptr = IntPtr.Zero;
                        }
                    }
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }

            }
            return;
        }
        /// <summary>
        /// Spinlock written at a known offset inside IumInvokeSecureService:
        ///     pause           ; F3 90 — yield the hyperthread
        ///     jmp $-2         ; EB FC — loop back to the pause forever
        ///     ret             ; C3    — preserved tail; harmless if the spinlock is reverted mid-flight
        /// Any vCPU that drifts into this location parks here, which lets us read its
        /// RIP and CR3 via HvGetVpRegisters and recover the secure-kernel base.
        /// </summary>
        public static byte[] pacthcode = new byte[] { 0xF3, 0x90, 0xEB, 0xFC, 0xC3 };

        /// <summary>
        /// Replacement bytes written once a vCPU has been trapped: five back-to-back
        /// rets, so any thread that reaches this site simply returns. The original
        /// prologue is intentionally not restored.
        /// </summary>
        public static byte[] revertcode = new byte[] { 0xC3, 0xC3, 0xC3, 0xC3, 0xC3 };

        public static byte[] pacthcodenop = Enumerable.Repeat((byte)0x90, 0xb).ToArray();


        public static bool memcmp(byte[] dumpbytes)
        {
            for (int i = 0; i < securekernelIumInvokeSecurePatchCheck.Length; i++)
            {
                if (dumpbytes[i] != securekernelIumInvokeSecurePatchCheck[i])
                {
                    return false;
                }
            }

            return true;
        }
        public static int onepage = 0x1000;
        public static IntPtr vpreg = Marshal.AllocHGlobal(onepage);
        public static byte[] cleanpage = new byte[onepage];

        public static bool HvModifyVtlProtectionMask(UInt64 PartitionId, ulong readaddr)
        {
            bool ret = true;
            IntPtr inbuf = Marshal.AllocHGlobal(onepage);
            HV_MODIFY_VTL_PROTECTION_MASK modify = new HV_MODIFY_VTL_PROTECTION_MASK(PartitionId,
            HV_MAP_GPA_FLAGS.HV_MAP_GPA_READABLE |
            HV_MAP_GPA_FLAGS.HV_MAP_GPA_EXECUTABLE, 0x11, readaddr >> 0xc);
            IntPtr idtreg = Marshal.AllocHGlobal(onepage);
            ushort HvCallModifyVtlProtectionMask = 0x000C;
            UInt64 Hypercallcode = Hypercall(HvCallModifyVtlProtectionMask, false, false, 1, 0, 0);

            using (SafeFileHandle hvmm = CreateFile("\\\\.\\hvmm", 0xC0000000, (System.IO.FileShare)3u, null,
                       (System.IO.FileMode)3u, 0x80u, IntPtr.Zero))
            {
                if (!hvmm.IsInvalid)
                {
                    Console.WriteLine(hvmm.DangerousGetHandle().ToString("x"));
                    Console.WriteLine(PartitionId.ToString("x"));



                    KD_HYPERCALL hycallbuf = new KD_HYPERCALL();

                    hycallbuf.HypercallInputs = Hypercallcode;

                    int modifylen = Marshal.SizeOf(modify);
                    hycallbuf.InputBuffer = inbuf;
                    Marshal.Copy(cleanpage, 0, hycallbuf.InputBuffer, onepage);
                    Marshal.Copy(modify.ToByteArray(), 0, hycallbuf.InputBuffer, modifylen);
                    hycallbuf.InputBufferLen = (ulong)onepage;

                    hycallbuf.OuputBuffer = idtreg;
                    hycallbuf.OuputBufferLen = (ulong)onepage;
                    // Marshal.Copy(cleanpage, 0, hycallbuf.OuputBuffer, onepage);
                    int inoutbuflen = Marshal.SizeOf(hycallbuf);
                    GCHandle gcHandle = GCHandle.Alloc((object)hycallbuf, GCHandleType.Pinned);
                    IntPtr inoutbufptr = gcHandle.AddrOfPinnedObject();


                    int pBytesReturned;

                    ret = DeviceIoControl(hvmm, 0x222114u, inoutbufptr, inoutbuflen, inoutbufptr,
                        inoutbuflen,
                        out pBytesReturned, IntPtr.Zero);

                    Console.WriteLine(ret.ToString());


                    Console.WriteLine(hycallbuf.HypercallResult.ToString("x"));

                }
            }

            return ret;
        }


        public static ulong TranslateLinearAddress(UInt64 VmHandle, ulong directoryTableBase, ulong virtualAddress)
        {
            ushort PML4 = (ushort)((virtualAddress >> 39) & 0x1FF);         // PML4 entry index (bits 47:39)
            ushort DirectoryPtr = (ushort)((virtualAddress >> 30) & 0x1FF); // Page-Directory-Pointer Table index (bits 38:30)
            ushort Directory = (ushort)((virtualAddress >> 21) & 0x1FF);    // Page Directory Table index (bits 29:21)
            ushort Table = (ushort)((virtualAddress >> 12) & 0x1FF);        // Page Table index (bits 20:12)

            // Read the PML4 entry. directoryTableBase is the physical address of the
            // PML4 itself, typically taken from CR3 or from the kernel process object.
            ulong PML4E = (ulong)Hvlib.ReadPhysicalMemoryPtr(VmHandle, directoryTableBase + (ulong)PML4 * sizeof(ulong));

            if (PML4E == 0)
                return 0;
            // The PML4E points to the next table in the walk: the Page-Directory-Pointer Table.
            ulong PDPTE = (ulong)Hvlib.ReadPhysicalMemoryPtr(VmHandle, (PML4E & 0xFFFFFFFFFFF000) + (ulong)DirectoryPtr * sizeof(ulong));

            if (PDPTE == 0)
                return 0;
            // Check the page-size (PS) bit: if set, this entry maps a large page directly.
            if ((PDPTE & (1 << 7)) != 0)
            {
                // PDPTE.PS = 1: the entry maps a 1-GiB page.
                //   Bits 51:30 of the physical address come from the PDPTE.
                //   Bits 29:0 come from the original virtual address.
                return (PDPTE & 0xFFFFFC0000000) + (virtualAddress & 0x3FFFFFFF);
            }

            // PS = 0: the PDPTE references the next table — the Page Directory.
            ulong PDE = (ulong)Hvlib.ReadPhysicalMemoryPtr(VmHandle, (PDPTE & 0xFFFFFFFFFF000) + (ulong)Directory * sizeof(ulong));

            if (PDE == 0)
                return 0;
            if ((PDE & (1 << 7)) != 0)
            {
                // PDE.PS = 1: the entry maps a 2-MiB page.
                //   Bits 51:21 of the physical address come from the PDE.
                //   Bits 20:0 come from the original virtual address.
                return (PDE & 0xFFFFFFFE00000) + (virtualAddress & 0x1FFFFF);
            }

            // PS = 0: the PDE references the final table — the Page Table.
            ulong PTE = (ulong)Hvlib.ReadPhysicalMemoryPtr(VmHandle, (PDE & 0xFFFFFFFFFF000) + (ulong)Table * sizeof(ulong));

            if (PTE == 0)
                return 0;

            PTE = (PTE & 0xFFFFFFFFFF000);

            // The PTE maps a 4-KiB page.
            //   Bits 51:12 of the physical address come from the PTE.
            //   Bits 11:0 come from the original virtual address.

            ulong ret = (PTE & 0xFFFFFFFFFF000) + (virtualAddress & 0xFFF);
            return ret;
        }

        public static byte[] securekernelIumInvokeSecurePatchCheck;
        public static void Patch(SecurekernelCtx ctx)
        {


            var partitions = Hvlib.EnumAllPartitions();
            if (partitions == null || partitions.Count == 0)
            {
                Ui.Err("No Hyper-V partitions found. Is the target VM running?");
                return;
            }

            foreach (VmListBox partition in partitions)
            {
                bool ret = true;

                Hvlib.SelectPartition(partition.VmHandle);

                UInt64 PartitionId = Hvlib.SdkGetData2((UInt64)partition.VmHandle, HVDD_INFORMATION_CLASS.HvddPartitionId);

                UIntPtr Cr3Kernel = UIntPtr.Zero;
                UIntPtr NumberOfCPU = UIntPtr.Zero;
                Hvlib.GetData(partition.VmHandle, Hvlib.HVDD_INFORMATION_CLASS.HvddKernelBase, out Cr3Kernel);
                Hvlib.GetData(partition.VmHandle, Hvlib.HVDD_INFORMATION_CLASS.HvddNumberOfCPU, out NumberOfCPU);

                Ui.Section("Stage 2 · Target VM");
                Ui.Ok("attached to partition");
                Ui.Kv("name", partition.VMName ?? "(unnamed)");
                Ui.KvHex("partition id", PartitionId);
                Ui.KvHex("vCPU count", NumberOfCPU.ToUInt64());
                Ui.KvHex("VTL0 kernel base", Cr3Kernel.ToUInt64());

                pacthcodenop = Enumerable.Repeat((byte)0x90, ctx.PatchLength).ToArray();
                IntPtr pacthcodeptr = Marshal.AllocHGlobal(pacthcode.Length);
                Marshal.Copy(pacthcode, 0, pacthcodeptr, pacthcode.Length);
                IntPtr pacthcodenopptr = Marshal.AllocHGlobal(pacthcodenop.Length);
                Marshal.Copy(pacthcodenop, 0, pacthcodenopptr, pacthcodenop.Length);

                IntPtr revertcodeptr = Marshal.AllocHGlobal(revertcode.Length);
                Marshal.Copy(revertcode, 0, revertcodeptr, revertcode.Length);
                int retoffsetcode = (int)(ulong)ctx.RetOffset & 0xfff;
                ulong retoffsetpage = ((ulong)ctx.RetOffset) & 0xfffffffffffff000;
                int magicoffsetchk = retoffsetcode & 0xf00;
                if (magicoffsetchk > 0x400)
                {
                    magicoffsetchk = magicoffsetchk - 0x200;
                }
                else if (magicoffsetchk > 0x300)
                {
                    magicoffsetchk = magicoffsetchk - 0x100;
                }
                else
                {
                    magicoffsetchk = 0;
                }
                securekernelIumInvokeSecurePatchCheck = ctx.CheckPage.Skip(magicoffsetchk).ToArray();
                int readlen = securekernelIumInvokeSecurePatchCheck.Length;
                ulong readaddr = 0x600000;
                ulong guestcr3 = 0x600000;
                ulong readaddrsve = 0x600000;
                IntPtr ntknrlbuf = Marshal.AllocHGlobal(readlen);
                byte[] dumpbytes = new byte[readlen];
                ulong readaddrphy = readaddr;



                Ui.Section("Stage 3 · Locate IumInvokeSecureService physical page");
                Ui.Step("scanning guest physical memory from 0x" + readaddr.ToString("x"));

                // Chunked physical-memory scan: one IOCTL per 1 MB instead of per 4 KB page,
                // and progress prints throttled to ~2 Hz so console I/O doesn't dominate runtime.
                const int chunkPages = 256;
                int chunkSize = chunkPages * onepage;
                IntPtr chunkBuf = Marshal.AllocHGlobal(chunkSize);
                byte[] chunkBytes = new byte[chunkSize];
                int patternLen = securekernelIumInvokeSecurePatchCheck.Length;
                int lastReportTick = Environment.TickCount;
                ulong scanStart = readaddr;
                bool foundMatch = false;

                while (!foundMatch)
                {
                    bool chunkOk = Hvlib.ReadPhysicalMemory(partition.VmHandle, readaddr, chunkSize, chunkBuf);

                    if (chunkOk)
                    {
                        Marshal.Copy(chunkBuf, chunkBytes, 0, chunkSize);

                        for (int p = 0; p < chunkPages; p++)
                        {
                            int offset = p * onepage + magicoffsetchk;
                            if (offset + patternLen > chunkSize) break;

                            bool match = true;
                            for (int j = 0; j < patternLen; j++)
                            {
                                if (chunkBytes[offset + j] != securekernelIumInvokeSecurePatchCheck[j])
                                {
                                    match = false;
                                    break;
                                }
                            }

                            if (match)
                            {
                                readaddr += (ulong)(p * onepage);
                                readaddrsve = readaddr;
                                Buffer.BlockCopy(chunkBytes, p * onepage + magicoffsetchk, dumpbytes, 0, patternLen);

                                Hvlib.ReadPhysicalMemory(partition.VmHandle, readaddr, onepage, vpreg);
                                Marshal.Copy(vpreg, cleanpage, 0, onepage);

                                readaddrphy = readaddr + (ulong)retoffsetcode;
                                ret = Hvlib.WritePhysicalMemory(partition.VmHandle, readaddrphy, pacthcode.Length, pacthcodeptr);

                                Hvlib.ReadPhysicalMemory(partition.VmHandle, readaddr, onepage, vpreg);
                                Marshal.Copy(vpreg, cleanpage, 0, onepage);
                                cleanpage = new byte[onepage];

                                foundMatch = true;
                                Ui.ProgressDone();
                                Ui.Ok("matched candidate page at GPA 0x" + readaddr.ToString("x"));
                                Ui.Kv("write spinlock at GPA", "0x" + readaddrphy.ToString("x") + (ret ? " (ok)" : " (FAILED)"));
                                break;
                            }
                        }

                        if (!foundMatch) readaddr += (ulong)chunkSize;
                    }
                    else
                    {
                        // Chunk read failed (likely an MMIO hole). Fall back to per-page reads
                        // across this chunk so we don't skip over a valid match.
                        for (int p = 0; p < chunkPages && !foundMatch; p++)
                        {
                            ulong pageAddr = readaddr + (ulong)(p * onepage);
                            readaddrphy = pageAddr + (ulong)magicoffsetchk;

                            if (!Hvlib.ReadPhysicalMemory(partition.VmHandle, readaddrphy, patternLen, ntknrlbuf))
                                continue;

                            Marshal.Copy(ntknrlbuf, dumpbytes, 0, patternLen);
                            if (!memcmp(dumpbytes)) continue;

                            readaddr = pageAddr;
                            readaddrsve = readaddr;

                            Hvlib.ReadPhysicalMemory(partition.VmHandle, readaddr, onepage, vpreg);
                            Marshal.Copy(vpreg, cleanpage, 0, onepage);

                            readaddrphy = readaddr + (ulong)retoffsetcode;
                            ret = Hvlib.WritePhysicalMemory(partition.VmHandle, readaddrphy, pacthcode.Length, pacthcodeptr);

                            Hvlib.ReadPhysicalMemory(partition.VmHandle, readaddr, onepage, vpreg);
                            Marshal.Copy(vpreg, cleanpage, 0, onepage);
                            cleanpage = new byte[onepage];

                            foundMatch = true;
                            Ui.ProgressDone();
                            Ui.Ok("matched candidate page at GPA 0x" + readaddr.ToString("x"));
                            Ui.Kv("write spinlock at GPA", "0x" + readaddrphy.ToString("x") + (ret ? " (ok)" : " (FAILED)"));
                        }

                        if (!foundMatch) readaddr += (ulong)chunkSize;
                    }

                    int now = Environment.TickCount;
                    if (!foundMatch && now - lastReportTick > 500)
                    {
                        Ui.Progress("scanning", readaddr, readaddr - scanStart);
                        lastReportTick = now;
                    }
                }
                Marshal.FreeHGlobal(chunkBuf);

                readlen = 4;



                ushort HvGetVpRegisters = 0x0050;
                ushort HvCallGetPartitionId = 0x0046;
                UInt64 Hypercallcode = Hypercall(HvGetVpRegisters, false, false, 3, 0, 0);

                ulong finalsecurekernel = 0;
                bool nextphase = false;
                readlen = 0x100;
                IntPtr idtreg = Marshal.AllocHGlobal(onepage);
                IntPtr inbuf = Marshal.AllocHGlobal(onepage);
                IntPtr idtvtl1Base = IntPtr.Zero;
                IntPtr vtl1rip = IntPtr.Zero;
                int regidx = 0;
                Ui.Section("Stage 4 · Resolve VTL1 RIP via HvGetVpRegisters");
                Ui.Step("polling " + ((int)NumberOfCPU) + " vCPU(s) until one lands in the spinlock");

                using (SafeFileHandle hvmm = CreateFile("\\\\.\\hvmm", 0xC0000000, (System.IO.FileShare)3u, null,
                           (System.IO.FileMode)3u, 0x80u, IntPtr.Zero))
                {
                    if (hvmm.IsInvalid)
                    {
                        Ui.Err("Could not open \\\\.\\hvmm. Is hvlib's helper driver loaded?");
                    }
                    else
                    {
                        int pollTries = 0;

                        while (true)
                        {
                            for (int i = 0; i < (int)NumberOfCPU; i++)
                            {
                                pollTries++;

                                KD_HYPERCALL hycallbuf = new KD_HYPERCALL();

                                hycallbuf.HypercallInputs = Hypercallcode;
                                HV_INPUT_GET_VP_REGISTERS vpreg1 = new HV_INPUT_GET_VP_REGISTERS(PartitionId, (uint)i,
                                    0x1, HV_REGISTER_NAME.HvX64RegisterCr3, HV_REGISTER_NAME.HvX64RegisterRip, HV_REGISTER_NAME.HvRegisterVpAssistPage);
                                int vpreg1len = Marshal.SizeOf(vpreg1);
                                hycallbuf.InputBuffer = inbuf;
                                Marshal.Copy(cleanpage, 0, hycallbuf.InputBuffer, onepage);
                                Marshal.Copy(vpreg1.ToByteArray(), 0, hycallbuf.InputBuffer, vpreg1len);
                                hycallbuf.InputBufferLen = (ulong)onepage;

                                hycallbuf.OuputBuffer = idtreg;
                                hycallbuf.OuputBufferLen = (ulong)onepage;
                                int inoutbuflen = Marshal.SizeOf(hycallbuf);
                                GCHandle gcHandle = GCHandle.Alloc((object)hycallbuf, GCHandleType.Pinned);
                                IntPtr inoutbufptr = gcHandle.AddrOfPinnedObject();

                                int pBytesReturned;

                                ret = DeviceIoControl(hvmm, 0x222114u, inoutbufptr, inoutbuflen, inoutbufptr,
                                    inoutbuflen,
                                    out pBytesReturned, IntPtr.Zero);

                                IntPtr vtl1cr3 = Marshal.ReadIntPtr(idtreg);
                                idtreg = idtreg + 0x10;
                                vtl1rip = Marshal.ReadIntPtr(idtreg);

                                int riplow = (int)(long)vtl1rip & 0xfff;
                                if (((int)hycallbuf.HypercallResult) == (int)0 && riplow > retoffsetcode - 0x20 && riplow < retoffsetcode + 0x20)
                                {
                                    regidx = i;
                                    guestcr3 = (ulong)vtl1cr3;
                                    nextphase = true;
                                    finalsecurekernel = ((ulong)vtl1rip & (ulong)0xfffffffffffff000);
                                    finalsecurekernel = finalsecurekernel - retoffsetpage;

                                    Ui.Ok("vCPU " + i + " trapped in spinlock after " + pollTries + " poll(s)");
                                    Ui.KvHex("VTL1 cr3", (ulong)vtl1cr3);
                                    Ui.KvHex("VTL1 rip", (ulong)vtl1rip);
                                    Ui.KvHex("securekernel base (GVA)", finalsecurekernel);
                                    break;
                                }
                                else
                                {
                                    Thread.Sleep(10);
                                }
                            }

                            if (nextphase)
                            {
                                break;
                            }
                        }
                    }
                }

                if (nextphase)
                {
                    Ui.Section("Stage 5 · Patch SkpsIsProcessDebuggingEnabled");

                    Hvlib.WritePhysicalMemory(partition.VmHandle, readaddrphy, revertcode.Length, revertcodeptr);
                    Ui.Ok("reverted spinlock at GPA 0x" + readaddrphy.ToString("x"));

                    readlen = 0x1000;
                    ntknrlbuf = Marshal.AllocHGlobal(readlen);

                    ulong virtbase = TranslateLinearAddress(partition.VmHandle, guestcr3, (ulong)finalsecurekernel);
                    Hvlib.ReadPhysicalMemory(partition.VmHandle, virtbase, readlen, ntknrlbuf);
                    dumpbytes = new byte[readlen];
                    Marshal.Copy(ntknrlbuf, dumpbytes, 0, readlen);

                    Ui.Ok("securekernel PE header (first 0x40 bytes):");
                    Ui.HexDump(dumpbytes.Take(0x40).ToArray(), finalsecurekernel);

                    Hvlib.ReadPhysicalMemory(partition.VmHandle, readaddrsve, onepage, vpreg);
                    Marshal.Copy(vpreg, cleanpage, 0, onepage);

                    readaddr = finalsecurekernel + (ulong)ctx.PatchOffset;
                    virtbase = TranslateLinearAddress(partition.VmHandle, guestcr3, (ulong)readaddr);
                    ret = Hvlib.WritePhysicalMemory(partition.VmHandle, virtbase, pacthcodenop.Length, pacthcodenopptr);
                    if (ret) Ui.Ok("wrote NOP sled at GPA 0x" + virtbase.ToString("x") + " (" + pacthcodenop.Length + " bytes)");
                    else Ui.Err("WritePhysicalMemory failed at GPA 0x" + virtbase.ToString("x"));

                    Hvlib.ReadPhysicalMemory(partition.VmHandle, virtbase, readlen, ntknrlbuf);
                    Marshal.Copy(ntknrlbuf, dumpbytes, 0, readlen);
                    Ui.Info("verify (first 0x10 bytes after patch):");
                    Ui.HexDump(dumpbytes.Take(0x10).ToArray(), readaddr);

                    goto endlable;
                }

            endlable:
                Hvlib.CloseAllPartitions();

                Ui.Result("Done. Paste this into windbg:",
                          ".reload /f securekernel.exe=0x" + finalsecurekernel.ToString("x"));
                return;
            }

            Hvlib.CloseAllPartitions();
        }
    }

    public partial class Hvlib
    {
        public Hvlib()
        {
        }

        public enum READ_MEMORY_METHOD
        {
            ReadInterfaceWinHv,
            ReadInterfaceHvmmDrvInternal,
            ReadInterfaceUnsupported
        }
        public enum WRITE_MEMORY_METHOD
        {
            WriteInterfaceWinHv,
            WriteInterfaceHvmmDrvInternal,
            WriteInterfaceUnsupported
        }

        public enum SUSPEND_RESUME_METHOD
        {
            SuspendResumeUnsupported,
            SuspendResumePowershell,
            SuspendResumeWriteSpecRegister
        }

        public enum VM_STATE_ACTION
        {
            SuspendVm = 0,
            ResumeVm = 1
        }

        public enum GET_CR3_TYPE
        {
            Cr3Process = 0,
            Cr3Kernel = 1,
            Cr3SecureKenerl = 2,
            Cr3Hypervisor = 3
        }

        public enum HVDD_INFORMATION_CLASS
        {
            HvddKdbgData,
            HvddPartitionFriendlyName,
            HvddPartitionId,
            HvddVmtypeString,
            HvddStructure,
            HvddKiProcessorBlock,
            HvddMmMaximumPhysicalPage,
            HvddKPCR,
            HvddNumberOfCPU,
            HvddKDBGPa,
            HvddNumberOfRuns,
            HvddKernelBase,
            HvddMmPfnDatabase,
            HvddPsLoadedModuleList,
            HvddPsActiveProcessHead,
            HvddNtBuildNumber,
            HvddNtBuildNumberVA,
            HvddDirectoryTableBase,
            HvddRun,
            HvddKdbgDataBlockArea,
            HvddVmGuidString,
            HvddPartitionHandle,
            HvddKdbgContext,
            HvddKdVersionBlock,
            HvddMmPhysicalMemoryBlock,
            HvddNumberOfPages,
            HvddIdleKernelStack,
            HvddSizeOfKdDebuggerData,
            HvddCpuContextVa,
            HvddSize,
            HvddMemoryBlockCount,
            HvddSuspendedCores,
            HvddSuspendedWorker,
            HvddIsContainer,
            HvddIsNeedVmwpSuspend,
            HvddGuestOsType,
            HvddSettingsCrashDumpEmulation,
            HvddSettingsUseDecypheredKdbg,
            HvddBuilLabBuffer,
            HvddHvddGetCr3byPid,
            HvddGetProcessesIds,
            // Set-only information classes
            HvddSetMemoryBlock,
            HvddEnlVmcsPointer
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VM_OPERATIONS_CONFIG
        {
            public READ_MEMORY_METHOD ReadMethod;
            public WRITE_MEMORY_METHOD WriteMethod;
            public SUSPEND_RESUME_METHOD SuspendMethod;
            public UInt64 LogLevel;
            [MarshalAs(UnmanagedType.I1)] public bool ForceFreezeCPU;
            [MarshalAs(UnmanagedType.I1)] public bool PausePartition;
            [MarshalAs(UnmanagedType.I1)] public bool ReloadDriver;
            [MarshalAs(UnmanagedType.I1)] public bool VSMScan;
            [MarshalAs(UnmanagedType.I1)] public bool UseDebugApiStopProcess;
            [MarshalAs(UnmanagedType.I1)] public bool ScanGuestOsImages;
            [MarshalAs(UnmanagedType.I1)] public bool BruteGuestReg;
            [MarshalAs(UnmanagedType.I1)] public bool ListWinHvrInfo;
            [MarshalAs(UnmanagedType.I1)] public bool SafeMode;
            [MarshalAs(UnmanagedType.I1)] public bool SimpleMemory;
            [MarshalAs(UnmanagedType.I1)] public bool DotNetNamedPipeLog;
        }

        [DllImport("hvlib.dll")]
        private static extern bool SdkGetDefaultConfig(ref VM_OPERATIONS_CONFIG cfg);

        [DllImport("hvlib.dll")]
        private static extern IntPtr SdkEnumPartitions(ref UInt64 PartitionCount, ref VM_OPERATIONS_CONFIG cfg);

        [DllImport("hvlib.dll")]
        private static extern void SdkCloseAllPartitions();

        [DllImport("hvlib.dll")]
        private static extern void SdkClosePartition(UInt64 PartitionHandle);

        [DllImport("hvlib.dll")]
        private static extern bool SdkSelectPartition(UInt64 PartitionHandle);

        [DllImport("hvlib.dll")]
        private static extern bool SdkGetData(UInt64 PartitionHandle, HVDD_INFORMATION_CLASS HvddInformationClass, out UIntPtr HvddInformation);

        [DllImport("hvlib.dll")]
        public static extern bool SdkReadPhysicalMemory(UInt64 PartitionHandle, UInt64 StartPosition, int ReadByteCount, IntPtr ClientBuffer, READ_MEMORY_METHOD Method);


        [DllImport("hvlib.dll")]
        public static unsafe extern bool SdkReadPhysicalMemory(UInt64 PartitionHandle, UInt64 StartPosition, int ReadByteCount, IntPtr* ClientBuffer, READ_MEMORY_METHOD Method);


        [DllImport("Hvlib.dll", EntryPoint = "SdkReadPhysicalMemory")]
        public static extern bool SdkReadPhysicalMemoryULong(UInt64 PartitionHandle, UInt64 StartPosition, int ReadByteCount, UIntPtr ClientBuffer, READ_MEMORY_METHOD Method);

        [DllImport("Hvlib.dll", EntryPoint = "SdkReadPhysicalMemory")]
        public static extern bool SdkReadPhysicalMemoryByte(UInt64 PartitionHandle, UInt64 StartPosition, UInt64 ReadByteCount, byte[] ClientBuffer, READ_MEMORY_METHOD Method);

        [DllImport("hvlib.dll")]
        private static extern bool SdkWritePhysicalMemory(UInt64 PartitionHandle, UInt64 StartPosition, int WriteByteCount, IntPtr ClientBuffer, WRITE_MEMORY_METHOD Method);

        [DllImport("hvlib.dll")]
        private static extern bool SdkReadVirtualMemory(UInt64 PartitionHandle, UInt64 StartPosition, IntPtr ClientBuffer, int ReadByteCount);

        [DllImport("hvlib.dll")]
        private static extern bool SdkWriteVirtualMemory(UInt64 PartitionHandle, UInt64 StartPosition, UIntPtr ClientBuffer, UInt64 WriteByteCount);

        [DllImport("hvlib.dll")]
        public static extern UInt64 SdkGetData2(UInt64 PartitionHandle, HVDD_INFORMATION_CLASS HvddInformationClass);



        // Write a single guest virtual-processor register.
        [DllImport("hvlib.dll")]
        public static extern bool SdkWriteVpRegister(UInt64 PartitionHandle, int VpIndex, VTL_LEVEL InputVtl, HV_REGISTER_NAME RegisterCode, IntPtr RegisterValue);

        // Read a single guest virtual-processor register.
        [DllImport("hvlib.dll")]
        public static extern bool SdkReadVpRegister(UInt64 PartitionHandle, int VpIndex, VTL_LEVEL InputVtl, HV_REGISTER_NAME RegisterCode, IntPtr RegisterValue);


        // [DllImport("hvlib.dll")]
        private static UInt64 SdkGetCr3FromPid(UInt64 PartitionHandle, UInt64 Pid, GET_CR3_TYPE Type)
        {
            return 0;

        }

        public static bool GetPreferredSettings(ref VM_OPERATIONS_CONFIG cfg)
        {

            Boolean bResult = SdkGetDefaultConfig(ref cfg);

            return bResult;
        }

        public static UInt64 GetSdkData(UInt64 PartitionHandle, HVDD_INFORMATION_CLASS HvddInformationClass)
        {
            return SdkGetData2(PartitionHandle, HvddInformationClass);
        }

        public static void TestHvLib()
        {
            Console.Write("Hvlib is loaded");
        }

        public static UInt64 VmHandle = 0x100000;

        private static VM_OPERATIONS_CONFIG cfg;

        public static List<VmListBox> EnumAllPartitions()
        {
            Hvlib.VM_OPERATIONS_CONFIG cfg = new Hvlib.VM_OPERATIONS_CONFIG();

            bool bResult = Hvlib.GetPreferredSettings(ref cfg);
            cfg.DotNetNamedPipeLog = false;
            cfg.ReadMethod = Hvlib.READ_MEMORY_METHOD.ReadInterfaceHvmmDrvInternal;
            cfg.WriteMethod = Hvlib.WRITE_MEMORY_METHOD.WriteInterfaceWinHv;
            Hvlib.cfg = cfg;

            List<VmListBox> res = EnumPartitions(ref cfg);
            return res;
        }
        public static List<VmListBox> EnumPartitions(ref VM_OPERATIONS_CONFIG cfg)
        {
            UInt64 PartitionCount = 0;
            Int64[] arPartition;
            IntPtr Partitions = SdkEnumPartitions(ref PartitionCount, ref cfg);

            List<VmListBox> ListObj = new List<VmListBox>();

            if (PartitionCount != 0)
            {
                arPartition = new Int64[PartitionCount];
                Marshal.Copy((IntPtr)Partitions, arPartition, 0, (int)PartitionCount);
            }
            else
            {
                return null;
            }

            for (ulong i = 0; i < PartitionCount; i += 1)
            {

                VmListBox lbItem = new VmListBox();
                IntPtr VmName = (IntPtr)SdkGetData2((UInt64)arPartition[i], HVDD_INFORMATION_CLASS.HvddPartitionFriendlyName);
                string VmNameStr = Marshal.PtrToStringUni(VmName);

                IntPtr VmGuid = (IntPtr)SdkGetData2((UInt64)arPartition[i], HVDD_INFORMATION_CLASS.HvddVmGuidString);
                string VmGuidStr = Marshal.PtrToStringUni(VmGuid);

                IntPtr VmType = (IntPtr)SdkGetData2((UInt64)arPartition[i], HVDD_INFORMATION_CLASS.HvddVmtypeString);
                string VmTypeStr = Marshal.PtrToStringUni(VmType);

                UInt64 PartitionId = SdkGetData2((UInt64)arPartition[i], HVDD_INFORMATION_CLASS.HvddPartitionId);

                lbItem.VmHandle = (UInt64)arPartition[i];
                lbItem.VMName = VmNameStr;

                ListObj.Add(lbItem);

            }

            return ListObj;
        }

        public static UInt64 GetData2(UInt64 VmHandle, HVDD_INFORMATION_CLASS InformationClass)
        {
            UInt64 uResult = Hvlib.SdkGetData2(VmHandle, InformationClass);
            return uResult;
        }

        public static bool GetData(UInt64 VmHandle, HVDD_INFORMATION_CLASS InformationClass, out UIntPtr HvddInformation)
        {
            bool bResult = Hvlib.SdkGetData(VmHandle, InformationClass, out HvddInformation);
            return bResult;
        }

        public static IntPtr[] GetProcessesList(UInt64 PartitionHandle)
        {

            IntPtr[] arPartition = new IntPtr[1];
            IntPtr aProcessList = (IntPtr)Hvlib.SdkGetData2(PartitionHandle, HVDD_INFORMATION_CLASS.HvddGetProcessesIds);
            Marshal.Copy(aProcessList, arPartition, 0, (int)1);
            IntPtr[] arProcess;

            UInt64 ProcessCount = (UInt64)arPartition[0];

            if (ProcessCount > 0)
            {
                arProcess = new IntPtr[ProcessCount + 1];
                Marshal.Copy(aProcessList, arProcess, 1, (int)ProcessCount);
            }
            else
            {
#pragma warning disable CS8603 // Possible null reference return.
                return null;
#pragma warning restore CS8603 // Possible null reference return.
            }

            return arProcess;
        }

        public static UInt64 GetCr3(UInt64 PartitionHandle, UInt64 Pid)
        {
            UInt64 Cr3 = 0;

            if (Pid == 0xFFFFFFFF)
                Cr3 = SdkGetCr3FromPid(PartitionHandle, Pid, GET_CR3_TYPE.Cr3Hypervisor);
            else if (Pid == 0xFFFFFFFE)
                Cr3 = SdkGetCr3FromPid(PartitionHandle, Pid, GET_CR3_TYPE.Cr3Kernel);
            else
                Cr3 = SdkGetCr3FromPid(PartitionHandle, Pid, GET_CR3_TYPE.Cr3Process);

            return Cr3;
        }
        public static bool SelectPartition(UInt64 Handle)
        {
            return SdkSelectPartition(Handle);
        }

        public static bool ReadPhysicalMemory(UInt64 PartitionHandle, UInt64 StartPosition, int ReadByteCount, IntPtr ClientBuffer)
        {
            return SdkReadPhysicalMemory(PartitionHandle, StartPosition, ReadByteCount, ClientBuffer, Hvlib.cfg.ReadMethod);
        }


        /*
        private static IntPtr ClientBuffer = Marshal.AllocHGlobal(0x1000);

        public static IntPtr ReadPhysicalMemoryPtr(UInt64 PartitionHandle, UInt64 StartPosition)

        {
            int ReadByteCount = 8;

            Marshal.WriteIntPtr(ClientBuffer, IntPtr.Zero);
            ReadPhysicalMemory(PartitionHandle, StartPosition, ReadByteCount, ClientBuffer);

            return Marshal.ReadIntPtr(ClientBuffer);
        }*/


        // private static IntPtr ClientBuffer = Marshal.AllocHGlobal(0x1000);

        public static unsafe IntPtr ReadPhysicalMemoryPtr(UInt64 PartitionHandle, UInt64 StartPosition)
        {
            UInt64 StartPositionPage = (StartPosition & 0xFFFFFFFFFF000);
            IntPtr* ClientBuffer1 = stackalloc IntPtr[0x200];
            int ReadByteCount = 0x1000;
            SdkReadPhysicalMemory(PartitionHandle, StartPositionPage, ReadByteCount, ClientBuffer1, Hvlib.cfg.ReadMethod);
            int idx = (int)((StartPosition & 0xFF8) >> 3);
            IntPtr ret = ClientBuffer1[idx];
            return ret;
        }

        public static bool ReadPhysicalMemory(UInt64 PartitionHandle, UInt64 StartPosition, UInt64 ReadByteCount, byte[] ClientBuffer)
        {
            return SdkReadPhysicalMemoryByte(PartitionHandle, StartPosition, ReadByteCount, ClientBuffer, Hvlib.cfg.ReadMethod);
        }

        public static bool ReadPhysicalMemory(UInt64 PartitionHandle, UInt64 StartPosition, int ReadByteCount, UIntPtr ClientBuffer)
        {
            return SdkReadPhysicalMemoryULong(PartitionHandle, StartPosition, ReadByteCount, ClientBuffer, Hvlib.cfg.ReadMethod);
        }

        public static bool WritePhysicalMemory(UInt64 PartitionHandle, UInt64 StartPosition, int WriteByteCount, IntPtr ClientBuffer)
        {
            return SdkWritePhysicalMemory(PartitionHandle, StartPosition, WriteByteCount, ClientBuffer, WRITE_MEMORY_METHOD.WriteInterfaceHvmmDrvInternal);
        }

        public static bool ReadVirtualMemory(UInt64 PartitionHandle, UInt64 StartPosition, int ReadByteCount, IntPtr ClientBuffer)
        {
            return SdkReadVirtualMemory(PartitionHandle, StartPosition, ClientBuffer, ReadByteCount);
        }

        public static bool WriteVirtualMemory(UInt64 PartitionHandle, UInt64 StartPosition, UInt64 WriteByteCount, UIntPtr ClientBuffer)
        {
            return SdkWriteVirtualMemory(PartitionHandle, StartPosition, ClientBuffer, WriteByteCount);
        }

        public static void CloseAllPartitions()
        {
            SdkCloseAllPartitions();
        }

        public static void ClosePartition(UInt64 PartitionHandle)
        {
            SdkClosePartition(PartitionHandle);
        }
    }
}