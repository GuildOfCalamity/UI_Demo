using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static UI_Demo.PeHeader;

namespace UI_Demo;

public enum MachineType : ushort
{
    IMAGE_FILE_NOT_FOUND         = 0xff,
    IMAGE_FILE_MACHINE_UNKNOWN   = 0x00,
    IMAGE_FILE_MACHINE_AM33      = 0x1d3,
    IMAGE_FILE_MACHINE_AMD64     = 0x8664,
    IMAGE_FILE_MACHINE_ARM       = 0x1c0,
    IMAGE_FILE_MACHINE_EBC       = 0xebc,
    IMAGE_FILE_MACHINE_I386      = 0x14c,
    IMAGE_FILE_MACHINE_IA64      = 0x200,
    IMAGE_FILE_MACHINE_M32R      = 0x9041,
    IMAGE_FILE_MACHINE_MIPS16    = 0x266,
    IMAGE_FILE_MACHINE_MIPSFPU   = 0x366,
    IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,
    IMAGE_FILE_MACHINE_POWERPC   = 0x1f0,
    IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,
    IMAGE_FILE_MACHINE_R4000     = 0x166,
    IMAGE_FILE_MACHINE_SH3       = 0x1a2,
    IMAGE_FILE_MACHINE_SH3DSP    = 0x1a3,
    IMAGE_FILE_MACHINE_SH4       = 0x1a6,
    IMAGE_FILE_MACHINE_SH5       = 0x1a8,
    IMAGE_FILE_MACHINE_THUMB     = 0x1c2,
    IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169,
}

public class MachineHelper
{
    /// <summary>
    /// Finding Machine Types in .NET with PE Headers
    /// https://www.codeguru.com/dotnet/machine-types-dot-net/
    /// A note on IMAGE_FILE Headers: the IMAGE_NT_HEADERS structure is the primary location where 
    /// specifics of the Portable Executables (PE) file are stored. There are two versions of the 
    /// IMAGE_NT_HEADER structure – 32-bit and 64-bit executables.
    /// </summary>
    /// <param name="dllPath">the DLL to sample</param>
    /// <returns><see cref="MachineType"/></returns>
    public static MachineType GetDllMachineType(string dllPath)
    {
        MachineType mt = MachineType.IMAGE_FILE_MACHINE_UNKNOWN;

        if (!File.Exists(dllPath))
            return MachineType.IMAGE_FILE_NOT_FOUND;

        using (FileStream fsStream = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
        {
            using (BinaryReader brReader = new BinaryReader(fsStream))
            {
                // Read the value at offset byte 60 ("e_lfanew" at offset 0x3C in the DOS header)
                fsStream.Seek(0x3c, SeekOrigin.Begin);
                Int32 peOffset = brReader.ReadInt32();

                // Use that value to read 4 bytes.
                fsStream.Seek(peOffset, SeekOrigin.Begin);
                UInt32 peHead = brReader.ReadUInt32();

                if (peHead != 0x00004550) // DWORD start of PE header - Signature ($00004550) Decimal=17744
                    throw new Exception($"Could not locate find PE header for {dllPath}");

                var machine = brReader.ReadUInt16(); // Machine => 34404 0x8664
                //var numberOfSections = brReader.ReadUInt16();
                //var timeDateStamp = brReader.ReadUInt32();

                mt = (MachineType)machine;
            }
        }

        return mt;
    }

    public static DateTime ConvertUInt32ToDateTime(uint timestamp = 1739996400)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
    }

    public static bool? DllIs64Bit(string dllPath)
    {
        switch (GetDllMachineType(dllPath))
        {
            case MachineType.IMAGE_FILE_MACHINE_AMD64:
            case MachineType.IMAGE_FILE_MACHINE_IA64:
                return true;
            case MachineType.IMAGE_FILE_MACHINE_I386:
                return false;
            default:
                return null;
        }
    }

    public static DateTime GetDllTimeStamp() => PeHeader.GetCallingAssemblyHeader().TimeStamp;
}

