using System;
using System.IO;

using WtTools.Formats.Extensions;
using WtTools.Formats.Vromfs;

using ZstdNet;

namespace WtTools.Formats
{
    public class VromfsInfo
    {
        internal Header Header { get; set; }
        internal ExtHeader? ExtHeader { get; set; }

        private static readonly byte[] _firstObfs = new byte[] { 0x55, 0xaa, 0x55, 0xaa, 0x0f, 0xf0, 0x0f, 0xf0, 0x55, 0xaa, 0x55, 0xaa, 0x48, 0x12, 0x48, 0x12 };
        private static readonly byte[] _secondObfs = new byte[] { 0x48, 0x12, 0x48, 0x12, 0x55, 0xaa, 0x55, 0xaa, 0x0f, 0xf0, 0x0f, 0xf0, 0x55, 0xaa, 0x55, 0xaa };

        internal NameMap NameMap { get; set; }
        public FileRecord[]? Files { get; set; }
        internal DecompressionOptions? DecompressionOptions { get; set; }

        /// <summary>
        /// Read Virtual ROM File System file 
        /// </summary>
        /// <param name="vromfsPath">Path to the .vromfs file</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static VromfsInfo FromFile(string vromfsPath)
        {
            var fileInfo = new FileInfo(vromfsPath);
            if (!fileInfo.Exists)
            {
                throw new ArgumentException($"File '{fileInfo.FullName} doesn't exist");
            }
            var vromfsData = File.ReadAllBytes(fileInfo.FullName);
            var vromfs = new VromfsInfo(vromfsData);
            return vromfs;
        }

        public VromfsInfo(byte[] vromfsData)
        {
            using var ms = new MemoryStream(vromfsData);
            using var reader = new BinaryReader(ms);
            Header = new Header(reader);
            if (Header.Magic == "VRFx")
            {
                ExtHeader = new ExtHeader(reader);
            }
            var data = reader.ReadToEnd();
            data = Deobfuscate(data);
            data = Decompress(data);
            ReadFiles(data);
        }

        #region Processing

        /// <summary>
        /// Deobfuscate the data.
        /// </summary>
        /// <param name="data">Obfuscated data</param>
        /// <returns>Modified array</returns>
        private byte[] Deobfuscate(byte[] data)
        {
            var pad = (int)Header.PackedSize % 4;
            var middleSize = (int)Header.PackedSize - (Header.PackedSize >= 32 ? 32 : (Header.PackedSize >= 16 ? 16 : 0)) - pad;
            int j = 16 + middleSize;
            var result = data[0..(middleSize + 32 + pad)];
            for (int i = 0; i < 16; ++i, ++j)
            {
                result[i] = (byte)(result[i] ^ _firstObfs[i]);
                result[j] = (byte)(result[j] ^ _secondObfs[i]);
            }

            return result;
        }

        /// <summary>
        /// Decompress the data according to the compression recognized in Header.
        /// </summary>
        /// <param name="compressedData">Compressed data</param>
        /// <returns>Decompressed data</returns>
        private byte[] Decompress(byte[] compressedData)
        {
            if (Header.PackageType == VromfsPackageType.ZstdPacked)
            {
                using var decompressor = new Decompressor();
                var data = decompressor.Unwrap(compressedData);
                return data;
            }
            return compressedData;
        }

        #endregion

        private void ReadFiles(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            var fileRecordsOffset = reader.ReadUInt32();
            var filesCount = reader.ReadUInt32();
            _ = reader.ReadBytes(8);
            var dataOffset = reader.ReadUInt32();
            stream.Seek(fileRecordsOffset, SeekOrigin.Begin);
            var firstFilenameOffset = reader.ReadUInt32();
            stream.Seek(firstFilenameOffset, SeekOrigin.Begin);

            var Names = new string[filesCount];
            var Offsets = new uint[filesCount];
            var Sizes = new int[filesCount];
            var Datas = new byte[filesCount][];

            int nmIndex = 0, dictIndex = 0;
            bool nmFound = false, dictFound = false;
            for (int i = 0; i < filesCount; ++i)
            {
                var Name = reader.BaseStream.ReadTerminatedString();
                if (!dictFound && Name.EndsWith(".dict"))
                {
                    dictIndex = i;
                    dictFound = true;
                }
                else if (!nmFound && Name.EndsWith("?nm"))
                {
                    Name = "nm";
                    nmIndex = i;
                    nmFound = true;
                }
                Names[i] = Name;
            }

            stream.Seek(dataOffset, SeekOrigin.Begin);
            for (int i = 0; i < filesCount; ++i)
            {
                Offsets[i] = reader.ReadUInt32();
                Sizes[i] = (int)reader.ReadUInt32();
                reader.BaseStream.Seek(8, SeekOrigin.Current);
            }
            var dataSpan = data.AsSpan();

            for (int i = 0; i < filesCount; ++i)
            {
                Datas[i] = dataSpan.Slice((int)Offsets[i], Sizes[i]).ToArray();
            }
            if (dictFound)
            {
                DecompressionOptions = new DecompressionOptions(Datas[dictIndex]);
            }
            else
            {
                DecompressionOptions = new DecompressionOptions();
            }

            if (nmFound)
            {
                NameMap = new NameMap(Datas[nmIndex], DecompressionOptions);
            }

            Files = new FileRecord[filesCount];

            for (int i = 0; i < Files.Length; i++)
            {
                Files[i] = new FileRecord(Names[i], Sizes[i], Offsets[i], Datas[i]);
            }
        }
    }
}
