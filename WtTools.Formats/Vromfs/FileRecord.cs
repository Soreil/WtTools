namespace WtTools.Formats.Vromfs;

public record FileRecord(string Name, int Size, uint Offset, byte[] Data);
