using System;
using System.IO;

using WtTools.Formats.Extensions;

namespace WtTools.Formats.Vromfs;

internal class Header
{
    internal string Magic { get; private set; }
    internal string Platform { get; private set; }
    internal uint OriginalSize { get; private set; }
    internal uint PackedSize { get; private set; }
    internal VromfsType VromfsType { get; private set; }
    internal VromfsPackageType PackageType { get; private set; }
    internal Header(BinaryReader reader)
    {
        Magic = reader.ReadBytes(4).ToUTF8String();
        Platform = reader.ReadBytes(4).ToUTF8String().Trim('\0');
        OriginalSize = reader.ReadUInt32();
        PackedSize = (uint)(reader.ReadUInt16() + (reader.ReadByte() << 16));
        VromfsType = (VromfsType)reader.ReadByte();

        PackageType = VromfsType switch
        {
            VromfsType.ZstdPacked => VromfsPackageType.ZstdPacked,
            VromfsType.MaybePacked => PackedSize switch
            {
                0 => VromfsPackageType.NotPacked,
                > 0 => VromfsPackageType.ZlibPacked,
            },
            _ => throw new Exception("Uknown vromfstype")
        };

    }
}
