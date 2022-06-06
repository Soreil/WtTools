using System;
using System.IO;

using WtTools.Formats.Extensions;

namespace WtTools.Formats.Blk;

public abstract class ParamBase { }

public class StringParam : ParamBase
{
    public StringParam(string s)
    {
        S = s;
    }

    public string S { get; }

    public override string? ToString()
    {
        return S.ToString();
    }
}

public class InvertedBool : ParamBase
{
    public InvertedBool(bool b)
    {
        B = b;
    }

    public bool B { get; }

    public override string? ToString()
    {
        return B.ToString();
    }

}
public class Bool : ParamBase
{
    public Bool(bool b)
    {
        B = b;
    }

    public bool B { get; }
    public override string? ToString()
    {
        return B.ToString();
    }
}
public class IntParam : ParamBase
{
    public IntParam(int val)
    {
        Val = val;
    }

    public int Val { get; }
    public override string? ToString()
    {
        return Val.ToString();
    }
}
public class FloatParam : ParamBase
{
    public FloatParam(float val)
    {
        Val = val;
    }

    public float Val { get; }
    public override string? ToString()
    {
        return Val.ToString();
    }
}

public class ColorParam : ParamBase
{
    public ColorParam(byte[] bytes)
    {
        R = bytes[0];
        G = bytes[1];
        B = bytes[2];
        A = bytes[3];
    }

    public ColorParam(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public override string ToString()
    {
        return $"{R}{G}{B}{A}";
    }
}

public class Size : ParamBase
{
    public Size(ushort lhs, ushort rhs)
    {
        Lhs = lhs;
        Rhs = rhs;
    }

    public ushort Lhs { get; }
    public ushort Rhs { get; }

    public override string? ToString()
    {
        return $"{Lhs},{Rhs}";
    }
}

public class UIntParam : ParamBase
{
    public UIntParam(uint val)
    {
        Val = val;
    }

    public uint Val { get; }
    public override string? ToString()
    {
        return Val.ToString();
    }
}

public class LongParam : ParamBase
{
    public LongParam(long val)
    {
        Val = val;
    }

    public long Val { get; }
    public override string? ToString()
    {
        return Val.ToString();
    }
}

public class UIntVec2 : ParamBase
{
    public UIntVec2(uint[] uints)
    {
        Lhs = uints[0];
        Rhs = uints[1];
    }

    public UIntVec2(uint lhs, uint rhs)
    {
        Lhs = lhs;
        Rhs = rhs;
    }

    public uint Lhs { get; }
    public uint Rhs { get; }

    public override string? ToString()
    {
        return $"{Lhs},{Rhs}";
    }
}
public class UIntVec3 : ParamBase
{
    public UIntVec3(uint[] uints)
    {
        First = uints[0];
        Second = uints[1];
        Third = uints[2];
    }

    public UIntVec3(uint lhs, uint mid, uint rhs)
    {
        First = lhs;
        Second = mid;
        Third = rhs;
    }

    public uint First { get; }
    public uint Second { get; }
    public uint Third { get; }

    public override string? ToString()
    {
        return $"{First},{Second},{Third}";
    }
}
public class FloatVec2 : ParamBase
{
    public FloatVec2(float[] floats)
    {
        Lhs = floats[0];
        Rhs = floats[1];
    }

    public FloatVec2(float lhs, float rhs)
    {
        Lhs = lhs;
        Rhs = rhs;
    }

    public float Lhs { get; }
    public float Rhs { get; }
    public override string? ToString()
    {
        return $"{Lhs},{Rhs}";
    }
}
public class FloatVec3 : ParamBase
{
    public FloatVec3(float[] floats)
    {
        First = floats[0];
        Second = floats[1];
        Third = floats[2];
    }

    public FloatVec3(float first, float second, float third)
    {
        First = first;
        Second = second;
        Third = third;
    }

    public float First { get; }
    public float Second { get; }
    public float Third { get; }

    public override string? ToString()
    {
        return $"{First},{Second},{Third}";
    }
}
public class FloatVec4 : ParamBase
{
    public FloatVec4(float[] floats)
    {
        First = floats[0];
        Second = floats[1];
        Third = floats[2];
        Fourth = floats[3];
    }

    public FloatVec4(float first, float second, float third, float fourth)
    {
        First = first;
        Second = second;
        Third = third;
        Fourth = fourth;
    }

    public float First { get; }
    public float Second { get; }
    public float Third { get; }
    public float Fourth { get; }

    public override string? ToString()
    {
        return $"{First},{Second},{Third},{Fourth}";
    }
}

public class FloatMatrix : ParamBase
{
    public FloatMatrix(FloatVec3 first, FloatVec3 second, FloatVec3 third, FloatVec3 fourth)
    {
        First = first;
        Second = second;
        Third = third;
        Fourth = fourth;
    }

    public FloatVec3 First { get; }
    public FloatVec3 Second { get; }
    public FloatVec3 Third { get; }
    public FloatVec3 Fourth { get; }

