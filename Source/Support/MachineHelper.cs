using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                var numberOfSections = brReader.ReadUInt16(); // Number of Sections => 2
                var timeDateStamp = brReader.ReadUInt32(); // TimeDateStamp
                                                           // 1,648,643,508
                                                           // 3,385,172,282
                                                           // Timestamp is a date offset from 1970
                DateTime returnValue = new DateTime(1970, 1, 1, 0, 0, 0);
                // Add in the number of seconds since 1970/1/1
                returnValue = returnValue.AddSeconds(timeDateStamp);
                // Adjust to local timezone
                returnValue += TimeZone.CurrentTimeZone.GetUtcOffset(returnValue);

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

}

// https://gist.github.com/augustoproiete/b51f29f74f5f5b2c59c39e47a8afc3a3
/*
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