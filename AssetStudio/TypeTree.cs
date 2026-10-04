using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssetStudio
{
    public class TypeTree
    {
        // ============================================================
        // ORIGINAL RAZTOOLS / ASSETSTUDIO FIELDS
        // ============================================================

        public List<TypeTreeNode> m_Nodes;
        public byte[] m_StringBuffer;


        // ============================================================
        // UNITY 6 - TEXTURE2D CLASSID 28 DIAGNOSTIC
        //
        // Produce un solo dump Texture2D per esecuzione.
        //
        // NON:
        // - modifica il reader
        // - modifica i nodi
        // - modifica la deserializzazione
        // - calcola offset inventati
        //
        // Serve esclusivamente a vedere il TypeTree reale dichiarato
        // dal file Unity 6.
        // ============================================================

        private static bool _texture2DUnity6Dumped = false;


        public static void DumpTexture2DTypeTree(
            string assetsFileName,
            int[] version,
            int classID,
            TypeTree typeTree)
        {
            try
            {
                // ----------------------------------------------------
                // ClassID 28 = Texture2D
                // ----------------------------------------------------

                if (classID != 28)
                {
                    return;
                }


                // ----------------------------------------------------
                // Solo Unity 6+
                // ----------------------------------------------------

                if (version == null ||
                    version.Length == 0 ||
                    version[0] < 6000)
                {
                    return;
                }


                // ----------------------------------------------------
                // Un solo dump per esecuzione
                // ----------------------------------------------------

                if (_texture2DUnity6Dumped)
                {
                    return;
                }


                // ----------------------------------------------------
                // TypeTree valido?
                // ----------------------------------------------------

                if (typeTree == null ||
                    typeTree.m_Nodes == null ||
                    typeTree.m_Nodes.Count == 0)
                {
                    return;
                }


                _texture2DUnity6Dumped = true;


                const string outputFile =
                    "TEXTURE2D_CLASSID28_UNITY6_TYPETREE.txt";


                List<TypeTreeNode> nodes =
                    typeTree.m_Nodes;


                StringBuilder sb =
                    new StringBuilder();


                // ====================================================
                // HEADER
                // ====================================================

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "UNITY 6 TEXTURE2D - CLASSID 28 - TYPETREE");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();

                sb.AppendLine(
                    $"Source file : {Safe(assetsFileName)}");

                sb.AppendLine(
                    $"ClassID     : {classID}");

                sb.AppendLine(
                    $"Unity       : {FormatVersion(version)}");

                sb.AppendLine(
                    $"Node count  : {nodes.Count}");

                sb.AppendLine();

                sb.AppendLine(
                    "Diagnostic purpose:");

                sb.AppendLine(
                    "Dump the REAL serialized Texture2D TypeTree stored in the Unity file.");

                sb.AppendLine();

                sb.AppendLine(
                    "This diagnostic does NOT modify:");

                sb.AppendLine(
                    "- reader position");

                sb.AppendLine(
                    "- TypeTree nodes");

                sb.AppendLine(
                    "- object deserialization");

                sb.AppendLine(
                    "- texture data");

                sb.AppendLine();


                // ====================================================
                // FULL RAW NODE LIST
                // ====================================================

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "FULL NODE LIST");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();

                sb.AppendLine(
                    "INDEX | LEVEL | VERSION | FLAGS | BYTE SIZE | INDEX FIELD | META | REF HASH | TYPE | NAME");

                sb.AppendLine(
                    "------------------------------------------------------------");


                for (int i = 0;
                     i < nodes.Count;
                     i++)
                {
                    TypeTreeNode node =
                        nodes[i];


                    sb.Append("#");

                    sb.Append(
                        i.ToString("D3"));


                    sb.Append(" | LEVEL=");

                    sb.Append(
                        node.m_Level);


                    sb.Append(" | VERSION=");

                    sb.Append(
                        node.m_Version);


                    sb.Append(" | FLAGS=");

                    sb.Append(
                        node.m_TypeFlags);


                    sb.Append(" | SIZE=");

                    sb.Append(
                        node.m_ByteSize);


                    sb.Append(" | INDEX=");

                    sb.Append(
                        node.m_Index);


                    sb.Append(" | META=0x");

                    sb.Append(
                        node.m_MetaFlag.ToString("X8"));


                    sb.Append(" | REFHASH=0x");

                    sb.Append(
                        node.m_RefTypeHash.ToString("X16"));


                    sb.Append(" | TYPE=");

                    sb.Append(
                        Safe(node.m_Type));


                    sb.Append(" | NAME=");

                    sb.Append(
                        Safe(node.m_Name));


                    sb.AppendLine();
                }


                // ====================================================
                // HIERARCHICAL VIEW
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "HIERARCHICAL VIEW");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                for (int i = 0;
                     i < nodes.Count;
                     i++)
                {
                    TypeTreeNode node =
                        nodes[i];


                    int level =
                        node.m_Level;


                    if (level < 0)
                    {
                        level = 0;
                    }


                    sb.Append(
                        new string(
                            ' ',
                            level * 4));


                    sb.Append("#");

                    sb.Append(
                        i.ToString("D3"));


                    sb.Append(" ");


                    sb.Append(
                        Safe(node.m_Type));


                    sb.Append(" ");


                    sb.Append(
                        Safe(node.m_Name));


                    sb.Append(
                        "  [size=");


                    sb.Append(
                        node.m_ByteSize);


                    sb.Append(
                        "]");


                    if (node.m_MetaFlag != 0)
                    {
                        sb.Append(
                            "  META=0x");

                        sb.Append(
                            node.m_MetaFlag.ToString("X8"));
                    }


                    sb.AppendLine();
                }


                // ====================================================
                // ROOT
                // ====================================================

                int rootIndex =
                    FindTexture2DRoot(nodes);


                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "TEXTURE2D ROOT");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                if (rootIndex >= 0)
                {
                    TypeTreeNode root =
                        nodes[rootIndex];


                    sb.AppendLine(
                        $"Index     : #{rootIndex:D3}");

                    sb.AppendLine(
                        $"Level     : {root.m_Level}");

                    sb.AppendLine(
                        $"Version   : {root.m_Version}");

                    sb.AppendLine(
                        $"TypeFlags : {root.m_TypeFlags}");

                    sb.AppendLine(
                        $"ByteSize  : {root.m_ByteSize}");

                    sb.AppendLine(
                        $"IndexField: {root.m_Index}");

                    sb.AppendLine(
                        $"MetaFlags : 0x{root.m_MetaFlag:X8}");

                    sb.AppendLine(
                        $"RefHash   : 0x{root.m_RefTypeHash:X16}");

                    sb.AppendLine(
                        $"Type      : {Safe(root.m_Type)}");

                    sb.AppendLine(
                        $"Name      : {Safe(root.m_Name)}");
                }
                else
                {
                    sb.AppendLine(
                        "Texture2D root node not found by type/name.");
                }


                // ====================================================
                // DIRECT CHILDREN
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "DIRECT CHILDREN OF TEXTURE2D");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                if (rootIndex >= 0)
                {
                    int rootLevel =
                        nodes[rootIndex].m_Level;

                    int childLevel =
                        rootLevel + 1;

                    int ordinal =
                        0;


                    for (int i = rootIndex + 1;
                         i < nodes.Count;
                         i++)
                    {
                        TypeTreeNode node =
                            nodes[i];


                        if (node.m_Level <= rootLevel)
                        {
                            break;
                        }


                        if (node.m_Level != childLevel)
                        {
                            continue;
                        }


                        sb.Append(
                            "[");

                        sb.Append(
                            ordinal.ToString("D2"));

                        sb.Append(
                            "] ");


                        sb.Append("#");

                        sb.Append(
                            i.ToString("D3"));


                        sb.Append(
                            " | TYPE=");

                        sb.Append(
                            Safe(node.m_Type));


                        sb.Append(
                            " | NAME=");

                        sb.Append(
                            Safe(node.m_Name));


                        sb.Append(
                            " | SIZE=");

                        sb.Append(
                            node.m_ByteSize);


                        sb.Append(
                            " | META=0x");

                        sb.Append(
                            node.m_MetaFlag.ToString("X8"));


                        sb.AppendLine();


                        ordinal++;
                    }
                }
                else
                {
                    sb.AppendLine(
                        "Cannot enumerate direct children because Texture2D root was not found.");
                }


                // ====================================================
                // IMPORTANT FIELD SEARCH
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "IMPORTANT TEXTURE FIELDS");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                string[] importantFields =
                {
                    "m_Name",

                    "m_ForcedFallbackFormat",
                    "m_DownscaleFallback",
                    "m_IsAlphaChannelOptional",

                    "m_Width",
                    "m_Height",

                    "m_CompleteImageSize",

                    "m_MipsStripped",

                    "m_TextureFormat",

                    "m_MipCount",
                    "m_MipMap",

                    "m_IsReadable",

                    "m_IsPreProcessed",

                    "m_IgnoreMasterTextureLimit",

                    "m_MipmapLimitGroupName",

                    "m_StreamingMipmaps",

                    "m_StreamingMipmapsPriority",

                    "m_ImageCount",

                    "m_TextureDimension",

                    "m_TextureSettings",

                    "m_FilterMode",

                    "m_Aniso",

                    "m_MipBias",

                    "m_WrapMode",

                    "m_WrapU",

                    "m_WrapV",

                    "m_WrapW",

                    "m_LightmapFormat",

                    "m_ColorSpace",

                    "m_PlatformBlob",

                    "image data",

                    "m_StreamData",

                    "offset",

                    "size",

                    "path"
                };


                for (int i = 0;
                     i < nodes.Count;
                     i++)
                {
                    TypeTreeNode node =
                        nodes[i];


                    if (!ContainsField(
                            importantFields,
                            node.m_Name))
                    {
                        continue;
                    }


                    AppendNodeLine(
                        sb,
                        nodes,
                        i,
                        null);
                }


                // ====================================================
                // m_WIDTH WINDOW
                // ====================================================

                int widthIndex =
                    FindNodeByName(
                        nodes,
                        "m_Width");


                AppendWindow(
                    sb,
                    nodes,
                    widthIndex,
                    15,
                    45,
                    "WINDOW AROUND m_Width",
                    "<<< m_Width");


                // ====================================================
                // m_HEIGHT WINDOW
                // ====================================================

                int heightIndex =
                    FindNodeByName(
                        nodes,
                        "m_Height");


                AppendWindow(
                    sb,
                    nodes,
                    heightIndex,
                    15,
                    45,
                    "WINDOW AROUND m_Height",
                    "<<< m_Height");


                // ====================================================
                // COMPLETE IMAGE SIZE WINDOW
                // ====================================================

                int completeImageIndex =
                    FindNodeByName(
                        nodes,
                        "m_CompleteImageSize");


                AppendWindow(
                    sb,
                    nodes,
                    completeImageIndex,
                    15,
                    45,
                    "WINDOW AROUND m_CompleteImageSize",
                    "<<< m_CompleteImageSize");


                // ====================================================
                // FORMAT WINDOW
                // ====================================================

                int formatIndex =
                    FindNodeByName(
                        nodes,
                        "m_TextureFormat");


                AppendWindow(
                    sb,
                    nodes,
                    formatIndex,
                    20,
                    50,
                    "WINDOW AROUND m_TextureFormat",
                    "<<< m_TextureFormat");


                // ====================================================
                // MIP COUNT WINDOW
                // ====================================================

                int mipCountIndex =
                    FindNodeByName(
                        nodes,
                        "m_MipCount");


                AppendWindow(
                    sb,
                    nodes,
                    mipCountIndex,
                    20,
                    50,
                    "WINDOW AROUND m_MipCount",
                    "<<< m_MipCount");


                // ====================================================
                // IMAGE DATA WINDOW
                // ====================================================

                int imageDataIndex =
                    FindNodeByName(
                        nodes,
                        "image data");


                if (imageDataIndex < 0)
                {
                    imageDataIndex =
                        FindNodeByName(
                            nodes,
                            "image_data");
                }


                AppendWindow(
                    sb,
                    nodes,
                    imageDataIndex,
                    20,
                    30,
                    "WINDOW AROUND IMAGE DATA",
                    "<<< IMAGE DATA");


                // ====================================================
                // STREAM DATA WINDOW
                // ====================================================

                int streamDataIndex =
                    FindNodeByName(
                        nodes,
                        "m_StreamData");


                AppendWindow(
                    sb,
                    nodes,
                    streamDataIndex,
                    25,
                    35,
                    "WINDOW AROUND m_StreamData",
                    "<<< m_StreamData");


                // ====================================================
                // SERIALIZATION ORDER
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "DECLARED DIRECT FIELD ORDER");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();

                sb.AppendLine(
                    "IMPORTANT:");

                sb.AppendLine(
                    "This is the declared TypeTree field order.");

                sb.AppendLine(
                    "It is NOT a calculated absolute byte-offset table.");

                sb.AppendLine(
                    "Strings, arrays and alignment rules make naive offset summation unsafe.");

                sb.AppendLine();


                if (rootIndex >= 0)
                {
                    int rootLevel =
                        nodes[rootIndex].m_Level;

                    int childLevel =
                        rootLevel + 1;

                    int ordinal =
                        0;


                    for (int i = rootIndex + 1;
                         i < nodes.Count;
                         i++)
                    {
                        TypeTreeNode node =
                            nodes[i];


                        if (node.m_Level <= rootLevel)
                        {
                            break;
                        }


                        if (node.m_Level != childLevel)
                        {
                            continue;
                        }


                        sb.Append(
                            "[");

                        sb.Append(
                            ordinal.ToString("D2"));

                        sb.Append(
                            "] ");


                        sb.Append(
                            Safe(node.m_Type));


                        sb.Append(
                            " ");


                        sb.Append(
                            Safe(node.m_Name));


                        sb.Append(
                            " | ByteSize=");

                        sb.Append(
                            node.m_ByteSize);


                        sb.Append(
                            " | Meta=0x");

                        sb.Append(
                            node.m_MetaFlag.ToString("X8"));


                        sb.AppendLine();


                        ordinal++;
                    }
                }


                // ====================================================
                // ALL LEVEL-1 FIELDS
                //
                // Utile nel caso in cui il root non venga chiamato
                // letteralmente Texture2D.
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "ALL LEVEL-1 NODES");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                for (int i = 0;
                     i < nodes.Count;
                     i++)
                {
                    TypeTreeNode node =
                        nodes[i];


                    if (node.m_Level == 1)
                    {
                        AppendNodeLine(
                            sb,
                            nodes,
                            i,
                            null);
                    }
                }


                // ====================================================
                // ALIGNMENT FLAGS
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "NODES WITH ALIGNMENT FLAG 0x4000");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                for (int i = 0;
                     i < nodes.Count;
                     i++)
                {
                    TypeTreeNode node =
                        nodes[i];


                    if ((node.m_MetaFlag & 0x4000) != 0)
                    {
                        AppendNodeLine(
                            sb,
                            nodes,
                            i,
                            "ALIGN");
                    }
                }


                // ====================================================
                // VARIABLE-SIZE NODES
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "VARIABLE-SIZE NODES");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                for (int i = 0;
                     i < nodes.Count;
                     i++)
                {
                    TypeTreeNode node =
                        nodes[i];


                    if (node.m_ByteSize < 0)
                    {
                        AppendNodeLine(
                            sb,
                            nodes,
                            i,
                            "VARIABLE");
                    }
                }


                // ====================================================
                // STRING OFFSETS
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "RAW STRING OFFSETS");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                for (int i = 0;
                     i < nodes.Count;
                     i++)
                {
                    TypeTreeNode node =
                        nodes[i];


                    sb.Append("#");

                    sb.Append(
                        i.ToString("D3"));


                    sb.Append(
                        " | TYPE_OFFSET=0x");

                    sb.Append(
                        node.m_TypeStrOffset.ToString("X8"));


                    sb.Append(
                        " | NAME_OFFSET=0x");

                    sb.Append(
                        node.m_NameStrOffset.ToString("X8"));


                    sb.Append(
                        " | TYPE=");

                    sb.Append(
                        Safe(node.m_Type));


                    sb.Append(
                        " | NAME=");

                    sb.Append(
                        Safe(node.m_Name));


                    sb.AppendLine();
                }


                // ====================================================
                // STRING BUFFER INFO
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "STRING BUFFER INFO");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                if (typeTree.m_StringBuffer != null)
                {
                    sb.AppendLine(
                        $"StringBuffer.Length = {typeTree.m_StringBuffer.Length}");
                }
                else
                {
                    sb.AppendLine(
                        "StringBuffer = NULL");
                }


                // ====================================================
                // SUMMARY
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "FIELD INDEX SUMMARY");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine();


                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_Width");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_Height");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_CompleteImageSize");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_MipsStripped");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_TextureFormat");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_MipCount");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_IsReadable");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_IsPreProcessed");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_IgnoreMasterTextureLimit");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_MipmapLimitGroupName");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_StreamingMipmaps");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_StreamingMipmapsPriority");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_ImageCount");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_TextureDimension");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_TextureSettings");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_PlatformBlob");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "image data");

                AppendFieldIndex(
                    sb,
                    nodes,
                    "m_StreamData");


                // ====================================================
                // END
                // ====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "END OF UNITY 6 TEXTURE2D TYPE TREE");

                sb.AppendLine(
                    "============================================================");


                // ====================================================
                // WRITE
                // ====================================================

                File.WriteAllText(
                    outputFile,
                    sb.ToString());


                Logger.Info(
                    $"[TEXTURE2D-U6-TYPETREE] " +
                    $"Saved ClassID 28 TypeTree | " +
                    $"File={assetsFileName} | " +
                    $"Nodes={nodes.Count} | " +
                    $"Output={outputFile}");
            }
            catch (Exception ex)
            {
                // La diagnostica non deve MAI rompere il parser.

                try
                {
                    File.WriteAllText(
                        "TEXTURE2D_CLASSID28_UNITY6_TYPETREE_ERROR.txt",
                        ex.ToString());
                }
                catch
                {
                    // Ignora anche eventuali problemi nella scrittura
                    // del file diagnostico.
                }


                try
                {
                    Logger.Info(
                        $"[TEXTURE2D-U6-TYPETREE] Diagnostic error: {ex}");
                }
                catch
                {
                    // Non permettere al logger di interferire.
                }
            }
        }


        // ============================================================
        // HELPERS
        // ============================================================

        private static int FindTexture2DRoot(
            List<TypeTreeNode> nodes)
        {
            if (nodes == null)
            {
                return -1;
            }


            // Prima prova: type == Texture2D

            for (int i = 0;
                 i < nodes.Count;
                 i++)
            {
                if (string.Equals(
                        nodes[i].m_Type,
                        "Texture2D",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }


            // Seconda prova: name == Texture2D

            for (int i = 0;
                 i < nodes.Count;
                 i++)
            {
                if (string.Equals(
                        nodes[i].m_Name,
                        "Texture2D",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }


            // In un TypeTree normale il nodo #0 è il root.

            if (nodes.Count > 0)
            {
                return 0;
            }


            return -1;
        }


        private static int FindNodeByName(
            List<TypeTreeNode> nodes,
            string name)
        {
            if (nodes == null ||
                string.IsNullOrEmpty(name))
            {
                return -1;
            }


            for (int i = 0;
                 i < nodes.Count;
                 i++)
            {
                if (string.Equals(
                        nodes[i].m_Name,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }


            return -1;
        }


        private static bool ContainsField(
            string[] fields,
            string name)
        {
            if (fields == null ||
                string.IsNullOrEmpty(name))
            {
                return false;
            }


            for (int i = 0;
                 i < fields.Length;
                 i++)
            {
                if (string.Equals(
                        fields[i],
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            return false;
        }


        private static void AppendWindow(
            StringBuilder sb,
            List<TypeTreeNode> nodes,
            int centerIndex,
            int before,
            int after,
            string title,
            string marker)
        {
            sb.AppendLine();

            sb.AppendLine(
                "============================================================");

            sb.AppendLine(
                title);

            sb.AppendLine(
                "============================================================");

            sb.AppendLine();


            if (centerIndex < 0)
            {
                sb.AppendLine(
                    "FIELD NOT FOUND");

                return;
            }


            int start =
                Math.Max(
                    0,
                    centerIndex - before);


            int end =
                Math.Min(
                    nodes.Count - 1,
                    centerIndex + after);


            for (int i = start;
                 i <= end;
                 i++)
            {
                string currentMarker =
                    i == centerIndex
                        ? marker
                        : null;


                AppendNodeLine(
                    sb,
                    nodes,
                    i,
                    currentMarker);
            }
        }


        private static void AppendNodeLine(
            StringBuilder sb,
            List<TypeTreeNode> nodes,
            int index,
            string marker)
        {
            if (nodes == null ||
                index < 0 ||
                index >= nodes.Count)
            {
                return;
            }


            TypeTreeNode node =
                nodes[index];


            sb.Append("#");

            sb.Append(
                index.ToString("D3"));


            if (!string.IsNullOrEmpty(marker))
            {
                sb.Append(" ");

                sb.Append(marker);
            }


            sb.Append(
                " | L=");

            sb.Append(
                node.m_Level);


            sb.Append(
                " | VER=");

            sb.Append(
                node.m_Version);


            sb.Append(
                " | FLAGS=");

            sb.Append(
                node.m_TypeFlags);


            sb.Append(
                " | SIZE=");

            sb.Append(
                node.m_ByteSize);


            sb.Append(
                " | INDEX=");

            sb.Append(
                node.m_Index);


            sb.Append(
                " | META=0x");

            sb.Append(
                node.m_MetaFlag.ToString("X8"));


            sb.Append(
                " | REFHASH=0x");

            sb.Append(
                node.m_RefTypeHash.ToString("X16"));


            sb.Append(
                " | TYPE=");

            sb.Append(
                Safe(node.m_Type));


            sb.Append(
                " | NAME=");

            sb.Append(
                Safe(node.m_Name));


            sb.AppendLine();
        }


        private static void AppendFieldIndex(
            StringBuilder sb,
            List<TypeTreeNode> nodes,
            string fieldName)
        {
            int index =
                FindNodeByName(
                    nodes,
                    fieldName);


            if (index >= 0)
            {
                TypeTreeNode node =
                    nodes[index];


                sb.Append(
                    fieldName);

                sb.Append(
                    " => #");

                sb.Append(
                    index.ToString("D3"));

                sb.Append(
                    " | Level=");

                sb.Append(
                    node.m_Level);

                sb.Append(
                    " | Type=");

                sb.Append(
                    Safe(node.m_Type));

                sb.Append(
                    " | ByteSize=");

                sb.Append(
                    node.m_ByteSize);

                sb.Append(
                    " | Meta=0x");

                sb.Append(
                    node.m_MetaFlag.ToString("X8"));

                sb.AppendLine();
            }
            else
            {
                sb.Append(
                    fieldName);

                sb.AppendLine(
                    " => NOT FOUND");
            }
        }


        private static string Safe(
            string value)
        {
            return value ?? "<NULL>";
        }


        private static string FormatVersion(
            int[] version)
        {
            if (version == null ||
                version.Length == 0)
            {
                return "<UNKNOWN>";
            }


            StringBuilder sb =
                new StringBuilder();


            for (int i = 0;
                 i < version.Length;
                 i++)
            {
                if (i > 0)
                {
                    sb.Append('.');
                }


                sb.Append(
                    version[i]);
            }


            return sb.ToString();
        }
    }
}