    public override string? ToString()
    {
        return $"{First}\n{Second}\n{Third}\n{Fourth}";
    }
}

//A parameter either holds an immediate value stored in a field up to 4 bytes large or that 4 bytes value is used as a pointer
//To the storage location of the actual value. This format seems somewhat similar to the way tags are defined in TIFF for example
internal struct ParamInfo
{
    public int Id;
    public string Name;
    public DataType Type;
    public ParamBase Value;

    public ParamInfo(BinaryReader reader, BlkInfo blk)
    {
        var data = reader.ReadBytes(8);
        Id = data[0] | (data[1] << 8) | (data[2] << 16);
        Type = (DataType)data[3];
        int index = BitConverter.ToInt32(data.AsSpan()[4..8]);
        switch (Type)
        {
            case DataType.Str:
                // we have a 32 bit int made up of the values from 4,5,6,7 and here we take out the value of 7
                // effectively trimming off the top (or bottom?) 8 bits.
                var strIndex = index - (data[7] << 24);
                if (blk.Parent is null)
                {
                    var lgd = blk.LargeData[strIndex..];
                    lgd = lgd[..lgd.IndexOf((byte)0)];
                    Value = new StringParam(System.Text.Encoding.UTF8.GetString(lgd));
                }
                else
                {
                    Value = new StringParam(blk.GetStringValue(strIndex));
                }
                break;
            case DataType.Int:
                Value = new IntParam(BitConverter.ToInt32(data.AsSpan()[4..8]));
                break;
            case DataType.Float:
                Value = new FloatParam(BitConverter.ToSingle(data.AsSpan()[4..8]));
                break;
            case DataType.Color:
                Value = new ColorParam(data[6], data[5], data[4], data[7]);
                break;
            case DataType.Size:
                Value = new Size(ReadUShort(data.AsSpan()[4..6]), ReadUShort(data.AsSpan()[6..8]));
                break;
            case DataType.Typex7:
                Value = new UIntParam(BitConverter.ToUInt32(data.AsSpan()[4..8]));
                break;
            case DataType.Long:
                Value = new LongParam(ReadLong(blk.LargeData.Slice(index, 8)));
                break;
            case DataType.Vec2:
                var raw = blk.LargeData.Slice(index, 8);
                Value = new UIntVec2
                (
                    (uint)ReadInt(raw[0..4]),
                    (uint)ReadInt(raw[4..8])
                );
                break;
            case DataType.Vec2F:
                raw = blk.LargeData.Slice(index, 8);
                Value = new FloatVec2
                (
                    ReadFloat(raw[0..4]),
                    ReadFloat(raw[4..8])
                );
                break;
            case DataType.Vec3:
                raw = blk.LargeData.Slice(index, 12);
                Value = new UIntVec3
                (
                    (uint)ReadInt(raw[0..4]),
                    (uint)ReadInt(raw[4..8]),
                    (uint)ReadInt(raw[8..12])
                );
                break;
            case DataType.Vec3F:
                raw = blk.LargeData.Slice(index, 12).ToArray();
                Value = new FloatVec3
                (
                    ReadFloat(raw[0..4]),
                    ReadFloat(raw[4..8]),
                    ReadFloat(raw[8..12])
                );
                break;
            case DataType.Vec4F:
                raw = blk.LargeData.Slice(index, 16).ToArray();
                Value = new FloatVec4
                (
                    ReadFloat(raw[0..4]),
                    ReadFloat(raw[4..8]),
                    ReadFloat(raw[8..12]),
                    ReadFloat(raw[12..16])
                );
                break;
            case DataType.M4x3F:
                raw = blk.LargeData.Slice(index, 48);
                Value = new FloatMatrix(
                    new FloatVec3(ReadFloat(raw[0..4]), ReadFloat(raw[4..8]), ReadFloat(raw[8..12])),
                    new FloatVec3(ReadFloat(raw[12..16]), ReadFloat(raw[16..20]), ReadFloat(raw[20..24])),
                    new FloatVec3(ReadFloat(raw[24..28]), ReadFloat(raw[28..32]), ReadFloat(raw[32..36])),
                    new FloatVec3(ReadFloat(raw[36..40]), ReadFloat(raw[40..44]), ReadFloat(raw[44..48]))
                );
                break;
            case DataType.Typex:
                Value = new InvertedBool(data[4] == 0);
                break;
            case DataType.Bool:
                Value = new Bool(data[4] == 1);
                break;
            default:
                throw new Exception($"Unknown type: {data[3]:x}");
        };
        Name = blk.GetStringValue(Id);
    }

    private static int ReadInt(ReadOnlySpan<byte> data) => BitConverter.ToInt32(data);

    private static ushort ReadUShort(ReadOnlySpan<byte> data) => BitConverter.ToUInt16(data);

    private static float ReadFloat(ReadOnlySpan<byte> data) => BitConverter.ToSingle(data);

    private static long ReadLong(ReadOnlySpan<byte> data) => BitConverter.ToInt64(data);

}