/// <summary>
/// Reads in the header information of the Portable Executable format.
/// Provides information such as the date the assembly was compiled.
/// </summary>
/// <remarks>
/// Portions of this code are from https://gist.github.com/augustoproiete/b51f29f74f5f5b2c59c39e47a8afc3a3
/// </remarks>
public class PeHeader
{
    #region [File Header Structures]
    public struct IMAGE_DOS_HEADER
    {   // DOS .EXE header
        public UInt16 e_magic;       // Magic number (0x5A4D)
        public UInt16 e_cblp;        // Bytes on last page of file
        public UInt16 e_cp;          // Pages in file
        public UInt16 e_crlc;        // Relocations
        public UInt16 e_cparhdr;     // Size of header in paragraphs
        public UInt16 e_minalloc;    // Minimum extra paragraphs needed
        public UInt16 e_maxalloc;    // Maximum extra paragraphs needed
        public UInt16 e_ss;          // Initial (relative) SS value
        public UInt16 e_sp;          // Initial SP value
        public UInt16 e_csum;        // Checksum
        public UInt16 e_ip;          // Initial IP value
        public UInt16 e_cs;          // Initial (relative) CS value
        public UInt16 e_lfarlc;      // File address of relocation table
        public UInt16 e_ovno;        // Overlay number
        public UInt16 e_res_0;       // Reserved words
        public UInt16 e_res_1;       // Reserved words
        public UInt16 e_res_2;       // Reserved words
        public UInt16 e_res_3;       // Reserved words
        public UInt16 e_oemid;       // OEM identifier (for e_oeminfo)
        public UInt16 e_oeminfo;     // OEM information; e_oemid specific
        public UInt16 e_res2_0;      // Reserved words
        public UInt16 e_res2_1;      // Reserved words
        public UInt16 e_res2_2;      // Reserved words
        public UInt16 e_res2_3;      // Reserved words
        public UInt16 e_res2_4;      // Reserved words
        public UInt16 e_res2_5;      // Reserved words
        public UInt16 e_res2_6;      // Reserved words
        public UInt16 e_res2_7;      // Reserved words
        public UInt16 e_res2_8;      // Reserved words
        public UInt16 e_res2_9;      // Reserved words
        public UInt32 e_lfanew;      // File address of new exe header
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_DATA_DIRECTORY
    {
        public UInt32 VirtualAddress;
        public UInt32 Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_OPTIONAL_HEADER32
    {
        public UInt16 Magic;
        public Byte MajorLinkerVersion;
        public Byte MinorLinkerVersion;
        public UInt32 SizeOfCode;
        public UInt32 SizeOfInitializedData;
        public UInt32 SizeOfUninitializedData;
        public UInt32 AddressOfEntryPoint;
        public UInt32 BaseOfCode;
        public UInt32 BaseOfData;
        public UInt32 ImageBase;
        public UInt32 SectionAlignment;
        public UInt32 FileAlignment;
        public UInt16 MajorOperatingSystemVersion;
        public UInt16 MinorOperatingSystemVersion;
        public UInt16 MajorImageVersion;
        public UInt16 MinorImageVersion;
        public UInt16 MajorSubsystemVersion;
        public UInt16 MinorSubsystemVersion;
        public UInt32 Win32VersionValue;
        public UInt32 SizeOfImage;
        public UInt32 SizeOfHeaders;
        public UInt32 CheckSum;
        public UInt16 Subsystem;
        public UInt16 DllCharacteristics;
        public UInt32 SizeOfStackReserve;
        public UInt32 SizeOfStackCommit;
        public UInt32 SizeOfHeapReserve;
        public UInt32 SizeOfHeapCommit;
        public UInt32 LoaderFlags;
        public UInt32 NumberOfRvaAndSizes;

        public IMAGE_DATA_DIRECTORY ExportTable;
        public IMAGE_DATA_DIRECTORY ImportTable;
        public IMAGE_DATA_DIRECTORY ResourceTable;
        public IMAGE_DATA_DIRECTORY ExceptionTable;
        public IMAGE_DATA_DIRECTORY CertificateTable;
        public IMAGE_DATA_DIRECTORY BaseRelocationTable;
        public IMAGE_DATA_DIRECTORY Debug;
        public IMAGE_DATA_DIRECTORY Architecture;
        public IMAGE_DATA_DIRECTORY GlobalPtr;
        public IMAGE_DATA_DIRECTORY TLSTable;
        public IMAGE_DATA_DIRECTORY LoadConfigTable;
        public IMAGE_DATA_DIRECTORY BoundImport;
        public IMAGE_DATA_DIRECTORY IAT;
        public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
        public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
        public IMAGE_DATA_DIRECTORY Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        public UInt16 Magic;
        public Byte MajorLinkerVersion;
        public Byte MinorLinkerVersion;
        public UInt32 SizeOfCode;
        public UInt32 SizeOfInitializedData;
        public UInt32 SizeOfUninitializedData;
        public UInt32 AddressOfEntryPoint;
        public UInt32 BaseOfCode;
        public UInt64 ImageBase;
        public UInt32 SectionAlignment;
        public UInt32 FileAlignment;
        public UInt16 MajorOperatingSystemVersion;
        public UInt16 MinorOperatingSystemVersion;
        public UInt16 MajorImageVersion;
        public UInt16 MinorImageVersion;
        public UInt16 MajorSubsystemVersion;
        public UInt16 MinorSubsystemVersion;
        public UInt32 Win32VersionValue;
        public UInt32 SizeOfImage;
        public UInt32 SizeOfHeaders;
        public UInt32 CheckSum;
        public UInt16 Subsystem;
        public UInt16 DllCharacteristics;
        public UInt64 SizeOfStackReserve;
        public UInt64 SizeOfStackCommit;
        public UInt64 SizeOfHeapReserve;
        public UInt64 SizeOfHeapCommit;
        public UInt32 LoaderFlags;
        public UInt32 NumberOfRvaAndSizes;

        public IMAGE_DATA_DIRECTORY ExportTable;
        public IMAGE_DATA_DIRECTORY ImportTable;
        public IMAGE_DATA_DIRECTORY ResourceTable;
        public IMAGE_DATA_DIRECTORY ExceptionTable;
        public IMAGE_DATA_DIRECTORY CertificateTable;
        public IMAGE_DATA_DIRECTORY BaseRelocationTable;
        public IMAGE_DATA_DIRECTORY Debug;
        public IMAGE_DATA_DIRECTORY Architecture;
        public IMAGE_DATA_DIRECTORY GlobalPtr;
        public IMAGE_DATA_DIRECTORY TLSTable;
        public IMAGE_DATA_DIRECTORY LoadConfigTable;
        public IMAGE_DATA_DIRECTORY BoundImport;
        public IMAGE_DATA_DIRECTORY IAT;
        public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
        public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
        public IMAGE_DATA_DIRECTORY Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_FILE_HEADER
    {
        public UInt16 Machine;
        public UInt16 NumberOfSections;
        public UInt32 TimeDateStamp;
        public UInt32 PointerToSymbolTable;
        public UInt32 NumberOfSymbols;
        public UInt16 SizeOfOptionalHeader;
        public UInt16 Characteristics;
    }

    // Grabbed the following 2 definitions from http://www.pinvoke.net/default.aspx/Structures/IMAGE_SECTION_HEADER.html
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_SECTION_HEADER
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] Name;
        [FieldOffset(8)]
        public UInt32 VirtualSize;
        [FieldOffset(12)]
        public UInt32 VirtualAddress;
        [FieldOffset(16)]
        public UInt32 SizeOfRawData;
        [FieldOffset(20)]
        public UInt32 PointerToRawData;
        [FieldOffset(24)]
        public UInt32 PointerToRelocations;
        [FieldOffset(28)]
        public UInt32 PointerToLinenumbers;
        [FieldOffset(32)]
        public UInt16 NumberOfRelocations;
        [FieldOffset(34)]
        public UInt16 NumberOfLinenumbers;
        [FieldOffset(36)]
        public DataSectionFlags Characteristics;

        public string Section
        {
            get { return new string(Name); }
        }
    }

