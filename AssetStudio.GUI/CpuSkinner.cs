//Animation Preview 07-10-26
using System;
using System.Collections.Generic;

namespace AssetStudio.GUI
{
    internal sealed class CpuSkinner
    {
        private readonly AnimationPlayer animationPlayer;

        public CpuSkinner(AnimationPlayer animationPlayer)
        {
            this.animationPlayer = animationPlayer
                ?? throw new ArgumentNullException(
                    nameof(animationPlayer));
        }

        public List<Vector3> SkinMesh(
            ImportedMesh mesh,
            Dictionary<string, Matrix4x4> worldMatrices,
            Matrix4x4 inverseMeshWorld)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            if (worldMatrices == null)
                throw new ArgumentNullException(nameof(worldMatrices));

            var result = new List<Vector3>(
                mesh.VertexList.Count);

            if (mesh.BoneList == null ||
                mesh.BoneList.Count == 0)
            {
                foreach (var vertex in mesh.VertexList)
                    result.Add(vertex.Vertex);

                return result;
            }

            var skinMatrices =
                new Matrix4x4[mesh.BoneList.Count];

            var validBones =
                new bool[mesh.BoneList.Count];

            for (int i = 0; i < mesh.BoneList.Count; i++)
            {
                var bone = mesh.BoneList[i];

                if (bone == null ||
                    string.IsNullOrEmpty(bone.Path) ||
                    !worldMatrices.TryGetValue(
                        bone.Path,
                        out var boneWorld))
                {
                    continue;
                }

                skinMatrices[i] =
                    inverseMeshWorld *
                    boneWorld *
                    bone.Matrix;

                validBones[i] = true;
            }

            foreach (var vertex in mesh.VertexList)
            {
                var original = vertex.Vertex;

                if (vertex.Weights == null ||
                    vertex.BoneIndices == null)
                {
                    result.Add(original);
                    continue;
                }

                float x = 0;
                float y = 0;
                float z = 0;
                float totalWeight = 0;

                int influenceCount = Math.Min(
                    vertex.Weights.Length,
                    vertex.BoneIndices.Length);

                for (int i = 0; i < influenceCount; i++)
                {
                    float weight = vertex.Weights[i];

                    if (weight <= 0)
                        continue;

                    int boneIndex = vertex.BoneIndices[i];

                    if (boneIndex < 0 ||
                        boneIndex >= skinMatrices.Length ||
                        !validBones[boneIndex])
                    {
                        continue;
                    }

                    var transformed =
                        AnimationPlayer.TransformPoint(
                            skinMatrices[boneIndex],
                            original);

                    x += transformed.X * weight;
                    y += transformed.Y * weight;
                    z += transformed.Z * weight;

                    totalWeight += weight;
                }

                if (totalWeight > 0.000001f)
                {
                    result.Add(new Vector3(
                        x / totalWeight,
                        y / totalWeight,
                        z / totalWeight));
                }
                else
                {
                    result.Add(original);
                }
            }

            return result;
        }
        public List<Vector3> SkinMeshRelative(
ImportedMesh mesh,
Dictionary<string, Matrix4x4> restWorldMatrices,
Dictionary<string, Matrix4x4> animatedWorldMatrices,
Matrix4x4 inverseRestMeshWorld)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            var result = new List<Vector3>(mesh.VertexList.Count);

            if (mesh.BoneList == null || mesh.BoneList.Count == 0)
            {
                foreach (var vertex in mesh.VertexList)
                    result.Add(vertex.Vertex);

                return result;
            }

            var skinMatrices = new Matrix4x4[mesh.BoneList.Count];
            var validBones = new bool[mesh.BoneList.Count];

            if (!restWorldMatrices.TryGetValue(mesh.Path, out var restMeshWorld))
            {
                foreach (var vertex in mesh.VertexList)
                    result.Add(vertex.Vertex);

                return result;
            }

            for (int i = 0; i < mesh.BoneList.Count; i++)
            {
                var bone = mesh.BoneList[i];

                if (bone == null ||
                    string.IsNullOrEmpty(bone.Path) ||
                    !restWorldMatrices.TryGetValue(bone.Path, out var restBoneWorld) ||
                    !animatedWorldMatrices.TryGetValue(bone.Path, out var animatedBoneWorld))
                {
                    continue;
                }

                try
                {
                    var inverseRestBoneWorld =
                        AnimationPlayer.InvertMatrix(restBoneWorld);

                    // Delta nel sistema di coordinate della mesh originale.
                    skinMatrices[i] =
                        inverseRestMeshWorld *
                        animatedBoneWorld *
                        inverseRestBoneWorld *
                        restMeshWorld;

                    validBones[i] = true;
                }
                catch (InvalidOperationException)
                {
                    validBones[i] = false;
                }
            }

            foreach (var vertex in mesh.VertexList)
            {
                var original = vertex.Vertex;

                if (vertex.Weights == null || vertex.BoneIndices == null)
                {
                    result.Add(original);
                    continue;
                }

                float x = 0, y = 0, z = 0;
                float totalWeight = 0;

                int influenceCount = Math.Min(
                    vertex.Weights.Length,
                    vertex.BoneIndices.Length);

                for (int i = 0; i < influenceCount; i++)
                {
                    float weight = vertex.Weights[i];
                    int boneIndex = vertex.BoneIndices[i];

                    if (weight <= 0 ||
                        boneIndex < 0 ||
                        boneIndex >= skinMatrices.Length ||
                        !validBones[boneIndex])
                        continue;

                    var transformed = AnimationPlayer.TransformPoint(
                        skinMatrices[boneIndex],
                        original);

                    x += transformed.X * weight;
                    y += transformed.Y * weight;
                    z += transformed.Z * weight;
                    totalWeight += weight;
                }

                result.Add(totalWeight > 0.000001f
                    ? new Vector3(
                        x / totalWeight,
                        y / totalWeight,
                        z / totalWeight)
                    : original);
            }

            return result;
        }
    }
}