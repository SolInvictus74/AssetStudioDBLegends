using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssetStudio
{
    public sealed class SkinnedMeshRenderer : Renderer
    {
        public PPtr<Mesh> m_Mesh;
        public List<PPtr<Transform>> m_Bones;
        public float[] m_BlendShapeWeights;
        public PPtr<Transform> m_RootBone;
        public AABB m_AABB;
        public bool m_DirtyAABB;

        private const int PPtrSize = 12;
        private const int MaxBones = 10000;
        private const int MaxBlendWeights = 100000;

        public SkinnedMeshRenderer(ObjectReader reader) : base(reader)
        {
            long objectStart = reader.byteStart;
            long objectEnd = reader.byteStart + (long)reader.byteSize;

            // ============================================================
            // COMMON SKINNED HEADER
            // ============================================================

            int m_Quality = reader.ReadInt32();

            var m_UpdateWhenOffscreen = reader.ReadBoolean();
            var m_SkinnedMotionVectorsOrLegacySkinNormals =
                reader.ReadBoolean();

            reader.AlignStream();

            if (version[0] == 2 && version[1] < 6)
            {
                var m_DisableAnimationWhenOffscreen =
                    new PPtr<Animation>(reader);
            }

            // ============================================================
            // MESH
            // ============================================================

            m_Mesh = new PPtr<Mesh>(reader);

            // ============================================================
            // LEGACY UNITY
            // ============================================================

            if (version[0] < 6000)
            {
                ParseLegacy(reader);
                return;
            }

            // ============================================================
            // UNITY 6
            //
            // We have observed two real layouts after m_Mesh:
            //
            // A:
            //   RootBone
            //   BoneCount
            //   Bones[]
            //   unknown/variant tail
            //
            // B:
            //   BoneCount
            //   Bones[]
            //   BlendCount
            //   BlendWeights[]
            //   RootBone
            //   AABB
            //   DirtyAABB
            //   Align4
            //
            // Detection is performed WITHOUT consuming either candidate.
            // ============================================================

            long payloadStart = reader.Position;

            if (payloadStart > objectEnd)
            {
                throw new Exception(
                    $"Invalid SkinnedMeshRenderer bounds: " +
                    $"payloadStart=0x{payloadStart:X}, " +
                    $"objectEnd=0x{objectEnd:X}");
            }

            byte[] raw = ReadRange(reader, payloadStart, objectEnd);

            LayoutBInfo layoutB = AnalyzeLayoutB(raw);
            LayoutAInfo layoutA = AnalyzeLayoutA(raw);

            // ============================================================
            // SELECTION POLICY
            //
            // B gets priority ONLY when it closes exactly at ObjectEnd
            // and its internal structure is semantically plausible.
            //
            // Otherwise A is selected when:
            // - Root PPtr is plausible
            // - BoneCount is plausible
            // - all Bone PPtrs are plausible
            // - BonesEnd stays inside ObjectEnd
            //
            // This deliberately avoids "B fits somewhere in the object"
            // being treated as proof of B.
            // ============================================================

            bool useB =
                layoutB.Valid &&
                layoutB.ExactEnd &&
                layoutB.RootPPtrPlausible &&
                layoutB.AllBonePPtrsPlausible;

            bool useA =
                layoutA.Valid &&
                layoutA.RootPPtrPlausible &&
                layoutA.AllBonePPtrsPlausible;

            if (useB)
            {
                reader.Position = payloadStart;
                ParseUnity6LayoutB(reader, objectEnd);
                return;
            }

            if (useA)
            {
                reader.Position = payloadStart;
                ParseUnity6LayoutA(reader, objectEnd);
                return;
            }

            // ============================================================
            // FALLBACK + UNITY 6 REMAINDER DIAGNOSTIC
            //
            // IMPORTANT:
            //
            // We still DO NOT accept a non-exact Layout B.
            //
            // If B is structurally valid but leaves bytes before the real
            // ObjectEnd, dump those bytes non-destructively so that the
            // remaining Unity 6 layout can be identified precisely.
            //
            // Parsing behaviour is unchanged.
            // ============================================================

            if (layoutB.Valid &&
                !layoutB.ExactEnd)
            {
                WriteUnity6RemainderDiagnostic(
                    reader,
                    raw,
                    objectStart,
                    objectEnd,
                    payloadStart,
                    layoutA,
                    layoutB);
            }

            throw new Exception(
                BuildDetectionFailureMessage(
                    reader,
                    objectStart,
                    objectEnd,
                    payloadStart,
                    layoutA,
                    layoutB));
        }

        // ================================================================
        // UNITY 6 - LAYOUT A
        //
        // RootBone
        // BoneCount
        // Bones[]
        // Variant tail
        //
        // Only the proven portion is decoded.
        // The remaining tail is skipped to the real ObjectEnd.
        // ================================================================

        private void ParseUnity6LayoutA(
            ObjectReader reader,
            long objectEnd)
        {
            m_RootBone =
                new PPtr<Transform>(reader);

            int numBones =
                reader.ReadInt32();

            if (numBones < 0 ||
                numBones > MaxBones)
            {
                throw new Exception(
                    $"Invalid Unity 6 Layout A bone count: {numBones}");
            }

            m_Bones =
                new List<PPtr<Transform>>(numBones);

            for (int i = 0; i < numBones; i++)
            {
                m_Bones.Add(
                    new PPtr<Transform>(reader));
            }

            if (reader.Position > objectEnd)
            {
                throw new Exception(
                    $"Unity 6 Layout A exceeded ObjectEnd. " +
                    $"Position=0x{reader.Position:X}, " +
                    $"ObjectEnd=0x{objectEnd:X}");
            }

            // ------------------------------------------------------------
            // IMPORTANT:
            //
            // Layout A has a real variable tail, but its complete semantic
            // structure has not yet been proven for every observed family.
            //
            // Do NOT fabricate BlendShapeWeights/AABB fields here.
            // Preserve the proven renderer data and consume the object.
            // ------------------------------------------------------------

            m_BlendShapeWeights =
                Array.Empty<float>();

            reader.Position = objectEnd;
        }

        // ================================================================
        // UNITY 6 - LAYOUT B
        //
        // TypeTree layout:
        //
        // BoneCount
        // Bones[]
        // BlendCount
        // BlendWeights[]
        // RootBone
        // AABB
        // DirtyAABB
        // Align4
        // ================================================================

        private void ParseUnity6LayoutB(
            ObjectReader reader,
            long objectEnd)
        {
            // ------------------------------------------------------------
            // BONES
            // ------------------------------------------------------------

            int numBones =
                reader.ReadInt32();

            if (numBones < 0 ||
                numBones > MaxBones)
            {
                throw new Exception(
                    $"Invalid Unity 6 Layout B bone count: {numBones}");
            }

            m_Bones =
                new List<PPtr<Transform>>(numBones);

            for (int i = 0; i < numBones; i++)
            {
                m_Bones.Add(
                    new PPtr<Transform>(reader));
            }

            // ------------------------------------------------------------
            // BLEND SHAPE WEIGHTS
            // ------------------------------------------------------------

            int blendCount =
                reader.ReadInt32();

            if (blendCount < 0 ||
                blendCount > MaxBlendWeights)
            {
                throw new Exception(
                    $"Invalid Unity 6 Layout B blend count: {blendCount}");
            }

            m_BlendShapeWeights =
                new float[blendCount];

            for (int i = 0; i < blendCount; i++)
            {
                m_BlendShapeWeights[i] =
                    reader.ReadSingle();
            }

            // ------------------------------------------------------------
            // ROOT
            // ------------------------------------------------------------

            m_RootBone =
                new PPtr<Transform>(reader);

            // ------------------------------------------------------------
            // AABB
            // ------------------------------------------------------------

            m_AABB =
                new AABB(reader);

            // ------------------------------------------------------------
            // DIRTY AABB + ALIGNMENT
            // ------------------------------------------------------------

            m_DirtyAABB =
                reader.ReadBoolean();

            reader.AlignStream();

            // Exact-end was already required during detection.
            // Check again after using the real ObjectReader.

            if (reader.Position != objectEnd)
            {
                throw new Exception(
                    $"Unity 6 Layout B did not close at ObjectEnd. " +
                    $"Position=0x{reader.Position:X}, " +
                    $"ObjectEnd=0x{objectEnd:X}, " +
                    $"Delta={objectEnd - reader.Position}");
            }
        }

        // ================================================================
        // LEGACY PARSER
        // ================================================================

        private void ParseLegacy(ObjectReader reader)
        {
            int numBones =
                reader.ReadInt32();

            if (numBones < 0 ||
                numBones > MaxBones)
            {
                throw new Exception(
                    $"Invalid SkinnedMeshRenderer bone count: {numBones}");
            }

            m_Bones =
                new List<PPtr<Transform>>(numBones);

            for (int i = 0; i < numBones; i++)
            {
                m_Bones.Add(
                    new PPtr<Transform>(reader));
            }

            if (version[0] > 4 ||
                (version[0] == 4 &&
                 version[1] >= 3))
            {
                int blendCount =
                    reader.ReadInt32();

                if (blendCount < 0 ||
                    blendCount > MaxBlendWeights)
                {
                    throw new Exception(
                        $"Invalid SkinnedMeshRenderer blend shape count: {blendCount}");
                }

                m_BlendShapeWeights =
                    new float[blendCount];

                for (int i = 0; i < blendCount; i++)
                {
                    m_BlendShapeWeights[i] =
                        reader.ReadSingle();
                }
            }

            if (reader.Game.Type.IsGIGroup())
            {
                m_RootBone =
                    new PPtr<Transform>(reader);

                m_AABB =
                    new AABB(reader);

                m_DirtyAABB =
                    reader.ReadBoolean();

                reader.AlignStream();
            }
        }

        // ================================================================
        // LAYOUT A ANALYSIS
        // ================================================================

        private sealed class LayoutAInfo
        {
            public bool Valid;

            public string FailureReason;

            public int BoneCount;

            public int BonesEnd;

            public int RemainingBytes;

            public bool RootPPtrPlausible;

            public bool AllBonePPtrsPlausible;
        }

        private static LayoutAInfo AnalyzeLayoutA(
            byte[] raw)
        {
            LayoutAInfo r =
                new LayoutAInfo();

            if (raw == null ||
                raw.Length < 16)
            {
                r.FailureReason =
                    "LESS_THAN_16_BYTES";

                return r;
            }

            // +00 -> RootBone PPtr
            r.RootPPtrPlausible =
                IsPPtrPlausible(
                    raw,
                    0);

            // +0C -> BoneCount
            int numBones =
                ReadInt32LE(
                    raw,
                    12);

            r.BoneCount =
                numBones;

            if (numBones < 0 ||
                numBones > MaxBones)
            {
                r.FailureReason =
                    "IMPLAUSIBLE_BONE_COUNT";

                return r;
            }

            long bonesEndLong;

            try
            {
                bonesEndLong =
                    checked(
                        16L +
                        ((long)numBones *
                         PPtrSize));
            }
            catch (OverflowException)
            {
                r.FailureReason =
                    "BONE_END_OVERFLOW";

                return r;
            }

            if (bonesEndLong >
                raw.Length)
            {
                r.FailureReason =
                    "BONES_EXCEED_OBJECT";

                return r;
            }

            r.BonesEnd =
                (int)bonesEndLong;

            bool allPPtrs =
                true;

            for (int i = 0;
                 i < numBones;
                 i++)
            {
                int offset =
                    16 +
                    (i * PPtrSize);

                if (!IsPPtrPlausible(
                        raw,
                        offset))
                {
                    allPPtrs = false;
                    break;
                }
            }

            r.AllBonePPtrsPlausible =
                allPPtrs;

            r.RemainingBytes =
                raw.Length -
                r.BonesEnd;

            // At minimum the candidate must not overrun.
            //
            // We deliberately do NOT require exactly 44 bytes because
            // the diagnostic proved multiple legitimate tail sizes:
            // 44, 48, 52, 56, 64, 68, 80, 96, ...
            //
            // However all known tails are aligned.

            if ((r.RemainingBytes & 3) != 0)
            {
                r.FailureReason =
                    "TAIL_NOT_4_BYTE_ALIGNED";

                return r;
            }

            r.Valid =
                r.RootPPtrPlausible &&
                r.AllBonePPtrsPlausible;

            if (!r.Valid &&
                string.IsNullOrEmpty(
                    r.FailureReason))
            {
                r.FailureReason =
                    "PTR_PLAUSIBILITY_FAILED";
            }

            return r;
        }

        // ================================================================
        // LAYOUT B ANALYSIS
        // ================================================================

        private sealed class LayoutBInfo
        {
            public bool Valid;

            public bool ExactEnd;

            public string FailureReason;

            public int BoneCount;

            public int BlendCount;

            public int FinalOffset;

            public int RemainingBytes;

            public bool RootPPtrPlausible;

            public bool AllBonePPtrsPlausible;
        }

        private static LayoutBInfo AnalyzeLayoutB(
            byte[] raw)
        {
            LayoutBInfo r =
                new LayoutBInfo();

            if (raw == null ||
                raw.Length < 4)
            {
                r.FailureReason =
                    "LESS_THAN_4_BYTES";

                return r;
            }

            int cursor = 0;

            // ------------------------------------------------------------
            // BoneCount
            // ------------------------------------------------------------

            int numBones =
                ReadInt32LE(
                    raw,
                    cursor);

            r.BoneCount =
                numBones;

            cursor += 4;

            if (numBones < 0 ||
                numBones > MaxBones)
            {
                r.FailureReason =
                    "IMPLAUSIBLE_BONE_COUNT";

                return r;
            }

            long bonesEndLong;

            try
            {
                bonesEndLong =
                    checked(
                        (long)cursor +
                        ((long)numBones *
                         PPtrSize));
            }
            catch (OverflowException)
            {
                r.FailureReason =
                    "BONE_END_OVERFLOW";

                return r;
            }

            if (bonesEndLong >
                raw.Length)
            {
                r.FailureReason =
                    "BONES_EXCEED_OBJECT";

                return r;
            }

            bool allBonePPtrs =
                true;

            for (int i = 0;
                 i < numBones;
                 i++)
            {
                int offset =
                    cursor +
                    (i * PPtrSize);

                if (!IsPPtrPlausible(
                        raw,
                        offset))
                {
                    allBonePPtrs = false;
                    break;
                }
            }

            r.AllBonePPtrsPlausible =
                allBonePPtrs;

            cursor =
                (int)bonesEndLong;

            // ------------------------------------------------------------
            // BlendCount
            // ------------------------------------------------------------

            if (!CanRead(
                    raw,
                    cursor,
                    4))
            {
                r.FailureReason =
                    "NO_ROOM_FOR_BLEND_COUNT";

                return r;
            }

            int blendCount =
                ReadInt32LE(
                    raw,
                    cursor);

            r.BlendCount =
                blendCount;

            cursor += 4;

            if (blendCount < 0 ||
                blendCount > MaxBlendWeights)
            {
                r.FailureReason =
                    "IMPLAUSIBLE_BLEND_COUNT";

                return r;
            }

            long blendEndLong;

            try
            {
                blendEndLong =
                    checked(
                        (long)cursor +
                        ((long)blendCount * 4L));
            }
            catch (OverflowException)
            {
                r.FailureReason =
                    "BLEND_END_OVERFLOW";

                return r;
            }

            if (blendEndLong >
                raw.Length)
            {
                r.FailureReason =
                    "BLEND_DATA_EXCEEDS_OBJECT";

                return r;
            }

            cursor =
                (int)blendEndLong;

            // ------------------------------------------------------------
            // RootBone
            // ------------------------------------------------------------

            if (!CanRead(
                    raw,
                    cursor,
                    12))
            {
                r.FailureReason =
                    "NO_ROOM_FOR_ROOT";

                return r;
            }

            r.RootPPtrPlausible =
                IsPPtrPlausible(
                    raw,
                    cursor);

            cursor += 12;

            // ------------------------------------------------------------
            // AABB
            // ------------------------------------------------------------

            if (!CanRead(
                    raw,
                    cursor,
                    24))
            {
                r.FailureReason =
                    "NO_ROOM_FOR_AABB";

                return r;
            }

            cursor += 24;

            // ------------------------------------------------------------
            // DirtyAABB
            // ------------------------------------------------------------

            if (!CanRead(
                    raw,
                    cursor,
                    1))
            {
                r.FailureReason =
                    "NO_ROOM_FOR_DIRTY_AABB";

                return r;
            }

            cursor += 1;

            cursor =
                Align4(cursor);

            if (cursor >
                raw.Length)
            {
                r.FailureReason =
                    "ALIGNMENT_EXCEEDS_OBJECT";

                return r;
            }

            r.FinalOffset =
                cursor;

            r.RemainingBytes =
                raw.Length -
                cursor;

            r.ExactEnd =
                cursor ==
                raw.Length;

            r.Valid =
                r.RootPPtrPlausible &&
                r.AllBonePPtrsPlausible;

            if (!r.Valid &&
                string.IsNullOrEmpty(
                    r.FailureReason))
            {
                r.FailureReason =
                    "PTR_PLAUSIBILITY_FAILED";
            }

            return r;
        }

        // ================================================================
        // NON-DESTRUCTIVE RANGE READ
        // ================================================================

        private static byte[] ReadRange(
            ObjectReader reader,
            long start,
            long end)
        {
            if (end < start)
            {
                throw new Exception(
                    $"Invalid diagnostic range: " +
                    $"start=0x{start:X}, end=0x{end:X}");
            }

            long lengthLong =
                end - start;

            if (lengthLong >
                int.MaxValue)
            {
                throw new Exception(
                    $"SkinnedMeshRenderer payload too large: {lengthLong}");
            }

            int length =
                (int)lengthLong;

            byte[] result =
                new byte[length];

            long saved =
                reader.Position;

            try
            {
                reader.Position =
                    start;

                for (int i = 0;
                     i < length;
                     i++)
                {
                    result[i] =
                        reader.ReadByte();
                }
            }
            finally
            {
                reader.Position =
                    saved;
            }

            return result;
        }

        // ================================================================
        // PPtr PLAUSIBILITY
        //
        // PPtr in this serialization:
        //
        // int32 FileID
        // int64 PathID
        //
        // PathID may legitimately be positive, negative or zero.
        // FileID is therefore the useful cheap discriminator.
        // ================================================================

        private static bool IsPPtrPlausible(
            byte[] raw,
            int offset)
        {
            if (!CanRead(
                    raw,
                    offset,
                    PPtrSize))
            {
                return false;
            }

            int fileID =
                ReadInt32LE(
                    raw,
                    offset);

            // Keep the same deliberately permissive heuristic used by
            // the successful compare diagnostic.
            return
                fileID >= -1 &&
                fileID <= 100000;
        }
        // ================================================================
        // UNITY 6 REMAINDER DIAGNOSTIC
        //
        // Dumps ONLY the bytes left after a structurally valid Layout B
        // candidate which does not close at ObjectEnd.
        //
        // This diagnostic is deliberately non-destructive:
        // - it works from the already captured raw[] payload
        // - it does not move ObjectReader.Position
        // - it does not change layout selection
        // - it does not make failed objects succeed
        // ================================================================

        private static readonly object
            Unity6RemainderDiagnosticLock =
                new object();

        private const string
            Unity6RemainderDiagnosticFile =
                "SKIN_U6_REMAINDER_DIAGNOSTIC.txt";

        private static void WriteUnity6RemainderDiagnostic(
            ObjectReader reader,
            byte[] raw,
            long objectStart,
            long objectEnd,
            long payloadStart,
            LayoutAInfo layoutA,
            LayoutBInfo layoutB)
        {
            try
            {
                if (raw == null)
                    return;

                if (!layoutB.Valid ||
                    layoutB.ExactEnd)
                {
                    return;
                }

                int start =
                    layoutB.FinalOffset;

                if (start < 0 ||
                    start > raw.Length)
                {
                    return;
                }

                int remaining =
                    raw.Length - start;

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    "UNITY 6 SKINNEDMESHRENDERER - LAYOUT B REMAINDER");

                sb.AppendLine(
                    "============================================================");

                sb.AppendLine(
                    $"File: {SafeFileName(reader)}");

                sb.AppendLine(
                    $"PathID: {reader.m_PathID}");

                sb.AppendLine();

                sb.AppendLine(
                    $"ObjectStart:  0x{objectStart:X}");

                sb.AppendLine(
                    $"ObjectEnd:    0x{objectEnd:X}");

                sb.AppendLine(
                    $"PayloadStart: 0x{payloadStart:X}");

                long absoluteFinalOffset =
                    payloadStart +
                    layoutB.FinalOffset;

                sb.AppendLine(
                    $"B.FinalOffset.Relative: 0x{layoutB.FinalOffset:X}");

                sb.AppendLine(
                    $"B.FinalOffset.Absolute: 0x{absoluteFinalOffset:X}");

                sb.AppendLine(
                    $"B.RemainingBytes: {layoutB.RemainingBytes}");

                sb.AppendLine(
                    $"ActualRemainderLength: {remaining}");

                sb.AppendLine();

                sb.AppendLine(
                    "LAYOUT A:");

                sb.AppendLine(
                    $"  Valid: {layoutA.Valid}");

                sb.AppendLine(
                    $"  BoneCount: {layoutA.BoneCount}");

                sb.AppendLine(
                    $"  BonesEnd: 0x{layoutA.BonesEnd:X}");

                sb.AppendLine(
                    $"  RemainingBytes: {layoutA.RemainingBytes}");

                sb.AppendLine(
                    $"  RootPPtrPlausible: {layoutA.RootPPtrPlausible}");

                sb.AppendLine(
                    $"  AllBonePPtrsPlausible: {layoutA.AllBonePPtrsPlausible}");

                sb.AppendLine(
                    $"  FailureReason: {layoutA.FailureReason ?? "NONE"}");

                sb.AppendLine();

                sb.AppendLine(
                    "LAYOUT B:");

                sb.AppendLine(
                    $"  Valid: {layoutB.Valid}");

                sb.AppendLine(
                    $"  ExactEnd: {layoutB.ExactEnd}");

                sb.AppendLine(
                    $"  BoneCount: {layoutB.BoneCount}");

                sb.AppendLine(
                    $"  BlendCount: {layoutB.BlendCount}");

                sb.AppendLine(
                    $"  FinalOffset: 0x{layoutB.FinalOffset:X}");

                sb.AppendLine(
                    $"  RemainingBytes: {layoutB.RemainingBytes}");

                sb.AppendLine(
                    $"  RootPPtrPlausible: {layoutB.RootPPtrPlausible}");

                sb.AppendLine(
                    $"  AllBonePPtrsPlausible: {layoutB.AllBonePPtrsPlausible}");

                sb.AppendLine(
                    $"  FailureReason: {layoutB.FailureReason ?? "NONE"}");

                sb.AppendLine();

                sb.AppendLine(
                    "REMAINDER HEX:");

                sb.AppendLine(
                    "------------------------------------------------------------");

                if (remaining == 0)
                {
                    sb.AppendLine(
                        "<EMPTY>");
                }
                else
                {
                    for (int lineStart = 0;
                         lineStart < remaining;
                         lineStart += 16)
                    {
                        int lineCount =
                            Math.Min(
                                16,
                                remaining - lineStart);

                        sb.Append(
                            $"+0x{lineStart:X4}  ");

                        for (int i = 0;
                             i < 16;
                             i++)
                        {
                            if (i < lineCount)
                            {
                                byte value =
                                    raw[
                                        start +
                                        lineStart +
                                        i];

                                sb.Append(
                                    value.ToString("X2"));

                                sb.Append(' ');
                            }
                            else
                            {
                                sb.Append(
                                    "   ");
                            }
                        }

                        sb.Append(" | ");

                        for (int i = 0;
                             i < lineCount;
                             i++)
                        {
                            byte value =
                                raw[
                                    start +
                                    lineStart +
                                    i];

                            char c =
                                value >= 32 &&
                                value <= 126
                                    ? (char)value
                                    : '.';

                            sb.Append(c);
                        }

                        sb.AppendLine();
                    }
                }

                sb.AppendLine(
                    "------------------------------------------------------------");

                sb.AppendLine();

                lock (Unity6RemainderDiagnosticLock)
                {
                    File.AppendAllText(
                        Unity6RemainderDiagnosticFile,
                        sb.ToString());
                }
            }
            catch (Exception ex)
            {
                // Diagnostic code must NEVER interfere with parsing.
                //
                // If logging itself fails, preserve the original
                // SkinnedMeshRenderer behaviour.

                try
                {
                    lock (Unity6RemainderDiagnosticLock)
                    {
                        File.AppendAllText(
                            Unity6RemainderDiagnosticFile,
                            "[DIAGNOSTIC ERROR] " +
                            ex.GetType().Name +
                            ": " +
                            ex.Message +
                            Environment.NewLine);
                    }
                }
                catch
                {
                }
            }
        }
        // ================================================================
        // FAILURE DESCRIPTION
        // ================================================================

        private static string BuildDetectionFailureMessage(
            ObjectReader reader,
            long objectStart,
            long objectEnd,
            long payloadStart,
            LayoutAInfo a,
            LayoutBInfo b)
        {
            return
                "Unable to determine Unity 6 SkinnedMeshRenderer layout. " +

                $"File={SafeFileName(reader)}, " +
                $"PathID={reader.m_PathID}, " +

                $"ObjectStart=0x{objectStart:X}, " +
                $"ObjectEnd=0x{objectEnd:X}, " +
                $"PayloadStart=0x{payloadStart:X}. " +

                $"A=[" +
                $"Valid={a.Valid}, " +
                $"Bones={a.BoneCount}, " +
                $"Remaining={a.RemainingBytes}, " +
                $"RootPPtr={a.RootPPtrPlausible}, " +
                $"BonePPtrs={a.AllBonePPtrsPlausible}, " +
                $"Reason={a.FailureReason ?? "NONE"}]. " +

                $"B=[" +
                $"Valid={b.Valid}, " +
                $"Exact={b.ExactEnd}, " +
                $"Bones={b.BoneCount}, " +
                $"Blend={b.BlendCount}, " +
                $"Remaining={b.RemainingBytes}, " +
                $"RootPPtr={b.RootPPtrPlausible}, " +
                $"BonePPtrs={b.AllBonePPtrsPlausible}, " +
                $"Reason={b.FailureReason ?? "NONE"}].";
        }

        // ================================================================
        // BYTE HELPERS
        // ================================================================

        private static bool CanRead(
            byte[] data,
            int offset,
            int count)
        {
            if (data == null)
                return false;

            if (offset < 0 ||
                count < 0)
                return false;

            long end =
                (long)offset +
                count;

            return
                end <= data.Length;
        }

        private static int Align4(
            int value)
        {
            return
                (value + 3) &
                ~3;
        }

        private static uint ReadUInt32LE(
            byte[] data,
            int offset)
        {
            return
                ((uint)data[offset]) |
                ((uint)data[offset + 1] << 8) |
                ((uint)data[offset + 2] << 16) |
                ((uint)data[offset + 3] << 24);
        }

        private static int ReadInt32LE(
            byte[] data,
            int offset)
        {
            return
                unchecked(
                    (int)ReadUInt32LE(
                        data,
                        offset));
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
                    return
                        reader.assetsFile.fileName;
                }
            }
            catch
            {
            }

            return
                "UNKNOWN";
        }
    }
}