    [Flags]
    public enum DataSectionFlags : uint
    {
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeReg = 0x00000000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeDsect = 0x00000001,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeNoLoad = 0x00000002,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeGroup = 0x00000004,
        /// <summary>
        /// The section should not be padded to the next boundary. This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES. This is valid only for object files.
        /// </summary>
        TypeNoPadded = 0x00000008,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeCopy = 0x00000010,
        /// <summary>
        /// The section contains executable code.
        /// </summary>
        ContentCode = 0x00000020,
        /// <summary>
        /// The section contains initialized data.
        /// </summary>
        ContentInitializedData = 0x00000040,
        /// <summary>
        /// The section contains uninitialized data.
        /// </summary>
        ContentUninitializedData = 0x00000080,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        LinkOther = 0x00000100,
        /// <summary>
        /// The section contains comments or other information. The .drectve section has this type. This is valid for object files only.
        /// </summary>
        LinkInfo = 0x00000200,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeOver = 0x00000400,
        /// <summary>
        /// The section will not become part of the image. This is valid only for object files.
        /// </summary>
        LinkRemove = 0x00000800,
        /// <summary>
        /// The section contains COMDAT data. For more information, see section 5.5.6, COMDAT Sections (Object Only). This is valid only for object files.
        /// </summary>
        LinkComDat = 0x00001000,
        /// <summary>
        /// Reset speculative exceptions handling bits in the TLB entries for this section.
        /// </summary>
        NoDeferSpecExceptions = 0x00004000,
        /// <summary>
        /// The section contains data referenced through the global pointer (GP).
        /// </summary>
        RelativeGP = 0x00008000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemPurgeable = 0x00020000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Memory16Bit = 0x00020000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemoryLocked = 0x00040000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemoryPreload = 0x00080000,
        /// <summary>
        /// Align data on a 1-byte boundary. Valid only for object files.
        /// </summary>
        Align1Bytes = 0x00100000,
        /// <summary>
        /// Align data on a 2-byte boundary. Valid only for object files.
        /// </summary>
        Align2Bytes = 0x00200000,
        /// <summary>
        /// Align data on a 4-byte boundary. Valid only for object files.
        /// </summary>
        Align4Bytes = 0x00300000,
        /// <summary>
        /// Align data on an 8-byte boundary. Valid only for object files.
        /// </summary>
        Align8Bytes = 0x00400000,
        /// <summary>
        /// Align data on a 16-byte boundary. Valid only for object files.
        /// </summary>
        Align16Bytes = 0x00500000,
        /// <summary>
        /// Align data on a 32-byte boundary. Valid only for object files.
        /// </summary>
        Align32Bytes = 0x00600000,
        /// <summary>
        /// Align data on a 64-byte boundary. Valid only for object files.
        /// </summary>
        Align64Bytes = 0x00700000,
        /// <summary>
        /// Align data on a 128-byte boundary. Valid only for object files.
        /// </summary>
        Align128Bytes = 0x00800000,
        /// <summary>
        /// Align data on a 256-byte boundary. Valid only for object files.
        /// </summary>
        Align256Bytes = 0x00900000,
        /// <summary>
        /// Align data on a 512-byte boundary. Valid only for object files.
        /// </summary>
        Align512Bytes = 0x00A00000,
        /// <summary>
        /// Align data on a 1024-byte boundary. Valid only for object files.
        /// </summary>
        Align1024Bytes = 0x00B00000,
        /// <summary>
        /// Align data on a 2048-byte boundary. Valid only for object files.
        /// </summary>
        Align2048Bytes = 0x00C00000,
        /// <summary>
        /// Align data on a 4096-byte boundary. Valid only for object files.
        /// </summary>
        Align4096Bytes = 0x00D00000,
        /// <summary>
        /// Align data on an 8192-byte boundary. Valid only for object files.
        /// </summary>
        Align8192Bytes = 0x00E00000,
        /// <summary>
        /// The section contains extended relocations.
        /// </summary>
        LinkExtendedRelocationOverflow = 0x01000000,
        /// <summary>
        /// The section can be discarded as needed.
        /// </summary>
        MemoryDiscardable = 0x02000000,
        /// <summary>
        /// The section cannot be cached.
        /// </summary>
        MemoryNotCached = 0x04000000,
        /// <summary>
        /// The section is not pageable.
        /// </summary>
        MemoryNotPaged = 0x08000000,
        /// <summary>
        /// The section can be shared in memory.
        /// </summary>
        MemoryShared = 0x10000000,
        /// <summary>
        /// The section can be executed as code.
        /// </summary>
        MemoryExecute = 0x20000000,
        /// <summary>
        /// The section can be read.
        /// </summary>
        MemoryRead = 0x40000000,
        /// <summary>
        /// The section can be written to.
        /// </summary>
        MemoryWrite = 0x80000000
    }
    #endregion

