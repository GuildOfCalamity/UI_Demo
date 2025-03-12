using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI_Demo;

public enum MachineType : ushort
{
    IMAGE_FILE_NOT_FOUND = 0xff,
    IMAGE_FILE_MACHINE_UNKNOWN = 0x00,
    IMAGE_FILE_MACHINE_AM33 = 0x1d3,
    IMAGE_FILE_MACHINE_AMD64 = 0x8664,
    IMAGE_FILE_MACHINE_ARM = 0x1c0,
    IMAGE_FILE_MACHINE_EBC = 0xebc,
    IMAGE_FILE_MACHINE_I386 = 0x14c,
    IMAGE_FILE_MACHINE_IA64 = 0x200,
    IMAGE_FILE_MACHINE_M32R = 0x9041,
    IMAGE_FILE_MACHINE_MIPS16 = 0x266,
    IMAGE_FILE_MACHINE_MIPSFPU = 0x366,
    IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,
    IMAGE_FILE_MACHINE_POWERPC = 0x1f0,
    IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,
    IMAGE_FILE_MACHINE_R4000 = 0x166,
    IMAGE_FILE_MACHINE_SH3 = 0x1a2,
    IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,
    IMAGE_FILE_MACHINE_SH4 = 0x1a6,
    IMAGE_FILE_MACHINE_SH5 = 0x1a8,
    IMAGE_FILE_MACHINE_THUMB = 0x1c2,
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
                // Read the value at offset byte 60 ("e_lfanew" at offset 0x3C in the DOS header.)
                fsStream.Seek(0x3c, SeekOrigin.Begin);
                Int32 peOffset = brReader.ReadInt32();

                // Use that value to read the next byte.
                fsStream.Seek(peOffset, SeekOrigin.Begin);
                UInt32 peHead = brReader.ReadUInt32();

                if (peHead != 0x00004550)
                    throw new Exception($"Could not locate find PE header for {dllPath}");

                mt = (MachineType)brReader.ReadUInt16();
            }
        }

        return mt;
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
