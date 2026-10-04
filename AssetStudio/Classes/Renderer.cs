using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public class StaticBatchInfo
    {
        public ushort firstSubMesh;
        public ushort subMeshCount;

        public StaticBatchInfo(ObjectReader reader)
        {
            firstSubMesh = reader.ReadUInt16();
            subMeshCount = reader.ReadUInt16();
        }
    }

    public abstract class Renderer : Component
    {
        public List<PPtr<Material>> m_Materials;
        public StaticBatchInfo m_StaticBatchInfo;
        public uint[] m_SubsetIndices;

        private bool isNewHeader = false;

        public static bool HasPrope(SerializedType type) =>
            type.Match(
                "F622BC5EE0E86D7BDF8C912DD94DCBF5",
                "9255FA54269ADD294011FDA525B5FCAC");

        protected Renderer(ObjectReader reader) : base(reader)
        {
            if (version[0] < 5) // 5.0 down
            {
                var m_Enabled = reader.ReadBoolean();
                var m_CastShadows = reader.ReadBoolean();
                var m_ReceiveShadows = reader.ReadBoolean();
                var m_LightmapIndex = reader.ReadByte();
            }
            else // 5.0 and up
            {
                if (version[0] > 5 ||
                    (version[0] == 5 && version[1] >= 4)) // 5.4 and up
                {
                    if (reader.Game.Type.IsGI())
                    {
                        CheckHeader(reader, 0x1A);
                    }

                    if (reader.Game.Type.IsBH3())
                    {
                        CheckHeader(reader, 0x12);
                    }

                    var m_Enabled = reader.ReadBoolean();
                    var m_CastShadows = reader.ReadByte();
                    var m_ReceiveShadows = reader.ReadByte();

                    if (version[0] > 2017 ||
                        (version[0] == 2017 && version[1] >= 2))
                    {
                        var m_DynamicOccludee = reader.ReadByte();
                    }

                    if (reader.Game.Type.IsBH3Group())
                    {
                        var m_AllowHalfResolution = reader.ReadByte();

                        int m_EnableGpuQuery =
                            isNewHeader
                                ? reader.ReadByte()
                                : 0;
                    }

                    if (reader.Game.Type.IsGIGroup())
                    {
                        var m_ReceiveDecals = reader.ReadByte();
                        var m_EnableShadowCulling = reader.ReadByte();
                        var m_EnableGpuQuery = reader.ReadByte();
                        var m_AllowHalfResolution = reader.ReadByte();

                        if (!reader.Game.Type.IsGICB1())
                        {
                            if (reader.Game.Type.IsGI())
                            {
                                var m_AllowPerMaterialProp =
                                    isNewHeader
                                        ? reader.ReadByte()
                                        : 0;
                            }

                            var m_IsRainOccluder = reader.ReadByte();

                            if (!reader.Game.Type.IsGICB2())
                            {
                                var m_IsDynamicAOOccluder =
                                    reader.ReadByte();

                                if (reader.Game.Type.IsGI())
                                {
                                    var m_IsHQDynamicAOOccluder =
                                        reader.ReadByte();

                                    var m_IsCloudObject =
                                        reader.ReadByte();

                                    var m_IsInteriorVolume =
                                        reader.ReadByte();
                                }
                            }

                            if (!reader.Game.Type.IsGIPack())
                            {
                                var m_IsDynamic =
                                    reader.ReadByte();
                            }

                            if (reader.Game.Type.IsGI())
                            {
                                var m_UseTessellation =
                                    reader.ReadByte();

                                var m_IsTerrainTessInfo =
                                    isNewHeader
                                        ? reader.ReadByte()
                                        : 0;

                                var m_UseVertexLightInForward =
                                    isNewHeader
                                        ? reader.ReadByte()
                                        : 0;

                                var m_CombineSubMeshInGeoPass =
                                    isNewHeader
                                        ? reader.ReadByte()
                                        : 0;
                            }
                        }
                    }

                    if (version[0] >= 2021)
                    {
                        var m_StaticShadowCaster =
                            reader.ReadByte();

                        if (reader.Game.Type.IsArknightsEndfield())
                        {
                            var m_RealtimeShadowCaster =
                                reader.ReadByte();

                            var m_SubMeshRenderMode =
                                reader.ReadByte();

                            var m_CharacterIndex =
                                reader.ReadByte();
                        }
                    }

                    var m_MotionVectors =
                        reader.ReadByte();

                    var m_LightProbeUsage =
                        reader.ReadByte();

                    var m_ReflectionProbeUsage =
                        reader.ReadByte();

                    if (version[0] > 2019 ||
                        (version[0] == 2019 && version[1] >= 3))
                    {
                        var m_RayTracingMode =
                            reader.ReadByte();
                    }

                    if (version[0] >= 2020)
                    {
                        var m_RayTraceProcedural =
                            reader.ReadByte();
                    }

                    /*
                     * =====================================================
                     * UNITY 6 / 6000.x
                     * =====================================================
                     *
                     * Embedded DB Legends TypeTree:
                     *
                     * UInt8  m_RayTracingAccelStructBuildFlagsOverride
                     * UInt8  m_RayTracingAccelStructBuildFlags
                     * UInt8  m_SmallMeshCulling
                     * SInt16 m_ForceMeshLod
                     * float  m_MeshLodSelectionBias
                     *
                     * IMPORTANT:
                     *
                     * The previous experimental parser incorrectly treated
                     * these as:
                     *
                     * bool + Align + Int32 + bool + Align ...
                     *
                     * That does NOT match the Unity 6000 TypeTree.
                     * =====================================================
                     */

                    if (version[0] >= 6000)
                    {
                        var m_RayTracingAccelStructBuildFlagsOverride =
                            reader.ReadByte();

                        var m_RayTracingAccelStructBuildFlags =
                            reader.ReadByte();

                        var m_SmallMeshCulling =
                            reader.ReadByte();

                        /*
                         * m_SmallMeshCulling carries the alignment flag
                         * in the embedded TypeTree.
                         *
                         * Align before SInt16 m_ForceMeshLod.
                         */
                        reader.AlignStream();

                        var m_ForceMeshLod =
                            reader.ReadInt16();

                        /*
                         * m_ForceMeshLod also carries alignment metadata.
                         * Align before the following float.
                         */
                        reader.AlignStream();

                        var m_MeshLodSelectionBias =
                            reader.ReadSingle();
                    }

                    if (reader.Game.Type.IsGI() ||
                        reader.Game.Type.IsGICB3() ||
                        reader.Game.Type.IsGICB3Pre())
                    {
                        var m_MeshShowQuality =
                            reader.ReadByte();
                    }

                    reader.AlignStream();
                }
                else
                {
                    var m_Enabled =
                        reader.ReadBoolean();

                    reader.AlignStream();

                    var m_CastShadows =
                        reader.ReadByte();

                    var m_ReceiveShadows =
                        reader.ReadBoolean();

                    reader.AlignStream();
                }

                if (version[0] >= 2018 ||
                    (reader.Game.Type.IsBH3() && isNewHeader))
                {
                    var m_RenderingLayerMask =
                        reader.ReadUInt32();
                }

                if (version[0] > 2018 ||
                    (version[0] == 2018 && version[1] >= 3))
                {
                    var m_RendererPriority =
                        reader.ReadInt32();
                }

                var m_LightmapIndex =
                    reader.ReadUInt16();

                var m_LightmapIndexDynamic =
                    reader.ReadUInt16();

                if (reader.Game.Type.IsGIGroup() &&
                    (m_LightmapIndex != 0xFFFF ||
                     m_LightmapIndexDynamic != 0xFFFF))
                {
                    throw new Exception(
                        "Not Supported !! skipping....");
                }
            }

            if (version[0] >= 3)
            {
                var m_LightmapTilingOffset =
                    reader.ReadVector4();
            }

            if (version[0] >= 5)
            {
                var m_LightmapTilingOffsetDynamic =
                    reader.ReadVector4();
            }

            if (reader.Game.Type.IsGIGroup())
            {
                var m_ViewDistanceRatio =
                    reader.ReadSingle();

                var m_ShaderLODDistanceRatio =
                    reader.ReadSingle();
            }

            var m_MaterialsSize =
                reader.ReadInt32();

            m_Materials =
                new List<PPtr<Material>>();

            for (int i = 0;
                 i < m_MaterialsSize;
                 i++)
            {
                m_Materials.Add(
                    new PPtr<Material>(reader));
            }

            if (version[0] < 3)
            {
                var m_LightmapTilingOffset =
                    reader.ReadVector4();
            }
            else
            {
                if (version[0] > 5 ||
                    (version[0] == 5 && version[1] >= 5))
                {
                    m_StaticBatchInfo =
                        new StaticBatchInfo(reader);
                }
                else
                {
                    m_SubsetIndices =
                        reader.ReadUInt32Array();
                }

                var m_StaticBatchRoot =
                    new PPtr<Transform>(reader);
            }

            if (reader.Game.Type.IsGIGroup())
            {
                var m_MatLayers =
                    reader.ReadInt32();
            }

            if (!reader.Game.Type.IsSR() ||
                !HasPrope(reader.serializedType))
            {
                if (version[0] > 5 ||
                    (version[0] == 5 && version[1] >= 4))
                {
                    var m_ProbeAnchor =
                        new PPtr<Transform>(reader);

                    var m_LightProbeVolumeOverride =
                        new PPtr<GameObject>(reader);
                }
                else if (version[0] > 3 ||
                         (version[0] == 3 &&
                          version[1] >= 5))
                {
                    var m_UseLightProbes =
                        reader.ReadBoolean();

                    reader.AlignStream();

                    if (version[0] >= 5)
                    {
                        var m_ReflectionProbeUsage =
                            reader.ReadInt32();
                    }

                    var m_LightProbeAnchor =
                        new PPtr<Transform>(reader);
                }
            }

            if (version[0] > 4 ||
                (version[0] == 4 && version[1] >= 3))
            {
                if (version[0] == 4 &&
                    version[1] == 3)
                {
                    var m_SortingLayer =
                        reader.ReadInt16();

                    var m_SortingOrder =
                        reader.ReadInt16();

                    reader.AlignStream();
                }
                else if (version[0] >= 6000)
                {
                    /*
                     * =================================================
                     * UNITY 6 RENDERER TAIL
                     * =================================================
                     *
                     * Embedded ClassID 137 TypeTree:
                     *
                     * #051 int    m_SortingLayerID
                     * #052 SInt16 m_SortingLayer
                     * #053 SInt16 m_SortingOrder
                     * #054 int    m_MaskInteraction
                     *
                     * Immediately afterwards the derived
                     * SkinnedMeshRenderer starts with:
                     *
                     * #055 int  m_Quality
                     * #056 bool m_UpdateWhenOffscreen
                     * ...
                     *
                     * Therefore the Renderer MUST consume all 12 bytes
                     * below before returning to SkinnedMeshRenderer.
                     * =================================================
                     */

                    var m_SortingLayerID =
                        reader.ReadUInt32();

                    var m_SortingLayer =
                        reader.ReadInt16();

                    var m_SortingOrder =
                        reader.ReadInt16();

                    var m_MaskInteraction =
                        reader.ReadInt32();

                    reader.AlignStream();
                }
                else
                {
                    var m_SortingLayerID =
                        reader.ReadUInt32();

                    var m_SortingOrder =
                        reader.ReadInt16();

                    reader.AlignStream();
                }

                if (reader.Game.Type.IsGIGroup() ||
                    reader.Game.Type.IsBH3())
                {
                    var m_UseHighestMip =
                        reader.ReadBoolean();

                    reader.AlignStream();
                }

                if (reader.Game.Type.IsSR())
                {
                    var RenderFlag =
                        reader.ReadUInt32();

                    reader.AlignStream();
                }
            }
        }

        private void CheckHeader(
            ObjectReader reader,
            int offset)
        {
            short value = 0;

            var pos =
                reader.Position;

            while (value != -1 &&
                   reader.Position <= pos + offset)
            {
                value =
                    reader.ReadInt16();
            }

            isNewHeader =
                (reader.Position - pos) == offset;

            reader.Position =
                pos;
        }
    }
}