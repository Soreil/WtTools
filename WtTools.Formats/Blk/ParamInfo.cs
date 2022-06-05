using System;
using System.IO;

using WtTools.Formats.Extensions;

namespace WtTools.Formats.Blk;

internal struct ParamInfo
{
    public int Id;
    public string Name;
    public DataType Type;
    public object Value;

    public ParamInfo(BinaryReader reader, BlkInfo blk)
    {
        var data = reader.ReadBytes(8);
        Id = data[0] | (data[1] << 8) | (data[2] << 16);
        Type = (DataType)data[3];
        int index = BitConverter.ToInt32(data.AsSpan()[4..8]);
        switch (Type)
        {
            case DataType.Str:
                var strIndex = index - (data[7] << 24);
                if (blk.Parent == null)
                {
                    var lgd = blk.LargeData.Slice(strIndex);
                    lgd = lgd[..lgd.IndexOf((byte)0)];
                    Value = lgd.ToArray().ToUTF8String();
                }
                else
                {
                    Value = blk.GetStringValue(strIndex);
                }
                break;
            case DataType.Int:
                Value = data[4] | (data[5] << 8) | (data[6] << 16) | (data[7] << 24);
                break;
            case DataType.Float:
                Value = ReadFloat(data.AsSpan(4, 4));
                break;
            case DataType.Color:
                Value = new byte[] { data[6], data[5], data[4], data[7] };
                break;
            case DataType.Size:
                Value = new ushort[] { ReadUShort(data.AsSpan()[4..6]), ReadUShort(data.AsSpan()[6..8]) };
                break;
            case DataType.Typex7:
                Value = (uint)(data[4] | (data[5] << 8) | (data[6] << 16) | (data[7] << 24));
                break;
            case DataType.Long:
                Value = ParamInfo.ReadLong(blk.LargeData.Slice(index, 8));
                break;
            case DataType.Vec2:
                var raw = blk.LargeData.Slice(index, 8);
                Value = new uint[]
                {
                    (uint)ReadInt(raw[0..4]),
                    (uint)ReadInt(raw[4..8])
                };
                break;
            case DataType.Vec2F:
                raw = blk.LargeData.Slice(index, 8);
                Value = new float[]
                {
                    ReadFloat(raw[0..4]),
                    ReadFloat(raw[4..8]),
                };
                break;
            case DataType.Vec3:
                raw = blk.LargeData.Slice(index, 12);
                Value = new uint[]
                {
                    (uint)ReadInt(raw[0..4]),
                    (uint)ReadInt(raw[4..8]),
                    (uint)ReadInt(raw[8..12])
                };
                break;
            case DataType.Vec3F:
                raw = blk.LargeData.Slice(index, 12).ToArray();
                Value = new float[]
                {
                    ReadFloat(raw[0..4]),
                    ReadFloat(raw[4..8]),
                    ReadFloat(raw[8..12])
                };
                break;
            case DataType.Vec4F:
                raw = blk.LargeData.Slice(index, 16).ToArray();
                Value = new float[]
                {
                    ReadFloat(raw[0..4]),
                    ReadFloat(raw[4..8]),
                    ReadFloat(raw[8..12]),
                    ReadFloat(raw[12..16])
                };
                break;
            case DataType.M4x3F:
                raw = blk.LargeData.Slice(index, 48);
                Value = new float[][]
                {
                    new float[] {ReadFloat(raw[0..4]),  ReadFloat(raw[4..8]), ReadFloat(raw[8..12])},
                    new float[] {ReadFloat(raw[12..16]),  ReadFloat(raw[16..20]), ReadFloat(raw[20..24])},
                    new float[] {ReadFloat(raw[24..28]),  ReadFloat(raw[28..32]), ReadFloat(raw[32..36])},
                    new float[] {ReadFloat(raw[36..40]),  ReadFloat(raw[40..44]), ReadFloat(raw[44..48])}
                };
                break;
            case DataType.Typex:
                Value = data[4] == 0;
                break;
            case DataType.Bool:
                Value = data[4] == 1;
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
