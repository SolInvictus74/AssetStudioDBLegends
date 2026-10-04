using System;
using System.IO;
using System.Text;

namespace AssetStudio
{
    public class StreamingInfo
    {
        public long offset; //ulong
        public uint size;
        public string path;

        public StreamingInfo(ObjectReader reader)
        {
            var version = reader.version;

            if (version[0] >= 2020)
            {
                offset = reader.ReadInt64();
            }
            else
            {
                offset = reader.ReadUInt32();
            }

            size = reader.ReadUInt32();

            path = reader.ReadAlignedString();
        }
    }

    public class GLTextureSettings
    {
        public int m_FilterMode;
        public int m_Aniso;
        public float m_MipBias;
        public int m_WrapMode;

        public GLTextureSettings(ObjectReader reader)
        {
            var version = reader.version;

            m_FilterMode = reader.ReadInt32();
            m_Aniso = reader.ReadInt32();
            m_MipBias = reader.ReadSingle();

            if (reader.Game.Type.IsExAstris())
            {
                var m_TextureGroup = reader.ReadInt32();
            }

            if (version[0] >= 2017)
            {
                m_WrapMode = reader.ReadInt32();

                int m_WrapV =
                    reader.ReadInt32();

                int m_WrapW =
                    reader.ReadInt32();
            }
            else
            {
                m_WrapMode =
                    reader.ReadInt32();
            }
        }
    }

    public sealed class Texture2D : Texture
    {
        public int m_Width;
        public int m_Height;
        public TextureFormat m_TextureFormat;
        public bool m_MipMap;
        public int m_MipCount;
        public GLTextureSettings m_TextureSettings;
        public ResourceReader image_data;
        public StreamingInfo m_StreamData;

        private void ReadUnity6Texture2D(ObjectReader reader)
        {
            // ============================================================
            // UNITY 6 TEXTURE2D
            // Verified against Unity 6000.3.14f1 ClassID 28 TypeTree.
            // ============================================================

            m_Width = reader.ReadInt32();
            m_Height = reader.ReadInt32();

            var m_CompleteImageSize =
                reader.ReadUInt32();

            var m_MipsStripped =
                reader.ReadInt32();

            m_TextureFormat =
                (TextureFormat)reader.ReadInt32();

            m_MipCount =
                reader.ReadInt32();

            // ------------------------------------------------------------
            // Flags
            // ------------------------------------------------------------

            var m_IsReadable =
                reader.ReadBoolean();

            var m_IsPreProcessed =
                reader.ReadBoolean();

            var m_IgnoreMipmapLimit =
                reader.ReadBoolean();

            reader.AlignStream();

            // ------------------------------------------------------------
            // Mipmap limit group
            // ------------------------------------------------------------

            var m_MipmapLimitGroupName =
                reader.ReadAlignedString();

            // ------------------------------------------------------------
            // Streaming mipmaps
            // ------------------------------------------------------------

            var m_StreamingMipmaps =
                reader.ReadBoolean();

            reader.AlignStream();

            var m_StreamingMipmapsPriority =
                reader.ReadInt32();

            // ------------------------------------------------------------
            // Texture information
            // ------------------------------------------------------------

            var m_ImageCount =
                reader.ReadInt32();

            var m_TextureDimension =
                reader.ReadInt32();

            m_TextureSettings =
                new GLTextureSettings(reader);

            var m_LightmapFormat =
                reader.ReadInt32();

            var m_ColorSpace =
                reader.ReadInt32();

            // ------------------------------------------------------------
            // Platform blob
            // ------------------------------------------------------------

            var m_PlatformBlob =
                reader.ReadUInt8Array();

            reader.AlignStream();

            // ------------------------------------------------------------
            // Inline image data
            // ------------------------------------------------------------

            var image_data_size =
                reader.ReadInt32();

            if (image_data_size < 0)
            {
                throw new Exception(
                    $"Invalid Unity 6 Texture2D image data size: " +
                    $"{image_data_size}");
            }

            long imageDataPosition =
                reader.BaseStream.Position;

            // StreamingInfo follows the inline payload.
            reader.BaseStream.Position =
                imageDataPosition + image_data_size;

            reader.AlignStream();

            // ------------------------------------------------------------
            // StreamingInfo
            // ------------------------------------------------------------

            m_StreamData =
                new StreamingInfo(reader);

            // ------------------------------------------------------------
            // Actual texture resource
            // ------------------------------------------------------------

            ResourceReader resourceReader;

            if (!string.IsNullOrEmpty(m_StreamData?.path))
            {
                resourceReader =
                    new ResourceReader(
                        m_StreamData.path,
                        assetsFile,
                        m_StreamData.offset,
                        m_StreamData.size
                    );
            }
            else
            {
                resourceReader =
                    new ResourceReader(
                        reader,
                        imageDataPosition,
                        image_data_size
                    );
            }

            image_data =
                resourceReader;
        }

        private const string DiagnosticFileName =
            "TEXTURE2D_U6_LAYOUT_DIAGNOSTIC.txt";

        private static readonly object DiagnosticLock =
            new object();

        private static bool HasGNFTexture(SerializedType type) =>
            type.Match(
                "1D52BB98AA5F54C67C22C39E8B2E400F"
            );