    #region [Private Fields]
    /// <summary>
    /// The DOS header
    /// </summary>
    private IMAGE_DOS_HEADER dosHeader;
    /// <summary>
    /// The file header
    /// </summary>
    private IMAGE_FILE_HEADER fileHeader;
    /// <summary>
    /// Optional 32 bit file header 
    /// </summary>
    private IMAGE_OPTIONAL_HEADER32 optionalHeader32;
    /// <summary>
    /// Optional 64 bit file header 
    /// </summary>
    private IMAGE_OPTIONAL_HEADER64 optionalHeader64;
    /// <summary>
    /// Image Section headers. Number of sections is in the file header.
    /// </summary>
    private IMAGE_SECTION_HEADER[] imageSectionHeaders;
    #endregion

    #region [Props]
    /// <summary>
    /// Gets if the file header is 32 bit or not
    /// </summary>
    public bool Is32BitHeader
    {
        get
        {
            UInt16 IMAGE_FILE_32BIT_MACHINE = 0x0100;
            return (IMAGE_FILE_32BIT_MACHINE & FileHeader.Characteristics) == IMAGE_FILE_32BIT_MACHINE;
        }
    }

    /// <summary>
    /// Gets the file header
    /// </summary>
    public IMAGE_FILE_HEADER FileHeader
    {
        get => fileHeader;
    }

