/*
 * Copyright (c) 2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.FileTransfer;

internal class SFFileCompressionTypes
{
    const byte MAX_MAGIC_BYTES = 4;

    static readonly byte[] GZIP_MAGIC = new byte[] { 0x1f, 0x8b };
    const string GZIP_NAME = "gzip";
    const string GZIP_EXTENSION = ".gz";

    static readonly byte[] DEFLATE_MAGIC_LOW = new byte[] { 0x78, 0x01 };
    static readonly byte[] DEFLATE_MAGIC_DEFAULT = new byte[] { 0x78, 0x9c };
    static readonly byte[] DEFLATE_MAGIC_BEST = new byte[] { 0x78, 0xda };
    const string DEFLATE_NAME = "deflate";
    const string DEFLATE_EXTENSION = ".deflate";

    const string RAW_DEFLATE_NAME = "raw_deflate";
    const string RAW_DEFLATE_EXTENSION = ".raw_deflate";

    static readonly byte[] BZIP2_MAGIC = new byte[] { 0x42, 0x5a };
    const string BZIP2_NAME = "bzip2";
    const string BZIP2_EXTENSION = ".bz2";

    static readonly byte[] ZSTD_MAGIC = new byte[] { 0x28, 0xb5, 0x2f, 0xfd };
    const string ZSTD_NAME = "zstd";
    const string ZSTD_EXTENSION = ".zst";

    static readonly byte[] BROTLI_MAGIC = new byte[] { 0xce, 0xb2, 0xcf, 0x81 };
    const string BROTLI_NAME = "brotli";
    const string BROTLI_EXTENSION = ".br";

    const string LZIP_NAME = "lzip";
    const string LZIP_EXTENSION = ".lz";

    const string LZMA_NAME = "lzma";
    const string LZMA_EXTENSION = ".lzma";

    const string LZO_NAME = "lzop";
    const string LZO_EXTENSION = ".lzo";

    const string XZ_NAME = "xz";
    const string XZ_EXTENSION = ".xz";

    const string COMPRESS_NAME = "compress";
    const string COMPRESS_EXTENSION = ".Z";

    static readonly byte[] PARQUET_MAGIC = new byte[] { 0x50, 0x41, 0x52, 0x31 };
    const string PARQUET_NAME = "parquet";
    const string PARQUET_EXTENSION = ".parquet";

    static readonly byte[] ORC_MAGIC = new byte[] { 0x4f, 0x52, 0x43 };
    const string ORC_NAME = "orc";
    const string ORC_EXTENSION = ".orc";

    const string NONE_NAME = "none";
    const string NONE_EXTENSION = "";

    static readonly byte[][] gzip_magics = new[] { GZIP_MAGIC };

    static readonly byte[][] deflate_magics = new[] { DEFLATE_MAGIC_LOW, DEFLATE_MAGIC_DEFAULT, DEFLATE_MAGIC_BEST };

    static readonly byte[][] bzip2_magics = new[] { BZIP2_MAGIC };
    static readonly byte[][] orc_magics = new[] { ORC_MAGIC };
    static readonly byte[][] parquet_magics = new[] { PARQUET_MAGIC };
    static readonly byte[][] zstd_magics = new[] { ZSTD_MAGIC };
    static readonly byte[][] brotli_magics = new[] { BROTLI_MAGIC };

    public struct SFFileCompressionType
    {
        public SFFileCompressionType(string fileExtension, string name, byte[][] magicNumbers, short magicBytes, bool isSupported)
        {
            FileExtension = fileExtension;
            IsSupported = isSupported;
            _magicNumbers = magicNumbers;
            _magicBytes = magicBytes;
            Name = name;
        }

        public SFFileCompressionType(string fileExtension, string name, bool isSupported)
        {
            FileExtension = fileExtension;
            IsSupported = isSupported;
            _magicNumbers = null;
            _magicBytes = 0;
            Name = name;
        }

        /// <summary>
        /// Check if the given header matches the magic number for this compression type
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public bool matchMagicNumber(byte[] header)
        {
            var isEquals = true;
            if ((null != _magicNumbers) && (null != header))
            {
                for (var i = 0; i < _magicNumbers.Length; i++)
                {
                    if (header.Length >= _magicNumbers[i].Length)
                    {
                        for (var j = 0; j < _magicNumbers[i].Length; j++)
                        {
                            if (header[j] != _magicNumbers[i][j])
                            {
                                isEquals = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        isEquals = false;
                        break;
                    }
                }
            }

            return isEquals;
        }

        internal string FileExtension { get; }
        internal string Name { get; }
        readonly byte[][]? _magicNumbers;
        readonly short _magicBytes;
        internal bool IsSupported { get; }
    }

    public static readonly SFFileCompressionType GZIP = new(GZIP_EXTENSION, GZIP_NAME, gzip_magics, 2, true);

    public static readonly SFFileCompressionType DEFLATE = new(DEFLATE_EXTENSION, DEFLATE_NAME, deflate_magics, 2, true);

    public static readonly SFFileCompressionType RAW_DEFLATE = new(RAW_DEFLATE_EXTENSION, RAW_DEFLATE_NAME, true);

    public static readonly SFFileCompressionType BZIP2 = new(BZIP2_EXTENSION, BZIP2_NAME, bzip2_magics, 2, true);

    public static readonly SFFileCompressionType ZSTD = new(ZSTD_EXTENSION, ZSTD_NAME, zstd_magics, 4, true);

    public static readonly SFFileCompressionType BROTLI = new(BROTLI_EXTENSION, BROTLI_NAME, brotli_magics, 4, true);

    public static readonly SFFileCompressionType ORC = new(ORC_EXTENSION, ORC_NAME, orc_magics, 3, true);

    public static readonly SFFileCompressionType PARQUET = new(PARQUET_EXTENSION, PARQUET_NAME, parquet_magics, 4, true);

    public static readonly SFFileCompressionType LZIP = new(LZIP_EXTENSION, LZIP_NAME, false);

    public static readonly SFFileCompressionType LZMA = new(LZMA_EXTENSION, LZMA_NAME, false);

    public static readonly SFFileCompressionType LZO = new(LZO_EXTENSION, LZO_NAME, false);

    public static readonly SFFileCompressionType XZ = new(XZ_EXTENSION, XZ_NAME, false);

    public static readonly SFFileCompressionType COMPRESS = new(COMPRESS_EXTENSION, COMPRESS_NAME, false);

    public static readonly SFFileCompressionType NONE = new(NONE_EXTENSION, NONE_NAME, true);

    static readonly IReadOnlyList<SFFileCompressionType> compressionTypes =
        new List<SFFileCompressionType> {
                GZIP,
                DEFLATE,
                RAW_DEFLATE,
                BZIP2,
                ZSTD,
                BROTLI,
                LZIP,
                LZMA,
                LZO,
                XZ,
                COMPRESS,
                ORC,
                PARQUET
        };

    public static SFFileCompressionType GuessCompressionType(string filePath)
    {
        // read first 4 bytes to determine compression type
        var header = new byte[MAX_MAGIC_BYTES];
        using (var fs = File.OpenRead(filePath))
        {
            fs.Read(header, 0, header.Length);
        }

        foreach (var compType in compressionTypes)
        {
            if (compType.matchMagicNumber(header))
            {
                // Found the compression type for this file
                var extension = Path.GetExtension(filePath);
                if (!string.IsNullOrEmpty(extension) && string.Equals(BROTLI.FileExtension, extension, StringComparison.OrdinalIgnoreCase))
                {
                    return BROTLI;
                }
            }
        }

        // Couldn't find a match, last fallback using the file name extension
        return LookUpByName(new FileInfo(filePath).Extension);
    }

    /// <summary>
    /// Lookup the compression type base on the given type name.
    /// </summary>
    /// <param name="name">The type name to lookup</param>
    /// <returns>The corresponding SFFileCompressionType if supported, None if no match</returns>
    public static SFFileCompressionType LookUpByName(string name)
    {
        if (name.StartsWith("."))
            name = name.Substring(1);

        foreach (var compType in compressionTypes)
        {
            if (compType.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return compType;
        }

        return NONE;
    }
}