        private static bool HasExternalMipRelativeOffset(
            SerializedType type) =>
            type.Match(
                "1D52BB98AA5F54C67C22C39E8B2E400F",
                "5390A985F58D5524F95DB240E8789704"
            );

        private static bool IsUnity6(ObjectReader reader)
        {
            try
            {
                return reader != null &&
                       reader.version != null &&
                       reader.version.Length > 0 &&
                       reader.version[0] >= 6000;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeVersion(
            ObjectReader reader)
        {
            try
            {
                return string.Join(
                    ".",
                    reader.version);
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        private static string SafeFileName(
            ObjectReader reader)
        {
            try
            {
                if (reader.assetsFile != null &&
                    !string.IsNullOrEmpty(
                        reader.assetsFile.fileName))
                {
                    return reader.assetsFile.fileName;
                }
            }
            catch
            {
            }

            return "UNKNOWN";
        }

        private static void WriteLog(
            string text)
        {
            try
            {
                lock (DiagnosticLock)
                {
                    File.AppendAllText(
                        DiagnosticFileName,
                        text);
                }
            }
            catch
            {
                // La diagnostica non deve mai interferire
                // con il parser.
            }
        }

        private static byte[] ReadDiagnosticBytes(
            ObjectReader reader,
            long position,
            int count)
        {
            long savedPosition =
                reader.Position;

            try
            {
                long objectEnd =
                    reader.byteStart +
                    (long)reader.byteSize;

                if (position < reader.byteStart ||
                    position >= objectEnd)
                {
                    return Array.Empty<byte>();
                }

                long available =
                    objectEnd - position;

                int realCount =
                    (int)Math.Min(
                        count,
                        available);

                if (realCount <= 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] result =
                    new byte[realCount];

                reader.Position =
                    position;

                for (int i = 0;
                     i < realCount;
                     i++)
                {
                    result[i] =
                        reader.ReadByte();
                }

                return result;
            }
            catch
            {
                return Array.Empty<byte>();
            }
            finally
            {
                reader.Position =
                    savedPosition;
            }
        }

        private static string HexDump(
            byte[] data,
            long absoluteStart)
        {
            if (data == null ||
                data.Length == 0)
            {
                return "<EMPTY>\r\n";
            }

            const int width = 16;

            StringBuilder sb =
                new StringBuilder();

            for (int offset = 0;
                 offset < data.Length;
                 offset += width)
            {
                int count =
                    Math.Min(
                        width,
                        data.Length - offset);

                sb.Append(
                    $"+0x{offset:X4} ");

                sb.Append(
                    $"ABS=0x{absoluteStart + offset:X} | ");

                for (int i = 0;
                     i < width;
                     i++)
                {
                    if (i < count)
                    {
                        sb.Append(
                            data[offset + i]
                                .ToString("X2"));

                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append("   ");
                    }
                }

                sb.Append("| ");

                for (int i = 0;
                     i < count;
                     i++)
                {
                    byte b =
                        data[offset + i];

                    if (b >= 32 &&
                        b <= 126)
                    {
                        sb.Append(
                            (char)b);
                    }
                    else
                    {
                        sb.Append('.');
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string Int32View(
            byte[] data)
        {
            if (data == null ||
                data.Length < 4)
            {
                return "<NO INT32 DATA>\r\n";
            }

            StringBuilder sb =
                new StringBuilder();

            for (int offset = 0;
                 offset + 4 <= data.Length;
                 offset += 4)
            {
                int value =
                    BitConverter.ToInt32(
                        data,
                        offset);

                uint unsignedValue =
                    BitConverter.ToUInt32(
                        data,
                        offset);

                float floatValue =
                    BitConverter.ToSingle(
                        data,
                        offset);

                sb.AppendLine(
                    $"+0x{offset:X4} | " +
                    $"HEX=0x{unsignedValue:X8} | " +
                    $"I32={value} | " +
                    $"U32={unsignedValue} | " +
                    $"F32={floatValue:R}");
            }

            return sb.ToString();
        }

        private static void LogTargetTextureRawWindow(
    ObjectReader reader,
    long afterTextureBase)
        {
            /*
             * ============================================================
             * UNITY 6 - TARGET TEXTURE RAW WINDOW
             *
             * Diagnostica NON distruttiva.
             *
             * Target:
             * ShalletSSG_01_RGM_LT
             * PathID: -7735706617283584571
             *
             * Leggiamo una finestra RAW che comprende:
             *
             *   - 96 byte PRIMA di AfterTextureBase
             *   - 160 byte DOPO AfterTextureBase
             *
             * La posizione originale del reader viene sempre ripristinata.
             * ============================================================
             */

            if (!IsUnity6(reader))
                return;

            const long targetPathID =
                -7735706617283584571L;

            if (reader.m_PathID != targetPathID)
                return;

            long originalPosition =
                reader.Position;

            try
            {
                long objectStart =
                    reader.byteStart;

                long objectEnd =
                    reader.byteStart +
                    (long)reader.byteSize;

                const int bytesBefore = 96;
                const int bytesAfter = 160;

                long requestedStart =
                    afterTextureBase - bytesBefore;

                long requestedEnd =
                    afterTextureBase + bytesAfter;

                long dumpStart =
                    Math.Max(
                        objectStart,
                        requestedStart);

                long dumpEnd =
                    Math.Min(
                        objectEnd,
                        requestedEnd);

                long lengthLong =
                    dumpEnd - dumpStart;

                if (lengthLong <= 0 ||
                    lengthLong > int.MaxValue)
                {
                    return;
                }

                int length =
                    (int)lengthLong;

                reader.Position =
                    dumpStart;

                byte[] raw =
                    reader.ReadBytes(length);

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "[UNITY 6 TARGET TEXTURE RAW WINDOW]");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    $"File: {SafeFileName(reader)}");

                sb.AppendLine(
                    $"PathID: {reader.m_PathID}");

                sb.AppendLine(
                    $"Unity: {SafeVersion(reader)}");

                sb.AppendLine();

                sb.AppendLine(
                    $"ObjectStart:      0x{objectStart:X}");

                sb.AppendLine(
                    $"ObjectEnd:        0x{objectEnd:X}");

                sb.AppendLine(
                    $"ObjectSize:       {reader.byteSize}");

                sb.AppendLine(
                    $"AfterTextureBase: 0x{afterTextureBase:X}");

                sb.AppendLine(
                    $"RelativeAfterBase: {afterTextureBase - objectStart}");

                sb.AppendLine();

                sb.AppendLine(
                    $"DumpStart:        0x{dumpStart:X}");

                sb.AppendLine(
                    $"DumpEnd:          0x{dumpEnd:X}");

                sb.AppendLine(
                    $"DumpLength:       {length}");

                sb.AppendLine(
                    $"BytesBeforeBase:  {afterTextureBase - dumpStart}");

                sb.AppendLine(
                    $"BytesAfterBase:   {dumpEnd - afterTextureBase}");

                sb.AppendLine();

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine(
                    "[RAW BYTES]");

                sb.AppendLine(
                    "------------------------------------------------------------");

                /*
                 * Dump personalizzato.
                 *
                 * Mostriamo contemporaneamente:
                 *
                 *   - offset rispetto all'inizio della finestra
                 *   - offset rispetto ad AfterTextureBase
                 *   - indirizzo assoluto
                 *   - HEX
                 *   - ASCII
                 */

                for (int offset = 0;
                     offset < raw.Length;
                     offset += 16)
                {
                    int count =
                        Math.Min(
                            16,
                            raw.Length - offset);

                    long absolute =
                        dumpStart + offset;

                    long relativeToBase =
                        absolute - afterTextureBase;

                    sb.Append(
                        $"+0x{offset:X4} ");

                    sb.Append(
                        $"BASE{relativeToBase:+#;-#;0} ");

                    sb.Append(
                        $"ABS=0x{absolute:X} | ");

                    for (int i = 0;
                         i < 16;
                         i++)
                    {
                        if (i < count)
                        {
                            sb.Append(
                                $"{raw[offset + i]:X2} ");
                        }
                        else
                        {
                            sb.Append(
                                "   ");
                        }
                    }

                    sb.Append(
                        "| ");

                    for (int i = 0;
                         i < count;
                         i++)
                    {
                        byte b =
                            raw[offset + i];

                        if (b >= 32 &&
                            b <= 126)
                        {
                            sb.Append(
                                (char)b);
                        }
                        else
                        {
                            sb.Append('.');
                        }
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine(
                    "[INT32 VIEW - ALIGNED TO DUMP START]");

                sb.AppendLine(
                    "------------------------------------------------------------");

                for (int offset = 0;
                     offset + 4 <= raw.Length;
                     offset += 4)
                {
                    int value =
                        BitConverter.ToInt32(
                            raw,
                            offset);

                    uint unsignedValue =
                        BitConverter.ToUInt32(
                            raw,
                            offset);

                    float floatValue =
                        BitConverter.ToSingle(
                            raw,
                            offset);

                    long absolute =
                        dumpStart + offset;

                    long relativeToBase =
                        absolute - afterTextureBase;

                    sb.AppendLine(
                        $"+0x{offset:X4} | " +
                        $"BASE{relativeToBase:+#;-#;0} | " +
                        $"ABS=0x{absolute:X} | " +
                        $"HEX=0x{unsignedValue:X8} | " +
                        $"I32={value} | " +
                        $"U32={unsignedValue} | " +
                        $"F32={floatValue:R}");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine(
                    "[KNOWN CURRENT PARSER BOUNDARY]");

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine(
                    $"AfterTextureBase = 0x{afterTextureBase:X}");

                sb.AppendLine(
                    "Current parser interprets:");

                sb.AppendLine(
                    "BASE+0x00 -> m_Width");

                sb.AppendLine(
                    "BASE+0x04 -> m_Height");

                sb.AppendLine(
                    "BASE+0x08 -> m_CompleteImageSize");

                sb.AppendLine(
                    "BASE+0x0C -> m_MipsStripped");

                sb.AppendLine(
                    "BASE+0x10 -> m_TextureFormat");

                sb.AppendLine(
                    "BASE+0x14 -> m_MipCount");

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();

                WriteLog(
                    sb.ToString());
            }
            catch (Exception ex)
            {
                try
                {
                    StringBuilder sb =
                        new StringBuilder();

                    sb.AppendLine(
                        "[UNITY 6 TARGET TEXTURE RAW WINDOW ERROR]");

                    sb.AppendLine(
                        $"PathID: {reader.m_PathID}");

                    sb.AppendLine(
                        $"Type: {ex.GetType().FullName}");

                    sb.AppendLine(
                        $"Message: {ex.Message}");

                    sb.AppendLine();

                    WriteLog(
                        sb.ToString());
                }
                catch
                {
                    // La diagnostica non deve interferire
                    // con il parser.
                }
            }
            finally
            {
                /*
                 * FONDAMENTALE:
                 * ripristiniamo SEMPRE la posizione originale.
                 */

                reader.Position =
                    originalPosition;
            }
        }

        private static void LogBaseBoundary(
            ObjectReader reader,
            long position)
        {
            if (!IsUnity6(reader))
                return;

            try
            {
                byte[] raw =
                    ReadDiagnosticBytes(
                        reader,
                        position,
                        64);

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "[TEXTURE2D BASE BOUNDARY]");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    $"File: {SafeFileName(reader)}");

                sb.AppendLine(
                    $"PathID: {reader.m_PathID}");

                sb.AppendLine(
                    $"Unity: {SafeVersion(reader)}");

                sb.AppendLine();

                sb.AppendLine(
                    $"ObjectStart: 0x{reader.byteStart:X}");

                sb.AppendLine(
                    $"ObjectSize: {reader.byteSize}");

                sb.AppendLine(
                    $"ObjectEnd: 0x{reader.byteStart + (long)reader.byteSize:X}");

                sb.AppendLine(
                    $"AfterTextureBase: 0x{position:X}");

                sb.AppendLine(
                    $"RelativeAfterBase: {position - reader.byteStart}");

                sb.AppendLine();

                sb.AppendLine(
                    "[FIRST 64 BYTES AFTER Texture BASE]");

                sb.Append(
                    HexDump(
                        raw,
                        position));

                sb.AppendLine();

                sb.AppendLine(
                    "[INT32 VIEW]");

                sb.Append(
                    Int32View(raw));

                sb.AppendLine();

                WriteLog(
                    sb.ToString());
            }
            catch
            {
            }
        }

        private static void LogTextureHeader(
            ObjectReader reader,
            long headerStart,
            long headerEnd,
            int width,
            int height,
            int completeImageSize,
            int mipsStripped,
            TextureFormat textureFormat,
            int mipCount)
        {
            if (!IsUnity6(reader))
                return;

            try
            {
                byte[] raw =
                    ReadDiagnosticBytes(
                        reader,
                        headerStart,
                        (int)Math.Min(
                            64,
                            Math.Max(
                                0,
                                headerEnd - headerStart)));

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine(
                    "[PARSED TEXTURE2D HEADER]");

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine(
                    $"PathID: {reader.m_PathID}");

                sb.AppendLine(
                    $"HeaderStart: 0x{headerStart:X}");

                sb.AppendLine(
                    $"HeaderEnd: 0x{headerEnd:X}");

                sb.AppendLine(
                    $"HeaderBytesRead: {headerEnd - headerStart}");

                sb.AppendLine();

                sb.AppendLine(
                    $"m_Width: {width}");

                sb.AppendLine(
                    $"m_Height: {height}");

                sb.AppendLine(
                    $"m_CompleteImageSize: {completeImageSize}");

                sb.AppendLine(
                    $"m_MipsStripped: {mipsStripped}");

                sb.AppendLine(
                    $"m_TextureFormat: {textureFormat} ({(int)textureFormat})");

                sb.AppendLine(
                    $"m_MipCount: {mipCount}");

                sb.AppendLine();

                long pixelCount =
                    (long)width * height;

                sb.AppendLine(
                    $"PixelCount: {pixelCount}");

                if ((int)textureFormat == 1)
                {
                    sb.AppendLine(
                        $"Alpha8ExpectedBytes: {pixelCount}");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "[RAW HEADER BYTES]");

                sb.Append(
                    HexDump(
                        raw,
                        headerStart));

                sb.AppendLine();

                sb.AppendLine(
                    "[RAW HEADER INT32 VIEW]");

                sb.Append(
                    Int32View(raw));

                sb.AppendLine();

                WriteLog(
                    sb.ToString());
            }
            catch
            {
            }
        }

        private static void LogImageData(
            ObjectReader reader,
            int imageDataSize,
            StreamingInfo streamData,
            ResourceReader resourceReader)
        {
            if (!IsUnity6(reader))
                return;

            try
            {
                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine(
                    "[IMAGE DATA]");

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine(
                    $"PathID: {reader.m_PathID}");

                sb.AppendLine(
                    $"image_data_size: {imageDataSize}");

                if (streamData != null)
                {
                    sb.AppendLine(
                        $"Stream.Offset: {streamData.offset}");

                    sb.AppendLine(
                        $"Stream.Size: {streamData.size}");

                    sb.AppendLine(
                        $"Stream.Path: {streamData.path ?? "<NULL>"}");
                }
                else
                {
                    sb.AppendLine(
                        "StreamData: <NULL>");
                }

                if (resourceReader != null)
                {
                    sb.AppendLine(
                        $"ResourceReader.Size: {resourceReader.Size}");
                }
                else
                {
                    sb.AppendLine(
                        "ResourceReader.Size: <NULL>");
                }

                sb.AppendLine();

                WriteLog(
                    sb.ToString());
            }
            catch
            {
            }
        }
        private static void Unity6TargetTypeTreeDiagnostic(
    ObjectReader reader)
        {
            // SOLO la texture che stiamo studiando.
            const long TargetPathID = -7735706617283584571L;

            if (reader.m_PathID != TargetPathID)
                return;

            if (reader.version == null ||
                reader.version.Length == 0 ||
                reader.version[0] < 6000)
                return;

            long savedPosition = reader.Position;

            try
            {
                var sb = new StringBuilder();

                sb.AppendLine();
                sb.AppendLine(
                    "============================================================");
                sb.AppendLine(
                    "UNITY 6 TARGET TEXTURE2D - TYPETREE RAW DECODE");
                sb.AppendLine(
                    "============================================================");

                sb.AppendLine($"File: {reader.assetsFile.fileName}");
                sb.AppendLine($"PathID: {reader.m_PathID}");
                sb.AppendLine(
                    $"Unity: {string.Join(".", reader.version)}");

                sb.AppendLine();
                sb.AppendLine($"SavedParserPosition: 0x{savedPosition:X}");
                sb.AppendLine($"ObjectStart: 0x{reader.byteStart:X}");
                sb.AppendLine($"ObjectSize: {reader.byteSize}");
                sb.AppendLine();

                // ========================================================
                // PARTIAMO DALL'INIZIO REALE DELL'OGGETTO
                // ========================================================

                reader.Position = reader.byteStart;

                void Pos(string field, long start, long end, object value)
                {
                    sb.AppendLine(
                        $"{field,-30} " +
                        $"START=0x{start:X} " +
                        $"END=0x{end:X} " +
                        $"SIZE={end - start,-5} " +
                        $"VALUE={value}");
                }

                // --------------------------------------------------------
                // #001 m_Name
                // --------------------------------------------------------

                long p = reader.Position;
                string ttName = reader.ReadAlignedString();

                Pos(
                    "m_Name",
                    p,
                    reader.Position,
                    $"\"{ttName}\"");

                // --------------------------------------------------------
                // #005 m_IsAlphaChannelOptional
                // META 0x4000 -> align after field
                // --------------------------------------------------------

                p = reader.Position;

                bool ttAlphaOptional =
                    reader.ReadBoolean();

                reader.AlignStream();

                Pos(
                    "m_IsAlphaChannelOptional",
                    p,
                    reader.Position,
                    ttAlphaOptional);

                // --------------------------------------------------------
                // #006 m_Width
                // --------------------------------------------------------

                p = reader.Position;
                int ttWidth = reader.ReadInt32();

                Pos(
                    "m_Width",
                    p,
                    reader.Position,
                    ttWidth);

                // #007 m_Height

                p = reader.Position;
                int ttHeight = reader.ReadInt32();

                Pos(
                    "m_Height",
                    p,
                    reader.Position,
                    ttHeight);

                // #008 m_CompleteImageSize

                p = reader.Position;
                uint ttCompleteImageSize =
                    reader.ReadUInt32();

                Pos(
                    "m_CompleteImageSize",
                    p,
                    reader.Position,
                    ttCompleteImageSize);

                // #009 m_MipsStripped

                p = reader.Position;
                int ttMipsStripped =
                    reader.ReadInt32();

                Pos(
                    "m_MipsStripped",
                    p,
                    reader.Position,
                    ttMipsStripped);

                // #010 m_TextureFormat

                p = reader.Position;
                int ttTextureFormat =
                    reader.ReadInt32();

                Pos(
                    "m_TextureFormat",
                    p,
                    reader.Position,
                    ttTextureFormat);

                // #011 m_MipCount

                p = reader.Position;
                int ttMipCount =
                    reader.ReadInt32();

                Pos(
                    "m_MipCount",
                    p,
                    reader.Position,
                    ttMipCount);

                // --------------------------------------------------------
                // #012 - #014 BOOL
                //
                // TypeTree:
                // m_IsReadable
                // m_IsPreProcessed
                // m_IgnoreMipmapLimit
                //
                // m_IgnoreMipmapLimit ha META 0x4000:
                // allineiamo DOPO il terzo bool.
                // --------------------------------------------------------

                p = reader.Position;

                bool ttReadable =
                    reader.ReadBoolean();

                Pos(
                    "m_IsReadable",
                    p,
                    reader.Position,
                    ttReadable);

                p = reader.Position;

                bool ttPreProcessed =
                    reader.ReadBoolean();

                Pos(
                    "m_IsPreProcessed",
                    p,
                    reader.Position,
                    ttPreProcessed);

                p = reader.Position;

                bool ttIgnoreMipmapLimit =
                    reader.ReadBoolean();

                long beforeBoolAlign =
                    reader.Position;

                reader.AlignStream();

                Pos(
                    "m_IgnoreMipmapLimit",
                    p,
                    reader.Position,
                    $"{ttIgnoreMipmapLimit} " +
                    $"(dataEnd=0x{beforeBoolAlign:X})");

                // --------------------------------------------------------
                // #015 m_MipmapLimitGroupName
                // string è aligned
                // --------------------------------------------------------

                p = reader.Position;

                string ttMipmapLimitGroupName =
                    reader.ReadAlignedString();

                Pos(
                    "m_MipmapLimitGroupName",
                    p,
                    reader.Position,
                    $"\"{ttMipmapLimitGroupName}\"");

                // --------------------------------------------------------
                // #019 m_StreamingMipmaps
                // META 0x4000 -> align
                // --------------------------------------------------------

                p = reader.Position;

                bool ttStreamingMipmaps =
                    reader.ReadBoolean();

                reader.AlignStream();

                Pos(
                    "m_StreamingMipmaps",
                    p,
                    reader.Position,
                    ttStreamingMipmaps);

                // --------------------------------------------------------
                // #020 m_StreamingMipmapsPriority
                // --------------------------------------------------------

                p = reader.Position;

                int ttStreamingPriority =
                    reader.ReadInt32();

                Pos(
                    "m_StreamingMipmapsPriority",
                    p,
                    reader.Position,
                    ttStreamingPriority);

                // #021 m_ImageCount

                p = reader.Position;

                int ttImageCount =
                    reader.ReadInt32();

                Pos(
                    "m_ImageCount",
                    p,
                    reader.Position,
                    ttImageCount);

                // #022 m_TextureDimension

                p = reader.Position;

                int ttTextureDimension =
                    reader.ReadInt32();

                Pos(
                    "m_TextureDimension",
                    p,
                    reader.Position,
                    ttTextureDimension);

                // ========================================================
                // #023 GLTextureSettings
                // ========================================================

                sb.AppendLine();
                sb.AppendLine(
                    "----- GLTextureSettings -----");

                p = reader.Position;
                int ttFilterMode = reader.ReadInt32();

                Pos(
                    "m_FilterMode",
                    p,
                    reader.Position,
                    ttFilterMode);

                p = reader.Position;
                int ttAniso = reader.ReadInt32();

                Pos(
                    "m_Aniso",
                    p,
                    reader.Position,
                    ttAniso);

                p = reader.Position;
                float ttMipBias = reader.ReadSingle();

                Pos(
                    "m_MipBias",
                    p,
                    reader.Position,
                    ttMipBias);

                p = reader.Position;
                int ttWrapU = reader.ReadInt32();

                Pos(
                    "m_WrapU",
                    p,
                    reader.Position,
                    ttWrapU);

                p = reader.Position;
                int ttWrapV = reader.ReadInt32();

                Pos(
                    "m_WrapV",
                    p,
                    reader.Position,
                    ttWrapV);

                p = reader.Position;
                int ttWrapW = reader.ReadInt32();

                Pos(
                    "m_WrapW",
                    p,
                    reader.Position,
                    ttWrapW);

                // --------------------------------------------------------
                // #030 m_LightmapFormat
                // --------------------------------------------------------

                p = reader.Position;

                int ttLightmapFormat =
                    reader.ReadInt32();

                Pos(
                    "m_LightmapFormat",
                    p,
                    reader.Position,
                    ttLightmapFormat);

                // #031 m_ColorSpace

                p = reader.Position;

                int ttColorSpace =
                    reader.ReadInt32();

                Pos(
                    "m_ColorSpace",
                    p,
                    reader.Position,
                    ttColorSpace);

                // ========================================================
                // #032 m_PlatformBlob
                // vector<UInt8>
                // ========================================================

                sb.AppendLine();
                sb.AppendLine(
                    "----- m_PlatformBlob -----");

                p = reader.Position;

                int platformBlobSize =
                    reader.ReadInt32();

                sb.AppendLine(
                    $"m_PlatformBlob.size           " +
                    $"START=0x{p:X} " +
                    $"END=0x{reader.Position:X} " +
                    $"VALUE={platformBlobSize}");

                if (platformBlobSize < 0 ||
                    platformBlobSize > reader.byteSize)
                {
                    throw new Exception(
                        $"Invalid PlatformBlob size: {platformBlobSize}");
                }

                long blobDataStart =
                    reader.Position;

                reader.ReadBytes(
                    platformBlobSize);

                reader.AlignStream();

                sb.AppendLine(
                    $"m_PlatformBlob.data           " +
                    $"START=0x{blobDataStart:X} " +
                    $"END=0x{reader.Position:X} " +
                    $"SIZE={platformBlobSize}");

                // ========================================================
                // #036 image data
                // TypelessData
                // ========================================================

                sb.AppendLine();
                sb.AppendLine(
                    "----- image data -----");

                p = reader.Position;

                int imageDataSize =
                    reader.ReadInt32();

                sb.AppendLine(
                    $"image data.size               " +
                    $"START=0x{p:X} " +
                    $"END=0x{reader.Position:X} " +
                    $"VALUE={imageDataSize}");

                if (imageDataSize < 0 ||
                    imageDataSize > reader.byteSize)
                {
                    throw new Exception(
                        $"Invalid image data size: {imageDataSize}");
                }

                long imageDataStart =
                    reader.Position;

                // Non ci interessa copiare 49 KB nel log.
                // Li attraversiamo soltanto.
                reader.Position += imageDataSize;

                reader.AlignStream();

                sb.AppendLine(
                    $"image data.data               " +
                    $"START=0x{imageDataStart:X} " +
                    $"END=0x{reader.Position:X} " +
                    $"SIZE={imageDataSize}");

                // ========================================================
                // #039 StreamingInfo
                // ========================================================

                sb.AppendLine();
                sb.AppendLine(
                    "----- StreamingInfo -----");

                p = reader.Position;

                ulong streamOffset =
                    reader.ReadUInt64();

                Pos(
                    "m_StreamData.offset",
                    p,
                    reader.Position,
                    streamOffset);

                p = reader.Position;

                uint streamSize =
                    reader.ReadUInt32();

                Pos(
                    "m_StreamData.size",
                    p,
                    reader.Position,
                    streamSize);

                p = reader.Position;

                string streamPath =
                    reader.ReadAlignedString();

                Pos(
                    "m_StreamData.path",
                    p,
                    reader.Position,
                    $"\"{streamPath}\"");

                // ========================================================
                // FINAL CHECK
                // ========================================================

                sb.AppendLine();
                sb.AppendLine(
                    "----- FINAL -----");

                sb.AppendLine(
                    $"FinalPosition: 0x{reader.Position:X}");

                sb.AppendLine(
                    $"ExpectedObjectEnd: " +
                    $"0x{(reader.byteStart + reader.byteSize):X}");

                sb.AppendLine(
                    $"BytesRemaining: " +
                    $"{(reader.byteStart + reader.byteSize) - reader.Position}");

                sb.AppendLine();

                sb.AppendLine(
                    $"DIMENSION CHECK: " +
                    $"{ttWidth} x {ttHeight}");

                sb.AppendLine(
                    $"FORMAT VALUE: {ttTextureFormat}");

                sb.AppendLine(
                    $"COMPLETE IMAGE SIZE: {ttCompleteImageSize}");

                sb.AppendLine(
                    $"INLINE IMAGE DATA SIZE: {imageDataSize}");

                sb.AppendLine(
                    $"STREAM SIZE: {streamSize}");

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                // ========================================================
                // OUTPUT
                // ========================================================

                string diagnosticPath =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "TEXTURE2D_TYPETREE_RAW_DECODE.txt");

                File.WriteAllText(
                    diagnosticPath,
                    sb.ToString(),
                    Encoding.UTF8);

                Logger.Info(
                    $"[TEXTURE2D-TYPETREE-RAW] " +
                    $"Saved diagnostic | " +
                    $"PathID={reader.m_PathID} | " +
                    $"Output={diagnosticPath}");
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"[TEXTURE2D-TYPETREE-RAW] " +
                    $"Diagnostic failed | " +
                    $"PathID={reader.m_PathID} | " +
                    $"{ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                // FONDAMENTALE:
                // il parser normale deve ritrovare esattamente
                // la posizione che aveva prima del diagnostico.
                reader.Position =
                    savedPosition;
            }
        }
        public Texture2D(ObjectReader reader)
            : base(reader)
        {
            if (version[0] >= 6000)
            {
                ReadUnity6Texture2D(reader);
                return;
            }


            Unity6TargetTypeTreeDiagnostic(reader);
            /*
             * ============================================================
             * IMPORTANTISSIMO
             *
             * A questo punto NamedObject + Texture sono già stati letti.
             *
             * Non cambiamo reader.Position.
             * Copiamo soltanto alcuni byte e poi la funzione diagnostica
             * ripristina la posizione originale.
             * ============================================================
             */

            long texture2DHeaderStart =
                reader.Position;

            LogBaseBoundary(
                reader,
                texture2DHeaderStart);

            LogTargetTextureRawWindow(
                reader,
                texture2DHeaderStart);

            // ============================================================
            // HEADER ORIGINALE
            // ============================================================

            m_Width =
                reader.ReadInt32();

            m_Height =
                reader.ReadInt32();

            var m_CompleteImageSize =
                reader.ReadInt32();

            int diagnosticMipsStripped = -1;

            if (version[0] >= 2020)
            {
                var m_MipsStripped =
                    reader.ReadInt32();

                diagnosticMipsStripped =
                    m_MipsStripped;
            }

            m_TextureFormat =
                (TextureFormat)reader.ReadInt32();

            if (version[0] < 5 ||
                (version[0] == 5 &&
                 version[1] < 2))
            {
                m_MipMap =
                    reader.ReadBoolean();
            }
            else
            {
                m_MipCount =
                    reader.ReadInt32();
            }

            long texture2DHeaderEnd =
                reader.Position;

            LogTextureHeader(
                reader,
                texture2DHeaderStart,
                texture2DHeaderEnd,
                m_Width,
                m_Height,
                m_CompleteImageSize,
                diagnosticMipsStripped,
                m_TextureFormat,
                m_MipCount);

            // ============================================================
            // RESTO DEL PARSER ORIGINALE
            // ============================================================

            if (version[0] > 2 ||
                (version[0] == 2 &&
                 version[1] >= 6))
            {
                var m_IsReadable =
                    reader.ReadBoolean();

                if (reader.Game.Type.IsGI() &&
                    HasGNFTexture(
                        reader.serializedType))
                {
                    var m_IsGNFTexture =
                        reader.ReadBoolean();
                }
            }

            if (version[0] >= 2020)
            {
                var m_IsPreProcessed =
                    reader.ReadBoolean();
            }

            // ============================================================
            // UNITY 6
            //
            // Nel TypeTree Unity 6000 questo campo si chiama:
            // m_IgnoreMipmapLimit
            //
            // Nelle versioni precedenti AssetStudio lo identifica come:
            // m_IgnoreMasterTextureLimit
            //
            // La dimensione serializzata rimane 1 bool.
            // ============================================================

            if (version[0] >= 6000)
            {
                var m_IgnoreMipmapLimit =
                    reader.ReadBoolean();
            }
            else if (version[0] > 2019 ||
                     (version[0] == 2019 &&
                      version[1] >= 3))
            {
                var m_IgnoreMasterTextureLimit =
                    reader.ReadBoolean();
            }

            // ============================================================
            // UNITY 2022.2+ / UNITY 6
            // ============================================================

            if ((version[0] == 2022 &&
                 version[1] >= 2) ||
                version[0] >= 6000)
            {
                reader.AlignStream();

                var m_MipmapLimitGroupName =
                    reader.ReadAlignedString();
            }

            if (version[0] >= 3)
            {
                if (version[0] < 5 ||
                    (version[0] == 5 &&
                     version[1] <= 4))
                {
                    var m_ReadAllowed =
                        reader.ReadBoolean();
                }
            }

            if (version[0] > 2018 ||
                (version[0] == 2018 &&
                 version[1] >= 2))
            {
                var m_StreamingMipmaps =
                    reader.ReadBoolean();
            }

            reader.AlignStream();

            if (reader.Game.Type.IsGI() &&
                HasGNFTexture(
                    reader.serializedType))
            {
                var m_TextureGroup =
                    reader.ReadInt32();
            }

            // ============================================================
            // UNITY 6 FIX GIÀ CONFERMATO NEL PROGETTO
            // ============================================================

            if ((version[0] > 2018 &&
                 version[0] < 6000) ||
                (version[0] == 2018 &&
                 version[1] >= 2))
            {
                var m_StreamingMipmapsPriority =
                    reader.ReadInt32();
            }

            var m_ImageCount =
                reader.ReadInt32();

            var m_TextureDimension =
                reader.ReadInt32();

            m_TextureSettings =
                new GLTextureSettings(reader);

            if (version[0] >= 3)
            {
                var m_LightmapFormat =
                    reader.ReadInt32();
            }

            if (version[0] > 3 ||
                (version[0] == 3 &&
                 version[1] >= 5))
            {
                var m_ColorSpace =
                    reader.ReadInt32();
            }

            if (version[0] > 2020 ||
                (version[0] == 2020 &&
                 version[1] >= 2))
            {
                var m_PlatformBlob =
                    reader.ReadUInt8Array();

                reader.AlignStream();
            }

            var image_data_size =
                reader.ReadInt32();

            if (image_data_size == 0 &&
                ((version[0] == 5 &&
                  version[1] >= 3) ||
                 version[0] > 5))
            {
                if (reader.Game.Type.IsGI() &&
                    HasExternalMipRelativeOffset(
                        reader.serializedType))
                {
                    var m_externalMipRelativeOffset =
                        reader.ReadUInt32();
                }

                m_StreamData =
                    new StreamingInfo(reader);
            }

            ResourceReader resourceReader;

            if (!string.IsNullOrEmpty(
                    m_StreamData?.path))
            {
                resourceReader =
                    new ResourceReader(
                        m_StreamData.path,
                        assetsFile,
                        m_StreamData.offset,
                        m_StreamData.size
                    );
            }
            else
            {
                resourceReader =
                    new ResourceReader(
                        reader,
                        reader.BaseStream.Position,
                        image_data_size
                    );
            }

            image_data =
                resourceReader;

            LogImageData(
                reader,
                image_data_size,
                m_StreamData,
                resourceReader);
        }
    }

    public enum TextureFormat
    {
        Alpha8 = 1,
        ARGB4444,
        RGB24,
        RGBA32,
        ARGB32,
        ARGBFloat,
        RGB565,
        BGR24,
        R16,
        DXT1,
        DXT3,
        DXT5,
        RGBA4444,
        BGRA32,
        RHalf,
        RGHalf,
        RGBAHalf,
        RFloat,
        RGFloat,
        RGBAFloat,
        YUY2,
        RGB9e5Float,
        RGBFloat,
        BC6H,
        BC7,
        BC4,
        BC5,
        DXT1Crunched,
        DXT5Crunched,
        PVRTC_RGB2,
        PVRTC_RGBA2,
        PVRTC_RGB4,
        PVRTC_RGBA4,
        ETC_RGB4,
        ATC_RGB4,
        ATC_RGBA8,

        EAC_R = 41,
        EAC_R_SIGNED,
        EAC_RG,
        EAC_RG_SIGNED,
        ETC2_RGB,
        ETC2_RGBA1,
        ETC2_RGBA8,

        ASTC_RGB_4x4,
        ASTC_RGB_5x5,
        ASTC_RGB_6x6,
        ASTC_RGB_8x8,
        ASTC_RGB_10x10,
        ASTC_RGB_12x12,

        ASTC_RGBA_4x4,
        ASTC_RGBA_5x5,
        ASTC_RGBA_6x6,
        ASTC_RGBA_8x8,
        ASTC_RGBA_10x10,
        ASTC_RGBA_12x12,

        ETC_RGB4_3DS,
        ETC_RGBA8_3DS,

        RG16,
        R8,

        ETC_RGB4Crunched,
        ETC2_RGBA8Crunched,

        R16_Alt,

        ASTC_HDR_4x4,
        ASTC_HDR_5x5,
        ASTC_HDR_6x6,
        ASTC_HDR_8x8,
        ASTC_HDR_10x10,
        ASTC_HDR_12x12,

        RG32,
        RGB48,
        RGBA64
    }
}