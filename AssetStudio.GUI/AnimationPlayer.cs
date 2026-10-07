using System;
using System.Collections.Generic;
using System.Security.Policy;

namespace AssetStudio.GUI
{
    internal sealed class AnimationPlayer
    {
        private readonly ImportedFrame rootFrame;

        public AnimationPlayer(ImportedFrame rootFrame)
        {
            this.rootFrame = rootFrame
                ?? throw new ArgumentNullException(nameof(rootFrame));
        }

        public List<SkeletonLine> GetSkeletonLines()
        {
            var lines = new List<SkeletonLine>();
            var rootWorld = CreateLocalMatrix(rootFrame);

            ProcessFrame(
                rootFrame,
                rootWorld,
                lines,
                null,
                0.0f);

            return lines;
        }

        public List<SkeletonLine> GetSkeletonLines(
            ImportedKeyframedAnimation animation,
            float time)
        {
            if (animation == null)
                return GetSkeletonLines();

            var lines = new List<SkeletonLine>();

            var rootLocal =
                CreateAnimatedLocalMatrix(
                    rootFrame,
                    animation,
                    time);

            ProcessFrame(
                rootFrame,
                rootLocal,
                lines,
                animation,
                time);

            return lines;
        }

        public Dictionary<string, Matrix4x4> GetWorldMatrices(
    ImportedKeyframedAnimation animation,
    float time)
        {
            var result = new Dictionary<string, Matrix4x4>();

            var rootWorld = animation == null
                ? CreateLocalMatrix(rootFrame)
                : CreateAnimatedLocalMatrix(rootFrame, animation, time);

            CollectWorldMatrices(
                rootFrame,
                rootWorld,
                animation,
                time,
                result);

            return result;
        }

        private static void CollectWorldMatrices(
            ImportedFrame frame,
            Matrix4x4 worldMatrix,
            ImportedKeyframedAnimation animation,
            float time,
            Dictionary<string, Matrix4x4> result)
        {
            result[frame.Path] = worldMatrix;

            for (int i = 0; i < frame.Count; i++)
            {
                var child = frame[i];

                var childLocal = animation == null
                    ? CreateLocalMatrix(child)
                    : CreateAnimatedLocalMatrix(child, animation, time);

                var childWorld = worldMatrix * childLocal;

                CollectWorldMatrices(
                    child,
                    childWorld,
                    animation,
                    time,
                    result);
            }
        }

        private static void ProcessFrame(
            ImportedFrame parent,
            Matrix4x4 parentWorld,
            List<SkeletonLine> lines,
            ImportedKeyframedAnimation animation,
            float time)
        {
            var parentPosition =
                GetPosition(parentWorld);

            for (int i = 0; i < parent.Count; i++)
            {
                var child = parent[i];

                Matrix4x4 childLocal;

                if (animation == null)
                {
                    childLocal =
                        CreateLocalMatrix(child);
                }
                else
                {
                    childLocal =
                        CreateAnimatedLocalMatrix(
                            child,
                            animation,
                            time);
                }

                var childWorld =
                    parentWorld * childLocal;

                var childPosition =
                    GetPosition(childWorld);

                lines.Add(
                    new SkeletonLine(
                        parentPosition,
                        childPosition));

                ProcessFrame(
                    child,
                    childWorld,
                    lines,
                    animation,
                    time);
            }
        }

        private static Matrix4x4 CreateAnimatedLocalMatrix(
            ImportedFrame frame,
            ImportedKeyframedAnimation animation,
            float time)
        {
            /*
             * Start from the frame's original local transform.
             * Missing animation channels therefore automatically
             * fall back to the original pose.
             */
            var position = frame.LocalPosition;
            var rotation = frame.LocalRotation;
            var scale = frame.LocalScale;

            /*
             * ModelConverter has already resolved animation paths
             * into the same coordinate system as RootFrame.
             */
            ImportedAnimationKeyframedTrack track = null;

            foreach (var candidate in animation.TrackList)
            {
                if (candidate.Path == null)
                    continue;

                var targetFrame =
                    frame.Parent == null
                        ? null
                        : frame;

                if (targetFrame != null &&
                    targetFrame.Path.EndsWith(
                        candidate.Path,
                        StringComparison.Ordinal))
                {
                    track = candidate;
                    break;
                }

                /*
                 * Root frames can also be animation targets.
                 */
                if (frame.Parent == null &&
                    frame.Path.EndsWith(
                        candidate.Path,
                        StringComparison.Ordinal))
                {
                    track = candidate;
                    break;
                }
            }

            if (track != null)
            {
                if (track.Translations.Count > 0)
                {
                    position =
                        SampleVector3(
                            track.Translations,
                            time);
                }

                if (track.Rotations.Count > 0)
                {
                    rotation =
                        SampleQuaternion(
                            track.Rotations,
                            time);
                }

                if (track.Scalings.Count > 0)
                {
                    scale =
                        SampleVector3(
                            track.Scalings,
                            time);
                }
            }

            var translation =
                Matrix4x4.Translate(position);

            var rotationMatrix =
                Matrix4x4.Rotate(rotation);

            var scaleMatrix =
                Matrix4x4.Scale(scale);

            return translation *
                   rotationMatrix *
                   scaleMatrix;
        }

