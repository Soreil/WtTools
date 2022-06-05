using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace WtTools.Formats.Extensions;

public static class StreamExtensions
{
    public async static Task<byte[]> ReadToEndAsync(this BinaryReader reader)
    {
        using var ms = new MemoryStream();
        await reader.BaseStream.CopyToAsync(ms);
        return ms.ToArray();
    }
    public static byte[] ReadToEnd(this BinaryReader reader)
    {
        return reader.ReadToEndAsync().GetAwaiter().GetResult();
    }

    public static string ReadTerminatedString(this Stream stream)
    {
        using var writer = new MemoryStream();

        for (var b = stream.ReadByte(); b > 0; b = stream.ReadByte())
        {
            writer.WriteByte((byte)b);
        }

        return Encoding.ASCII.GetString(writer.ToArray());
    }

    public static byte[] ReadToEnd(this Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
