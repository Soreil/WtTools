namespace WtTools.Formats.Vromfs;

internal enum VromfsType : byte
{
    ZstdPacked = 0xc0,
    MaybePacked = 0x80,
    Unknown = 0x40
}
