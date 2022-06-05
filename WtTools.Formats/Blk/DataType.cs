namespace WtTools.Formats.Blk;

public enum DataType : byte
{
    Size = 0x00,
    Str = 0x01,
    Int = 0x02,
    Float = 0x03,
    Vec2F = 0x04,
    Vec3F = 0x05,
    Vec4F = 0x06,
    Vec2 = 0x07,
    Vec3 = 0x08,
    Bool = 0x09,
    Color = 0x0a,
    M4x3F = 0x0b,
    Long = 0x0c,
    Typex7 = 0x10,
    Typex = 0x89
}
