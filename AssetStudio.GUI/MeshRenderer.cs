//AnimationPreview 07-10-26
using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace AssetStudio.GUI
{
    /// <summary>
    /// Renders static, untextured meshes as gray triangles.
    /// Does not perform skinning or animation.
    /// </summary>
    internal sealed class MeshRenderer : IDisposable
    {
        private int program;
        private int vertexArray;
        private int vertexBuffer;
        private int indexBuffer;

        private int vertexPositionLocation;
        private int vertexColorLocation;

        private int modelMatrixLocation;
        private int viewMatrixLocation;
        private int projectionMatrixLocation;

        private int indexCount;
        //AnimationPreview 07-10-26
        private int vertexCount;
        private readonly List<MeshDrawGroup> drawGroups =
            new List<MeshDrawGroup>();

        internal readonly struct MeshDrawGroup
        {
            public readonly int StartIndex;
            public readonly int IndexCount;
            public readonly Vector4 Color;

            public MeshDrawGroup(
                int startIndex,
                int indexCount,
                Vector4 color)
            {
                StartIndex = startIndex;
                IndexCount = indexCount;
                Color = color;
            }
        }
        //AnimationPreview 07-10-26
        private bool initialized;
        private bool disposed;

        public void Initialize()
        {
            if (initialized)
                return;

            if (disposed)
                throw new ObjectDisposedException(nameof(MeshRenderer));

            int vertexShader = CompileShader(
                ShaderType.VertexShader,
                GetResourceString("vs"));

            int fragmentShader = CompileShader(
                ShaderType.FragmentShader,
                GetResourceString("fsColor"));

            program = GL.CreateProgram();

            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            GL.GetProgram(
                program,
                GetProgramParameterName.LinkStatus,
                out int linkStatus);

            if (linkStatus == 0)
            {
                string log = GL.GetProgramInfoLog(program);

                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);
                GL.DeleteProgram(program);
                program = 0;

                throw new InvalidOperationException(
                    "Unable to link mesh shader:\n" + log);
            }

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            vertexPositionLocation =
                GL.GetAttribLocation(program, "vertexPosition");

            vertexColorLocation =
                GL.GetAttribLocation(program, "vertexColor");

            modelMatrixLocation =
                GL.GetUniformLocation(program, "modelMatrix");

            viewMatrixLocation =
                GL.GetUniformLocation(program, "viewMatrix");

            projectionMatrixLocation =
                GL.GetUniformLocation(program, "projMatrix");

            GL.GenVertexArrays(1, out vertexArray);
            GL.GenBuffers(1, out vertexBuffer);
            GL.GenBuffers(1, out indexBuffer);

            GL.BindVertexArray(vertexArray);

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                vertexBuffer);

            GL.BindBuffer(
                BufferTarget.ElementArrayBuffer,
                indexBuffer);

            GL.VertexAttribPointer(
                vertexPositionLocation,
                3,
                VertexAttribPointerType.Float,
                false,
                3 * sizeof(float),
                0);

            GL.EnableVertexAttribArray(vertexPositionLocation);

            GL.DisableVertexAttribArray(vertexColorLocation);

            GL.VertexAttrib4(
                vertexColorLocation,
                0.65f,
                0.68f,
                0.72f,
                1.0f);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            initialized = true;
        }

        /// <summary>
        /// Uploads already-converted mesh geometry.
        /// Indices refer to the combined vertex array.
        /// </summary>
        public void UpdateMeshes(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> indices,
            IReadOnlyList<MeshDrawGroup> groups = null)
        {
            EnsureInitialized();
            drawGroups.Clear();

            if (vertices == null || indices == null ||
                vertices.Count == 0 || indices.Count == 0)
            {
                indexCount = 0;
                vertexCount = 0;
                return;
            }

            var vertexArrayData = new Vector3[vertices.Count];
            var indexArrayData = new int[indices.Count];

            for (int i = 0; i < vertices.Count; i++)
                vertexArrayData[i] = vertices[i];

            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];

                if (index < 0 || index >= vertices.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(indices),
                        $"Mesh index {index} is outside the vertex array.");
                }

                indexArrayData[i] = index;
            }

            GL.BindVertexArray(vertexArray);

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                vertexBuffer);

            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(vertexArrayData.Length * 3 * sizeof(float)),
                vertexArrayData,
                BufferUsageHint.DynamicDraw);

            GL.BindBuffer(
                BufferTarget.ElementArrayBuffer,
                indexBuffer);

            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexArrayData.Length * sizeof(int)),
                indexArrayData,
                BufferUsageHint.StaticDraw);

            GL.BindVertexArray(0);

            indexCount = indexArrayData.Length;
            vertexCount = vertexArrayData.Length;

            if (groups != null)
            {
                foreach (var group in groups)
                {
                    if (group.StartIndex < 0 ||
                        group.IndexCount < 0 ||
                        (long)group.StartIndex + group.IndexCount > indexCount ||
                        group.IndexCount % 3 != 0)
                    {
                        throw new ArgumentException(
                            "Invalid mesh draw group.",
                            nameof(groups));
                    }

                    drawGroups.Add(group);
                }
            }
        }

        public void UpdateVertexPositions(
            IReadOnlyList<Vector3> vertices)
        {
            EnsureInitialized();

            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            if (vertices.Count != vertexCount)
            {
                throw new ArgumentException(
                    $"Expected {vertexCount} vertices, received {vertices.Count}.",
                    nameof(vertices));
            }

            if (vertexCount == 0)
                return;

            var data = new Vector3[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                data[i] = vertices[i];
            }

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                vertexBuffer);

            GL.BufferSubData(
                BufferTarget.ArrayBuffer,
                IntPtr.Zero,
                (IntPtr)(data.Length * 3 * sizeof(float)),
                data);

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                0);
        }


        public void Render(
            Matrix4 modelMatrix,
            Matrix4 viewMatrix,
            Matrix4 projectionMatrix)
        {
            EnsureInitialized();

            if (indexCount == 0)
                return;

            GL.UseProgram(program);

            GL.UniformMatrix4(
                modelMatrixLocation,
                false,
                ref modelMatrix);

            GL.UniformMatrix4(
                viewMatrixLocation,
                false,
                ref viewMatrix);

            GL.UniformMatrix4(
                projectionMatrixLocation,
                false,
                ref projectionMatrix);

            GL.BindVertexArray(vertexArray);

            
                if (drawGroups.Count == 0)
            {
                GL.VertexAttrib4(
                    vertexColorLocation,
                    0.65f, 0.68f, 0.72f, 1.0f);

                GL.DrawElements(
                    PrimitiveType.Triangles,
                    indexCount,
                    DrawElementsType.UnsignedInt,
                    0);
            }
            else
            {
                foreach (var group in drawGroups)
                {
                    // Diagnostic: orange mesh is visible through other geometry.
                    bool isDiagnosticFace =
                        group.Color.X == 1.0f &&
                        group.Color.Y == 0.55f &&
                        group.Color.Z == 0.15f;

                    if (isDiagnosticFace)
                        GL.Disable(EnableCap.DepthTest);

                    GL.VertexAttrib4(
                        vertexColorLocation,
                        group.Color.X,
                        group.Color.Y,
                        group.Color.Z,
                        group.Color.W);

                    GL.DrawElements(
                        PrimitiveType.Triangles,
                        group.IndexCount,
                        DrawElementsType.UnsignedInt,
                        (IntPtr)(group.StartIndex * sizeof(int)));

                    if (isDiagnosticFace)
                        GL.Enable(EnableCap.DepthTest);
                }
            }

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        private static int CompileShader(
            ShaderType type,
            string source)
        {
            int shader = GL.CreateShader(type);

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(
                shader,
                ShaderParameter.CompileStatus,
                out int status);

            if (status == 0)
            {
                string log = GL.GetShaderInfoLog(shader);
                GL.DeleteShader(shader);

                throw new InvalidOperationException(
                    $"Unable to compile {type}:\n{log}");
            }

            return shader;
        }

        private static string GetResourceString(string name)
        {
            object resource =
                Properties.Resources.ResourceManager.GetObject(name);

            if (resource is string text &&
                !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            throw new InvalidOperationException(
                $"OpenGL shader resource '{name}' was not found.");
        }

        private void EnsureInitialized()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(MeshRenderer));

            if (!initialized)
            {
                throw new InvalidOperationException(
                    "MeshRenderer has not been initialized.");
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (initialized)
            {
                if (indexBuffer != 0)
                    GL.DeleteBuffer(indexBuffer);

                if (vertexBuffer != 0)
                    GL.DeleteBuffer(vertexBuffer);

                if (vertexArray != 0)
                    GL.DeleteVertexArray(vertexArray);

                if (program != 0)
                    GL.DeleteProgram(program);
            }

            indexBuffer = 0;
            vertexBuffer = 0;
            vertexArray = 0;
            program = 0;

            initialized = false;
            disposed = true;
        }
    }
}
