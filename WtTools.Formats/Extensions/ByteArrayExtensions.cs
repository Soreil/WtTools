namespace WtTools.Formats.Extensions;

public static class ByteArrayExtensions
{
    public static string ToUTF8String(this byte[] data) => System.Text.Encoding.UTF8.GetString(data);
}