        private static Vector3 SampleVector3(
            List<ImportedKeyframe<Vector3>> keys,
            float time)
        {
            if (keys.Count == 1 ||
                time <= keys[0].time)
            {
                return keys[0].value;
            }

            for (int i = 0; i < keys.Count - 1; i++)
            {
                var a = keys[i];
                var b = keys[i + 1];

                if (time <= b.time)
                {
                    float duration =
                        b.time - a.time;

                    if (Math.Abs(duration) < 0.000001f)
                        return b.value;

                    float t =
                        (time - a.time) / duration;

                    return new Vector3(
                        a.value.X +
                            (b.value.X - a.value.X) * t,
                        a.value.Y +
                            (b.value.Y - a.value.Y) * t,
                        a.value.Z +
                            (b.value.Z - a.value.Z) * t);
                }
            }

            return keys[keys.Count - 1].value;
        }

        private static Quaternion SampleQuaternion(
            List<ImportedKeyframe<Quaternion>> keys,
            float time)
        {
            if (keys.Count == 1 ||
                time <= keys[0].time)
            {
                return keys[0].value;
            }

            for (int i = 0; i < keys.Count - 1; i++)
            {
                var a = keys[i];
                var b = keys[i + 1];

                if (time <= b.time)
                {
                    float duration =
                        b.time - a.time;

                    if (Math.Abs(duration) < 0.000001f)
                        return b.value;

                    float t =
                        (time - a.time) / duration;

                    return Slerp(
                        a.value,
                        b.value,
                        t);
                }
            }

            return keys[keys.Count - 1].value;
        }

        private static Quaternion Slerp(
            Quaternion a,
            Quaternion b,
            float t)
        {
            float dot =
                a.X * b.X +
                a.Y * b.Y +
                a.Z * b.Z +
                a.W * b.W;

            /*
             * q and -q represent the same rotation.
             * Flip one quaternion so interpolation follows
             * the shortest arc.
             */
            if (dot < 0.0f)
            {
                b = new Quaternion(
                    -b.X,
                    -b.Y,
                    -b.Z,
                    -b.W);

                dot = -dot;
            }

            dot = Math.Max(
                -1.0f,
                Math.Min(1.0f, dot));

            if (dot > 0.9995f)
            {
                var result =
                    new Quaternion(
                        a.X + (b.X - a.X) * t,
                        a.Y + (b.Y - a.Y) * t,
                        a.Z + (b.Z - a.Z) * t,
                        a.W + (b.W - a.W) * t);

                return Normalize(result);
            }

            float theta0 =
                (float)Math.Acos(dot);

            float theta =
                theta0 * t;

            float sinTheta =
                (float)Math.Sin(theta);

            float sinTheta0 =
                (float)Math.Sin(theta0);

            float s0 =
                (float)Math.Cos(theta) -
                dot * sinTheta / sinTheta0;

            float s1 =
                sinTheta / sinTheta0;

            return new Quaternion(
                s0 * a.X + s1 * b.X,
                s0 * a.Y + s1 * b.Y,
                s0 * a.Z + s1 * b.Z,
                s0 * a.W + s1 * b.W);
        }

        private static Quaternion Normalize(
            Quaternion q)
        {
            float length =
                (float)Math.Sqrt(
                    q.X * q.X +
                    q.Y * q.Y +
                    q.Z * q.Z +
                    q.W * q.W);

            if (length < 0.000001f)
                return q;

            return new Quaternion(
                q.X / length,
                q.Y / length,
                q.Z / length,
                q.W / length);
        }

        private static Matrix4x4 CreateLocalMatrix(
            ImportedFrame frame)
        {
            var translation =
                Matrix4x4.Translate(frame.LocalPosition);

            var rotation =
                Matrix4x4.Rotate(frame.LocalRotation);

            var scale =
                Matrix4x4.Scale(frame.LocalScale);

            return translation *
                   rotation *
                   scale;
        }

        public static Vector3 TransformPoint(
            Matrix4x4 matrix,
            Vector3 point)
        {
            float x =
                matrix.M00 * point.X +
                matrix.M01 * point.Y +
                matrix.M02 * point.Z +
                matrix.M03;

            float y =
                matrix.M10 * point.X +
                matrix.M11 * point.Y +
                matrix.M12 * point.Z +
                matrix.M13;

            float z =
                matrix.M20 * point.X +
                matrix.M21 * point.Y +
                matrix.M22 * point.Z +
                matrix.M23;

            float w =
                matrix.M30 * point.X +
                matrix.M31 * point.Y +
                matrix.M32 * point.Z +
                matrix.M33;

            if (Math.Abs(w) > 0.000001f &&
                Math.Abs(w - 1.0f) > 0.000001f)
            {
                x /= w;
                y /= w;
                z /= w;
            }

            return new Vector3(x, y, z);
        }