    /// <summary>
    /// Gets the optional header
    /// </summary>
    public IMAGE_OPTIONAL_HEADER32 OptionalHeader32
    {
        get => optionalHeader32;
    }

    /// <summary>
    /// Gets the optional header
    /// </summary>
    public IMAGE_OPTIONAL_HEADER64 OptionalHeader64
    {
        get => optionalHeader64;
    }

    public IMAGE_SECTION_HEADER[] ImageSectionHeaders
    {
        get => imageSectionHeaders;
    }

    /// <summary>
    /// Gets the TimeStamp from the file header starting at January 1st 1970 and adjusts by <see cref="TimeZoneInfo.Local"/>.
    /// </summary>
    public DateTime TimeStamp
    {
        get
        {
            // TimeStamp is a date offset from 1970
            DateTime returnValue = new DateTime(1970, 1, 1, 0, 0, 0);
            // Add in the number of seconds since January 1st 1970
            returnValue = returnValue.AddSeconds(fileHeader.TimeDateStamp);
            // Adjust to local timezone
            //returnValue += TimeZone.CurrentTimeZone.GetUtcOffset(returnValue);
            returnValue += TimeZoneInfo.Local.GetUtcOffset(returnValue);
            return returnValue;
        }
    }

    /// <summary>
    /// Gets the TimeStamp from the file header using the <see cref="DateTimeOffset.FromUnixTimeSeconds"/>.
    /// </summary>
    public DateTime TimeStampUnix
    {
        get => DateTimeOffset.FromUnixTimeSeconds(fileHeader.TimeDateStamp).UtcDateTime;
    }
    #endregion

    #region [Public Methods]
    /// <summary>
    /// Main constructor
    /// </summary>
    /// <param name="filePath">path to dll or exe</param>
    public PeHeader(string filePath)
    {
        // Read in the DLL or EXE and get the timestamp
        using (FileStream stream = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
        {
            BinaryReader reader = new BinaryReader(stream);
            dosHeader = FromBinaryReader<IMAGE_DOS_HEADER>(reader);

            // Add 4 bytes to the offset
            stream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);

            UInt32 ntHeadersSignature = reader.ReadUInt32();
            fileHeader = FromBinaryReader<IMAGE_FILE_HEADER>(reader);
            if (this.Is32BitHeader)
                optionalHeader32 = FromBinaryReader<IMAGE_OPTIONAL_HEADER32>(reader);
            else
                optionalHeader64 = FromBinaryReader<IMAGE_OPTIONAL_HEADER64>(reader);

            imageSectionHeaders = new IMAGE_SECTION_HEADER[fileHeader.NumberOfSections];
            for (int headerNo = 0; headerNo < imageSectionHeaders.Length; ++headerNo)
            {
                imageSectionHeaders[headerNo] = FromBinaryReader<IMAGE_SECTION_HEADER>(reader);
            }

        }
    }