        public static Matrix4x4 InvertMatrix(Matrix4x4 matrix)
        {
            // Inversione mediante Gauss-Jordan con pivoting.
            var a = new double[4, 8];

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    a[row, col] = matrix[row, col];
                }

                a[row, row + 4] = 1.0;
            }

            for (int col = 0; col < 4; col++)
            {
                int pivotRow = col;
                double max = Math.Abs(a[col, col]);

                for (int row = col + 1; row < 4; row++)
                {
                    double value = Math.Abs(a[row, col]);

                    if (value > max)
                    {
                        max = value;
                        pivotRow = row;
                    }
                }

                if (max < 1e-12)
                {
                    throw new InvalidOperationException(
                        "Cannot invert a singular matrix.");
                }

                if (pivotRow != col)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        double temp = a[col, j];
                        a[col, j] = a[pivotRow, j];
                        a[pivotRow, j] = temp;
                    }
                }

                double pivot = a[col, col];

                for (int j = 0; j < 8; j++)
                {
                    a[col, j] /= pivot;
                }

                for (int row = 0; row < 4; row++)
                {
                    if (row == col)
                        continue;

                    double factor = a[row, col];

                    for (int j = 0; j < 8; j++)
                    {
                        a[row, j] -= factor * a[col, j];
                    }
                }
            }

            var result = new Matrix4x4();

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    result[row, col] =
                        (float)a[row, col + 4];
                }
            }

            return result;
        }

        public string DiagnoseBindPoses(
            List<ImportedMesh> meshes)
        {
            var worldMatrices = GetWorldMatrices(null, 0f);
            var selectedBoneErrors =
                new Dictionary<string, double>();
            int checkedBones = 0;
            int missingBones = 0;
            int invalidMatrices = 0;

            double maxError = 0;
            double totalError = 0;

            string worstBone = "";
            string worstMesh = "";

            foreach (var mesh in meshes)
            {
                if (mesh?.BoneList == null ||
                    mesh.BoneList.Count == 0)
                    continue;

                if (!worldMatrices.TryGetValue(
                    mesh.Path, out var meshWorld))
                    continue;

                Matrix4x4 inverseMesh;

                try
                {
                    inverseMesh = InvertMatrix(meshWorld);
                }
                catch (InvalidOperationException)
                {
                    invalidMatrices++;
                    continue;
                }

                foreach (var bone in mesh.BoneList)
                {
                    if (bone == null ||
                        string.IsNullOrEmpty(bone.Path) ||
                        !worldMatrices.TryGetValue(
                            bone.Path, out var boneWorld))
                    {
                        missingBones++;
                        continue;
                    }

                    var skinMatrix =
                        inverseMesh *
                        boneWorld *
                        bone.Matrix;

                    double error = 0;

                    for (int row = 0; row < 4; row++)
                    {
                        for (int col = 0; col < 4; col++)
                        {
                            double expected =
                                row == col ? 1.0 : 0.0;

                            error = Math.Max(
                                error,
                                Math.Abs(
                                    skinMatrix[row, col] -
                                    expected));
                        }
                    }

                    if (error > maxError)
                    {
                        maxError = error;
                        worstBone = bone.Path;
                        worstMesh = mesh.Path;
                    }

                    string boneName =
                        bone.Path.Substring(
                            bone.Path.LastIndexOf('/') + 1);

                    if (boneName == "b_C_Pelvis" ||
                        boneName == "b_C_Head" ||
                        boneName == "h_L_Thumb3")
                    {
                        selectedBoneErrors[boneName] = error;
                    }

                    totalError += error;
                    checkedBones++;
                }
            }

            double averageError =
                checkedBones > 0
                    ? totalError / checkedBones
                    : 0;

            string selectedReport = "";

            foreach (var entry in selectedBoneErrors)
            {
                selectedReport +=
                    $"{entry.Key}: {entry.Value:F6}\n";
            }

            return
                $"Bones checked: {checkedBones}\n" +
                $"Missing bones: {missingBones}\n" +
                $"Invalid mesh matrices: {invalidMatrices}\n" +
                $"Maximum error: {maxError:F6}\n" +
                $"Worst bone: {worstBone}\n" +
                $"Worst mesh: {worstMesh}\n" +
                $"Average error: {averageError:F6}\n" +
                $"\nSelected bone errors:\n" +
                selectedReport;

        }



        private static SkeletonPoint GetPosition(
            Matrix4x4 matrix)
        {
            return new SkeletonPoint(
                matrix.M03,
                matrix.M13,
                matrix.M23);
        }
    }

    internal readonly struct SkeletonPoint
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public SkeletonPoint(
            float x,
            float y,
            float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    internal readonly struct SkeletonLine
    {
        public readonly SkeletonPoint Start;
        public readonly SkeletonPoint End;

        public SkeletonLine(
            SkeletonPoint start,
            SkeletonPoint end)
        {
            Start = start;
            End = end;
        }
    }
}