    /// <summary>
    /// Gets the header of the .NET assembly that called this function
    /// </summary>
    /// <returns></returns>
    public static PeHeader? GetCallingAssemblyHeader()
    {
        // Get the path to the calling assembly, which is the path to the
        // DLL or EXE that we want the time of
        string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;

        if (!string.IsNullOrEmpty(filePath))
            return new PeHeader(filePath);

        return null;
    }

    /// <summary>
    /// Gets the header of the .NET assembly that called this function
    /// </summary>
    /// <returns></returns>
    public static PeHeader? GetAssemblyHeader()
    {
        // Get the path to the calling assembly, which is the path to the
        // DLL or EXE that we want the time of
        string filePath = System.Reflection.Assembly.GetAssembly(typeof(PeHeader))?.Location ?? "";

        if (!string.IsNullOrEmpty(filePath))
            return new PeHeader(filePath);

        return null;
    }

    /// <summary>
    /// Reads in a block from a file and converts it to the struct
    /// type specified by the template parameter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader"></param>
    /// <returns></returns>
    public static T? FromBinaryReader<T>(BinaryReader reader)
    {
        // Read in a byte array
        byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

        // Pin the managed memory while copying it to data then unpin it.
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        T? theStructure = (T?)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();

        return theStructure;
    }
    #endregion
}

/* https://www.sunshine2k.de/reversing/tuts/tut_pe.htm
DOS MZ Header:
+00        WORD                 e_magic     Magic Number MZ (0x5A4D)
+02        WORD                 e_cblp      Bytes on last page of file
+04        WORD                 e_cp        Pages in file
+06        WORD                 e_crlc      Relocations
+08        WORD                 e_cparhdr   Size of header in paragraphs
+0A (10)   WORD                 e_minalloc  Minimum extra paragraphs needed
+0C (12)   WORD                 e_maxalloc  Maximum extra paragraphs needed
+0E (14)   WORD                 e_ss        Initial (relative) SS value
+10 (16)   WORD                 e_sp        Initial SP value
+12 (18)   WORD                 e_csum      Checksum
+14 (20)   WORD                 e_ip        Initial IP value
+16 (22)   WORD                 e_cs        Initial (relative) CS value
+18 (24)   WORD                 e_lfarlc    File address of relocation table
+1A (26)   WORD                 e_ovno      Overlay number
+1C (28)   Array[4] of WORD     e_res       Reserved words
+24 (36)   WORD                 e_oemid     OEM identifier (for e_oeminfo)
+26 (28)   WORD                 e_oeminfo   OEM information; e_oemid specific
+28 (40)   Array[10] of WORD    e_res2      Reserved words
+3C (60)   DWORD                e_lfanew    File address of new exe header

PE Header:
+00        DWORD   Signature (offset 0x00004550)
+04        WORD    Machine
+06        WORD    Number of Sections
+08        DWORD   TimeDateStamp
+0C (12)   DWORD   PointerToSymbolTable
+10 (16)   DWORD   NumberOfSymbols
+14 (20)   WORD    SizeOfOptionalHeader
+16 (22)   WORD    Characteristics

Optional Header:
+18 (24)   WORD    Magic
+1A (26)   BYTE    MajorLinkerVersion
+1B (27)   BYTE    MinorLinkerVersion
+1C (28)   DWORD   SizeOfCode
+20 (32)   DWORD   SizeOfInitializedData
+24 (36)   DWORD   SizeOfUnitializedData
+28 (40)   DWORD   AddressOfEntryPoint
+2C (44)   DWORD   BaseOfCode
+30 (48)   DWORD   BaseOfData
+34 (52)   DWORD   ImageBase
+38 (56)   DWORD   SectionAlignment
+3C (60)   DWORD   FileAlignment
+40 (64)   WORD    MajorOperatingSystemVersion
+42 (66)   WORD    MinorOperatingSystemVersion
+44 (68)   WORD    MajorImageVersion
+46 (70)   WORD    MinorImageVersion
+48 (72)   WORD    MajorSubsystemVersion
+4A (74)   WORD    MinorSubsystemVersion
+4C (76)   DWORD   Reserved1
+50 (80)   DWORD   SizeOfImage
+54 (84)   DWORD   SizeOfHeaders
+58 (88)   DWORD   CheckSum
+5C (92)   WORD    Subsystem
+5E (94)   WORD    DllCharacteristics
+60 (96)   DWORD   SizeOfStackReserve
+64 (100)  DWORD   SizeOfStackCommit
+68 (104)  DWORD   SizeOFHeapReserve
+6C (108)  DWORD   SizeOfHeapCommit
+70 (112)  DWORD   LoaderFlags
+74 (116)  DWORD   NumberOfRvaAndSizes
+78 (120)  DWORD   ExportDirectory VA
+7C (124)  DWORD   ExportDirectory Size
+80 (128)  DWORD   ImportDirectory VA
+84 (132)  DWORD   ImportDirectory Size
+88 (136)  DWORD   ResourceDirectory VA
+8C (140)  DWORD   ResourceDirectory Size
+90 (144)  DWORD   ExceptionDirectory VA
+94 (148)  DWORD   ExceptionDirectory Size
+98 (152)  DWORD   SecurityDirectory VA
+9C (156)  DWORD   SecurityDirectory Size
+A0 (160)  DWORD   BaseRelocationTable VA
+A4 (164)  DWORD   BaseRelocationTable Size
+A8 (168)  DWORD   DebugDirectory VA
+AC (172)  DWORD   DebugDirectory Size
+B0 (176)  DWORD   ArchitectureSpecificData VA
+B4 (180)  DWORD   ArchitectureSpecificData Size
+B8 (184)  DWORD   RVAofGP VA
+BC (188)  DWORD   RVAofGP Size
+C0 (192)  DWORD   TLSDirectory VA
+C4 (196)  DWORD   TLSDirectory Size
+C8 (200)  DWORD   LoadConfigurationDirectory VA
+CC (204)  DWORD   LoadConfigurationDirectory Size
+D0 (208)  DWORD   BoundImportDirectoryinheaders VA
+D4 (212)  DWORD   BoundImportDirectoryinheaders Size
+D8 (216)  DWORD   ImportAddressTable VA
+DC (220)  DWORD   ImportAddressTable Size
+E0 (224)  DWORD   DelayLoadImportDescriptors VA
+E4 (228)  DWORD   DelayLoadImportDescriptors Size
+E8 (232)  DWORD   COMRuntimedescriptor VA
+EC (236)  DWORD   COMRuntimedescriptor Size
+F0 (240)  DWORD   0
+F4 (244)  DWORD   0

Section Header:
The first section starts immediately after the optional header (+F8 in the PE Header). 
The second header comes after the first header and so on. How many sections there are 
gives us the value NumberOfSections at offset 06 in the PE Header.
+0 Array[8] of BYTE Name
+08        DWORD   PhysicalAddress / Virtual Size
+0C        DWORD   VirtualAddress
+10  (16)  DWORD   SizeOfRawData
+14  (20)  DWORD   PointerToRawData
+18  (24)  DWORD   PointerToRelocations
+1C  (28)  DWORD   PointerToLineNumbers
+20  (32)  WORD    NumberOfRelocations
+22  (34)  WORD    NumberOfLineNumbers
+24  (36)  DWORD   Characteristics

Export Directory:
+0         DWORD   Characteristics
+04        DWORD   TimeDateStamp
+08        WORD    MajorVersion
+0A        WORD    MinorVersion
+0C        DWORD   Name
+10 (16)   DWORD   Base
+14 (20)   DWORD   NumberOfFunctions
+18 (24)   DWORD   NumberOfNames
+1C (28)   DWORD   *AddressOfFunctions
+20 (32)   DWORD   *AddressOfNames
+24 (36)   DWORD   *AddressOfNameOrdinals

Import Directory:
+0         DWORD   OriginalFirstThunk
+04        DWORD   TimeDateStamp
+08        DWORD   ForwarderChain
+0C        DWORD   Name
+10        DWORD   FirstThunk
*/